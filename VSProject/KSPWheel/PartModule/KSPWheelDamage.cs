using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    /// <summary>
    /// Replacement for stock wheel motor module.<para/>
    /// Manages wheel motor input and resource use.
    /// TODO:
    /// Traction control / anti-slip.
    /// Torque curve vs rpm.
    /// </summary>
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

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!String.IsNullOrEmpty(wheelName)) { wheelMesh = part.transform.FindRecursive(wheelName); }
            if (!String.IsNullOrEmpty(bustedWheelName)) { bustedWheelMesh = part.transform.FindRecursive(bustedWheelName); }
            //TODO start co-routine to wait for wheel to be populated, and then add an OnImpact callback
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
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

        /// <summary>
        /// Called from the KSPWheelCollider on first ground contact<para/>
        /// The input Vector3 is the wheel-local impact velocity.  Relative impact speed can be derived from localImpactVelocity.magnitude
        /// </summary>
        /// <param name="localImpactVelocity"></param>
        public void onWheelImpact(Vector3 localImpactVelocity)
        {
            //TODO
        }

    }
}
