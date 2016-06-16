using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel.Component
{
    public class KSPWheelConstraint : MonoBehaviour
    {
        public Vector3 origin;
        public Vector3 anchor;
        public Vector3 upAxis;
        public Vector3 forwardAxis;

        public float linearDist = 0.403f;

        public Vector3 dist;
        public Vector3 force;
        public float hitDist;
        public float hitMult = 1f;

        public void FixedUpdate()
        {
            Vector3 p1 = gameObject.transform.position + gameObject.transform.TransformVector(origin);
            Vector3 p2 = p1 + gameObject.transform.TransformVector(anchor);
            dist = p1 - p2;
            RaycastHit hit;
            if (Physics.Raycast(p1, -gameObject.transform.up, out hit, 1f))
            {
                Rigidbody rb = gameObject.GetComponent<Rigidbody>();
                hitDist = hit.distance;
                if (hitDist < linearDist)
                {
                    float d = linearDist - hitDist;//distance
                    float v = d / Time.fixedDeltaTime;
                    force = new Vector3(0, v, 0);
                    force = gameObject.transform.TransformVector(force);
                    rb.AddForceAtPosition(force * hitMult, p1, ForceMode.VelocityChange);
                }
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
