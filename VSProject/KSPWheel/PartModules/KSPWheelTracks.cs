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

        [KSPField]
        public int smrIndex = 0;

        private float factorSum;
        private float[] shares;
        private SkinnedMeshRenderer smr;
        private Vector2 offset = Vector2.zero;
        private Material mat;
        private float trackVelocity = 0f;//velocity of the track surface, in m/s

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            if (HighLogic.LoadedSceneIsEditor) { return; }
            smr = part.GetComponentsInChildren<SkinnedMeshRenderer>()[smrIndex];
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
            updateScaleValues();
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (mat != null && controller.wheelState==KSPWheelState.DEPLOYED)
            {
                offset.x += (-trackVelocity * Time.deltaTime * trackDir) / (trackLength * part.rescaleFactor * controller.scale); ;
                mat.SetTextureOffset("_MainTex", offset);
                mat.SetTextureOffset("_BumpMap", offset);
            }
        }

        protected void updateScaleValues()
        {
            if (this.wheel == null) { return; }//wheel not initialized

            KSPWheelCollider wheel;
            int len = controller.wheelData.Length;
            factorSum = 0;
            float[] factors = new float[len];
            shares = new float[len];
            for (int i = 0; i < len; i++)
            {
                wheel = controller.wheelData[i].wheel;
                factors[i] = wheel.momentOfInertia / wheel.radius;
                factorSum += factors[i];
            }
            for (int i = 0; i < len; i++)
            {
                shares[i] = factors[i] / factorSum;
            }
        }

        /// <summary>
        /// This method sets each wheel torque to that which would be needed
        /// exert the same linear velocity change on each wheel in the group.
        /// The moment of inertia and radius of each wheel determines a wheels share
        /// of the torque.
        /// </summary>
        protected override void updateMotor()
        {
            base.updateMotor();
            if (shares == null) { updateScaleValues(); }
            float totalSystemTorque = 0f;
            float totalBrakeTorque = this.wheel.brakeTorque;
            float totalMotorTorque = torqueOutput;
            KSPWheelCollider wheel;
            int len = controller.wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                wheel = controller.wheelData[i].wheel;
                totalSystemTorque += wheel.angularVelocity * wheel.momentOfInertia;
            }
            for (int i = 0; i < len; i++)
            {
                wheel = controller.wheelData[i].wheel;
                wheel.motorTorque = shares[i] * totalMotorTorque;
                wheel.brakeTorque = shares[i] * totalBrakeTorque;
                wheel.angularVelocity = shares[i] * totalSystemTorque / wheel.momentOfInertia;
            }
            trackVelocity = this.wheel.linearVelocity;
        }

    }
}
