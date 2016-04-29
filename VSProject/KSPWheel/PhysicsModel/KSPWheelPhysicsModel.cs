using System;
using UnityEngine;

namespace KSPWheel
{
    public abstract class KSPWheelPhysicsModel
    {
        public abstract Vector3 calculateSideFriction(GameObject wheel, Rigidbody rigidBody, float downForce, Vector3 surfaceVelocity);
    }
}
