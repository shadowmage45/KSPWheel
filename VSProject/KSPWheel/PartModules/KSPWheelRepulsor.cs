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

        [KSPField]
        public float easeTimeMult = 0.5f;

        /// <summary>
        /// EC/s * tons of weight supported
        /// </summary>
        [KSPField]
        public float energyUse = 1f;

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
            });
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(repulsorEnabled)].uiControlFlight.onFieldChanged = repulsorToggled;
            Fields[nameof(repulsorHeight)].uiControlFlight.onFieldChanged = Fields[nameof(repulsorHeight)].uiControlEditor.onFieldChanged = repulsorHeightUpdated;
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            wheel.length = repulsorHeight*5f;
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
