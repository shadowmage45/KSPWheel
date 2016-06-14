using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel.Component
{
    public class KSPWheelStickyConstraint : MonoBehaviour
    {

        public Vector3 origin;
        public Vector3 anchor;

        public Vector3 maxLocalDiff = new Vector3(0.25f, 0f, 0.25f);
        public Vector3 worldHitPos;
        public Vector3 stickyWorldPos;
        public Vector3 worldDiff;
        public Vector3 localDiff;
        public Vector3 localForce;
        public Vector3 worldForce;

        private bool init;


        public void FixedUpdate()
        {
            Vector3 p1 = gameObject.transform.position + gameObject.transform.TransformVector(origin);
            Vector3 p2 = p1 + gameObject.transform.TransformVector(anchor);
            float dist = (p1 - p2).magnitude;
            RaycastHit hit;
            if (Physics.Raycast(p1, -gameObject.transform.up, out hit, dist))
            {
                this.worldHitPos = hit.point;
                if (!init)
                {
                    init = true;
                    stickyWorldPos = worldHitPos;
                }
                else
                {
                    worldDiff = stickyWorldPos - worldHitPos;
                    localDiff = gameObject.transform.InverseTransformVector(worldDiff);
                    localDiff.y = 0;
                    if (Mathf.Abs(localDiff.x) > maxLocalDiff.x)
                    {
                        init = false;
                    }
                    else if (Mathf.Abs(localDiff.z) > maxLocalDiff.z)
                    {
                        init = false;
                    }
                    else
                    {
                        localForce = localDiff / Time.fixedDeltaTime;
                        worldForce = gameObject.transform.TransformVector(localForce);
                        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
                        rb.AddForceAtPosition(worldForce * 0.1f, p1, ForceMode.VelocityChange);
                    }
                }
            }
            else
            {
                init = false;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 start, end;
            start = gameObject.transform.position + gameObject.transform.TransformVector(origin);
            end = start + gameObject.transform.right;
            Gizmos.DrawLine(start, end);
            end = start + gameObject.transform.up;
            Gizmos.DrawLine(start, end);
            end = start + gameObject.transform.forward;
            Gizmos.DrawLine(start, end);

            Gizmos.color = Color.blue;
            start = start + gameObject.transform.TransformVector(anchor);
            end = start + gameObject.transform.right;
            Gizmos.DrawLine(start, end);
            end = start + gameObject.transform.up;
            Gizmos.DrawLine(start, end);
            end = start + gameObject.transform.forward;
            Gizmos.DrawLine(start, end);
        }
    }
}
