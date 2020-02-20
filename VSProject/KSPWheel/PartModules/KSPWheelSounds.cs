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
        public float runningEffectMaxSpeed;

        [KSPField]
        public string motorEffect = String.Empty;

        [KSPField]
        public string runningEffect = String.Empty;

        private KSPWheelMotor motor;

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            motor = part.GetComponent<KSPWheelMotor>();
            if (runningEffectMaxSpeed <= 0)
            {
                KSPWheelDamage dmg = controller.subModules.Find(m => m.wheelIndex == this.wheelIndex && m is KSPWheelDamage) as KSPWheelDamage;
                if (dmg != null && dmg.maxSpeed > 0)
                {
                    runningEffectMaxSpeed = dmg.maxSpeed;
                }
                else
                {
                    runningEffectMaxSpeed = controller.GetDefaultMaxSpeed(400);
                }
            }
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            float runPower = 0f;
            float motorPower = 0f;
            float longSlipPower = 0f;
            float latSlipPower = 0f;
            if (controller.wheelState == KSPWheelState.DEPLOYED)
            {
                if (!string.IsNullOrEmpty(runningEffect))
                {
                    float velocity = Mathf.Abs(wheel.wheelLocalVelocity.z);
                    float max = controller.GetScaledMaxSpeed(runningEffectMaxSpeed);
                    runPower = velocity > max ? 1 : velocity / max;
                }

                if (!string.IsNullOrEmpty(motorEffect) && motor != null)
                {
                    motorPower = Mathf.Abs(motor.torqueOut) / motor.gearRatio / controller.GetScaledMotorTorque(motor.maxMotorTorque);
                }

                if (!string.IsNullOrEmpty(longSlipEffect))
                {
                    float range = longSlipPeak - longSlipStart;
                    float val = (wheel.longitudinalSlip - longSlipStart) / range;
                    val = val < 0 ? 0 : val > 1 ? 1 : val;
                    longSlipPower = val;
                }

                if (!string.IsNullOrEmpty(latSlipEffect))
                {
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
                        float gFactor = (float)vessel.gravityForPos.magnitude / 9.81f;
                        latSlipPower = slipFactor * velocityFactor * gFactor;
                    }
                }
            }

            if (!string.IsNullOrEmpty(runningEffect))
            {
                part.Effect(runningEffect, runPower);
            }
            if (!string.IsNullOrEmpty(motorEffect) && motor != null)
            {
                part.Effect(motorEffect, motorPower);
            }
            if (!string.IsNullOrEmpty(longSlipEffect))
            {
                part.Effect(longSlipEffect, longSlipPower);
            }
            if (!string.IsNullOrEmpty(latSlipEffect))
            {
                part.Effect(latSlipEffect, latSlipPower);
            }
        }

    }
}
