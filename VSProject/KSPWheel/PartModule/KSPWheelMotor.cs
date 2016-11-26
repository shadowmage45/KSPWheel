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
    public class KSPWheelMotor : KSPWheelSubmodule
    {
        
        [KSPField(guiName = "Motor Torque", guiActive = true, guiActiveEditor = true),
         UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 0.5f)]
        public float maxMotorTorque = 0f;

        [KSPField]
        public float resourceAmount = 0f;

        [KSPField]
        public float throttleResponse = 5f;

        [KSPField]
        public float maxRPM = 600f;

        [KSPField]
        public bool tankSteering = false;

        [KSPField(guiName = "Invert Steering", guiActive = false, guiActiveEditor = false, isPersistant = true),
         UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool invertSteering = false;

        /// <summary>
        /// If true, motor response will be inverted for this wheel.  Toggleable in editor and flight.  Persistent.
        /// </summary>
        [KSPField(guiName = "Invert Motor", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool invertMotor;

        /// <summary>
        /// If true, motor response will be inverted for this wheel.  Toggleable in editor and flight.  Persistent.
        /// </summary>
        [KSPField(guiName = "Motor Lock", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Locked", disabledText = "Free", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool motorLocked;

        [KSPField(guiActive = true, guiName = "Motor EC Use", guiUnits = "ec/s")]
        public float guiResourceUse = 0f;

        [KSPField]
        public bool useTorqueCurve = true;

        [KSPField]
        public FloatCurve torqueCurve = new FloatCurve();

        [KSPField(guiName = "Traction Control", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool useTractionControl = false;

        [KSPField(guiName = "Traction Val", guiActive = true, guiActiveEditor = true),
         UI_FloatRange(minValue = 0, maxValue = 1, stepIncrement = 0.025f)]
        public float tractionControl = 0.1f;

        private float fwdInput;
        public float torqueOutput;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(invertSteering)].guiActive = tankSteering;
            Fields[nameof(invertSteering)].guiActiveEditor = tankSteering;
            if (torqueCurve.Curve.length == 0)
            {
                torqueCurve.Add(0, 1, 0, 0);
                torqueCurve.Add(1, 0, 0, 0);
            }
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            float fI = part.vessel.ctrlState.wheelThrottle + part.vessel.ctrlState.wheelThrottleTrim;
            if (motorLocked) { fI = 0; }
            if (invertMotor) { fI = -fI; }
            if (tankSteering)
            {
                float rI = part.vessel.ctrlState.wheelSteer + part.vessel.ctrlState.wheelSteerTrim;
                if (invertSteering) { rI = -rI; }
                fI = fI + rI;
                if (fI > 1) { fI = 1; }
                if (fI < -1) { fI = -1; }
            }

            if (useTractionControl)
            {
                if (wheel.longitudinalSlip > tractionControl)
                {
                    fI = 0;
                }
            }

            if (throttleResponse > 0)
            {
                fI = Mathf.Lerp(fwdInput, fI, throttleResponse * Time.deltaTime);
            }

            float rpm = wheel.rpm;
            if (fI > 0 && wheel.rpm > maxRPM) { fI = 0; }
            else if (fI < 0 && wheel.rpm < -maxRPM) { fI = 0; }
            fwdInput = fI * updateResourceDrain(Mathf.Abs(fI));
            float mult = useTorqueCurve ? torqueCurve.Evaluate(Mathf.Abs(rpm) / maxRPM) : 1f;
            torqueOutput = wheel.motorTorque = maxMotorTorque * fwdInput * mult;
        }

        //TODO fix resource drain, it was causing the world to explode...
        private float updateResourceDrain(float input)
        {
            float percent = 1f;
            //if (input > 0 && resourceAmount > 0)
            //{
            //    float drain = maxMotorTorque * input * resourceAmount * TimeWarp.fixedDeltaTime;
            //    double d = part.RequestResource("ElectricCharge", drain);
            //    percent = (float)d / drain;
            //    guiResourceUse = (float)d / TimeWarp.fixedDeltaTime;
            //}
            return percent;
        }
    }
}
