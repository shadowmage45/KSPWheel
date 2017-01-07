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
        public float impactTolerance = 75f;

        [KSPField]
        public float persistentWear = 0f;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Wheel Status: ")]
        public string displayStatus = "Operational";

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Load Stress"),
         UI_ProgressBar(minValue = 0, maxValue = 1, suppressEditorShipModified = true)]
        public float loadStress = 0f;

        [KSPField]
        public float maxOverloadTime = 2f;

        private float overloadTime = 0f;

        private float invulnerableTime = 0f;
        
        private Transform wheelMesh;
        private Transform bustedWheelMesh;
        
        [KSPEvent(guiName = "Repair Wheel/Gear", guiActive = false, guiActiveEditor = false, externalToEVAOnly = true, guiActiveUnfocused = true, guiActiveUncommand = true)]
        public void repairWheel()
        {
            //TODO check for engineer?
            KSPWheelWearType wearType = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wearType;
            switch (wearType)
            {
                case KSPWheelWearType.NONE:
                    break;
                case KSPWheelWearType.SIMPLE:
                    controller.wheelState = KSPWheelState.DEPLOYED;
                    invulnerableTime += 2f;
                    break;
                case KSPWheelWearType.ADVANCED:
                    controller.wheelState = KSPWheelState.DEPLOYED;
                    invulnerableTime += 2f;
                    //TODO resource use, check for engineer, ??
                    break;
                default:
                    break;
            }
            updateWheelMeshes();
            updateDisplayState();
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            if (!String.IsNullOrEmpty(wheelName)) { wheelMesh = part.transform.FindRecursive(wheelName); }
            if (!String.IsNullOrEmpty(bustedWheelName)) { bustedWheelMesh = part.transform.FindRecursive(bustedWheelName); }
            updateWheelMeshes();
            updateDisplayState();
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            int len = controller.wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                controller.wheelData[i].wheel.setImpactCallback(onWheelImpact);
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
            float load = 0f;
            int len = controller.wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                load += controller.wheelData[i].wheel.springForce / 10f;
            }
            //TODO what power does load scale with?
            float maxLoad = controller.maxLoadRating * controller.tweakScaleCorrector;
            if (load > maxLoad)
            {
                overloadTime += Time.fixedDeltaTime * (load - maxLoad) * 4;
                loadStress = 1f;
            }
            else
            {
                loadStress = load / maxLoad;
                overloadTime = Mathf.Max(0, overloadTime - Time.fixedDeltaTime);
            }
            if (overloadTime > maxOverloadTime)
            {
                ScreenMessages.PostScreenMessage("<color=orange><b>[" + this.part + "]:</b> Broke from overloading.</color>", 5f, ScreenMessageStyle.UPPER_LEFT);
                controller.wheelState = KSPWheelState.BROKEN;
                overloadTime = 0f;
                updateWheelMeshes();
                updateDisplayState();
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
                if (bustedWheelMesh != null)
                {
                    if (wheelMesh != null) { wheelMesh.gameObject.SetActive(false); }
                    bustedWheelMesh.gameObject.SetActive(true);
                }
            }
            else
            {
                if (wheelMesh != null) { wheelMesh.gameObject.SetActive(true); }
                if (bustedWheelMesh != null) { bustedWheelMesh.gameObject.SetActive(false); }
            }
        }

        private void updateDisplayState()
        {
            KSPWheelState wheelState = controller.wheelState;
            KSPWheelWearType wearType = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wearType;
            Events["repairWheel"].guiActive = wheelState == KSPWheelState.BROKEN;
            Fields[nameof(loadStress)].guiActive = wearType != KSPWheelWearType.NONE;
            Fields[nameof(persistentWear)].guiActive = wearType == KSPWheelWearType.ADVANCED;
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

        /// <summary>
        /// Called from the KSPWheelCollider on first ground contact<para/>
        /// The input Vector3 is the wheel-local impact velocity.  Relative impact speed can be derived from localImpactVelocity.magnitude
        /// </summary>
        /// <param name="localImpactVelocity"></param>
        public void onWheelImpact(Vector3 localImpactVelocity)
        {
            //TODO
            MonoBehaviour.print("Wheel impact, velocity: " + localImpactVelocity);
            KSPWheelWearType wearType = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wearType;
            switch (wearType)
            {
                case KSPWheelWearType.NONE:
                    //NOOP
                    break;
                case KSPWheelWearType.SIMPLE:
                    if (localImpactVelocity.sqrMagnitude > impactTolerance * impactTolerance)
                    {
                        ScreenMessages.PostScreenMessage("<color=orange><b>[" + this.part + "]:</b> Broke from impact.</color>", 5f, ScreenMessageStyle.UPPER_LEFT);
                        controller.wheelState = KSPWheelState.BROKEN;
                        updateDisplayState();
                        updateWheelMeshes();
                    }
                    break;
                case KSPWheelWearType.ADVANCED:
                    //TODO change this to a slightly more advanced method...
                    if (localImpactVelocity.sqrMagnitude > impactTolerance * impactTolerance)
                    {
                        ScreenMessages.PostScreenMessage("<color=orange><b>[" + this.part + "]:</b> Broke from impact.</color>", 5f, ScreenMessageStyle.UPPER_LEFT);
                        controller.wheelState = KSPWheelState.BROKEN;
                        updateDisplayState();
                        updateWheelMeshes();
                    }
                    break;
                default:
                    //NOOP
                    break;
            }
        }

    }
}
