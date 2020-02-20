using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{

    public class KSPWheelDamage : KSPWheelSubmodule
    {

        /// <summary>
        /// The config specified max-load rating for the part.  This will be further adjusted by the current part scale, and the current game difficulty selection.
        /// </summary>
        [KSPField]
        public float maxLoadRating;

        /// <summary>
        /// The config specified max-speed rating for the part.  This will be further adjusted by the current part scale, and the current game difficulty selection.
        /// </summary>
        [KSPField]
        public float maxSpeed;

        /// <summary>
        /// Name of the non-broken wheel mesh.  When functional, this mesh will be active and displayed.  This mesh will be hidden when the wheel is non-functional.
        /// </summary>
        [KSPField]
        public string wheelName = "wheel";

        /// <summary>
        /// Name of the broken wheel mesh.  When non-functional, this mesh will be active and displayed.  This mesh will be hidden when the wheel is functional.
        /// </summary>
        [KSPField]
        public string bustedWheelName = "bustedWheel";

        /// <summary>
        /// The star-level of the kerbal required for repairs for this part.  Only enforced in career or science modes / ignored in sandbox mode.
        /// </summary>
        [KSPField]
        public int repairLevel = 3;

        /// <summary>
        /// UI display value for the current (post-scaling/post-difficulty) max-speed rating.
        /// </summary>
        [KSPField(guiName= "Max Safe Speed",guiActive = true, guiActiveEditor = true, guiUnits ="m/s", guiFormat = "F2")]
        public float maxSafeSpeed = 0f;

        /// <summary>
        /// UI display value for the current (post-scaling/post-difficulty) max-load rating.
        /// </summary>
        [KSPField(guiName = "Max Safe Load", guiActive = true, guiActiveEditor = true, guiUnits = "t", guiFormat = "F2")]
        public float maxSafeLoad = 0f;

        /// <summary>
        /// The UI 'status' to be displayed.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Wheel Status: ")]
        public string displayStatus = "Operational";

        /// <summary>
        /// Decimal representation of the percentage of max load currently being experienced.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Wheel Stress", guiFormat = "F2"),
         UI_ProgressBar(minValue = 0, maxValue = 1.5f, suppressEditorShipModified = true, scene = UI_Scene.Flight)]
        public float loadStress = 0f;

        /// <summary>
        /// The duration and severity of overloading.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Failure Time", guiFormat = "F2"),
         UI_ProgressBar(minValue = 0, maxValue = 1, suppressEditorShipModified = true, scene = UI_Scene.Flight)]
        public float stressTime = 0f;

        /// <summary>
        /// The current accumulated wheel wear value.  Increased by wheel slip and sharp stresses.  As wear increases, rolling resistance
        /// also increases.
        /// </summary>
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Wheel Wear", guiFormat = "F2", isPersistant = true),
         UI_ProgressBar(minValue = 0, maxValue = 1, suppressEditorShipModified = true, scene = UI_Scene.Flight)]
        public float wheelWear = 0f;

        /// <summary>
        /// The current accumulated motor wear value.  Increased by long-duration loading of the motor.  As wear increases, the efficiency of the motor decreases.
        /// </summary>
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Motor Wear", guiFormat = "F2", isPersistant = true),
         UI_ProgressBar(minValue = 0, maxValue = 1, suppressEditorShipModified = true, scene = UI_Scene.Flight)]
        public float motorWear = 0f;

        /// <summary>
        /// The current accumulated wear to the suspension components.  As wear increases, the maximum available force output decreases.
        /// </summary>
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Suspension Wear", guiFormat = "F2", isPersistant = true),
         UI_ProgressBar(minValue = 0, maxValue = 1, suppressEditorShipModified = true, scene = UI_Scene.Flight)]
        public float suspensionWear = 0f;

        private float speed = 0f;
        private float load = 0f;
        private float invulnerableTime = 0f;

        private float[] defaultRollingResistance;//per wheel collider rolling resistance tracking
        private float[] defaultMotorEfficiency;//per-motor-module efficiency tracking
        
        private Transform[] wheelMeshes;
        private Transform[] bustedWheelMeshes;

        private KSPWheelMotor[] motors;
        
        [KSPEvent(guiName = "Repair Wheel/Gear", guiActive = false, guiActiveEditor = false, guiActiveUnfocused = false, externalToEVAOnly = true, unfocusedRange = 8f)]
        public void repairWheel()
        {
            KSPWheelWearType wearType = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wearType;
            if (controller.wheelState == KSPWheelState.BROKEN || (controller.wheelState==KSPWheelState.DEPLOYED && wearType==KSPWheelWearType.ADVANCED))
            {
                MonoBehaviour.print("Repairing wheel!");
                switch (wearType)
                {
                    case KSPWheelWearType.NONE:
                        break;
                    case KSPWheelWearType.SIMPLE:
                        changeWheelState(KSPWheelState.DEPLOYED);
                        invulnerableTime += 5f;
                        controller.wheelRepairTimer = 0.0001f;
                        MonoBehaviour.print("Repaired wheel Simple");
                        break;
                    case KSPWheelWearType.ADVANCED:
                        if (HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>().KerbalExperienceEnabled(HighLogic.CurrentGame.Mode) && FlightGlobals.ActiveVessel.VesselValues.RepairSkill.value < repairLevel)
                        {
                            ScreenMessages.PostScreenMessage("Crew member has insufficient repair skill to fix this "+controller.wheelType.ToLower()+"\nLevel " + repairLevel + " or higher is required.");
                            return;
                        }
                        changeWheelState(KSPWheelState.DEPLOYED);
                        motorWear = 0f;
                        wheelWear = 0f;
                        suspensionWear = 0f;
                        invulnerableTime += 5f;
                        controller.wheelRepairTimer = 0.0001f;
                        MonoBehaviour.print("Repaired wheel.  Damage: " + motorWear + " : " + wheelWear + " : " + suspensionWear);
                        //TODO -- add a delay before repairing based on how damaged things were
                        break;
                    default:
                        break;
                }
                changeWheelState(KSPWheelState.DEPLOYED);
                updateWheelMeshes(controller.wheelState);
                updateDisplayState();
            }
        }

        public void Start()
        {
            motors = this.getControllerSubmodules<KSPWheelMotor>();
            int len = motors.Length;
            defaultMotorEfficiency = new float[len];
            for (int i = 0; i < len; i++)
            {
                defaultMotorEfficiency[i] = motors[i].motorEfficiency;
            }

            len = controller.wheelData.Length;
            defaultRollingResistance = new float[len];
            for (int i = 0; i < len; i++)
            {
                defaultRollingResistance[i] = controller.wheelData[i].wheel.rollingResistance;
            }
        }

        internal override string getModuleInfo()
        {
            string val = "Max Speed: " + maxLoadRating + " m/s\n";
            val = val + "Max Load : " + maxSpeed +" t";
            return val;
        }

        public override void OnIconCreate()
        {
            base.OnIconCreate();
            if (!String.IsNullOrEmpty(wheelName))
            {
                wheelMeshes = part.transform.FindChildren(wheelName);
            }
            if (!String.IsNullOrEmpty(bustedWheelName))
            {
                bustedWheelMeshes = part.transform.FindChildren(bustedWheelName);
            }
            //clear out broken wheel meshes from icon rendering
            updateWheelMeshes(KSPWheelState.DEPLOYED);
        }

        internal override void onUIControlsUpdated(bool show)
        {
            base.onUIControlsUpdated(show);
        }

        internal override void onScaleUpdated()
        {
            base.onScaleUpdated();
            maxSafeSpeed = controller.GetScaledMaxSpeed(maxSpeed);
            maxSafeLoad = controller.GetScaledMaxLoad(maxLoadRating);
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            //attempt to parse the max load and max speed values from the config node from the controller
            //providing any values specified in this module's config as the defaults.  Thus if no value
            //is found in the controllers config, it will use the local value specified in this module.
            ConfigNode node = ConfigNode.Parse(controller.configNodeData).nodes[0];
            maxLoadRating = node.GetFloatValue("maxLoadRating", maxSafeLoad);
            maxSpeed = node.GetFloatValue("maxSpeed");
            if (maxSpeed <= 0)
            {

            }
            if (!String.IsNullOrEmpty(wheelName))
            {
                wheelMeshes = part.transform.FindChildren(wheelName);
            }
            if (!String.IsNullOrEmpty(bustedWheelName))
            {
                bustedWheelMeshes = part.transform.FindChildren(bustedWheelName);
            }
            updateWheelMeshes(controller.wheelState);
            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                updateDisplayState();
            }
            onScaleUpdated();
            //TODO -- update stats for initial persistent wear setup

            //if no max speed was specified in the config, calculate a default value based on wheel radius and a constant max RPM value
            if (maxSpeed <= 0)
            {
                maxSpeed = controller.GetDefaultMaxSpeed(400);
            }

        }

        internal override void postWheelPhysicsUpdate()
        {
            base.postWheelPhysicsUpdate();
            if (invulnerableTime > 0)
            {
                invulnerableTime -= Time.fixedDeltaTime;
                return;
            }
            if (controller.wheelState != KSPWheelState.DEPLOYED)
            {
                return;
            }
            switch (HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wearType)
            {
                case KSPWheelWearType.NONE:
                    //NOOP
                    break;
                case KSPWheelWearType.SIMPLE:
                    wearUpdateSimple();
                    break;
                case KSPWheelWearType.ADVANCED:
                    wearUpdateAdvanced();
                    break;
                default:
                    //NOOP
                    break;
            }
        }

        private void wearUpdateSimple()
        {
            // -- SIMPLE MODE LOAD HANDLING --
            load = 0f;
            int len = controller.wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                load += controller.wheelData[i].wheel.springForce / 10f;
            }
            loadStress = load / maxSafeLoad;
            if (load > maxSafeLoad)
            {
                float overStress = (load / maxSafeLoad) - 1f;
                stressTime += Time.fixedDeltaTime * overStress * HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelWearSettings>().stressDamageMultiplier * 0.25f;
            }

            // -- SIMPLE MODE SPEED HANDLING --
            speed = 0f;
            for (int i = 0; i < len; i++)
            {
                speed += Mathf.Abs(controller.wheelData[i].wheel.linearVelocity) / TimeWarp.CurrentRate;
            }
            speed /= controller.wheelData.Length;
            if (speed > maxSafeSpeed )
            {
                float overSpeedPercent = (speed / maxSafeSpeed) - 1f;
                stressTime += Time.fixedDeltaTime * overSpeedPercent * HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelWearSettings>().speedDamageMultiplier;
            }

            // -- SIMPLE MODE BREAKAGE HANDLING --
            if (stressTime >= 1.0f)
            {
                MonoBehaviour.print("Wheel broke from overstressing! load: " + load + " max: " + maxSafeLoad+" speed: "+speed+" maxSpeed: "+maxSafeSpeed);
                ScreenMessages.PostScreenMessage("<color=orange><b>[" + this.part + "]:</b> Broke from overstressing.</color>", 5f, ScreenMessageStyle.UPPER_LEFT);
                changeWheelState(KSPWheelState.BROKEN);
                stressTime = 0f;
                updateWheelMeshes(controller.wheelState);
                updateDisplayState();
            }
            if (speed < maxSafeSpeed && load < maxSafeLoad)
            {
                stressTime = Mathf.Max(0, stressTime - Time.fixedDeltaTime);
            }
        }

        private void wearUpdateAdvanced()
        {
            wearUpdateSimple();
            // -- ADVANCED MODE MOTOR WEAR UPDATING --
            int len = motors.Length;
            float heatProduction = 0f;
            for (int i = 0; i < len; i++)
            {
                //TODO - this should be reduced by the motors 'min power' figure, the amount of power that is actually consumed to maintain the magnetic field
                heatProduction += (motors[i].powerInKW - motors[i].powerOutKW) * HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelWearSettings>().motorHeatMultiplier;
            }
            part.AddThermalFlux(heatProduction);
            //TODO these should both be config fields
            float heatTolerance = 400f;
            float peakDamageHeat = 1000f;
            if (part.temperature > heatTolerance)
            {
                float heatWear = (float)part.temperature - heatTolerance / (peakDamageHeat - heatTolerance);
                motorWear += heatWear * Time.fixedDeltaTime * HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelWearSettings>().motorDamageMultiplier;
                len = motors.Length;
                for (int i = 0; i < len; i++)
                {
                    motors[i].motorEfficiency = defaultMotorEfficiency[i] * (1f - motorWear);
                }
            }

            // -- ADVANCED MODE SPEED WEAR UPDATING --
            float speedPercent = Mathf.Pow(Mathf.Max((speed / maxSafeSpeed) - 0.75f, 0), 4);
            if (speedPercent > 0)
            {
                wheelWear += speedPercent * Time.fixedDeltaTime * 0.05f * HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelWearSettings>().speedDamageMultiplier;//should give ~80 minutes at max speed before wear hits 1.0
                len = controller.wheelData.Length;
                for (int i = 0; i < len; i++)
                {
                    controller.wheelData[i].wheel.rollingResistance = defaultRollingResistance[i] + defaultRollingResistance[i] * wheelWear;
                }
            }

            //// -- ADVANCED MODE SLIP WEAR UPDATING --
            //float slip = 0f;
            //for (int i = 0; i < len; i++)
            //{
            //    slip += Mathf.Abs(controller.wheelData[i].wheel.wheelLocalVelocity.x);
            //}
            //slip /= controller.wheelData.Length;
            //float slipPercent = Mathf.Pow(Mathf.Max((slip / (maxSafeSpeed * 0.1f)), 0), 4);
            //if (speedPercent > 0)
            //{
            //    wheelWear += speedPercent * Time.fixedDeltaTime * 0.05f * HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelWearSettings>().slipDamageMultiplier;//should give ~80 minutes at max speed before wear hits 1.0
            //    len = controller.wheelData.Length;
            //    for (int i = 0; i < len; i++)
            //    {
            //        controller.wheelData[i].wheel.rollingResistance = defaultRollingResistance[i] + defaultRollingResistance[i] * wheelWear;
            //    }
            //}

            // -- ADVANCED MODE SUSPENSION WEAR UPDATING --
            float loadpercent = Mathf.Pow(Mathf.Max((load / maxSafeLoad) - 0.9f, 0), 2);
            suspensionWear += loadpercent * Time.fixedDeltaTime * HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelWearSettings>().stressDamageMultiplier;
            suspensionWear = Mathf.Clamp01(suspensionWear);
            controller.wheelRepairTimer = 1f - suspensionWear;
        }

        private void updateWheelMeshes(KSPWheelState wheelState)
        {
            if (wheelState == KSPWheelState.BROKEN)
            {
                if (bustedWheelMeshes != null)
                {
                    int len = bustedWheelMeshes.Length;
                    for (int i = 0; i < len; i++)
                    {
                        bustedWheelMeshes[i].gameObject.SetActive(true);
                    }
                    if (wheelMeshes != null)
                    {
                        len = wheelMeshes.Length;
                        for (int i = 0; i < len; i++)
                        {
                            wheelMeshes[i].gameObject.SetActive(false);
                        }
                    }
                }
                if (wheel != null)
                {
                    int len = controller.wheelData.Length;
                    for (int i = 0; i < len; i++)
                    {
                        controller.wheelData[i].wheel.angularVelocity = 0f;
                        controller.wheelData[i].wheel.motorTorque = 0f;
                        controller.wheelData[i].wheel.brakeTorque = 0f;
                    }
                }
            }
            else
            {
                if (bustedWheelMeshes != null)
                {
                    int len = bustedWheelMeshes.Length;
                    for (int i = 0; i < len; i++)
                    {
                        bustedWheelMeshes[i].gameObject.SetActive(false);
                    }
                }
                if (wheelMeshes != null)
                {
                    int len = wheelMeshes.Length;
                    for (int i = 0; i < len; i++)
                    {
                        wheelMeshes[i].gameObject.SetActive(true);
                    }
                }
            }
        }

        private void updateDisplayState()
        {
            KSPWheelState wheelState = controller.wheelState;
            KSPWheelWearType wearType = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wearType;
            Events[nameof(repairWheel)].guiActiveUnfocused = wheelState == KSPWheelState.BROKEN || wearType==KSPWheelWearType.ADVANCED;
            Fields[nameof(loadStress)].guiActive = wearType != KSPWheelWearType.NONE;
            Fields[nameof(stressTime)].guiActive = wearType != KSPWheelWearType.NONE;
            Fields[nameof(displayStatus)].guiActive = wearType != KSPWheelWearType.NONE;
            Fields[nameof(wheelWear)].guiActive = wearType == KSPWheelWearType.ADVANCED;
            Fields[nameof(motorWear)].guiActive = wearType == KSPWheelWearType.ADVANCED;
            Fields[nameof(suspensionWear)].guiActive = wearType == KSPWheelWearType.ADVANCED;
            Fields[nameof(maxSafeSpeed)].guiActive = Fields[nameof(maxSafeSpeed)].guiActiveEditor = wearType != KSPWheelWearType.NONE;
            Fields[nameof(maxSafeLoad)].guiActive = Fields[nameof(maxSafeLoad)].guiActiveEditor = wearType != KSPWheelWearType.NONE;
            switch (wheelState)
            {
                case KSPWheelState.RETRACTED:
                case KSPWheelState.RETRACTING:
                case KSPWheelState.DEPLOYED:
                case KSPWheelState.DEPLOYING:
                    displayStatus = "Operational";
                    break;
                case KSPWheelState.BROKEN:
                    displayStatus = "Broken";
                    break;
                default:
                    break;
            }
        }

    }
}
