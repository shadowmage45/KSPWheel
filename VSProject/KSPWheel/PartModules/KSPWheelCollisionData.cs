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

        private PhysicMaterial wheelMat;
        private PhysicMaterial prevMat;

        public void Start()
        {
            wheelMat = new PhysicMaterial();
            wheelMat.frictionCombine = PhysicMaterialCombine.Multiply;
        }

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
                updateCollisionForce(null);
            }
        }

        private void updateCollisionForce(Collision c)
        {
            int len = c.contacts.Length;
            if (c == null)
            {
                collisionForce = 0f;
            }
            else
            {
                collisionForce = c.impulse.magnitude / Time.fixedDeltaTime;
            }
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
