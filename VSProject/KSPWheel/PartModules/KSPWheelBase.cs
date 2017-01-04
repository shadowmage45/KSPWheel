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

        [KSPField]
        public string boundsColliderName = String.Empty;

        [KSPField]
        public float groundHeightOffset = 0f;

        [KSPField(guiName = "SymmetrySuspensionUpdates", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(affectSymCounterparts = UI_Scene.All, controlEnabled = true, disabledText = "Disabled", enabledText = "Enabled", requireFullControl = false, suppressEditorShipModified = true, scene = UI_Scene.All)]
        public bool symmetrySuspensionUpdates = true;

        #endregion

        #region REGION - Persistent data

        [KSPField(isPersistant = true)]
        public string persistentState = KSPWheelState.DEPLOYED.ToString();

        [KSPField(isPersistant = true)]
        public bool grounded = false;

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

            Fields[nameof(suspensionTarget)].guiActive = Fields[nameof(suspensionTarget)].guiActiveEditor = showControls;
            Fields[nameof(loadRating)].guiActive = Fields[nameof(loadRating)].guiActiveEditor = showControls;
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
            advancedMode = !HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().advancedMode;

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
            field.guiActive = field.guiActiveEditor = advancedMode;

            field = Fields[nameof(showControls)];
            field.uiControlEditor.onFieldChanged = field.uiControlFlight.onFieldChanged = onShowUIUpdated;

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
            if (HighLogic.LoadedSceneIsEditor && !advancedMode && wheelData != null)
            {
                updateSuspension();
            }
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
                        onWheelCreated(i, wheelData[i]);
                    }
                    onLoadUpdated(null, null);
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

        private void updateSuspensionOld()
        {
            KSPWheelData data;
            KSPWheelCollider wheel;
            float vesselMass = 1;
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (vessel.ctrlState.wheelSteer != 0 || vessel.ctrlState.wheelThrottle != 0 || vessel.ActionGroups[KSPActionGroup.Brakes])
                {
                    return;
                }                
                vesselMass = (vessel == null) ? 1 : (float)vessel.totalMass;
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                vesselMass = EditorLogic.fetch.ship.GetTotalMass();
            }
            if (vesselMass <= 0 || float.IsNaN(vesselMass))
            {
                MonoBehaviour.print("ERROR: Vessel mass is invalid: " + vesselMass);
                return;
            }
            float suspensionSpring, suspensionDamper;
            float comp, prevComp, compPercent;
            float min = 0.2f;
            float max = 0.8f;
            int len = wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                data = wheelData[i];
                if ((wheel = data.wheel) == null)//assign and null-test
                {
                    continue;
                }
                if (wheel.isGrounded)
                {
                    comp = wheel.compressionDistance;
                    prevComp = data.prevComp;
                    compPercent = comp / wheel.length;
                    if (compPercent < min && comp < prevComp)//not compressed far enough -and- getting less compressed already
                    {
                        data.suspBoost -= vesselMass * 0.01f;
                    }
                    else if (compPercent > max && comp > prevComp)//too far compressed -and- getting more compressed already
                    {
                        data.suspBoost += vesselMass * 0.01f;
                    }                    
                    data.loadRating = (data.loadShare * vesselMass) + data.suspBoost;
                    calcSuspension(data.loadRating, suspensionTravel, suspensionTarget, dampRatio, out suspensionSpring, out suspensionDamper);
                    wheel.spring = suspensionSpring;
                    wheel.damper = suspensionDamper;
                    data.prevComp = comp;
                }
            }
        }

        private void updateSuspension()
        {
            float vesselMass = 0;
            if (vessel == null || (vesselMass = (float)vessel.totalMass) <= 0)
            {
                return;
            }
            if (part.isClone && symmetrySuspensionUpdates)
            {
                return;
            }
            int len = wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                updateSuspensionData(vesselMass, wheelData[i], i, symmetrySuspensionUpdates);
            }
        }

        private void updateSuspensionData(float vesselMass, KSPWheelData baseData, int index, bool symmetry)
        {
            float suspensionSpring, suspensionDamper;
            float min = 0.2f;
            float max = 0.8f;
            float move = 0f;
            float perc = 0;
            float inc = 0.01f * vesselMass;
            if (baseData.wheel.isGrounded)
            {
                perc = baseData.wheel.length / baseData.wheel.compressionDistance;
                if (perc < min)
                {
                    move -= inc;
                }
                else if (perc > max)
                {
                    move += inc;
                }
            }
            if (symmetry)
            {
                KSPWheelData symData;
                int len = part.symmetryCounterparts.Count;
                for (int i = 0; i < len; i++)
                {
                    symData = part.symmetryCounterparts[i].GetComponent<KSPWheelBase>().wheelData[index];
                    if (!symData.wheel.isGrounded) { continue; }
                    perc = symData.wheel.length / symData.wheel.compressionDistance;
                    if (perc < min)
                    {
                        move -= inc;
                    }
                    else if (perc > max)
                    {
                        move += inc;
                    }
                }
            }

            move = Mathf.Clamp(move, -inc, inc);
            baseData.suspBoost += move;
            baseData.loadRating = (baseData.loadShare * vesselMass) + baseData.suspBoost;
            calcSuspension(baseData.loadRating, baseData.suspensionTravel, suspensionTarget, dampRatio, out suspensionSpring, out suspensionDamper);
            baseData.wheel.spring = suspensionSpring;
            baseData.wheel.damper = suspensionDamper;

            if (symmetry)
            {
                KSPWheelData symData;
                int len = part.symmetryCounterparts.Count;
                for (int i = 0; i < len; i++)
                {
                    symData = part.symmetryCounterparts[i].GetComponent<KSPWheelBase>().wheelData[index];
                    symData.suspBoost += move;
                    symData.loadRating = baseData.loadRating;
                    baseData.wheel.spring = suspensionSpring;
                    baseData.wheel.damper = suspensionDamper;
                }
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
            public float suspBoost;
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
                mat.bounciness = 0f;
                mat.dynamicFriction = 0;
                mat.staticFriction = 0;
                mat.frictionCombine = PhysicMaterialCombine.Minimum;
                mat.bounceCombine = PhysicMaterialCombine.Minimum;
                bumpStopCollider.material = mat;
                bumpStopGameObject.transform.NestToParent(wheelTransform);
                bumpStopGameObject.transform.Rotate(0, 0, 90, Space.Self);//rotate it so that it is in the proper orientation (collider y+ is the flat side, so it needs to point along wheel x+/-)
            }

        }

    }

}
