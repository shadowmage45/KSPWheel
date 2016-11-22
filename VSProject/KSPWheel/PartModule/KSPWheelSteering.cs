using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelSteering : PartModule
    {

        [KSPField]
        public string steeringName = "steering";

        [KSPField]
        public string wheelColliderName = "wheelCollider";

        [KSPField]
        public int indexInDuplicates = 0;

        [KSPField]
        public float maxSteeringAngle = 0f;

        [KSPField]
        public float steeringResponse = 0;

        /// <summary>
        /// If true the steering will be locked to zero and will not respond to steering input.
        /// </summary>
        [KSPField(guiName = "Steering Lock", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Locked", disabledText = "Free", suppressEditorShipModified = true)]
        public bool steeringLocked;

        /// <summary>
        /// If true, steering will be inverted for this wheel.  Toggleable in editor and flight.  Persistent.
        /// </summary>
        [KSPField(guiName = "Invert Steering", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true)]
        public bool invertSteering = false;
        
        [KSPField]
        public Vector3 steeringAxis = Vector3.up;
        
        private Transform steeringTransform;
        private Transform wheelColliderTransform;
        private KSPWheelCollider wheel;
        private float rotInput;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Transform[] sus = part.transform.FindChildren(steeringName);
            Transform[] wcs = part.transform.FindChildren(wheelColliderName);
            steeringTransform = sus[indexInDuplicates];
            wheelColliderTransform = wcs[indexInDuplicates];
        }

        public void FixedUpdate()
        {
            if (wheel == null) { wheel = wheelColliderTransform.GetComponent<KSPWheelCollider>(); return; }
            float fI = part.vessel.ctrlState.wheelThrottle + part.vessel.ctrlState.wheelThrottleTrim;
            float rI = part.vessel.ctrlState.wheelSteer + part.vessel.ctrlState.wheelSteerTrim;
            if (steeringLocked) { rI = 0; }
            if (invertSteering) { rI = -rI; }            
            if (steeringResponse > 0)
            {
                rI = Mathf.Lerp(rotInput, rI, steeringResponse * Time.deltaTime);
            }
            rotInput = rI;
            wheel.steeringAngle = maxSteeringAngle * rotInput;
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || steeringTransform == null) { return; }
            if (wheel == null) { wheel = wheelColliderTransform.GetComponent<KSPWheelCollider>(); return; }
            steeringTransform.localRotation = Quaternion.Euler(steeringAxis * wheel.steeringAngle);
        }

    }
}
