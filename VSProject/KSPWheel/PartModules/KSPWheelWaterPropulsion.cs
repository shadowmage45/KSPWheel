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

        internal override void postWheelPhysicsUpdate()
        {
            base.postWheelPhysicsUpdate();
            if (vessel.mainBody.ocean)
            {
                int len = controller.wheelData.Length;
                KSPWheelBase.KSPWheelData data;
                KSPWheelCollider wheel;
                Vector3 wheelPos, surfaceNormal, wheelSurfaceForward, wheelForward;
                float radius, alt, depth, torque, accel, forceOutput, forcePercent, submergedPercent, submergedDepth;
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

                    forceOutput = forceSpeedFactor * forceRadiusFactor * forcePercent * radius * wheel.angularVelocity;

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
                    data.waterEffectPos = wheelPos - wheelSurfaceForward * radius * Mathf.Sign(forceOutput);
                    data.waterEffectSize = Mathf.Abs(wheel.angularVelocity * radius) * 0.15f;
                    data.waterEffectForce = Mathf.Abs(forceOutput) * 0.15f;
                }
            }
        }

    }
}
