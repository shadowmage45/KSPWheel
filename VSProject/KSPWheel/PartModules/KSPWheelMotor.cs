using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    /// <summary>
    /// Replacement for stock wheel motor module.<para/>
    /// Manages wheel motor input and resource use.
    /// </summary>
    public class KSPWheelMotor : KSPWheelSubmodule
    {
        
        /// <summary>
        /// Peak Motor power, in kw (e.g. kn).  Used to determine EC/s
        /// </summary>
        [KSPField]
        public float motorPower = 1f;

        /// <summary>
        /// Motor stall torque; e.g. motor torque output at zero rpm
        /// </summary>
        [KSPField]
        public float maxMotorTorque = 10f;

        /// <summary>
        /// Max rpm of the motor at shaft.  Used with motor stall torque to determine output curve and power use.
        /// </summary>
        [KSPField]
        public float maxRPM = 2500f;

        [KSPField(guiName = "Motor Output", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 0.5f)]
        public float motorOutput = 100f;

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

        [KSPField]
        public bool tankSteering = false;

        [KSPField(guiName = "Tank Steer Invert", guiActive = false, guiActiveEditor = false, isPersistant = true),
         UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool invertSteering = false;

        [KSPField(guiName = "Tank Steer Lock", guiActive = false, guiActiveEditor = false, isPersistant = true),
         UI_Toggle(enabledText = "Locked", disabledText = "Free", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool steeringLocked = false;

        [KSPField(guiName = "Gear Ratio (x:1)", guiActive = true, guiActiveEditor = true),
         UI_FloatEdit(suppressEditorShipModified = true, minValue = 0.25f, maxValue = 20f, incrementSlide = 0.05f, incrementLarge = 1f, incrementSmall = 0.25f)]
        public float gearRatio = 4f;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Max Drive Speed", guiUnits = "m/s")]
        public float maxDrivenSpeed = 0f;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Max Power Output", guiUnits = "n")]
        public float maxPowerOutput = 0f;

        [KSPField(guiActive = true, guiName = "Motor EC Use", guiUnits = "ec/s")]
        public float guiResourceUse = 0f;

        [KSPField]
        public bool useTorqueCurve = true;

        [KSPField]
        public FloatCurve torqueCurve = new FloatCurve();

        private float fwdInput;
        public float torqueOutput;

        public void onMotorInvert(BaseField field, System.Object obj)
        {
            if (HighLogic.LoadedSceneIsEditor && part.symmetryCounterparts.Count==1)
            {
                part.symmetryCounterparts[0].GetComponent<KSPWheelMotor>().invertMotor = !invertMotor;
            }
        }

        public void onGearUpdated(BaseField field, System.Object ob)
        {
            float scale = 1f;
            float mass = wheelData.scaledMass(scale);
            float radius = wheelData.scaledRadius(scale);
            float torque = Mathf.Pow(scale, 3) * maxMotorTorque;
            float force = torque / radius;
            maxPowerOutput = force;

            float rpm = maxRPM / gearRatio;
            float rps = rpm / 60;
            float circ = radius * 2 * Mathf.PI;
            float ms = rps * circ;
            maxDrivenSpeed = ms;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(invertMotor)].uiControlEditor.onFieldChanged = onMotorInvert;
            Fields[nameof(gearRatio)].uiControlEditor.onFieldChanged = Fields[nameof(gearRatio)].uiControlFlight.onFieldChanged = onGearUpdated;
            if (torqueCurve.Curve.length == 0)
            {
                torqueCurve.Add(0, 1, 0, 0);
                torqueCurve.Add(1, 0, 0, 0);
            }
            if (HighLogic.LoadedSceneIsEditor && part.isClone)
            {
                invertMotor = !part.symmetryCounterparts[0].GetComponent<KSPWheelMotor>().invertMotor;
            }
        }

        internal override void onUIControlsUpdated(bool show)
        {
            base.onUIControlsUpdated(show);

            Fields[nameof(motorOutput)].guiActive = Fields[nameof(motorOutput)].guiActiveEditor = show;
            Fields[nameof(invertMotor)].guiActive = Fields[nameof(invertMotor)].guiActiveEditor = show;
            Fields[nameof(motorLocked)].guiActive = Fields[nameof(motorLocked)].guiActiveEditor = show;

            Fields[nameof(invertSteering)].guiActive = Fields[nameof(invertSteering)].guiActiveEditor = tankSteering && show;
            Fields[nameof(steeringLocked)].guiActive = Fields[nameof(steeringLocked)].guiActiveEditor = tankSteering && show;

            onGearUpdated(null, null);
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            updateMotor();
        }

        protected virtual void updateMotor()
        {
            float fI = part.vessel.ctrlState.wheelThrottle + part.vessel.ctrlState.wheelThrottleTrim;
            if (motorLocked) { fI = 0; }
            if (invertMotor) { fI = -fI; }
            float rawOutput = calcRawTorque(fI);
            float powerUse = calcPower(rawOutput);
            rawOutput *= updateResourceDrain(powerUse);
            float gearedOutput = rawOutput * gearRatio;
            wheel.motorTorque = gearedOutput;
        }

        protected float calcRawTorque(float fI)
        {
            float motorRPM = Mathf.Abs(wheel.rpm * gearRatio);
            if (motorRPM > maxRPM) { motorRPM = maxRPM; }
            float curveOut = torqueCurve.Evaluate(motorRPM / maxRPM);
            float outputTorque = curveOut * maxMotorTorque * fI;
            return outputTorque;
        }

        protected float calcPower(float rawTorque)
        {
            float motorRPM = Mathf.Abs(wheel.rpm * gearRatio);
            return Mathf.Abs(rawTorque) * motorRPM / 9.5488f;
        }

        protected float updateResourceDrain(float ecs)
        {
            float percent = 1f;
            guiResourceUse = 0f;
            if (ecs > 0)
            {
                float drain = ecs * Time.fixedDeltaTime;
                if (drain > 0)
                {
                    float used = part.RequestResource("ElectricCharge", drain);
                    percent = used / drain;
                    guiResourceUse = percent * ecs;
                }
            }
            return percent;
        }
    }
}
