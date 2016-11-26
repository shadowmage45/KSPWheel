using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelTracks : KSPWheelSubmodule
    {

        public bool manageMotor = true;
        public bool manageBrakes = true;
        public bool averageRPM = true;

        private KSPWheelMotor motor;
        private KSPWheelBrakes brakes;

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            motor = part.GetComponent<KSPWheelMotor>();
            brakes = part.GetComponent<KSPWheelBrakes>();
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            int len = controller.wheelData.Length;
            float mt = motor.torqueOutput;
            float bt = brakes.torqueOutput;
            float rpmTotal = 0;
            for (int i = 0; i < len; i++)
            {
                rpmTotal += controller.wheelData[i].wheel.rpm * controller.wheelData[i].wheel.radius;
                controller.wheelData[i].wheel.motorTorque = mt;
                controller.wheelData[i].wheel.brakeTorque = bt;
            }
            if (averageRPM)
            {
                float rpmAvg = rpmTotal / len;
                for (int i = 0; i < len; i++)
                {
                    controller.wheelData[i].wheel.rpm = rpmAvg / controller.wheelData[i].wheel.radius;
                }
            }
        }
    }
}
