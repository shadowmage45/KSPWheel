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
        public float latSlipStart = 0.2f;

        [KSPField]
        public float latSlipPeak = 0.6f;

        [KSPField]
        public string motorEffect = String.Empty;

        [KSPField]
        public string runningEffect = String.Empty;

        [KSPField]
        public float runningRpmPeak = 200f;

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
                float rpm = Mathf.Abs(wheel.rpm);
                float power = rpm > runningRpmPeak ? 1 : rpm / runningRpmPeak;
                part.Effect(runningEffect, power);
            }

            if (!string.IsNullOrEmpty(motorEffect) && motor != null)
            {
                float rpm = Mathf.Abs(wheel.rpm);
                rpm *= motor.gearRatio;
                float power = rpm > motor.maxRPM ? 1 : rpm / motor.maxRPM;
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
                float div = 1 / (latSlipPeak - latSlipStart);
                float power = Mathf.Max(Mathf.Min((wheel.lateralSlip - latSlipStart) * div, 1), 0);
                part.Effect(latSlipEffect, power);
            }
        }

    }
}
