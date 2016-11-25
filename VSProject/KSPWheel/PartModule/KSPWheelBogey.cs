using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelBogey : KSPWheelSubmodule
    {

        [KSPField]
        public string bogeyName = "bogey";

        [KSPField]
        public Vector3 bogeyRotAxis = Vector3.left;

        [KSPField]
        public float rotationSpeed = 5f;

        [KSPField]
        public float restingRotation = 0f;

        [KSPField]
        public float rotationOffset = 0f;

        [KSPField(guiActive = true)]
        public float angle;

        private Quaternion defaultRotation;
        private Transform bogeyTransform;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            bogeyTransform = part.transform.FindRecursive(bogeyName);
            if (bogeyTransform == null)
            {
                MonoBehaviour.print("ERROR: Could not locate bogey for name: " + bogeyName);
            }
            defaultRotation = bogeyTransform.localRotation;
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (controller.wheelState != KSPWheelState.DEPLOYED)
            {
                return;
            }
            //TODO update bogey orientation
            if (wheel.isGrounded)
            {
                //the 'up' direction of the contacted surface
                Vector3 normal = wheel.contactNormal;
                //transformed to local coordinates of the bogey
                normal = bogeyTransform.InverseTransformDirection(normal);
                angle = getBogeyAngle(normal) + rotationOffset;
                if (angle >= 180) { angle -= 360f; }
                if (angle < -180) { angle += 360f; }
                if (rotationSpeed > 0) { angle = Mathf.Lerp(0, angle, Time.deltaTime * rotationSpeed); }
                bogeyTransform.Rotate(bogeyRotAxis, angle, Space.Self);
            }
            else
            {
                angle = restingRotation + rotationOffset;
                if (angle >= 180) { angle -= 360f; }
                if (angle < -180) { angle += 360f; }
                Quaternion dest = defaultRotation * Quaternion.Euler(angle * bogeyRotAxis);
                bogeyTransform.localRotation = rotationSpeed > 0 ? Quaternion.Lerp(bogeyTransform.localRotation, dest, Time.deltaTime) : dest;
            }
        }

        private float getBogeyAngle(Vector3 localHitNorm)
        {
            float a1=0, b1=0;
            bool invert = false;
            if (bogeyRotAxis.x != 0)
            {
                a1 = localHitNorm.y;
                b1 = localHitNorm.z;
                invert = bogeyRotAxis.x < 0;
            }
            else if (bogeyRotAxis.y != 0)
            {
                a1 = localHitNorm.x;
                b1 = localHitNorm.z;
                invert = bogeyRotAxis.y < 0;
            }
            else if (bogeyRotAxis.z != 0)
            {
                a1 = localHitNorm.x;
                b1 = localHitNorm.y;
                invert = bogeyRotAxis.z < 0;
            }
            angle = Mathf.Atan2(b1, a1) * Mathf.Rad2Deg;
            if (invert) { angle = -angle; }
            return angle;
        }

    }
}
