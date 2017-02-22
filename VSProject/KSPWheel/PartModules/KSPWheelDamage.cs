using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{

    public class KSPWheelDamage : KSPWheelSubmodule
    {
                
        [KSPField]
        public string wheelName = "wheel";

        [KSPField]
        public string bustedWheelName = "bustedWheel";

        [KSPField]
        public float persistentWear = 0f;

        [KSPField(guiName= "Max Safe Speed",guiActive = true, guiActiveEditor = true, guiUnits ="m/s", guiFormat = "F2")]
        public float maxSafeSpeed = 0f;

        [KSPField(guiName = "Max Safe Load", guiActive = true, guiActiveEditor = true, guiUnits = "t", guiFormat = "F2")]
        public float maxSafeLoad = 0f;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Wheel Status: ")]
        public string displayStatus = "Operational";

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Wheel Stress"),
         UI_ProgressBar(minValue = 0, maxValue = 1.5f, suppressEditorShipModified = true, scene = UI_Scene.Flight)]
        public float loadStress = 0f;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Failure Time"),
         UI_ProgressBar(minValue = 0, maxValue = 1, suppressEditorShipModified = true, scene = UI_Scene.Flight)]
        public float stressTime = 0f;

        private float invulnerableTime = 0f;
        
        private Transform[] wheelMeshes;
        private Transform[] bustedWheelMeshes;
        
        [KSPEvent(guiName = "Repair Wheel/Gear", guiActive = false, guiActiveEditor = false, guiActiveUnfocused = false, externalToEVAOnly = true, unfocusedRange = 8f)]
        public void repairWheel()
        {
            if (controller.wheelState == KSPWheelState.BROKEN)
            {
                MonoBehaviour.print("Repairing wheel!");
                KSPWheelWearType wearType = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wearType;
                switch (wearType)
                {
                    case KSPWheelWearType.NONE:
                        break;
                    case KSPWheelWearType.SIMPLE:
                        changeWheelState(KSPWheelState.DEPLOYED);
                        invulnerableTime += 5f;
                        controller.wheelRepairTimer = 0.0001f;
                        //TODO check for engineer?
                        break;
                    case KSPWheelWearType.ADVANCED:
                        changeWheelState(KSPWheelState.DEPLOYED);
                        invulnerableTime += 5f;
                        controller.wheelRepairTimer = 0.0001f;
                        //TODO resource use, check for engineer, ??
                        break;
                    default:
                        break;
                }
                changeWheelState(KSPWheelState.DEPLOYED);
                updateWheelMeshes();
                updateDisplayState();
            }
        }

        internal override void onUIControlsUpdated(bool show)
        {
            base.onUIControlsUpdated(show);
            Fields[nameof(maxSafeSpeed)].guiActive = Fields[nameof(maxSafeSpeed)].guiActiveEditor = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wearType != KSPWheelWearType.NONE;
            Fields[nameof(maxSafeLoad)].guiActive = Fields[nameof(maxSafeLoad)].guiActiveEditor = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wearType != KSPWheelWearType.NONE;
        }

        internal override void onScaleUpdated()
        {
            base.onScaleUpdated();
            maxSafeSpeed = controller.maxSpeed * controller.wheelMaxSpeedScalingFactor;
            maxSafeLoad = controller.maxLoadRating * controller.wheelMaxLoadScalingFactor;
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            if (!String.IsNullOrEmpty(wheelName))
            {
                wheelMeshes = part.transform.FindChildren(wheelName);
            }
            if (!String.IsNullOrEmpty(bustedWheelName))
            {
                bustedWheelMeshes = part.transform.FindChildren(bustedWheelName);
            }
            updateWheelMeshes();
            updateDisplayState();
            onScaleUpdated();
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
            float load = 0f;
            int len = controller.wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                load += controller.wheelData[i].wheel.springForce / 10f;
            }
            float maxLoad = maxSafeLoad;
            loadStress = load / maxLoad;
            if (load > maxLoad)
            {
                float overStress = (load / maxLoad) - 1f;
                stressTime += Time.fixedDeltaTime * overStress * HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelWearSettings>().stressDamageMultiplier * 0.25f;
            }

            float maxSpeed = maxSafeSpeed;
            float speed = Mathf.Abs( wheel.linearVelocity );
            if (speed > maxSpeed )
            {
                float overSpeedPercent = (speed / maxSpeed) - 1f;
                stressTime += Time.fixedDeltaTime * overSpeedPercent * HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelWearSettings>().speedDamageMultiplier;
            }

            if (stressTime >= 1.0f)
            {
                MonoBehaviour.print("Wheel broke from overstressing! load: " + load + " max: " + maxLoad+" speed: "+speed+" maxSpeed: "+maxSpeed);
                ScreenMessages.PostScreenMessage("<color=orange><b>[" + this.part + "]:</b> Broke from overstressing.</color>", 5f, ScreenMessageStyle.UPPER_LEFT);
                changeWheelState(KSPWheelState.BROKEN);
                stressTime = 0f;
                updateWheelMeshes();
                updateDisplayState();
            }
            if (speed < maxSpeed && load < maxLoad)
            {
                stressTime = Mathf.Max(0, stressTime - Time.fixedDeltaTime);
            }
        }

        private void wearUpdateAdvanced()
        {
            wearUpdateSimple();
        }

        private void updateWheelMeshes()
        {
            KSPWheelState wheelState = controller.wheelState;
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
            Events[nameof(repairWheel)].guiActiveUnfocused = wheelState == KSPWheelState.BROKEN;
            Fields[nameof(loadStress)].guiActive = wearType != KSPWheelWearType.NONE;
            Fields[nameof(stressTime)].guiActive = wearType != KSPWheelWearType.NONE;
            Fields[nameof(persistentWear)].guiActive = wearType == KSPWheelWearType.ADVANCED;
            Fields[nameof(displayStatus)].guiActive = wearType != KSPWheelWearType.NONE;
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

            switch (wearType)
            {
                case KSPWheelWearType.NONE:
                    break;
                case KSPWheelWearType.SIMPLE:
                    break;
                case KSPWheelWearType.ADVANCED:
                    displayStatus = displayStatus + " - " + (1 - persistentWear)+"%";
                    break;
                default:
                    break;
            }
        }

    }
}
