using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelSounds : KSPWheelSubmodule
    {

        [KSPField]
        public string longSlipEffect = String.Empty;

        [KSPField]
        public float longSlipStart = 0.2f;

        [KSPField]
        public float longSlipPeak = 0.6f;

        [KSPField]
        public string latSlipEffect = String.Empty;

        [KSPField]
        public float latSlipStart = 0.15f;

        [KSPField]
        public float latSlipPeak = 0.5f;

        [KSPField]
        public float latSlipStartVelocity = 0.075f;

        [KSPField]
        public float latSlipPeakVelocity = 2f;

        [KSPField]
        public string motorEffect = String.Empty;

        [KSPField]
        public string runningEffect = String.Empty;

        private KSPWheelMotor motor;

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            motor = part.GetComponent<KSPWheelMotor>();
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();

            if (!string.IsNullOrEmpty(runningEffect))
            {
                float velocity = Mathf.Abs(wheel.wheelLocalVelocity.z);
                float max = controller.maxSpeed * controller.wheelMaxSpeedScalingFactor;
                float power = velocity > max ? 1 : velocity / max;
                part.Effect(runningEffect, power);
            }

            if (!string.IsNullOrEmpty(motorEffect) && motor != null)
            {
                float power = Mathf.Abs(wheel.motorTorque) / motor.gearRatio / motor.maxMotorTorque;
                part.Effect(motorEffect, power);
            }

            if (!string.IsNullOrEmpty(longSlipEffect))
            {
                float range = longSlipPeak - longSlipStart;
                float val = (wheel.longitudinalSlip - longSlipStart) / range;
                val = val < 0 ? 0 : val > 1 ? 1 : val;
                part.Effect(longSlipEffect, val);
            }

            if (!string.IsNullOrEmpty(latSlipEffect))
            {
                float power = 0;
                float velocity = Mathf.Abs(wheel.wheelLocalVelocity.x);
                float slip = wheel.lateralSlip;
                if (velocity > latSlipStartVelocity && slip > latSlipStart)
                {
                    float div = 1 / (latSlipPeak - latSlipStart);//range
                    float slipFactor = (slip - latSlipStart) * div;
                    slipFactor = Mathf.Clamp01(slipFactor);
                    div = 1 / (latSlipPeakVelocity - latSlipStartVelocity);
                    float velocityFactor = (velocity - latSlipStartVelocity) * div;
                    velocityFactor = Mathf.Clamp01(velocityFactor);
                    power = slipFactor * velocityFactor;
                }
                part.Effect(latSlipEffect, power);
            }
        }

    }
}
