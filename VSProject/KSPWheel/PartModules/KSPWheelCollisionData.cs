using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelCollisionData : MonoBehaviour
    {

        public Collider monitoredCollider;
        public float collisionForce;
        public Vector3 collisionPoint;
        public Vector3 surfaceNormal;

        public void OnCollisionEnter(Collision c)
        {
            if (isMonitoredCollision(c))
            {
                updateCollisionForce(c);
            }
        }

        public void OnCollisionStay(Collision c)
        {
            if (isMonitoredCollision(c))
            {
                updateCollisionForce(c);
            }
        }

        public void OnCollisionExit(Collision c)
        {
            if (isMonitoredCollision(c))
            {
                collisionForce = 0f;
                collisionPoint = monitoredCollider.transform.position;
                surfaceNormal = Vector3.up;
            }
        }

        private void updateCollisionForce(Collision c)
        {
            collisionForce = c.impulse.magnitude / Time.fixedDeltaTime;
            collisionPoint = c.contacts[0].point;
            surfaceNormal = (collisionPoint - monitoredCollider.transform.position).normalized;
        }

        private bool isMonitoredCollision(Collision c)
        {
            int len = c.contacts.Length;
            for (int i = 0; i < len; i++)
            {
                if (c.contacts[i].thisCollider == monitoredCollider || c.contacts[i].otherCollider == monitoredCollider)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
