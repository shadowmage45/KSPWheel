using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelSteering : KSPWheelSubmodule
    {
        /// <summary>
        /// Name of the transform that will be rotated for visual steering effect
        /// </summary>
        [KSPField]
        public string steeringName = "steering";
        
        /// <summary>
        /// Maximum deflection angle of the steering transform, measured from its default state (rotation = 0,0,0)
        /// </summary>
        [KSPField]
        public float maxSteeringAngle = 0f;

        /// <summary>
        /// Steering lerp response speed.  Higher values = faster response.
        /// </summary>
        [KSPField]
        public float steeringResponse = 0;

        /// <summary>
        /// If true the steering will be locked to zero and will not respond to steering input.
        /// </summary>
        [KSPField(guiName = "Steering Lock", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Locked", disabledText = "Free", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool steeringLocked;

        /// <summary>
        /// If true, steering will be inverted for this wheel.  Toggleable in editor and flight.  Persistent.
        /// </summary>
        [KSPField(guiName = "Invert Steering", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool invertSteering = false;


        [KSPField(guiName = "latFrict", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = -1, maxValue = 1, stepIncrement = 0.025f, suppressEditorShipModified = true)]
        public float steeringBias = 0f;
        
        /// <summary>
        /// The local axis of the steering transform to rotate around.  Defaults to 0, 1, 0 -- rotate around y+ axis, with z+ facing forward.
        /// </summary>
        [KSPField]
        public Vector3 steeringAxis = Vector3.up;

        [KSPField]
        public bool useSteeringCurve = false;

        [KSPField]
        public FloatCurve steeringCurve;

        private Transform steeringTransform;
        private Quaternion defaultRotation;
        private float rotInput;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            steeringTransform = part.transform.FindRecursive(steeringName);
            defaultRotation = steeringTransform.localRotation;
            if(steeringCurve== null)
            {
                steeringCurve = new FloatCurve();
                steeringCurve.Add(0, 1, 0, 0);
                steeringCurve.Add(1, 1, 0, 0);
            }
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            float fI = part.vessel.ctrlState.wheelThrottle + part.vessel.ctrlState.wheelThrottleTrim;
            float rI = part.vessel.ctrlState.wheelSteer + part.vessel.ctrlState.wheelSteerTrim;
            if (steeringLocked) { rI = 0; }
            if (invertSteering) { rI = -rI; }
            if (steeringResponse > 0)
            {
                rI = Mathf.Lerp(rotInput, rI, steeringResponse * Time.deltaTime);
            }
            rotInput = rI;
            if (useSteeringCurve)
            {
                float speed = wheel.wheelLocalVelocity.magnitude;
                float mult = steeringCurve.Evaluate(speed);
                rI *= mult;
            }
            wheel.steeringAngle = maxSteeringAngle * rotInput;
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || steeringTransform == null) { return; }
            if (wheel == null) { return; }
            steeringTransform.localRotation = defaultRotation;
            steeringTransform.Rotate(wheel.steeringAngle * steeringAxis, Space.Self);
        }

    }
}
