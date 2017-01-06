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
        public float impactTolerance = 100f;
        
        private Transform wheelMesh;
        private Transform bustedWheelMesh;
        
        //TODO -- enable/disable for broken status
        [KSPEvent(guiName = "Repair Gear", guiActive = false, guiActiveEditor = false)]
        public void repairWheel()
        {

        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            if (!String.IsNullOrEmpty(wheelName)) { wheelMesh = part.transform.FindRecursive(wheelName); }
            if (!String.IsNullOrEmpty(bustedWheelName)) { bustedWheelMesh = part.transform.FindRecursive(bustedWheelName); }
            KSPWheelState wheelState = controller.wheelState;
            Events["repairWheel"].active = wheelState == KSPWheelState.BROKEN;
            if (wheelState == KSPWheelState.BROKEN)
            {
                if (wheelMesh != null) { wheelMesh.gameObject.SetActive(false); }
                if (bustedWheelMesh != null) { bustedWheelMesh.gameObject.SetActive(true); }
            }
            else
            {
                if (wheelMesh != null) { wheelMesh.gameObject.SetActive(true); }
                if (bustedWheelMesh != null) { bustedWheelMesh.gameObject.SetActive(false); }
            }
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            wheel.setImpactCallback(onWheelImpact);
        }

        internal override void postWheelPhysicsUpdate()
        {
            base.postWheelPhysicsUpdate();
            KSPWheelWearType wearType = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wearType;
            switch (wearType)
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

        }

        private void wearUpdateAdvanced()
        {

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
            if (localImpactVelocity.sqrMagnitude > impactTolerance*impactTolerance)
            {
                MonoBehaviour.print("EXPLOSIONS!!! -- Impact tolerance exceeded, should destroy part!");
            }
        }

    }
}
