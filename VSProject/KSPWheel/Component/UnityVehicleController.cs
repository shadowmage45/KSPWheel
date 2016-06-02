using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    /// <summary>
    /// Test script to calculate sprung mass for each wheel.
    /// This script assumes that all wheels are in contact with the surface; will need to be adapted later to only calculate sprung mass for wheels in contact with the surface
    /// </summary>
    public class UnityVehicleController : MonoBehaviour
    {
        public KSPWheelComponent[] colliders;
        public float[] sprungMasses;
        public Rigidbody rb;
        public float cLen;

        public float sm1;
        public float sm2;
        public float sm3;
        public float sm4;

        public void Start()
        {
            rb = gameObject.GetComponent<Rigidbody>();
            colliders = gameObject.GetComponentsInChildren<KSPWheelComponent>(true);
            float totalMass = rb.mass;
            Vector3 com = rb.worldCenterOfMass;

            Vector3 pos = rb.position;
            Vector3 delta;
            int len = colliders.Length;
            for (int i = 0; i < len; i++)
            {
                delta = colliders[i].transform.position - pos;
                cLen += delta.magnitude;
            }
            sprungMasses = new float[len];
            for (int i = 0; i < len; i++)
            {
                delta = colliders[i].transform.position - pos;
                sprungMasses[i] = totalMass * (delta.magnitude / cLen);
                if (i == 0) { sm1 = sprungMasses[0]; }
                if (i == 1) { sm2 = sprungMasses[1]; }
                if (i == 2) { sm3 = sprungMasses[2]; }
                if (i == 3) { sm4 = sprungMasses[3]; }
            }
        }

    }
}
