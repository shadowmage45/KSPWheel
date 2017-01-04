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
        
        [KSPField]
        public float maxMotorTorque = 0f;

        [KSPField(guiName = "Motor Torque", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 0.5f)]
        public float motorOutput = 100f;

        [KSPField]
        public float resourceAmount = 0f;

        [KSPField]
        public float maxRPM = 600f;

        [KSPField]
        public bool tankSteering = false;

        [KSPField(guiName = "Tank Steer Invert", guiActive = false, guiActiveEditor = false, isPersistant = true),
         UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool invertSteering = false;

        [KSPField(guiName = "Tank Steer Lock", guiActive = false, guiActiveEditor = false, isPersistant = true),
         UI_Toggle(enabledText = "Locked", disabledText = "Free", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool steeringLocked = false;

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
         UI_Toggle(enabledText = "Locked", disabledText = "Free", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public bool motorLocked;

        [KSPField(guiActive = true, guiName = "Motor EC Use", guiUnits = "ec/s")]
        public float guiResourceUse = 0f;

        [KSPField]
        public bool useTorqueCurve = true;

        [KSPField]
        public FloatCurve torqueCurve = new FloatCurve();

        //[KSPField(guiName = "Traction Control", guiActive = true, guiActiveEditor = true, isPersistant = true),
        // UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool useTractionControl = false;

        //[KSPField(guiName = "Traction Val", guiActive = true, guiActiveEditor = true),
        // UI_FloatRange(minValue = 0.05f, maxValue = 0.5f, stepIncrement = 0.01f)]
        public float tractionControl = 0.25f;

        private float fwdInput;
        public float torqueOutput;

        public void onMotorInvert(BaseField field, System.Object obj)
        {
            if (HighLogic.LoadedSceneIsEditor && part.symmetryCounterparts.Count==1)
            {
                part.symmetryCounterparts[0].GetComponent<KSPWheelMotor>().invertMotor = !invertMotor;
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(invertMotor)].uiControlEditor.onFieldChanged = onMotorInvert;
            if (torqueCurve.Curve.length == 0)
            {
                torqueCurve.Add(0, 1, 0, 0);
                torqueCurve.Add(1, 0, 0, 0);
            }
            if (HighLogic.LoadedSceneIsEditor && part.isClone)
            {
                invertMotor = !part.symmetryCounterparts[0].GetComponent<KSPWheelMotor>().invertMotor;
            }
            //TODO how to determine if is 'original' part or a symmetry part?
        }

        internal override void onUIControlsUpdated(bool show)
        {
            base.onUIControlsUpdated(show);

            Fields[nameof(motorOutput)].guiActive = Fields[nameof(motorOutput)].guiActiveEditor = show;
            Fields[nameof(invertMotor)].guiActive = Fields[nameof(invertMotor)].guiActiveEditor = show;
            Fields[nameof(motorLocked)].guiActive = Fields[nameof(motorLocked)].guiActiveEditor = show;

            Fields[nameof(invertSteering)].guiActive = Fields[nameof(invertSteering)].guiActiveEditor = tankSteering && show;
            Fields[nameof(steeringLocked)].guiActive = Fields[nameof(steeringLocked)].guiActiveEditor = tankSteering && show;            
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            float fI = part.vessel.ctrlState.wheelThrottle + part.vessel.ctrlState.wheelThrottleTrim;
            if (motorLocked) { fI = 0; }
            if (invertMotor) { fI = -fI; }

            fI *= (motorOutput * 0.01f);

            if (useTractionControl)
            {
                if (wheel.longitudinalSlip > tractionControl)
                {
                    fI = Mathf.Lerp(fwdInput, 0, Time.deltaTime);
                }
                else if (fI!=0)
                {
                    fI = Mathf.Lerp(fwdInput, fI, Time.deltaTime);
                }
            }

            float rpm = wheel.rpm;
            if (fI > 0 && wheel.rpm > maxRPM) { fI = 0; }
            else if (fI < 0 && wheel.rpm < -maxRPM) { fI = 0; }


            float mult = useTorqueCurve && maxRPM > 0 ? torqueCurve.Evaluate(Mathf.Abs(rpm) / maxRPM) : 1f;
            fI *= mult;

            if (tankSteering && !steeringLocked)
            {
                float rI = -(part.vessel.ctrlState.wheelSteer + part.vessel.ctrlState.wheelSteerTrim);
                if (invertSteering) { rI = -rI; }
                fI = fI + rI;
                if (fI > 1) { fI = 1; }
                if (fI < -1) { fI = -1; }
            }

            fI *= updateResourceDrain(Mathf.Abs(fI));
            
            fwdInput = fI;
            torqueOutput = wheel.motorTorque = maxMotorTorque * fwdInput * mult * controller.tweakScaleCorrector;
        }

        //TODO fix resource drain, it was causing the world to explode...
        private float updateResourceDrain(float input)
        {
            float percent = 1f;
            guiResourceUse = 0f;
            if (input > 0)
            {
                float drain = input * resourceAmount * Time.fixedDeltaTime;
                if (drain > 0)
                {
                    float used = part.RequestResource("ElectricCharge", drain);
                    percent = used / drain;
                    guiResourceUse = used / Time.fixedDeltaTime;
                }
            }
            return percent;
        }
    }
}
