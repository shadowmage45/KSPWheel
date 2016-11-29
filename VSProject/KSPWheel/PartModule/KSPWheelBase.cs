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

        [KSPField]
        public bool useParentRigidbody = true;

        /// <summary>
        /// Name of the transform that the wheel collider component should be attached to/manipulate.
        /// </summary>
        [KSPField]
        public string wheelColliderName;
                
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
        public float wheelMass = 0.25f;

        [KSPField]
        public float suspensionTravel = 0.25f;

        [KSPField]
        public float frictionMult = 1f;

        [KSPField(guiName = "Ride Height", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.05f, maxValue = 1, stepIncrement = 0.05f, suppressEditorShipModified = true)]
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
        
        /// <summary>
        /// If true the steering will be locked to zero and will not respond to steering input.
        /// </summary>
        [KSPField(guiName = "Auto-Tune(WIP)", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool autoTuneSuspension = false;

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
                    calcSuspension(rating, wheel.suspensionTravel * tweakScaleCorrector, suspensionTarget, dampRatio, out suspensionSpring, out suspensionDamper);
                    if (wheel.wheel != null)
                    {
                        wheel.wheel.spring = suspensionSpring;
                        wheel.wheel.damper = suspensionDamper;
                    }
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
                        wheelData[i].setupWheel(rb, raycastMask, tweakScaleCorrector);
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
                for (int i = 0; i < len; i++)
                {
                    wheel = wheelData[i].wheel;
                    wheel.gravityVector = vessel.gravityForPos;
                    wheel.updateWheel();
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
            float pos = part.transform.position.y - groundHeightOffset * tweakScaleCorrector;
            MonoBehaviour.print("put on ground: " + pos+"  current: "+phq.lowestOnParts[part]+" tot: "+phq.lowestPoint);
            phq.lowestOnParts[part] = Mathf.Min(phq.lowestOnParts[part], pos);
            phq.lowestPoint = Mathf.Min(phq.lowestPoint, phq.lowestOnParts[part]);
            MonoBehaviour.print("post put on ground: "+ phq.lowestOnParts[part] + " tot: " + phq.lowestPoint);
        }

        #endregion

        #region REGION - Custom update methods

        [KSPField]
        float susRes = 1f;

        /// <summary>
        /// Auto-suspension tuning.
        /// Works, but causes interference with traction and normal suspension response.
        /// </summary>
        private void updateSuspension()
        {
            int len = wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                KSPWheelCollider wheel = wheelData[i].wheel;
                float target = wheel.springForce * 0.1f;
                if (target < minLoadRating) { target = minLoadRating; }
                if (target > maxLoadRating) { target = maxLoadRating; }
                loadRating = Mathf.Lerp(loadRating, target, Time.deltaTime * susRes);
                float suspensionSpring, suspensionDamper;
                calcSuspension(loadRating, suspensionTravel, suspensionTarget, dampRatio, out suspensionSpring, out suspensionDamper);
                wheel.spring = suspensionSpring;
                wheel.damper = suspensionDamper;
            }
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
                    MonoBehaviour.print("calling wheel created for index: " + index + " for module: "+subModules[i]);
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
            public readonly float wheelMass;
            public readonly float suspensionTravel;
            public readonly float loadShare;
            public readonly float offset;
            public KSPWheelCollider wheel;
            public Transform wheelTransform;
            public GameObject bumpStopGameObject;
            public SphereCollider bumpStopCollider;

            public KSPWheelData(ConfigNode node)
            {
                wheelColliderName = node.GetStringValue("colliderName", "WheelCollider");
                wheelRadius = node.GetFloatValue("radius", 0.25f);
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
                wheelTransform.localPosition += Vector3.up * offset * scaleFactor;
                wheel = wheelTransform.gameObject.AddComponent<KSPWheelCollider>();
                wheel.rigidbody = rb;
                wheel.radius = wheelRadius * scaleFactor;
                wheel.mass = wheelMass * scaleFactor;
                wheel.length = suspensionTravel * scaleFactor;
                wheel.raycastMask = raycastMask;

                bumpStopGameObject = new GameObject("KSPWheelBumpStop-" + wheelColliderName);
                bumpStopGameObject.layer = 26;
                bumpStopCollider = bumpStopGameObject.AddComponent<SphereCollider>();
                bumpStopCollider.center = Vector3.zero;
                bumpStopCollider.radius = wheelRadius * scaleFactor;
                PhysicMaterial mat = new PhysicMaterial("TEST");
                mat.bounciness = 0.0f;
                mat.dynamicFriction = 0;
                mat.staticFriction = 0;
                bumpStopCollider.material = mat;
                bumpStopGameObject.transform.NestToParent(wheelTransform);
            }

        }
    }

}
