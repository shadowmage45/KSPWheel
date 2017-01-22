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
        public float motorEfficiency = 0.85f;

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

        [KSPField]
        public float motorPower = 1f;

        [KSPField(guiName = "Motor Limit", guiActive = true, guiActiveEditor = true, isPersistant = true),
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

        [KSPField(guiName = "Half-Track", guiActive = false, guiActiveEditor = false, isPersistant = true),
         UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool halfTrackSteering = false;

        [KSPField(guiName = "Gear Ratio (x:1)", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatEdit(suppressEditorShipModified = true, minValue = 0.25f, maxValue = 20f, incrementSlide = 0.05f, incrementLarge = 1f, incrementSmall = 0.25f, sigFigs = 2)]
        public float gearRatio = 4f;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Max Drive Speed", guiUnits = "m/s")]
        public float maxDrivenSpeed = 0f;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Peak Power Output", guiUnits = "kN")]
        public float maxPowerOutput = 0f;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Torque To Wheel", guiUnits = "kN/M")]
        public float torqueOut = 0f;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Pre-Slip Power", guiUnits = "kN")]
        public float powerOut = 0f;

        [KSPField(guiActive = true, guiName = "Motor EC Use", guiUnits = "ec/s")]
        public float guiResourceUse = 0f;

        [KSPField]
        public bool useTorqueCurve = true;

        [KSPField]
        public FloatCurve torqueCurve = new FloatCurve();

        private float powerScalar = 1f;
        private float torqueScalar = 1f;
        private float rpmScalar = 1f;
        public float torqueOutput;

        public void onMotorInvert(BaseField field, System.Object obj)
        {
            if (HighLogic.LoadedSceneIsEditor && part.symmetryCounterparts.Count == 1)
            {
                part.symmetryCounterparts[0].GetComponent<KSPWheelMotor>().invertMotor = !invertMotor;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
                {
                    m.invertMotor = invertMotor;
                });
            }
        }

        public void onGearUpdated(BaseField field, System.Object ob)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.gearRatio = gearRatio;

                float maxRPM = m.maxRPM * m.rpmScalar;
                float scale = m.part.rescaleFactor * m.controller.scale;
                float radius = m.wheelData.scaledRadius(scale);
                float torque = m.torqueScalar * m.maxMotorTorque;
                float force = torque * m.gearRatio / radius;
                m.maxPowerOutput = force;

                float rpm = maxRPM / m.gearRatio;
                float rps = rpm / 60;
                float circ = radius * 2 * Mathf.PI;
                float ms = rps * circ;
                m.maxDrivenSpeed = ms;
                m.updateUIFloatEditControl(nameof(m.gearRatio), m.gearRatio);
            });
        }

        private void onMotorLock(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.motorLocked = motorLocked;
            });
        }

        private void onSteeringLock(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.steeringLocked = steeringLocked;
            });
        }

        private void onSteeringInvert(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.invertSteering = invertSteering;
            });
        }

        private void onMotorLimitUpdated(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.motorOutput = motorOutput;
            });
        }

        private void onHalftrackToggle(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.halfTrackSteering = halfTrackSteering;
            });
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(invertMotor)].uiControlEditor.onFieldChanged = Fields[nameof(invertMotor)].uiControlFlight.onFieldChanged = onMotorInvert;
            Fields[nameof(gearRatio)].uiControlEditor.onFieldChanged = Fields[nameof(gearRatio)].uiControlFlight.onFieldChanged = onGearUpdated;
            Fields[nameof(motorLocked)].uiControlEditor.onFieldChanged = Fields[nameof(motorLocked)].uiControlFlight.onFieldChanged = onMotorLock;
            Fields[nameof(steeringLocked)].uiControlEditor.onFieldChanged = Fields[nameof(steeringLocked)].uiControlFlight.onFieldChanged = onSteeringLock;
            Fields[nameof(invertSteering)].uiControlEditor.onFieldChanged = Fields[nameof(invertSteering)].uiControlFlight.onFieldChanged = onSteeringInvert;
            Fields[nameof(motorOutput)].uiControlEditor.onFieldChanged = Fields[nameof(motorOutput)].uiControlFlight.onFieldChanged = onMotorLimitUpdated;
            Fields[nameof(halfTrackSteering)].uiControlEditor.onFieldChanged = Fields[nameof(halfTrackSteering)].uiControlFlight.onFieldChanged = onHalftrackToggle;
            if (torqueCurve.Curve.length == 0)
            {
                torqueCurve.Add(0, 1, 0, 0);
                torqueCurve.Add(1, 0, 0, 0);
            }
            if (HighLogic.LoadedSceneIsEditor && part.isClone && part.symmetryCounterparts!=null && part.symmetryCounterparts.Count>0)
            {
                invertMotor = !part.symmetryCounterparts[0].GetComponent<KSPWheelMotor>().invertMotor;
            }
            updateScaleValues();
        }

        public override string GetInfo()
        {
            String val = "Motor\n";
            val = val + "Max RPM: " + maxRPM + "\n";
            val = val + "Torque : " + maxMotorTorque + "\n";
            val = val + "Max EC : " + motorPower + "\n";
            return val;
        }

        internal override void onScaleUpdated()
        {
            base.onScaleUpdated();
            updateScaleValues();
        }

        internal override void onUIControlsUpdated(bool show)
        {
            base.onUIControlsUpdated(show);

            Fields[nameof(motorOutput)].guiActive = Fields[nameof(motorOutput)].guiActiveEditor = show;
            Fields[nameof(invertMotor)].guiActive = Fields[nameof(invertMotor)].guiActiveEditor = show;
            Fields[nameof(motorLocked)].guiActive = Fields[nameof(motorLocked)].guiActiveEditor = show;

            Fields[nameof(invertSteering)].guiActive = Fields[nameof(invertSteering)].guiActiveEditor = tankSteering && show;
            Fields[nameof(steeringLocked)].guiActive = Fields[nameof(steeringLocked)].guiActiveEditor = tankSteering && show;
            Fields[nameof(halfTrackSteering)].guiActive = Fields[nameof(halfTrackSteering)].guiActiveEditor = tankSteering && show;

            Fields[nameof(gearRatio)].guiActive = Fields[nameof(gearRatio)].guiActiveEditor = show && HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().manualGearing;

            onGearUpdated(null, null);
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            updateMotor();
            powerOut = Mathf.Abs(wheel.motorTorque) / wheel.radius;
        }

        protected virtual void updateScaleValues()
        {
            torqueScalar = Mathf.Pow(controller.scale, HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelScaleSettings>().motorTorqueScalingPower);
            rpmScalar = Mathf.Pow(controller.scale, HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelScaleSettings>().motorMaxRPMScalingPower);
            powerScalar = Mathf.Pow(controller.scale, HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelScaleSettings>().motorPowerScalingPower);
        }

        protected virtual void updateMotor()
        {
            float fI = part.vessel.ctrlState.wheelThrottle + part.vessel.ctrlState.wheelThrottleTrim;
            if (motorLocked) { fI = 0; }
            if (invertMotor) { fI = -fI; }
            if (tankSteering && !steeringLocked && !motorLocked)
            {
                float rI = -(part.vessel.ctrlState.wheelSteer + part.vessel.ctrlState.wheelSteerTrim);
                if (invertSteering) { rI = -rI; }
                if (halfTrackSteering)
                {
                    bool spinningBackwards = false;
                    if ((fI < 0 && !invertMotor) || (fI > 0 && invertMotor) || spinningBackwards)
                    {
                        rI = -rI;
                    }
                }
                fI += rI;
            }
            fI = Mathf.Clamp(fI, -1, 1);
            float motorRPM = wheel.rpm * gearRatio;
            //integrateMotorEuler(fI, motorRPM);
            integrateMotorEulerSub(fI, motorRPM, 5);
        }

        protected void integrateMotorEuler(float fI, float motorRPM)
        {
            motorRPM = Mathf.Abs(motorRPM);
            float rawOutput = calcRawTorque(fI, motorRPM);
            float powerUse = calcECUse(Mathf.Abs(fI), motorRPM);
            rawOutput *= updateResourceDrain(powerUse);
            float gearedOutput = rawOutput * gearRatio;
            wheel.motorTorque = gearedOutput;
            torqueOutput = torqueOut = wheel.motorTorque;
        }

        /// <summary>
        /// Quick and semi-hacky sub-step integration for motor rpm/acceleration.
        /// Simulates accelerating the wheel with the motor torque (which is what happens in the wheel code currently)
        /// This -helps- to limit single-tick torques driving wheels past safe RPM values
        /// TODO -- unknown if the EC/s integration works properly....
        /// </summary>
        /// <param name="fI"></param>
        /// <param name="motorRPM"></param>
        /// <param name="substeps"></param>
        protected void integrateMotorEulerSub(float fI, float motorRPM, int substeps)
        {
            float p = 1.0f / substeps;
            float dt = Time.fixedDeltaTime * p;
            float ecs = 0f;
            float t = 0f;
            float rpm = motorRPM;
            for (int i = 0; i < substeps; i++)
            {
                t += p * calcRawTorque(fI, rpm);
                ecs += p * calcECUse(fI, rpm);
                rpm = wheelRPMIntegration(rpm, wheel.mass, t, dt);
            }
            t *= updateResourceDrain(ecs);
            t *= gearRatio;
            wheel.motorTorque = t;
            torqueOutput = torqueOut = t;
        }

        protected float calcRawTorque(float fI, float motorRPM)
        {
            motorRPM = Mathf.Abs(motorRPM);
            float maxRPM = this.maxRPM * rpmScalar;
            if (motorRPM > maxRPM) { motorRPM = maxRPM; }
            float curveOut = torqueCurve.Evaluate(motorRPM / maxRPM);
            float outputTorque = curveOut * maxMotorTorque * fI * torqueScalar;
            return outputTorque;
        }

        protected float calcECUse(float fI, float motorRPM)
        {
            motorRPM = Mathf.Abs(motorRPM);
            float maxRPM = this.maxRPM * rpmScalar;
            if (motorRPM > maxRPM) { motorRPM = maxRPM; }
            float percent = 1 - ( motorRPM / maxRPM );
            float totalPower = motorPower * powerScalar * Mathf.Abs(fI);
            float minPower = 0.05f * totalPower;
            float diff = totalPower - minPower;
            return minPower + percent * diff;
        }

        private static float rpmToRad = 0.104719755f;
        private static float radToRPM = 1 / 0.104719755f;

        protected float wheelRPMIntegration(float rpm, float wm, float t, float dt)
        {
            float outRPM = rpm;
            float wheelRPM = rpm / gearRatio;
            float wWheel = rpm * rpmToRad;
            float wAccel = t / wm * dt;
            wWheel += wAccel;
            return outRPM = wWheel * radToRPM * gearRatio;
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
                    if (used != drain)
                    {
                        percent = used / drain;
                    }
                    guiResourceUse = percent * ecs;
                }
            }
            return percent;
        }

        ///// <summary>
        ///// RK4 integration for motor torque, to prevent wheel from spinning excessively
        ///// </summary>
        ///// <param name="fI"></param>
        ///// <param name="torque"></param>
        ///// <param name="ec"></param>
        //protected void integrateMotor(float fI, float rpm, float wm, out float torque, out float ec)
        //{
        //    //initial state input
        //    float dt = 0f;
        //    float t = 0f;
        //    //derivative outputs
        //    float ec1, ec2, ec3, ec4;
        //    float rpm1, rpm2, rpm3, rpm4;
        //    float t1, t2, t3, t4;
        //    //final outputs
        //    float ot;
        //    //float or;//not needed
        //    float oe;
        //    //derivative calcs
        //    motorDerivative(fI, rpm, wm, t, 0f, 0, 0, out t1, out rpm1, out ec1);
        //    motorDerivative(fI, rpm, wm, t, dt * 0.5f, rpm1, t1, out t2, out rpm2, out ec2);
        //    motorDerivative(fI, rpm, wm, t, dt * 0.5f, rpm2, t2, out t3, out rpm3, out ec3);
        //    motorDerivative(fI, rpm, wm, t, dt, rpm3, t3, out t4, out rpm4, out ec4);
        //    //derivative integration
        //    ot = 1.0f / 6.0f * (t1 + 2.0f * (t2 + t3) + t4);
        //    //or = 1.0f / 6.0f * (rpm1 + 2.0f * (rpm2 + rpm3) + rpm4);
        //    oe = 1.0f / 6.0f * (ec1 + 2.0f * (ec2 + ec3) + ec4);
        //    torque = ot;
        //    ec = oe;
        //}

        //protected void motorDerivative(float fI, float rpm, float wm, float t, float time, float dRpm, float dt, out float outTorque, out float outRpm, out float outECUse)
        //{
        //    outECUse = calcECUse(fI, dRpm);
        //    outTorque = 0f;            
        //    outRpm = 0f;
        //    //TODO
        //}

    }
}
