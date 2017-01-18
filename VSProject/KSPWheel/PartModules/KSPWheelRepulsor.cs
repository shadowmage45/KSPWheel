using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelRepulsor : KSPWheelSubmodule
    {

        [KSPField(guiName = "Repulsor Height", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.1f, maxValue = 1, stepIncrement = 0.01f, suppressEditorShipModified = true)]
        public float repulsorHeight = 1f;

        [KSPField(guiName = "Repuslor Power", guiActiveEditor = false, guiActive = true),
         UI_Toggle(enabledText ="On", disabledText ="Off", suppressEditorShipModified = true)]
        public bool repulsorEnabled = true;

        [KSPField(guiName = "Energy Use", guiActive = true, guiUnits = "EC/s")]
        public float guiEnergyUse = 0f;

        [KSPField(guiName = "Force Application", guiActiveEditor = false, guiActive = true),
         UI_Toggle(enabledText = "Offset", disabledText = "Standard", suppressEditorShipModified = true)]
        public bool forcePointOffset = true;

        [KSPField(guiName = "Force Axis", guiActiveEditor = false, guiActive = true),
         UI_Toggle(enabledText = "Suspension", disabledText = "HitNormal", suppressEditorShipModified = true)]
        public bool suspensionNormal = true;

        [KSPField]
        public float easeTimeMult = 0.5f;

        /// <summary>
        /// EC/s * tons of weight supported
        /// </summary>
        [KSPField]
        public float energyUse = 1f;

        private float currentRepulsorLength;
        private float desiredRepulsorLength;

        private Vector3 oceanHitPos;

        private void repulsorToggled(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m => 
            {
                m.repulsorEnabled = repulsorEnabled;
                if (m.repulsorEnabled)
                {
                    m.controller.wheelState = KSPWheelState.DEPLOYED;
                    m.controller.springEaseMult = 0f;
                }
                else
                {
                    //handled by per-tick updating
                }
            });
        }

        private void repulsorHeightUpdated(BaseField field, System.Object ob)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.repulsorHeight = repulsorHeight;
                desiredRepulsorLength = m.repulsorHeight;
            });
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            oceanHitPos = part.transform.position;
            Fields[nameof(repulsorEnabled)].uiControlFlight.onFieldChanged = repulsorToggled;
            Fields[nameof(repulsorHeight)].uiControlFlight.onFieldChanged = Fields[nameof(repulsorHeight)].uiControlEditor.onFieldChanged = repulsorHeightUpdated;
            currentRepulsorLength = desiredRepulsorLength = repulsorHeight;
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
        }

        internal override void preWheelSuspensionCalc()
        {
            base.preWheelSuspensionCalc();
            //update repulsor 'length' stats
            wheel.length = currentRepulsorLength * 5f;
            currentRepulsorLength = Mathf.MoveTowards(currentRepulsorLength, desiredRepulsorLength, 1 * Time.fixedDeltaTime);
            wheel.useSuspensionNormal = suspensionNormal;
            wheel.forceApplicationOffset = forcePointOffset ? 1f : 0f;

            if (vessel.mainBody.ocean)
            {
                float alt = FlightGlobals.getAltitudeAtPos(wheel.transform.position);
                float susLen = wheel.length + wheel.radius;
                if (alt > susLen)//impossible that wheel contacted surface regardless of orientation
                {
                    wheel.useExternalHit = false;
                    return;
                }
                Vector3 surfaceNormal = vessel.mainBody.GetSurfaceNVector(vessel.latitude, vessel.longitude);
                float surfSuspDot = Vector3.Dot(oceanHitPos, surfaceNormal);
                oceanHitPos = wheel.transform.position - surfaceNormal * susLen * surfSuspDot;
                if (alt - surfSuspDot * susLen < 0)
                {
                    wheel.useExternalHit = true;
                    wheel.externalHitPoint = oceanHitPos;
                    wheel.externalHitNormal = surfaceNormal;
                }
                else
                {
                    wheel.useExternalHit = false;
                }
            }
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();

            if (repulsorEnabled && controller.springEaseMult < 1)
            {
                controller.springEaseMult = Mathf.Clamp01(controller.springEaseMult + Time.fixedDeltaTime * easeTimeMult);
            }
            else if (!repulsorEnabled && controller.springEaseMult > 0)
            {
                controller.springEaseMult = Mathf.Clamp01(controller.springEaseMult - Time.fixedDeltaTime * easeTimeMult);
                if (controller.springEaseMult <= 0)
                {
                    controller.wheelState = KSPWheelState.RETRACTED;
                }
            }
            float ecPerSecond = wheel.springForce * 0.1f * controller.springEaseMult * energyUse;
            float ecPerTick = ecPerSecond * Time.fixedDeltaTime;
            float used = part.RequestResource("ElectricCharge", ecPerTick);
            if (used < ecPerTick)
            {
                repulsorEnabled = false;
                //TODO - print to screen that there was a power failure in the repulsor
            }
            guiEnergyUse = ecPerSecond;
        }

        internal override void onUIControlsUpdated(bool show)
        {
            base.onUIControlsUpdated(show);
            Fields[nameof(repulsorHeight)].guiActive = Fields[nameof(repulsorHeight)].guiActiveEditor = show;
            Fields[nameof(repulsorEnabled)].guiActive = show;
            Fields[nameof(repulsorEnabled)].guiActive = Fields[nameof(repulsorEnabled)].guiActiveEditor = show;
        }

    }
}
