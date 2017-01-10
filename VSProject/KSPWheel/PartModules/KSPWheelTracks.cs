using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelTracks : KSPWheelSubmodule
    {

        [KSPField]
        public float trackLength = 10f;

        [KSPField]
        public int trackDir = 1;

        [KSPField]
        public bool manageMotor = true;

        [KSPField]
        public bool manageBrakes = true;

        [KSPField]
        public bool averageRPM = true;

        private KSPWheelMotor motor;
        private KSPWheelBrakes brakes;
        private float averagedRPM = 0f;
        private SkinnedMeshRenderer smr;
        private Vector2 offset = Vector2.zero;
        private Material mat;

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            motor = part.GetComponent<KSPWheelMotor>();
            brakes = part.GetComponent<KSPWheelBrakes>();
            smr = part.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null)
            {
                mat = smr.material;
                if (mat != null)
                {
                    Vector2 scaling = mat.mainTextureScale;
                    scaling.x *= trackDir;
                    mat.SetTextureScale("_MainTex", scaling);
                    mat.SetTextureScale("_BumpMap", scaling);
                }
            }
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (mat != null)
            {
                float offsetAmount = (((-averagedRPM * 2f * Mathf.PI) / 60f) * Time.deltaTime * trackDir) / trackLength;
                offset.x += offsetAmount;
                mat.SetTextureOffset("_MainTex", offset);
                mat.SetTextureOffset("_BumpMap", offset);
            }
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            int len = controller.wheelData.Length;
            float mt = manageMotor && motor==null? 0 : motor.torqueOutput / len;
            float bt = manageBrakes && brakes==null? 0 : brakes.torqueOutput;
            float rpmTotal = 0;
            for (int i = 0; i < len; i++)
            {
                rpmTotal += controller.wheelData[i].wheel.rpm;// * controller.wheelData[i].wheel.radius;
                if (manageMotor)
                {
                    controller.wheelData[i].wheel.motorTorque = mt;
                }
                if (manageBrakes)
                {
                    controller.wheelData[i].wheel.brakeTorque = bt;
                }

            }
            if (averageRPM)
            {
                averagedRPM = rpmTotal / len;
                for (int i = 0; i < len; i++)
                {
                    controller.wheelData[i].wheel.rpm = averagedRPM;// / controller.wheelData[i].wheel.radius;
                }
            }
        }
    }
}
