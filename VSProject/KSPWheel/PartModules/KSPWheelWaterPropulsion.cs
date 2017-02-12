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
        public float forceSpeedFactor = 1f;

        [KSPField]
        public float forceRadiusFactor = 1f;

        [KSPField(guiName ="SubD", guiActive = true)]
        public float submergedDepth = 0f;

        [KSPField(guiName = "SubP", guiActive = true)]
        public float submergedPercent = 0f;

        [KSPField(guiName = "FrcO", guiActive = true)]
        public float forceOutput = 0f;

        [KSPField(guiName = "FrcP", guiActive = true)]
        public float forcePercent = 0f;

        [KSPField(guiName = "FrcP", guiActive = true)]
        public float torque = 0f;

        [KSPField(guiName = "FrcP", guiActive = true)]
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
                float radius, alt, depth;
                for (int i = 0; i < len; i++)
                {
                    data = controller.wheelData[i];
                    wheel = data.wheel;
                    radius = wheel.radius;
                    Vector3 wheelPos = wheel.transform.position - wheel.transform.up * (wheel.length - wheel.compressionDistance);
                    alt = FlightGlobals.getAltitudeAtPos(wheelPos);
                    if (alt > radius)//impossible that wheel contacted surface regardless of orientation
                    {
                        return;
                    }
                    else if (alt < -radius)//fully submerged, net force output is zero (but should add a torque?)
                    {
                        return;
                    }
                    submergedDepth = radius - alt;
                    submergedPercent = submergedDepth / (radius * 2f);
                    depth = Mathf.Abs(alt);
                    forcePercent = 1 - (depth / radius);
                    forceOutput = forceSpeedFactor * wheel.angularVelocity * forceRadiusFactor * radius;
                    //need to slow the wheel by forceOutput
                    torque = forceOutput * radius;
                    accel = torque / wheel.momentOfInertia;
                    //how to tell what direction to apply the force?
                    //it should be along the plane defined by the ocean surface normal, in the forwards direction of the wheel
                    //can use the 'wheelForward' vector from wheel collider
                    //and use the wheel colliders projection code to derive the forward direction of wheel as projected onto the plane...
                }
            }
        }

    }
}
