using UnityEngine;

namespace KSPWheel
{
     public struct WheelHitSource
    {
        public Collider Collider; //The other Collider the wheel is hitting.
        public Vector3 Point; //The point of contact between the wheel and the ground.
        public Vector3 Normal; //The normal at the point of contact.
        public Vector3 ForwardDir; //The direction the wheel is pointing in.
        public Vector3 SidewaysDir; //The sideways direction of the wheel.
        public Vector3 Force; //The magnitude of the force being applied for the contact.
        public float ForwardSlip; //Tire slip in the rolling direction. Acceleration slip is negative, braking slip is positive.
        public float SidewaysSlip; //Tire slip in the sideways direction.
    }
}
