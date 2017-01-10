using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelTracks : KSPWheelMotor
    {

        [KSPField]
        public float trackLength = 10f;

        [KSPField]
        public int trackDir = 1;

        private SkinnedMeshRenderer smr;
        private Vector2 offset = Vector2.zero;
        private Material mat;
        private float trackRPM = 0f;

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
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
                float offsetAmount = (((-trackRPM * 2f * Mathf.PI) / 60f) * Time.deltaTime * trackDir) / trackLength;
                offset.x += offsetAmount;
                mat.SetTextureOffset("_MainTex", offset);
                mat.SetTextureOffset("_BumpMap", offset);
            }
        }

        /// <summary>
        /// This method sets each wheel torque to that which would be needed
        /// exert the same linear velocity change on each wheel in the group.
        /// The moment of inertia of each wheel determines a wheels share
        /// of the torque.
        /// </summary>
        protected override void updateMotor()
        {
            base.updateMotor();
            float totalBrakeTorque = wheel.brakeTorque;
            float torqueInputDivisor = 0f;
            float totalRadius = 0f;
            float totalSystemTorque = 0f;
            float totalTorque = torqueOutput;
            int len = controller.wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                torqueInputDivisor += controller.wheelData[i].wheel.momentOfInertia * controller.wheelData[i].wheel.radius;
                totalRadius += controller.wheelData[i].wheel.radius;
                totalSystemTorque += controller.wheelData[i].wheel.angularVelocity * controller.wheelData[i].wheel.momentOfInertia;
            }
            for (int i = 0; i < len; i++)
            {
                float torqueDiv = controller.wheelData[i].wheel.momentOfInertia / torqueInputDivisor / controller.wheelData[i].wheel.radius;
                controller.wheelData[i].wheel.motorTorque = totalTorque * torqueDiv;
                controller.wheelData[i].wheel.brakeTorque = totalBrakeTorque * torqueDiv;
                controller.wheelData[i].wheel.angularVelocity = (controller.wheelData[i].wheel.radius / totalRadius) * totalSystemTorque;
            }
            trackRPM = wheel.rpm;
        }

    }
}
