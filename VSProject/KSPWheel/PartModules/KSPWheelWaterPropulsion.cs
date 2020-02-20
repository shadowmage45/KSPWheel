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

        [KSPField]
        public float maxSubmerged = 0.5f;

        internal override string getModuleInfo()
        {
            return "This part can provide propulsion in water.";
        }

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
                        continue;
                    }
                    else if (maxSubmerged < 1f && alt < -radius)//fully submerged, net force output is zero (but should add a torque?)
                    {
                        continue;
                    }
                    data.waterMode = true;
                    depth = Mathf.Abs(alt);
                    submergedDepth = radius - alt;
                    submergedPercent = Mathf.Clamp01(submergedDepth / (radius * 2f));

                    if (submergedPercent <= maxSubmerged)
                    {
                        forcePercent = submergedPercent / maxSubmerged;
                    }
                    else
                    {
                        forcePercent = 1 - ((submergedPercent / maxSubmerged) - 1);
                    }
                    forcePercent = Mathf.Clamp01(forcePercent);
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
