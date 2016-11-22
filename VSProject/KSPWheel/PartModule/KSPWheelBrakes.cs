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
    public class KSPWheelBrakes : PartModule
    {

        [KSPField]
        public string wheelColliderName = "wheelCollider";

        [KSPField]
        public int indexInDuplicates = 0;

        [KSPField]
        public float maxBrakeTorque = 0f;

        [KSPField]
        public float brakeResponse = 0f;
        
        [KSPField]
        public bool brakesLocked = false;

        private Transform wheelColliderTransform;
        private KSPWheelCollider wheel;
        private float brakeInput;
        private ModuleStatusLight statusLightModule;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Transform[] wcs = part.transform.FindChildren(wheelColliderName);
            wheelColliderTransform = wcs[indexInDuplicates];
        }

        public void Start()
        {
            statusLightModule = part.GetComponent<ModuleStatusLight>();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) { return; }
            if (wheel == null) { wheel = wheelColliderTransform.GetComponent<KSPWheelCollider>(); return; }
            
            float bI = brakesLocked ? 1 : part.vessel.ActionGroups[KSPActionGroup.Brakes] ? 1 : 0;
            if (!brakesLocked && brakeResponse > 0)
            {
                bI = Mathf.Lerp(brakeInput, bI, brakeResponse * Time.deltaTime);
            }
            
            brakeInput = bI;
            wheel.brakeTorque = maxBrakeTorque * brakeInput;

            if (statusLightModule != null)
            {
                statusLightModule.SetStatus(brakeInput != 0);
            }
        }

    }
}
