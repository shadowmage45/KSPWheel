using System;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelDefaultFrictionModel : KSPWheelPhysicsModel
    {
        public override Vector3 calculateSideFriction(GameObject wheel, Rigidbody rigidBody, float downForce, Vector3 surfaceVelocity)
        {
            throw new NotImplementedException();
        }
    }
}
