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
         UI_FloatRange(minValue = 0, maxValue = 5, stepIncrement = 0.1f, suppressEditorShipModified = true)]
        public float repulsorHeight = 1f;

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            wheel.length = repulsorHeight;
            //TODO add a horizontal force based on surface normal vs. suspension normal

        }

        internal override void onUIControlsUpdated(bool show)
        {
            base.onUIControlsUpdated(show);
            Fields[nameof(repulsorHeight)].guiActive = Fields[nameof(repulsorHeight)].guiActiveEditor = show;
        }

    }
}
