﻿using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{

    /// <summary>
    /// A replacement for the stock wheel system that uses the KSPWheelCollider class for phsyics handling.
    /// Intended to be a fully-functional (but possibly not fully-equivalent) replacement for the stock wheel modules and U5 WheelCollider component
    /// </summary>
    public class KSPWheelBase : PartModule, IPartCostModifier, IPartMassModifier, IModuleInfo
    {

        #region REGION - Basic config parameters

        /// <summary>
        /// The raycast mask to use for the wheel-collider suspension sweep. <para/>
        /// By default ignore layers 26, 10, and 16 (wheelCollidersIgnore & scaledScenery & kerbals/IVAs & transparentFX)
        /// </summary>
        [KSPField]
        public int raycastMask = ~(1 << 26 | 1 << 10 | 1 << 16 | 1 << 1);

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

        [KSPField(guiName = "Friction Multiplier", guiActive = true, guiActiveEditor = true, isPersistant = true, guiFormat = "F3"),
         UI_FloatRange(minValue = 0f, maxValue = 4f, suppressEditorShipModified = true, stepIncrement = 0.05f)]
        public float frictionControl = 1f;

        [KSPField]
        public float rollingResistance = 0.005f;

        [KSPField]
        public float rotationalResistance = 0f;

        [KSPField(guiName = "Show Wheel Controls", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(affectSymCounterparts = UI_Scene.All, controlEnabled = true, disabledText = "Hidden", enabledText = "Shown", requireFullControl = false, suppressEditorShipModified = true, scene = UI_Scene.All)]
        public bool showControls = true;

        [KSPField]
        public float minLoadRating = 0.05f;

        [KSPField]
        public float maxLoadRating = 5f;

        [KSPField(guiName = "Spring Rating", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.2f, maxValue = 0.8f, stepIncrement = 0.05f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public float springRating = 0.5f;

        [KSPField]
        public float minSpringRating = 0.2f;

        [KSPField]
        public float maxSpringRating = 0.8f;

        [KSPField(guiName = "Damp Ratio", guiActive = true, guiActiveEditor = true, isPersistant = true),
        UI_FloatRange(minValue = 0.35f, maxValue = 1, stepIncrement = 0.025f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public float dampRatio = 0.65f;

        [KSPField(guiName = "Wheel Group", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_ChooseOption(options =new string[] { "0","1","2","3","4","5","6","7","8","9","10"}, display = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" }, suppressEditorShipModified = true)]
        public string wheelGroup = "0";

        [KSPField]
        public float minDampRatio = 0.25f;

        [KSPField]
        public float maxDampRatio = 1f;

        [KSPField]
        public float maxSpeed = 0f;

        [KSPField]
        public string boundsColliderName = String.Empty;

        [KSPField]
        public float groundHeightOffset = 0f;

        [KSPField]
        public bool allowScaling = true;

        [KSPField]
        public float minScale = 0.1f;

        [KSPField]
        public float maxScale = 10f;

        [KSPField(guiName = "Scale", guiActive = false, guiActiveEditor = true, isPersistant = true, guiUnits = "x"),
         UI_FloatEdit(suppressEditorShipModified = true, minValue = 0.1f, maxValue = 10f, incrementLarge = 1f, incrementSmall = 0.25f, incrementSlide = 0.01f, sigFigs = 2)]
        public float scale = 1f;

        [KSPField]
        public string scalingTransform = string.Empty;

        [KSPField]
        public bool scaleDragCubes = true;

        [KSPField]
        public float forwardFriction = 1f;

        [KSPField]
        public float sidewaysFriction = 1f;

        /// <summary>
        /// The coefficient of the wheel colliders spring force that will be used for anti-roll-bar simulation
        /// </summary>
        [KSPField(guiName = "Anti Roll", guiActive = true, guiActiveEditor = true, isPersistant = true, guiFormat = "F2"),
         UI_FloatRange(minValue = 0f, maxValue = 1f, stepIncrement = 0.05f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
        public float antiRoll = 0f;

        /// <summary>
        /// Determines if the wheel-collider indices should be inverted for symmetry parts -- needs set to false on parts that have proper L/R counterparts (KSPWheelSidedModel)
        /// </summary>
        [KSPField]
        public bool antiRollInvertIndices = true;

        /// <summary>
        /// If true, will use wheel colliders within the same part for anti-roll functionality
        /// </summary>
        [KSPField]
        public bool useSelfAntiRoll = false;

        /// <summary>
        /// Used for in-editor part information display.  Sets the title of the module to this (rather than KSPWheelBase)
        /// </summary>
        [KSPField]
        public string wheelType = "Wheel";

        [KSPField]
        public bool showGUISpring = true;

        [KSPField]
        public bool showGUIDamper = true;

        [KSPField]
        public bool showGUIAntiRoll = true;

        [KSPField]
        public bool showGUIScale = true;

        [KSPField]
        public bool showGUIWheelGroup = true;
                
        [KSPField]
        public float exSlipPoint = 0.06f;

        [KSPField]
        public float exSlipVal = 1.0f;

        [KSPField]
        public float asSlipPoint = 0.08f;

        [KSPField]
        public float asSlipVal = 1.2f;

        [KSPField]
        public float tailSlipVal = 0.6f;

        #endregion

        #region REGION - Persistent data

        [KSPField(isPersistant = true)]
        public string label = string.Empty;

        [KSPField(isPersistant = true)]
        public string persistentState = KSPWheelState.DEPLOYED.ToString();

        [KSPField(isPersistant = true)]
        public string persistentData = String.Empty;

        [KSPField(isPersistant = true)]
        public bool grounded = false;

        [KSPField(isPersistant = true)]
        public bool initializedEditor = false;

        [Persistent]
        public string configNodeData = string.Empty;

        #endregion

        #region REGION - Private working/cached variables

        public KSPWheelData[] wheelData;

        public KSPWheelState wheelState
        {
            get { return currentWheelState; }
        }

        //TODO -- there has to be a better way to fix the lack of OnLoad for root parts in editor than this, but I can't find one
        //TODO - might still have problems in other places, as that is where the [Persistent] tag for config-node data came from
        //Serialized to allow the state to be loaded from config data in prefab and persisted to in-editor/etc.
        [SerializeField]
        private KSPWheelState currentWheelState = KSPWheelState.DEPLOYED;

        public float wheelRepairTimer = 1f;

        [SerializeField]
        internal float deployAnimationTime = 1f;

        internal float partMassScaleFactor = 1;
        internal float partCostScaleFactor = 1;
        internal float wheelMassScaleFactor = 1;
        internal float wheelMaxSpeedScalingFactor = 1f;
        internal float wheelMaxLoadScalingFactor = 1f;
        internal float rollingResistanceScalingFactor = 1f;
        internal float motorTorqueScalingFactor = 1f;
        internal float motorPowerScalingFactor = 1f;
        internal float motorMaxRPMScalingFactor = 1f;

        //serialize in editor/etc, should fix cloned-parts starting with improperly offset nodes
        [SerializeField]
        private float prevScale = 1f;

        private bool initializedWheels = false;

        private bool initializedScaling = false;

        private bool prevGrounded = false;

        private float prevVMass = -1f;
        private float prevG = 0f;
        private bool updateSpring = false;

        internal List<KSPWheelSubmodule> subModules = new List<KSPWheelSubmodule>();

        //custom physics material set to combine mode, used to create zero-friction bump-stop colliders
        private static PhysicMaterial customCollisionMaterial;

        #endregion

        #region REGION - GUI Handling methods

        public void onLoadUpdated(BaseField field, object obj)
        {
            this.wheelGroupUpdateBase(int.Parse(wheelGroup), m =>
            {
                if (m != this)
                {
                    m.springRating = springRating;
                    m.dampRatio = dampRatio;
                }
            });
            updateSpring = true;
        }

        public void onShowUIUpdated(BaseField field, object obj)
        {
            int len = subModules.Count;
            for (int i = 0; i < len; i++)
            {
                subModules[i].onUIControlsUpdated(showControls);
            }
            Fields[nameof(springRating)].guiActive = Fields[nameof(springRating)].guiActiveEditor = showControls && showGUISpring;
            Fields[nameof(dampRatio)].guiActive = Fields[nameof(dampRatio)].guiActiveEditor = showControls && showGUIDamper;
            Fields[nameof(wheelGroup)].guiActive = Fields[nameof(wheelGroup)].guiActiveEditor = showControls && showGUIWheelGroup;
            Fields[nameof(antiRoll)].guiActive = Fields[nameof(antiRoll)].guiActiveEditor = showControls && showGUIAntiRoll;
            Fields[nameof(scale)].guiActiveEditor = showControls && allowScaling && showGUIScale;
        }

        private void onScaleAdjusted(BaseField field, System.Object obj)
        {
            setScale(scale, true);
            foreach (Part p in part.symmetryCounterparts)
            {
                p.GetComponent<KSPWheelBase>().setScale(scale, true);
            }
        }

        private void setScale(float newScale, bool userInput)
        {
            scale = newScale;
            if (allowScaling)
            {
                Vector3 scale = new Vector3(newScale, newScale, newScale);
                Transform modelRoot = part.transform.FindRecursive("model");
                if (!string.IsNullOrEmpty(scalingTransform))
                {
                    Transform scalar = modelRoot.FindRecursive(scalingTransform);
                    if (scalar != null)
                    {
                        scalar.localScale = scale;
                    }
                }
                else
                {
                    foreach (Transform child in modelRoot)
                    {
                        child.localScale = scale;
                    }
                }
                Utils.updateAttachNodes(part, prevScale, newScale, userInput);
            }
            else
            {
                scale = newScale = 1f;
            }
            prevScale = newScale;
            onScaleUpdated();
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
            currentWheelState = (KSPWheelState)Enum.Parse(typeof(KSPWheelState), persistentState);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            persistentState = currentWheelState.ToString();
            node.SetValue("persistentState", persistentState, true);
            if (wheelData != null)
            {
                persistentData = string.Empty;
                int len = wheelData.Length;
                for (int i = 0; i < len; i++)
                {
                    if (i > 0) { persistentData = persistentData + ";"; }
                    persistentData = persistentData + wheelData[i].saveData();
                }
            }
        }
        
        /// <summary>
        /// Initializes wheel parameters, removes stock wheel collider component, instantiates custom wheel collider component container, sets up animation handling (if needed)
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            currentWheelState = (KSPWheelState)Enum.Parse(typeof(KSPWheelState), persistentState);

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

            if (maxSpeed <= 0)
            {
                int len = wheelData.Length;
                float maxRad = 0f;
                for (int i = 0; i < len; i++)
                {
                    if (wheelData[i].wheelRadius > maxRad) { maxRad = wheelData[i].wheelRadius; }
                }
                maxSpeed = maxRad * 400f * Mathf.PI * 2 * 0.01666666f;
            }

            BaseField field = Fields[nameof(dampRatio)];
            field.uiControlEditor.onFieldChanged = field.uiControlFlight.onFieldChanged = onLoadUpdated;
            UI_FloatRange rng = (UI_FloatRange)field.uiControlFlight;
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

            field = Fields[nameof(springRating)];
            field.uiControlEditor.onFieldChanged = field.uiControlFlight.onFieldChanged = onLoadUpdated;
            rng = (UI_FloatRange)field.uiControlFlight;
            if (rng != null)
            {
                rng.minValue = minSpringRating;
                rng.maxValue = maxSpringRating;
            }
            rng = (UI_FloatRange)field.uiControlEditor;
            if (rng != null)
            {
                rng.minValue = minSpringRating;
                rng.maxValue = maxSpringRating;
            }

            field = Fields[nameof(showControls)];
            field.uiControlEditor.onFieldChanged = field.uiControlFlight.onFieldChanged = onShowUIUpdated;

            Fields[nameof(springRating)].uiControlFlight.onFieldChanged = onLoadUpdated;

            Fields[nameof(scale)].uiControlEditor.onFieldChanged = onScaleAdjusted;
            Fields[nameof(scale)].guiActiveEditor = allowScaling;
            UI_FloatEdit ufe = (UI_FloatEdit)Fields[nameof(scale)].uiControlEditor;
            ufe.minValue = minScale;
            ufe.maxValue = maxScale;

            bool frictionControlEnabled = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().enableFrictionControl;
            Callback<BaseField, System.Object> frictionAction = delegate (BaseField a, System.Object b)
            {
                float fMult = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().globalFrictionAdjustment;
                this.symmetryUpdate(m =>
                {
                    if (m != this) { m.frictionControl = frictionControl; }
                    int len = m.wheelData.Length;
                    for (int i = 0; i < len; i++)
                    {
                        m.wheelData[i].wheel.surfaceFrictionCoefficient = m.frictionControl * m.frictionMult * fMult;
                    }
                });
            };
            Fields[nameof(frictionControl)].guiActive = Fields[nameof(frictionControl)].guiActiveEditor = frictionControlEnabled;
            Fields[nameof(frictionControl)].uiControlEditor.onFieldChanged = Fields[nameof(frictionControl)].uiControlFlight.onFieldChanged = frictionAction;

            //destroy stock collision enhancer collider
            if (HighLogic.LoadedSceneIsFlight)
            {
                Collider[] colliders = part.GetComponentsInChildren<Collider>();
                int len = colliders.Length;
                for (int i = 0; i < len; i++)
                {
                    if (colliders[i].gameObject.name.ToLower() == "collisionenhancer")
                    {
                        GameObject.DestroyImmediate(colliders[i].gameObject);
                    }
                }
            }

            //destroy bounds collider, if specified and present (KF wheels)
            if (!string.IsNullOrEmpty(boundsColliderName))
            {
                Transform[] boundsColliders = part.transform.FindChildren(boundsColliderName);
                int len = boundsColliders.Length;
                for (int i = 0; i < len; i++)
                {
                    GameObject.DestroyImmediate(boundsColliders[i].gameObject);
                }
            }

            if (part.collider == null)
            {
                Utils.setPartColliderField(part);
            }
            initializeScaling();

            if (customCollisionMaterial == null)
            {
                customCollisionMaterial = new PhysicMaterial("SlideMaterial");
                customCollisionMaterial.frictionCombine = PhysicMaterialCombine.Multiply;
            }
        }

        public void Start()
        {
            int count = wheelData.Length;
            string[] wheelPersistentDatas = persistentData.Split(';');
            float fMult = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().globalFrictionAdjustment;
            for (int i = 0; i < count; i++)
            {
                wheelData[i].locateTransform(part.transform);
                wheelData[i].setupWheel(null, raycastMask, part.rescaleFactor * scale);
                wheelData[i].wheel.forwardFrictionCurve = new KSPWheelFrictionCurve(exSlipPoint, exSlipVal, asSlipPoint, asSlipVal, tailSlipVal);
                wheelData[i].wheel.sidewaysFrictionCurve = new KSPWheelFrictionCurve(exSlipPoint, exSlipVal, asSlipPoint, asSlipVal, tailSlipVal);
                if (HighLogic.LoadedSceneIsFlight)
                {
                    CollisionManager.IgnoreCollidersOnVessel(vessel, wheelData[i].bumpStopCollider);
                    wheelData[i].bumpStopCollider.enabled = currentWheelState == KSPWheelState.DEPLOYED || currentWheelState == KSPWheelState.BROKEN;
                }
                wheelData[i].wheel.surfaceFrictionCoefficient = frictionMult * fMult * frictionControl;
                wheelData[i].wheel.forwardFrictionCoefficient = forwardFriction;
                wheelData[i].wheel.sideFrictionCoefficient = sidewaysFriction;
                wheelData[i].wheel.rollingResistance = rollingResistance;
                wheelData[i].wheel.rotationalResistance = rotationalResistance;
                if (!string.IsNullOrEmpty(persistentData))
                {
                    wheelData[i].loadData(wheelPersistentDatas[i]);
                }
            }
            //run wheel init on a second pass so that all wheels are available
            //some modules may use more than a single wheel (damage, tracks, dust, debug)
            for (int i = 0; i < count; i++)
            {
                onWheelCreated(i, wheelData[i]);
            }
            this.onShowUIUpdated(null, null);
            initializedEditor = true;
            //if this is flight scene, update stock drag cubes
            if (scaleDragCubes && HighLogic.LoadedSceneIsFlight)
            {
                updateDragCubes(prevScale, scale);
            }
            //finally, also update FAR voxelization
            part.SendMessage("GeometryPartModuleRebuildMeshData");
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
            if (TimeWarp.WarpMode == TimeWarp.Modes.HIGH && TimeWarp.CurrentRateIndex > 0)
            {
                //MonoBehaviour.print("Skipping wheel update from non-physics timewarp");
                return;
            }
            if (!initializedWheels)
            {
                Rigidbody rb = null;
                if (useParentRigidbody && part.parent != null)
                {
                    rb = Utils.locateRigidbodyUpwards(part);
                }
                else
                {
                    rb = part.GetComponent<Rigidbody>();
                }
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
                        wheelData[i].wheel.rigidbody = rb;
                    }
                    if (part.collisionEnhancer != null)
                    {
                        part.collisionEnhancer.OnTerrainPunchThrough = CollisionEnhancerBehaviour.DO_NOTHING;
                    }
                    if (scaleDragCubes)
                    {
                        updateDragCubes(1, scale);
                    }
                    part.SendMessage("GeometryPartModuleRebuildMeshData");
                }
            }

            int len = wheelData.Length;
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

            if (currentWheelState == KSPWheelState.DEPLOYED)
            {
                int subLen = subModules.Count;
                for (int i = 0; i < subLen; i++)
                {
                    subModules[i].preWheelSuspensionCalc();
                }
                updateSuspension();
                for (int i = 0; i < subLen; i++)
                {
                    subModules[i].preWheelPhysicsUpdate();
                }
                KSPWheelCollider wheel;
                for (int i = 0; i < len; i++)
                {
                    wheel = wheelData[i].wheel;
                    wheel.gravityVector = vessel.gravityForPos;
                    wheel.updateWheel();
                    if (wheel.contactColliderHit != null)
                    {
                        PhysicMaterial pm = wheel.contactColliderHit.material;
                        if (pm == null) { wheel.contactColliderHit.material = customCollisionMaterial; }
                        pm.frictionCombine = PhysicMaterialCombine.Multiply;
                    }
                }
                if (antiRoll > 0)
                {
                    if (useSelfAntiRoll)
                    {
                        KSPWheelCollider otherWheel;
                        for (int i = 0; i < len; i++)
                        {
                            wheel = wheelData[i].wheel;
                            otherWheel = wheelData[wheelData[i].symmetryIndex].wheel;
                            if (wheel.isGrounded && otherWheel.isGrounded)
                            {
                                float force = (wheel.compressionDistance - otherWheel.compressionDistance) * antiRoll * wheel.spring;
                                wheel.rigidbody.AddForceAtPosition(force * wheel.contactNormal, wheel.transform.position);
                            }
                        }
                        //TODO -- linking / use of multiple base-modules in the same part
                        //TODO -- might require adding a second 'baseModuleSymmetryIndex' to the wheel-data instances
                    }
                    else if (part.symmetryCounterparts != null && part.symmetryCounterparts.Count > 0)
                    {
                        //TODO -- find index of this base module within set of base modules in the part
                        //TODO -- use that index as the index-in-duplicates of the base module on the other part.
                        KSPWheelBase otherModule = (KSPWheelBase)part.symmetryCounterparts[0].Modules[part.Modules.IndexOf(this)];
                        KSPWheelCollider otherWheel;
                        int otherIndex = 0;
                        for (int i = 0; i < len; i++)
                        {
                            wheel = wheelData[i].wheel;
                            otherIndex = antiRollInvertIndices ? len - 1 - i : i;
                            otherWheel = otherModule.wheelData[otherIndex].wheel;
                            if (wheel.isGrounded && otherWheel.isGrounded)
                            {
                                float force = (wheel.compressionDistance - otherWheel.compressionDistance) * antiRoll * wheel.spring;
                                wheel.rigidbody.AddForceAtPosition(force * wheel.contactNormal, wheel.transform.position);
                            }
                        }
                    }

                }
                for (int i = 0; i < subLen; i++)
                {
                    subModules[i].postWheelPhysicsUpdate();
                }
            }

            if (wheelRepairTimer < 1)
            {
                wheelRepairTimer = Mathf.MoveTowards(wheelRepairTimer, 1, Time.fixedDeltaTime);
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
            float pos = part.transform.position.y - groundHeightOffset * (part.rescaleFactor * scale);
            phq.lowestOnParts[part] = Mathf.Min(phq.lowestOnParts[part], pos);
            phq.lowestPoint = Mathf.Min(phq.lowestPoint, phq.lowestOnParts[part]);
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            return -defaultCost + Mathf.Pow(scale, 3) * defaultCost;
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            return -defaultMass + Mathf.Pow(scale, 3) * defaultMass;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        public string GetModuleTitle()
        {
            return wheelType;
        }

        public string GetPrimaryField()
        {
            return "Max Load: "+maxLoadRating;
        }

        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }

        public override string GetInfo()
        {
            String val = "Max Speed: " + maxSpeed + "\n";
            val = val + "Max Load : " + maxLoadRating;
            string moduleInfo = "";
            if (subModules != null)
            {
                int len = subModules.Count;
                for (int i = 0; i < len; i++)
                {
                    moduleInfo = subModules[i].getModuleInfo();
                    if (!string.IsNullOrEmpty(moduleInfo))
                    {
                        val = val + "\n" + moduleInfo;
                    }
                }
            }
            return val;
        }

        #endregion

        #region REGION - Custom update methods

        private void initializeScaling()
        {
            if (initializedScaling) { return; }
            prevScale = scale;
            initializedScaling = true;
            setScale(scale, false);
        }

        private void updateSuspension()
        {
            if (wheelData == null || wheelData[0].wheel==null)
            {
                return;
            }
            float vesselMass = 0;
            if (vessel == null || (vesselMass = (float)vessel.totalMass) <= 0)
            {
                return;
            }
            float g = (float)vessel.gravityForPos.magnitude;
            if (vesselMass == prevVMass && prevG==g && !updateSpring)//only update spring stuff when vessel mass has changed
            {
                return;
            }
            updateSpring = false;
            prevVMass = vesselMass;
            prevG = g;

            float spring, damper, springLoad, natFreq, criticalDamping;
            float lengthCorrectedMass;
            int len = wheelData.Length;
            KSPWheelData data;
            for (int i = 0; i < len; i++)
            {
                data = wheelData[i];
                lengthCorrectedMass = data.wheel.length <= 0 ? 0 : vesselMass / data.wheel.length * data.loadShare;//allows for a wheel to support full vessel mass at full compression
                spring = lengthCorrectedMass * springRating * g * wheelRepairTimer * wheelRepairTimer;
                if (spring > 0)
                {
                    springLoad = spring * data.wheel.length * 0.5f * 1 / g;//target load for damper calc is spring at half compression
                    natFreq = Mathf.Sqrt(spring / springLoad);//natural frequency
                    criticalDamping = 2 * springLoad * natFreq;//critical damping
                    damper = criticalDamping * dampRatio * wheelRepairTimer;
                }
                else
                {
                    damper = 0f;
                }
                data.wheel.spring = spring;
                data.wheel.damper = damper;
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

        internal void changeWheelState(KSPWheelState newState, KSPWheelSubmodule module, bool selfCallback = false)
        {
            KSPWheelState oldState = currentWheelState;
            currentWheelState = newState;
            this.persistentState = currentWheelState.ToString();
            int len = subModules.Count;
            for (int i = 0; i < len; i++)
            {
                if (module == subModules[i])
                {
                    if (selfCallback)
                    {
                        module.onStateChanged(oldState, newState);
                    }
                }
                else
                {
                    subModules[i].onStateChanged(oldState, newState);
                }
            }

            if (HighLogic.LoadedSceneIsFlight && wheelData!=null)
            {
                len = wheelData.Length;
                for (int i = 0; i < len; i++)
                {
                    if (wheelData[i].bumpStopCollider == null) { break; }//if one is not present, none will be, as something is not initialized yet;
                    wheelData[i].bumpStopCollider.enabled = currentWheelState == KSPWheelState.DEPLOYED || currentWheelState == KSPWheelState.BROKEN;
                    wheelData[i].bumpStopMat.dynamicFriction = currentWheelState == KSPWheelState.DEPLOYED ? 0f : 1f;
                    wheelData[i].bumpStopMat.staticFriction = currentWheelState == KSPWheelState.DEPLOYED ? 0f : 1f;
                    wheelData[i].bumpStopCollider.material = wheelData[i].bumpStopMat;
                    if (wheelData[i].wheel != null)
                    {
                        wheelData[i].wheel.angularVelocity = 0f;
                        wheelData[i].wheel.motorTorque = 0f;
                        wheelData[i].wheel.brakeTorque = 0f;
                        wheelData[i].wheel.clearGroundedState();
                    }
                    prevCollider = null;
                    landedOnVessel = null;
                    landedBiomeName = string.Empty;
                }
            }
            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                part.SendMessage("GeometryPartModuleRebuildMeshData");
            }
        }

        internal void setDeployTime(float value)
        {
            if (value != deployAnimationTime)
            {
                deployAnimationTime = value;
                int len = subModules.Count;
                for (int i = 0; i < len; i++)
                {
                    subModules[i].onDeployTimeUpdated(value);
                }
            }
        }

        private void onScaleUpdated()
        {
            if (HighLogic.CurrentGame != null)//should not happen for on start
            {
                KSPWheelScaleSettings scales = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelScaleSettings>();
                float localScale = scale * part.rescaleFactor;
                partMassScaleFactor = Mathf.Pow(localScale, scales.partMassScalingPower);
                partCostScaleFactor = Mathf.Pow(localScale, scales.partCostScalingPower);
                wheelMassScaleFactor = Mathf.Pow(localScale, scales.wheelMassScalingPower);
                wheelMaxLoadScalingFactor = Mathf.Pow(localScale, scales.wheelMaxLoadScalingPower);
                wheelMaxSpeedScalingFactor = Mathf.Pow(localScale, scales.wheelMaxSpeedScalingPower);
                motorMaxRPMScalingFactor = Mathf.Pow(localScale, scales.motorMaxRPMScalingPower);
                motorPowerScalingFactor = Mathf.Pow(localScale, scales.motorPowerScalingPower);
                motorTorqueScalingFactor = Mathf.Pow(localScale, scales.motorTorqueScalingPower);
            }
            if (wheelData != null)
            {
                int wlen = wheelData.Length;
                for (int i = 0; i < wlen; i++)
                {
                    if (wheelData[i].wheel != null) { wheelData[i].wheel.length = wheelData[i].suspensionTravel * scale * part.rescaleFactor; }
                }
            }
            int len = subModules.Count;
            for (int i = 0; i < len; i++)
            {
                subModules[i].onScaleUpdated();
            }
        }

        private void updateDragCubes(float prevScale, float newScale)
        {
            if (part.DragCubes != null && part.DragCubes.Cubes != null)// && prevScale!=newScale && newScale!=1)
            {
                DragCube cube;
                float area, depth;
                int len = part.DragCubes.Cubes.Count;
                int l2;
                for (int i = 0; i < len; i++)
                {
                    cube = part.DragCubes.Cubes[i];
                    l2 = cube.Area.Length;
                    for (int k = 0; k < l2; k++)
                    {
                        area = cube.Area[k];
                        area /= prevScale;
                        area *= newScale;
                        cube.Area[k] = area;
                    }
                    l2 = cube.Depth.Length;
                    for (int k = 0; k < l2; k++)
                    {
                        depth = cube.Depth[k];
                        depth /= prevScale;
                        depth *= newScale;
                        cube.Depth[k] = depth;
                    }
                }
                part.DragCubes.ForceUpdate(true, true);
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

        private Collider prevCollider;
        private Vessel landedOnVessel;
        private string landedBiomeName;
        //TODO also need to check the rest of the parts' colliders for contact/grounded state somehow (or they are handled by regular Part methods?)
        private void updateLandedState()
        {
            bool updateVesselLandedState = false;
            bool splashed = false;
            bool wheelGrounded = false;
            Collider collider = null;
            int len = wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                if (wheelData[i].waterMode)
                {
                    splashed = true;
                }
                if (wheelData[i].wheel.contactColliderHit != null)
                {
                    collider = wheelData[i].wheel.contactColliderHit;
                }
                if (wheelData[i].wheel.isGrounded)
                {
                    wheelGrounded = true;
                }
            }

            if (prevCollider != collider)//something has changed
            {
                //set to default values for no hit
                updateVesselLandedState = true;
                landedOnVessel = null;
                grounded = false;
                landedBiomeName = string.Empty;
                if (collider != null)//something was hit, set new values depending on what it was
                {
                    setLandedat(ref collider, out landedOnVessel, out landedBiomeName, out grounded);
                }
            }
            else if (landedOnVessel != null)//else nothing changed, but should update the vessel landed on, if any
            {
                grounded = landedOnVessel.LandedOrSplashed;
                if (grounded)
                {
                    if (landedBiomeName != landedOnVessel.landedAt)//change of state, new landed/not landed
                    {
                        landedBiomeName = landedOnVessel.landedAt;
                        updateVesselLandedState = true;
                    }
                }
                else if (!string.IsNullOrEmpty(landedBiomeName))//was previously landed, but now is not
                {
                    updateVesselLandedState = true;
                    landedBiomeName = string.Empty;
                }
            }
            else if (wheelGrounded && collider == null)//repulsors get into this state while levitating over water; wheel returns grounded, but no collider
            {
                //TODO
                //unknown...
                //grounded = false;
                //landedBiomeName = string.Empty;
            }
            else if (splashed)//wheels will get into this state while in the water
            {
                //TODO -- does stock code fully handle splashed setting?
                //unknown...
            }
            else if (collider != null && string.IsNullOrEmpty(vessel.landedAt))
            {
                setLandedat(ref collider, out landedOnVessel, out landedBiomeName, out grounded);
                updateVesselLandedState = true;
            }
            prevCollider = collider;
            prevGrounded = grounded;
            part.GroundContact = grounded;
            if (updateVesselLandedState)
            {
                vessel.checkLanded();//this clears landed biome name
                vessel.SetLandedAt(landedBiomeName);
            }
            else if (grounded && !string.IsNullOrEmpty(landedBiomeName))
            {
                vessel.SetLandedAt(landedBiomeName);//has to run every tick, else stock code clobbers it during init
            }
        }

        private void setLandedat(ref Collider collider, out Vessel landedOnVessel, out string landedBiomeName, out bool grounded)
        {
            landedOnVessel = null;
            landedBiomeName = string.Empty;
            grounded = false;
            if (collider.gameObject.layer == 0)//possibly a part
            {
                //check for if same vessel
                Part hitPart = collider.gameObject.GetComponentUpwards<Part>();
                if (hitPart != null)
                {
                    if (hitPart.vessel == vessel)//ignore same vessel collision data, treat as ungrounded
                    {
                        //noop, handled by defaults for no hit above
                    }
                    else if (hitPart.vessel != null)//not same vessel, use hit vessels grounded state
                    {
                        landedOnVessel = hitPart.vessel;
                        landedBiomeName = landedOnVessel.landedAt;
                        grounded = landedOnVessel.LandedOrSplashed;
                    }
                    else//null vessel, not sure why/when this would occur, but treat as undefined
                    {
                        collider = null;//setting collider to null will cause it to be re-checked next update if the same object is hit
                    }
                }
                //else -- not a part, undefined, use default 'no hit' data from above
            }
            else if (collider.gameObject.layer == 15)//scenery
            {
                if (string.IsNullOrEmpty(collider.gameObject.tag) || collider.gameObject.tag == "Untagged")
                {
                    landedBiomeName = string.Empty;
                }
                else
                {
                    landedBiomeName = collider.gameObject.tag;
                }
                grounded = true;
            }
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

        public string getDebugInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Part: "+part.name);
            sb.AppendLine("ID: " + part.craftID);
            sb.AppendLine("Wheels in part: " + wheelData.Length);
            sb.AppendLine("Spring: " + springRating);
            sb.AppendLine("Damper: " + dampRatio);
            sb.AppendLine("Per-Wheel data: ");
            int len = wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                sb.AppendLine(wheelData[i].getDebugInfo());
            }
            return sb.ToString();
        }

        #endregion

        public class KSPWheelData
        {

            #region REGION - Config defined values
            public readonly String wheelColliderName;
            public readonly float wheelRadius;
            public readonly float wheelWidth;
            public readonly float wheelMass;
            public readonly float suspensionTravel;
            public readonly float loadShare;
            public readonly float offset;
            public readonly int indexInDuplicates;
            public readonly int symmetryIndex;
            public readonly string colliderType;
            public readonly bool complexBumpStop;
            #endregion
            public KSPWheelCollider wheel;
            public Transform wheelTransform;
            public GameObject bumpStopGameObject;
            public MeshCollider bumpStopCollider;
            public PhysicMaterial bumpStopMat;
            public GameObject bumpStopGameObject2;
            public MeshCollider bumpStopCollider2;
            public float loadRating;
            public float loadTarget;
            public float timeBoostFactor;
            public float prevComp;
            public bool waterMode;
            public float waterEffectForce;
            public float waterEffectSize;
            public Vector3 waterEffectPos;

            public KSPWheelData(ConfigNode node)
            {
                wheelColliderName = node.GetStringValue("colliderName", "WheelCollider");
                colliderType = node.GetStringValue("colliderType", "RAY");
                wheelRadius = node.GetFloatValue("radius", 0.25f);
                wheelWidth = node.GetFloatValue("width", wheelRadius * 0.2f);
                wheelMass = node.GetFloatValue("mass", 0.05f);
                suspensionTravel = node.GetFloatValue("travel", 0.25f);
                loadShare = node.GetFloatValue("load", 1f);
                offset = node.GetFloatValue("offset", 0f);
                indexInDuplicates = node.GetIntValue("indexInDuplicates", 0);
                symmetryIndex = node.GetIntValue("symmetryIndex", 0);
                complexBumpStop = node.GetBoolValue("complexBumpStop", false);
            }

            public void locateTransform(Transform root)
            {
                wheelTransform = root.FindChildren(wheelColliderName)[indexInDuplicates];
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
                try
                {
                    wheel.sweepType = (KSPWheelSweepType)Enum.Parse(typeof(KSPWheelSweepType), colliderType);
                }
                catch (Exception e)
                {
                    wheel.sweepType = KSPWheelSweepType.RAY;
                }

                if (HighLogic.LoadedSceneIsFlight)
                {
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

                    bumpStopMat = new PhysicMaterial("BumpStopPhysicsMaterial");
                    bumpStopMat.bounciness = 0f;
                    bumpStopMat.dynamicFriction = 0;
                    bumpStopMat.staticFriction = 0;
                    bumpStopMat.frictionCombine = PhysicMaterialCombine.Multiply;
                    bumpStopMat.bounceCombine = PhysicMaterialCombine.Multiply;
                    bumpStopCollider.material = bumpStopMat;
                    bumpStopGameObject.transform.NestToParent(wheelTransform);
                    bumpStopGameObject.transform.Rotate(0, 0, 90, Space.Self);//rotate it so that it is in the proper orientation (collider y+ is the flat side, so it needs to point along wheel x+/-)

                    if (complexBumpStop)
                    {
                        scaleY = wheelRadius * 0.205f * scaleFactor;//wheel width
                        scaleY *= 0.5f;//default is 2 units high, fix to 1 unit * width
                        scaleXZ = wheelRadius * 1.995f * scaleFactor;

                        bumpStopGameObject2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        bumpStopGameObject2.name = "KSPWheelBumpStop2-" + wheelColliderName;
                        bumpStopGameObject2.transform.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);
                        bumpStopGameObject2.layer = 26;
                        //remove existing capsule collider
                        GameObject.DestroyImmediate(bumpStopGameObject2.GetComponent<CapsuleCollider>());
                        //remove existing mesh renderer
                        GameObject.DestroyImmediate(bumpStopGameObject2.GetComponent<MeshRenderer>());
                        //add mesh collider
                        bumpStopCollider2 = bumpStopGameObject2.AddComponent<MeshCollider>();
                        //mark as convex
                        bumpStopCollider2.convex = true;
                        //uses standard physics material properties
                        bumpStopGameObject2.transform.NestToParent(wheelTransform);
                        bumpStopGameObject2.transform.Rotate(0, 0, 90, Space.Self);//rotate it so that it is in the proper orientation (collider y+ is the flat side, so it needs to point along wheel x+/-)
                    }
                }
            }

            public float scaledMass(float scaleFactor)
            {
                return Mathf.Pow(scaleFactor, HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelScaleSettings>().wheelMassScalingPower) * wheelMass;
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

            internal string saveData()
            {
                string data = timeBoostFactor+",";
                if (wheel != null)
                {
                    data = data + wheel.rpm;
                }
                else
                {
                    data = data + "0";
                }
                return data;
            }

            internal void loadData(string data)
            {
                string[] vals = data.Split(',');
                timeBoostFactor = float.Parse(vals[0]);
                if (wheel != null)
                {
                    wheel.rpm = float.Parse(vals[1]);
                }
            }

            public string getDebugInfo()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Travel: " + wheel.length);
                sb.AppendLine("Mass  : " + wheel.mass);
                sb.AppendLine("Share : " + loadShare);
                sb.AppendLine("Rating: " + loadRating);
                sb.AppendLine("LdTarg: " + loadTarget);
                sb.AppendLine("Spring: " + wheel.spring);
                sb.AppendLine("Damper: " + wheel.damper);
                sb.AppendLine("SForce: " + wheel.springForce);
                sb.AppendLine("DampFc: " + wheel.dampForce);
                sb.AppendLine("HitDst: " + wheel.compressionDistance);
                sb.AppendLine("FX    : " + wheel.lateralForce);
                sb.AppendLine("FZ    : " + wheel.longitudinalForce);
                sb.AppendLine("SX    : " + wheel.lateralSlip);
                sb.AppendLine("SZ    : " + wheel.longitudinalSlip);
                sb.AppendLine("SteerA: " + wheel.steeringAngle);
                sb.AppendLine("Torque: " + wheel.motorTorque);
                return sb.ToString();
            }

        }

    }

}
