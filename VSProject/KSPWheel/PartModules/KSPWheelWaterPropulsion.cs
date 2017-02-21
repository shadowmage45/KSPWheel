using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelWaterPropulsion : KSPWheelSubmodule
    {

        [KSPField]
        public float forceSpeedFactor = 0.5f;

        [KSPField]
        public float forceRadiusFactor = 0.5f;

        [KSPField(guiName = "SubD", guiActive = true)]
        public float submergedDepth = 0f;

        [KSPField(guiName = "SubP", guiActive = true)]
        public float submergedPercent = 0f;

        [KSPField(guiName = "FrcP", guiActive = true)]
        public float forcePercent = 0f;

        [KSPField(guiName = "FrcO", guiActive = true, guiUnits = "kN")]
        public float forceOutput = 0f;

        [KSPField(guiName = "Trq", guiActive = true)]
        public float torque = 0f;

        [KSPField(guiName = "Wacc", guiActive = true)]
        public float accel = 0f;

        internal override void postWheelPhysicsUpdate()
        {
            base.postWheelPhysicsUpdate();
            submergedDepth = 0f;
            submergedPercent = 0f;
            if (vessel.mainBody.ocean)
            {
                int len = controller.wheelData.Length;
                KSPWheelBase.KSPWheelData data;
                KSPWheelCollider wheel;
                Vector3 wheelPos, surfaceNormal, wheelSurfaceForward, wheelForward;
                float radius, alt, depth, inertiaTorque, inertiaForce;
                for (int i = 0; i < len; i++)
                {
                    data = controller.wheelData[i];
                    wheel = data.wheel;
                    radius = wheel.radius;
                    data.waterMode = false;
                    wheelPos = wheel.transform.position - wheel.transform.up * (wheel.length - wheel.compressionDistance);
                    alt = FlightGlobals.getAltitudeAtPos(wheelPos);
                    if (alt > radius)//impossible that wheel contacted surface regardless of orientation
                    {
                        return;
                    }
                    else if (alt < -radius)//fully submerged, net force output is zero (but should add a torque?)
                    {
                        return;
                    }
                    data.waterMode = true;
                    submergedDepth = radius - alt;
                    submergedPercent = submergedDepth / (radius * 2f);
                    depth = Mathf.Abs(alt);
                    forcePercent = 1 - (depth / radius);

                    inertiaTorque = wheel.angularVelocity * wheel.momentOfInertia;//torque over one second needed to completely arrest wheel velocity
                    inertiaForce = wheel.radius * inertiaTorque;//force over one second needed to completely arrest wheel velocity

                    forceOutput = forceSpeedFactor * forceRadiusFactor * forcePercent * wheel.radius * wheel.angularVelocity;

                    //need to slow the wheel by forceOutput
                    torque = forceOutput * radius;
                    accel = (torque / wheel.momentOfInertia) * Time.fixedDeltaTime;
                    accel = Mathf.Clamp(accel, -Mathf.Abs(wheel.angularVelocity), Mathf.Abs(wheel.angularVelocity));//ensure it cannot drive the wheel backwards??
                    wheel.angularVelocity -= accel;

                    //calculate the point and direction of force application
                    surfaceNormal = vessel.mainBody.GetSurfaceNVector(vessel.latitude, vessel.longitude);
                    //wheel-local 'forward' direction, including the steering angle
                    wheelForward = Quaternion.AngleAxis(wheel.steeringAngle, wheel.transform.up) * wheel.transform.forward;
                    //surface-local projected wheel 'forward' direction
                    wheelSurfaceForward = wheelForward - surfaceNormal * Vector3.Dot(wheelForward, surfaceNormal);

                    wheel.rigidbody.AddForceAtPosition(wheelSurfaceForward * forceOutput, wheelPos, ForceMode.Force);

                }
            }
        }

    }
}
