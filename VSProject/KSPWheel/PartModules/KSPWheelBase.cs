using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    /// <summary>
    /// A replacement for the stock wheel system that uses the KSPWheelCollider class for phsyics handling.
    /// Intended to be a fully-functional (but possibly not fully-equivalent) replacement for the stock wheel modules and U5 WheelCollider component
    /// </summary>
    public class KSPWheelBase : PartModule
    {

        #region REGION - Basic config parameters

        /// <summary>
        /// The raycast mask to use for the wheel-collider suspension sweep. <para/>
        /// By default ignore layers 26 and 10 (wheelCollidersIgnore & scaledScenery)
        /// </summary>
        [KSPField]
        public int raycastMask = ~(1 << 26 | 1 << 10);

        /// <summary>
        /// If true, will use the rigidbody of the parent part rather than this.part.  Aides in jitter reduction and joint flex by (hopefully) applying forces to a part with higher mass.
        /// </summary>
        [KSPField]
        public bool useParentRigidbody = true;

        /// <summary>
        /// Name of the transform that the wheel collider component should be attached to/manipulate.
        /// </summary>
        [KSPField]
        public string wheelColliderName = string.Empty;
                
        /// <summary>
        /// Determines how far above the initial position in the model that the wheel-collider should be located.
        /// This is needed as the setup for stock models varies widely for wheel-collider positioning;
        /// some have it near the top of suspension travel, others at the bottom.
        /// Needs to be set on a per-part/model basis.
        /// </summary>
        [KSPField]
        public float wheelColliderOffset = 0f;
        
        [KSPField]
        public float wheelRadius = 0.25f;

        [KSPField]
        public float wheelWidth = -1;

        [KSPField]
        public float wheelMass = 0.25f;

        [KSPField]
        public float suspensionTravel = 0.25f;

        [KSPField]
        public float frictionMult = 1f;

        [KSPField(guiName = "Ride Height", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.2f, maxValue = 0.8f, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float suspensionTarget = 0.5f;

        [KSPField(guiName = "Load Rating", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.05f, maxValue = 5, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float loadRating = 2.5f;

        [KSPField]
        public float minLoadRating = 0.05f;

        [KSPField]
        public float maxLoadRating = 5f;

        [KSPField(guiName = "Damp Ratio", guiActive = true, guiActiveEditor = true, isPersistant = true),
        UI_FloatRange(minValue = 0.05f, maxValue = 2, stepIncrement = 0.025f, suppressEditorShipModified = true)]
        public float dampRatio = 0.65f;

        [KSPField]
        public float minDampRatio = 0.05f;

        [KSPField]
        public float maxDampRatio = 2f;

        [KSPField(guiName = "Compression", guiActive = true, guiActiveEditor = false, isPersistant = false),
         UI_ProgressBar(minValue = 0, maxValue = 1, suppressEditorShipModified = true)]
        public float guiCompression = 0.0f;

        [KSPField(guiName = "Auto-Tune Suspension", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool autoTuneSuspension = false;

        [KSPField(guiName = "Auto-Tune Load %", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 1f, suppressEditorShipModified = true)]
        public float autoLoadShare = 25f;

        [KSPField]
        public string boundsColliderName = String.Empty;

        [KSPField]
        public float groundHeightOffset = 0f;

        #endregion

        #region REGION - Persistent data

        [KSPField(isPersistant = true)]
        public string persistentState = KSPWheelState.DEPLOYED.ToString();

        [KSPField(isPersistant = true)]
        public bool grounded = false;

        #endregion

        #region REGION - Private working/cached variables

        public float tweakScaleCorrector = 1f;

        public KSPWheelState wheelState = KSPWheelState.DEPLOYED;

        [Persistent]
        public string configNodeData = string.Empty;
        private bool initializedWheels = false;
        public KSPWheelData[] wheelData;
        private List<KSPWheelSubmodule> subModules = new List<KSPWheelSubmodule>();
        #endregion

        #region REGION - GUI Handling methods

        public void onLoadUpdated(BaseField field, object obj)
        {
            if (wheelData != null)
            {
                KSPWheelData wheel;
                float suspensionSpring, suspensionDamper;
                float rating;
                int len = wheelData.Length;
                for (int i = 0; i < len; i++)
                {
                    wheel = wheelData[i];
                    rating = loadRating * wheel.loadShare;
                    calcSuspension(rating, wheel.suspensionTravel, suspensionTarget, dampRatio, out suspensionSpring, out suspensionDamper);
                    if (wheel.wheel != null)
                    {
                        wheel.wheel.spring = suspensionSpring;
                        wheel.wheel.damper = suspensionDamper;
                    }
                    wheelData[i].loadRating = loadRating;
                    wheelData[i].loadTarget = loadRating;
                }
            }
        }

        #endregion

        #region REGION - Standard KSP/Unity Overrides

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (string.IsNullOrEmpty(configNodeData))
            {
                configNodeData = node.ToString();
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.SetValue("persistentState", persistentState, true);
        }
        
        /// <summary>
        /// Initializes wheel parameters, removes stock wheel collider component, instantiates custom wheel collider component container, sets up animation handling (if needed)
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            wheelState = (KSPWheelState)Enum.Parse(typeof(KSPWheelState), persistentState);

            ConfigNode node = ConfigNode.Parse(configNodeData).nodes[0];

            List<KSPWheelData> wheelDatas = new List<KSPWheelData>();
            if (!string.IsNullOrEmpty(wheelColliderName))
            {
                ConfigNode newWheelNode = new ConfigNode("WHEEL");
                newWheelNode.AddValue("radius", wheelRadius);
                newWheelNode.AddValue("width", wheelWidth > 0 ? wheelWidth : wheelRadius * 0.2f);
                newWheelNode.AddValue("mass", wheelMass);
                newWheelNode.AddValue("travel", suspensionTravel);
                newWheelNode.AddValue("colliderName", wheelColliderName);
                newWheelNode.AddValue("offset", wheelColliderOffset);
                wheelDatas.Add(new KSPWheelData(newWheelNode));
            }

            ConfigNode[] wheelnodes = node.GetNodes("WHEEL");
            foreach (ConfigNode wn in wheelnodes)
            {
                wheelDatas.Add(new KSPWheelData(wn));
            }
            wheelData = wheelDatas.ToArray();
            foreach (KSPWheelData wheel in wheelDatas)
            {                
                wheel.locateTransform(part.transform);
            }
            
            BaseField field = Fields[nameof(loadRating)];
            field.uiControlEditor.onFieldChanged = field.uiControlFlight.onFieldChanged = onLoadUpdated;
            UI_FloatRange rng = (UI_FloatRange)field.uiControlFlight;
            if (rng != null)
            {
                rng.minValue = minLoadRating * tweakScaleCorrector;
                rng.maxValue = maxLoadRating * tweakScaleCorrector;
                rng.stepIncrement = 0.1f;
            }
            rng = (UI_FloatRange)field.uiControlEditor;
            if (rng != null)
            {
                rng.minValue = minLoadRating * tweakScaleCorrector;
                rng.maxValue = maxLoadRating * tweakScaleCorrector;
                rng.stepIncrement = 0.1f;
            }
            if (loadRating > maxLoadRating * tweakScaleCorrector) { loadRating = maxLoadRating * tweakScaleCorrector; }

            field = Fields[nameof(dampRatio)];
            field.uiControlEditor.onFieldChanged = field.uiControlFlight.onFieldChanged = onLoadUpdated;
            rng = (UI_FloatRange)field.uiControlFlight;
            if (rng != null)
            {
                rng.minValue = minDampRatio;
                rng.maxValue = maxDampRatio;
                rng.stepIncrement = 0.1f;
            }
            rng = (UI_FloatRange)field.uiControlEditor;
            if (rng != null)
            {
                rng.minValue = minDampRatio;
                rng.maxValue = maxDampRatio;
                rng.stepIncrement = 0.1f;
            }

            field = Fields[nameof(suspensionTarget)];
            field.uiControlEditor.onFieldChanged = field.uiControlFlight.onFieldChanged = onLoadUpdated;

            //destroy stock collision enhancer collider
            if (HighLogic.LoadedSceneIsFlight)
            {
                Collider[] colliders = part.GetComponentsInChildren<Collider>();
                int len = colliders.Length;
                for (int i = 0; i < len; i++)
                {
                    if (colliders[i].gameObject.name.ToLower() == "collisionenhancer")
                    {
                        GameObject.Destroy(colliders[i].gameObject);
                    }
                }
            }
            part.collider = null;//clear the part collider that causes explosions.... collisions still happen, but things won't break

            //destroy bounds collider, if specified and present (KF wheels)
            if (!string.IsNullOrEmpty(boundsColliderName))
            {
                Transform boundsCollider = part.transform.FindRecursive(boundsColliderName);
                if (boundsCollider != null)
                {
                    GameObject.Destroy(boundsCollider.gameObject);
                }
            }
        }

        /// <summary>
        /// Updates the wheel collider component physics if it is not broken or retracted
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready || !FlightDriver.fetch) { return; }
            if (!initializedWheels)
            {
                Rigidbody rb = part.GetComponent<Rigidbody>();
                if (useParentRigidbody && part.parent != null) { rb = part.parent.GetComponent<Rigidbody>(); }
                if (rb == null)
                {
                    return;
                }
                else
                {
                    initializedWheels = true;
                    int count = wheelData.Length;
                    for (int i = 0; i < count; i++)
                    {
                        wheelData[i].setupWheel(rb, raycastMask, tweakScaleCorrector * part.rescaleFactor);
                        wheelData[i].wheel.surfaceFrictionCoefficient = frictionMult;
                        onWheelCreated(i, wheelData[i]);
                    }
                    onLoadUpdated(null, null);
                }
            }

            if (part.collisionEnhancer != null) { part.collisionEnhancer.OnTerrainPunchThrough = CollisionEnhancerBehaviour.DO_NOTHING; }

            int len = wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                wheelData[i].bumpStopCollider.enabled = wheelState == KSPWheelState.DEPLOYED;
            }
            if (useParentRigidbody && part.parent == null)
            {
                useParentRigidbody = false;
                Rigidbody rb = part.GetComponent<Rigidbody>();
                for (int i = 0; i < len; i++)
                {
                    wheelData[i].wheel.rigidbody = rb;
                }
            }
            if (wheelState == KSPWheelState.DEPLOYED)
            {
                if (autoTuneSuspension)
                {
                    updateSuspension();
                }
                KSPWheelCollider wheel;
                int subLen = subModules.Count;
                for (int i = 0; i < subLen; i++)
                {
                    subModules[i].preWheelPhysicsUpdate();
                }
                guiCompression = 0f;
                for (int i = 0; i < len; i++)
                {
                    wheel = wheelData[i].wheel;
                    wheel.gravityVector = vessel.gravityForPos;
                    wheel.updateWheel();
                    float p = wheel.compressionDistance / wheel.length;
                    if (p > guiCompression) { guiCompression = p; }
                }
                for (int i = 0; i < subLen; i++)
                {
                    subModules[i].postWheelPhysicsUpdate();
                }
            }

            updateLandedState();
        }

        /// <summary>
        /// Updates the mesh animation status from the wheel collider components current state (steer angle, wheel rotation, suspension compression)
        /// </summary>
        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready || !FlightDriver.fetch || wheelData == null || !initializedWheels) { return; }
            int len = subModules.Count;
            for (int i = 0; i < len; i++)
            {
                subModules[i].preWheelFrameUpdate();
            }
        }

        /// <summary>
        /// Override of stock code use of Unity SendMessage mechanic.
        /// </summary>
        /// <param name="phq"></param>
        public void OnPutToGround(PartHeightQuery phq)
        {
            float pos = part.transform.position.y - groundHeightOffset * (tweakScaleCorrector * part.rescaleFactor);
            MonoBehaviour.print("put on ground: " + pos+"  current: "+phq.lowestOnParts[part]+" tot: "+phq.lowestPoint);
            phq.lowestOnParts[part] = Mathf.Min(phq.lowestOnParts[part], pos);
            phq.lowestPoint = Mathf.Min(phq.lowestPoint, phq.lowestOnParts[part]);
            MonoBehaviour.print("post put on ground: "+ phq.lowestOnParts[part] + " tot: " + phq.lowestPoint);
        }

        #endregion

        #region REGION - Custom update methods

        /// <summary>
        /// Auto-suspension tuning.
        /// Needs a bit more work so as to not cause interference with traction; perhaps just changed response rate.<para/>
        /// Could certainly be cleaned up a bit more to change output in a smoother manner.
        /// </summary>
        private void updateSuspension()
        {
            float massShare = (float)vessel.totalMass * autoLoadShare * 0.01f * (float)vessel.gravityForPos.magnitude * 0.1f;
            massShare = Mathf.Clamp(massShare, minLoadRating, maxLoadRating);
            int len = wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                KSPWheelCollider wheel = wheelData[i].wheel;
                wheelData[i].loadTarget = massShare * wheelData[i].loadShare;
                wheelData[i].loadRating = Mathf.MoveTowards(wheelData[i].loadRating, wheelData[i].loadTarget, maxLoadRating * 0.1f);
                float suspensionSpring, suspensionDamper;
                calcSuspension(wheelData[i].loadRating, suspensionTravel, suspensionTarget, dampRatio, out suspensionSpring, out suspensionDamper);
                wheel.spring = suspensionSpring;
                wheel.damper = suspensionDamper;
            }
            //update displayed load rating
            loadRating = Mathf.Clamp(massShare, minLoadRating, maxLoadRating);
        }

        internal void addSubmodule(KSPWheelSubmodule module)
        {
            subModules.AddUnique(module);
        }

        internal void removeSubmodule(KSPWheelSubmodule module)
        {
            subModules.Remove(module);
        }

        private void onWheelCreated(int index, KSPWheelData wheelData)
        {
            int len = subModules.Count;
            for (int i = 0; i < len; i++)
            {
                if (subModules[i].wheelIndex == index)
                {
                    subModules[i].onWheelCreated(wheelData.wheelTransform, wheelData.wheel);
                }
            }
        }

        //TODO also need to check the rest of the parts' colliders for contact/grounded state somehow
        private void updateLandedState()
        {
            grounded = false;
            int len = wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                if (wheelData[i].wheel.isGrounded) { grounded = true; break; }
            }
            part.GroundContact = grounded;
            vessel.checkLanded();
        }
        
        /// <summary>
        /// Input load in tons, suspension length, target (0-1), and desired damp ratio (1 = critical)
        /// and output spring and damper for that load and ratio
        /// </summary>
        private void calcSuspension(float load, float length, float target, float dampRatio, out float spring, out float damper)
        {
            float targetCompression = target * length;
            if (targetCompression <= 0) { targetCompression = 0.01f; }
            spring = load * 10 / targetCompression;
            //damper = 2 * Mathf.Sqrt(load * spring) * dampRatio;
            float k = spring;
            float o = Mathf.Sqrt(k / load);//natural frequency
            float cd = 2 * load * o;//critical damping coefficient
            //cd = 2 * Mathf.Sqrt(k * load);
            damper = cd * dampRatio;
        }

        #endregion

        public class KSPWheelData
        {
            public readonly String wheelColliderName;
            public readonly float wheelRadius;
            public readonly float wheelWidth;
            public readonly float wheelMass;
            public readonly float suspensionTravel;
            public readonly float loadShare;
            public readonly float offset;
            public KSPWheelCollider wheel;
            public Transform wheelTransform;
            public GameObject bumpStopGameObject;
            public MeshCollider bumpStopCollider;
            public float loadRating;
            public float loadTarget;

            public KSPWheelData(ConfigNode node)
            {
                wheelColliderName = node.GetStringValue("colliderName", "WheelCollider");
                wheelRadius = node.GetFloatValue("radius", 0.25f);
                wheelWidth = node.GetFloatValue("width", wheelRadius * 0.2f);
                wheelMass = node.GetFloatValue("mass", 0.05f);
                suspensionTravel = node.GetFloatValue("travel", 0.25f);
                loadShare = node.GetFloatValue("load", 1f);
                offset = node.GetFloatValue("offset");
            }

            public void locateTransform(Transform root)
            {
                wheelTransform = root.FindRecursive(wheelColliderName);
                WheelCollider wc = wheelTransform.GetComponent<WheelCollider>();
                GameObject.Destroy(wc);
            }

            public void setupWheel(Rigidbody rb, int raycastMask, float scaleFactor)
            {
                wheelTransform.localPosition += Vector3.up * offset;
                wheel = wheelTransform.gameObject.AddComponent<KSPWheelCollider>();
                wheel.rigidbody = rb;
                wheel.radius = wheelRadius * scaleFactor;
                wheel.mass = wheelMass * scaleFactor;
                wheel.length = suspensionTravel * scaleFactor;
                wheel.raycastMask = raycastMask;

                //calculate the size/scale of the bump-stop collider
                float scaleY = wheelRadius * 0.2f * scaleFactor;//wheel width
                scaleY *= 0.5f;//default is 2 units high, fix to 1 unit * width
                float scaleXZ = wheelRadius * 2 * scaleFactor;
                bumpStopGameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bumpStopGameObject.name = "KSPWheelBumpStop-" + wheelColliderName;
                bumpStopGameObject.transform.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);
                bumpStopGameObject.layer = 26;
                //remove existing capsule collider
                GameObject.DestroyImmediate(bumpStopGameObject.GetComponent<CapsuleCollider>());
                //remove existing mesh renderer
                GameObject.DestroyImmediate(bumpStopGameObject.GetComponent<MeshRenderer>());
                //add mesh collider
                bumpStopCollider = bumpStopGameObject.AddComponent<MeshCollider>();
                //mark as convex
                bumpStopCollider.convex = true;

                PhysicMaterial mat = new PhysicMaterial("BumpStopPhysicsMaterial");
                mat.bounciness = 1f;//to retain energy from collisions, otherwise it 'sticks'
                mat.dynamicFriction = 0;
                mat.staticFriction = 0;
                mat.frictionCombine = PhysicMaterialCombine.Minimum;
                mat.bounceCombine = PhysicMaterialCombine.Maximum;
                bumpStopCollider.material = mat;
                bumpStopGameObject.transform.NestToParent(wheelTransform);
                bumpStopGameObject.transform.Rotate(0, 0, 90, Space.Self);//rotate it so that it is in the proper orientation (collider y+ is the flat side, so it needs to point along wheel x+/-)
            }

        }
    }

}
