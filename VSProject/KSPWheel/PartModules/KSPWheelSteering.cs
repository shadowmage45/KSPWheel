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
        /// If true the steering will be locked to zero and will not respond to steering input.
        /// </summary>
        [KSPField(guiName = "Steering Lock", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Locked", disabledText = "Free", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public bool steeringLocked;

        /// <summary>
        /// If true, steering will be inverted for this wheel.  Toggleable in editor and flight.  Persistent.
        /// </summary>
        [KSPField(guiName = "Invert Steering", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public bool invertSteering = false;

        [KSPField(guiName = "Steering Limit", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0, maxValue = 1, stepIncrement = 0.01f, suppressEditorShipModified = true)]
        public float steeringLimit = 1f;

        [KSPField(guiName = "Steering Bias", guiActive = true, guiActiveEditor = true, isPersistant = true),
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
        public FloatCurve steeringCurve = new FloatCurve();

        private Transform steeringTransform;
        private Quaternion defaultRotation;
        private float rotInput;

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            steeringTransform = part.transform.FindRecursive(steeringName);
            defaultRotation = steeringTransform.localRotation;
            if (steeringCurve == null || steeringCurve.Curve.length == 0)
            {
                steeringCurve = new FloatCurve();
                steeringCurve.Add(0, 1, 0, 0);
                steeringCurve.Add(10, 1, 0, 0);
                steeringCurve.Add(30, 0.5f, 0, 0);
            }
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            float rI = -(part.vessel.ctrlState.wheelSteer + part.vessel.ctrlState.wheelSteerTrim);
            if (steeringLocked) { rI = 0; }
            if (invertSteering) { rI = -rI; }
            float bias = steeringBias;
            if (rI < 0)
            {
                rI = rI * (1 - bias);
            }
            if (rI > 0)
            {
                rI = rI * (1 + bias);
            }
            if (rI > 1) { rI = 1; }
            if (rI < -1) { rI = -1; }
            if (useSteeringCurve)
            {
                float speed = wheel.wheelLocalVelocity.magnitude;
                float mult = steeringCurve.Evaluate(speed);
                rI *= mult;
            }
            rotInput = rI;
            wheel.steeringAngle = maxSteeringAngle * rotInput * steeringLimit;
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || steeringTransform == null || wheel==null) { return; }
            steeringTransform.localRotation = defaultRotation;
            if (controller.wheelState == KSPWheelState.DEPLOYED)
            {
                steeringTransform.Rotate(wheel.steeringAngle * steeringAxis, Space.Self);
            }
        }

    }
}
