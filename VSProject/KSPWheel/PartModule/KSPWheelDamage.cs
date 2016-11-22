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
    public class KSPWheelDamage : PartModule
    {

        [KSPField]
        public string wheelColliderName = "wheelCollider";

        [KSPField]
        public int indexInDuplicates = 0;

        [KSPField]
        public string wheelName = "wheel";

        [KSPField]
        public string bustedWheelName = "bustedWheel";

        [KSPField]
        public float impactTolerance = 100f;

        private Transform wheelColliderTransform;
        private Transform wheelMesh;
        private Transform bustedWheelMesh;
        private KSPWheelBase wheelBase;
        private KSPWheelCollider wheel;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Transform[] wcs = part.transform.FindChildren(wheelColliderName);
            wheelColliderTransform = wcs[indexInDuplicates];
            if (!String.IsNullOrEmpty(wheelName)) { wheelMesh = part.transform.FindRecursive(wheelName); }
            if (!String.IsNullOrEmpty(bustedWheelName)) { bustedWheelMesh = part.transform.FindRecursive(bustedWheelName); }
            //TODO start co-routine to wait for wheel to be populated, and then add an OnImpact callback
        }

        public void Start()
        {
            wheelBase = part.GetComponent<KSPWheelBase>();
            KSPWheelState wheelState = wheelBase.wheelState;
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

    }
}
