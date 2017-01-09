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
        /// By default ignore layers 26, 10, and 16 (wheelCollidersIgnore & scaledScenery & kerbals/IVAs)
        /// </summary>
        [KSPField]
        public int raycastMask = ~(1 << 26 | 1 << 10 | 1 << 16);

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

        [KSPField(guiName = "Show Wheel Controls", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(affectSymCounterparts = UI_Scene.All, controlEnabled = true, disabledText = "Hidden", enabledText = "Shown", requireFullControl = false, suppressEditorShipModified = true, scene = UI_Scene.All)]
        public bool showControls = true;

        [KSPField(guiName = "Ride Height", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.2f, maxValue = 0.8f, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float suspensionTarget = 0.5f;

        [KSPField(guiName = "Load Rating", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.05f, maxValue = 5, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float loadRating = 2.5f;

        [KSPField]
        public float minLoadRating = 0.05f;

        [KSPField(guiName = "Max Load", guiActive = false, guiActiveEditor = false)]
        public float maxLoadRating = 5f;

        [KSPField(guiName = "Spring Rating", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.05f, maxValue = 1, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float springRating = 0.65f;

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

        [KSPField(isPersistant = true)]
        public bool initializedEditor = false;

        [Persistent]
        public string configNodeData = string.Empty;

        #endregion

        #region REGION - Private working/cached variables

        public KSPWheelData[] wheelData;

        public float tweakScaleCorrector = 1f;

        public KSPWheelState wheelState = KSPWheelState.DEPLOYED;

        private bool initializedWheels = false;

        private bool advancedMode = false;

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

        public void onShowUIUpdated(BaseField field, object obj)
        {
            int len = subModules.Count;
            for (int i = 0; i < len; i++)
            {
                subModules[i].onUIControlsUpdated(showControls);
            }

            Fields[nameof(suspensionTarget)].guiActive = Fields[nameof(suspensionTarget)].guiActiveEditor = showControls && advancedMode;
            Fields[nameof(loadRating)].guiActive = Fields[nameof(loadRating)].guiActiveEditor = showControls && advancedMode;
            Fields[nameof(springRating)].guiActive = Fields[nameof(springRating)].guiActiveEditor = showControls && !advancedMode;
            Fields[nameof(dampRatio)].guiActive = Fields[nameof(dampRatio)].guiActiveEditor = showControls;
            Fields[nameof(guiCompression)].guiActive = Fields[nameof(guiCompression)].guiActiveEditor = showControls;
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
            wheelState = (KSPWheelState)Enum.Parse(typeof(KSPWheelState), persistentState);

        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            persistentState = wheelState.ToString();
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
            advancedMode = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().advancedMode;

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
            field.guiActive = field.guiActiveEditor = advancedMode;

            field = Fields[nameof(dampRatio)];
            field.uiControlEditor.onFieldChanged = field.uiControlFlight.onFieldChanged = onLoadUpdated;
            rng = (UI_FloatRange)field.uiControlFlight;
            if (rng != null)
            {
                rng.minValue = minDampRatio;
                rng.maxValue = maxDampRatio;
            }
            rng = (UI_FloatRange)field.uiControlEditor;
            if (rng != null)
            {
                rng.minValue = minDampRatio;
                rng.maxValue = maxDampRatio;
            }

            field = Fields[nameof(suspensionTarget)];
            field.uiControlEditor.onFieldChanged = field.uiControlFlight.onFieldChanged = onLoadUpdated;
            field.guiActive = field.guiActiveEditor = advancedMode;

            field = Fields[nameof(showControls)];
            field.uiControlEditor.onFieldChanged = field.uiControlFlight.onFieldChanged = onShowUIUpdated;

            field = Fields[nameof(maxLoadRating)];
            field.guiActiveEditor = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wearType != KSPWheelWearType.NONE;

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

        public void Start()
        {
            this.onShowUIUpdated(null, null);
            initializedEditor = true;
        }

        /// <summary>
        /// Updates the wheel collider component physics if it is not broken or retracted
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready || !FlightDriver.fetch)
            {
                return;
            }
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
                    }
                    //run wheel init on a second pass so that all wheels are available
                    //some modules may use more than a single wheel (damage, tracks, dust, debug)
                    for (int i = 0; i < count; i++)
                    {
                        onWheelCreated(i, wheelData[i]);
                    }
                    if (HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().advancedMode)
                    {
                        onLoadUpdated(null, null);
                    }
                }
            }

            //TODO -- should only need to set this once on part init
            if (part.collisionEnhancer != null)
            {
                part.collisionEnhancer.OnTerrainPunchThrough = CollisionEnhancerBehaviour.DO_NOTHING;
            }

            //TODO -- better handling of bump-stop collider state, should NOT need to reset it every tick
            int len = wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                wheelData[i].bumpStopCollider.enabled = wheelState == KSPWheelState.DEPLOYED;
            }

            //TODO -- subscribe to vessel modified events and update rigidbody assignment whenever parts/etc are modified
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
                if (!advancedMode)
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
            phq.lowestOnParts[part] = Mathf.Min(phq.lowestOnParts[part], pos);
            phq.lowestPoint = Mathf.Min(phq.lowestPoint, phq.lowestOnParts[part]);
        }

        #endregion

        #region REGION - Custom update methods

        private void updateSuspension()
        {
            float vesselMass = 0;
            if (vessel == null || (vesselMass = (float)vessel.totalMass) <= 0)
            {
                return;
            }
            float compressionBoostFactor;
            float spring, damper, springLoad, natFreq, criticalDamping;
            float compression = 0;
            int len = wheelData.Length;
            KSPWheelData data;
            for (int i = 0; i < len; i++)
            {
                data = wheelData[i];
                compression = data.wheel.compressionDistance / data.wheel.length;
                if (compression > 0.8f)
                {
                    data.timeBoostFactor = data.timeBoostFactor + 0.5f * Time.fixedDeltaTime;
                }
                else if (data.wheel.isGrounded && compression < 0.4f)
                {
                    data.timeBoostFactor = data.timeBoostFactor - 0.2f * Time.fixedDeltaTime;
                }
                data.timeBoostFactor = Mathf.Clamp(data.timeBoostFactor, 0.01f, 0.85f);
                compressionBoostFactor = 1.0f + Mathf.Clamp(compression * 2f, 0, 1f);
                spring = Mathf.Clamp(vesselMass * evaluateCurve(compressionBoostFactor, data.timeBoostFactor) * springRating * 10f, 0.01f, 50000f);

                springLoad = spring * data.wheel.length * 0.5f * 0.1f;//target load for damper calc is spring at half compression
                natFreq = Mathf.Sqrt(spring / springLoad);//natural frequency
                criticalDamping = 2 * springLoad * natFreq;//critical damping
                damper = criticalDamping * dampRatio;

                data.wheel.spring = spring;
                data.wheel.damper = damper;
            }
        }

        //TODO derive cleaner curves / alternate curves / alternate spring-setting methods for over and under compression
        // can use a curve that starts at y=1 when x=comp limit
        private float evaluateCurve(float comp, float time)
        {
            return Mathf.Clamp(1f / Mathf.Abs(1f - 2f / Mathf.Pow(comp, time)), 0.01f, 10000000f);
        }

        internal void addSubmodule(KSPWheelSubmodule module)
        {
            subModules.AddUnique(module);
        }

        internal void removeSubmodule(KSPWheelSubmodule module)
        {
            subModules.Remove(module);
        }

        internal void onScaleUpdated(KSPWheelScaling scaling)
        {
            int len = subModules.Count;
            for (int i = 0; i < len; i++)
            {
                subModules[i].onScaleUpdated(scaling);
            }
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

        //TODO also need to check the rest of the parts' colliders for contact/grounded state somehow (or they are handled by regular Part methods?)
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

        /// <summary>
        /// Input load in tons, suspension length, target (0-1), and desired damp ratio (1 = critical)
        /// and output spring and damper for that load and ratio
        /// </summary>
        private void calcSuspensionCurved(float load, float length, float target, float dampRatio, float curveFactor, out float spring, out float damper)
        {
            float targetCompression = target * length;
            if (targetCompression <= 0) { targetCompression = 0.1f; }
            //k = x / (y(ay+1))
            float k = (load * 10) / (targetCompression * (curveFactor * targetCompression + 1));
            float o = Mathf.Sqrt(k / load);//natural frequency
            float cd = 2 * load * o;//critical damping coefficient
            //critical damping factor = 2 * Mathf.Sqrt(k * load);
            //damper output = 2 * Mathf.Sqrt(load * spring) * dampRatio;
            spring = k;
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
            public float timeBoostFactor;
            public float prevComp;

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
                wheelTransform.position += wheelTransform.up * offset * scaleFactor;
                wheel = wheelTransform.gameObject.AddComponent<KSPWheelCollider>();
                wheel.rigidbody = rb;
                wheel.radius = scaledRadius(scaleFactor);
                wheel.mass = scaledMass(scaleFactor);
                wheel.length = scaledLength(scaleFactor);
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
                mat.bounciness = 0f;
                mat.dynamicFriction = 0;
                mat.staticFriction = 0;
                mat.frictionCombine = PhysicMaterialCombine.Minimum;
                mat.bounceCombine = PhysicMaterialCombine.Minimum;
                bumpStopCollider.material = mat;
                bumpStopGameObject.transform.NestToParent(wheelTransform);
                bumpStopGameObject.transform.Rotate(0, 0, 90, Space.Self);//rotate it so that it is in the proper orientation (collider y+ is the flat side, so it needs to point along wheel x+/-)
            }

            public float scaledMass(float scaleFactor)
            {
                return Mathf.Pow(scaleFactor, 3) * wheelMass;
            }

            public float scaledRadius(float scaleFactor)
            {
                return wheelRadius * scaleFactor;
            }

            public float scaledLength(float scaleFactor)
            {
                return suspensionTravel * scaleFactor;
            }

            public float scaledWidth(float scaleFactor)
            {
                return wheelWidth * scaleFactor;
            }

        }

    }

}
