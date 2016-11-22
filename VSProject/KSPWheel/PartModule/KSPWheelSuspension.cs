using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelSuspension : KSPWheelSubmodule
    {
        /// <summary>
        /// The name of the transform to be animated for suspension response.  May be null if no transform is to be manipulated.
        /// </summary>
        [KSPField]
        public string suspensionName = "suspension";

        /// <summary>
        /// The visual offset to the suspension transform compared to its default location and the wheel-colliders location.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "off"),
         UI_FloatEdit(suppressEditorShipModified = true, minValue = -5, maxValue = 5, incrementSmall = 0.25f, incrementLarge = 1f, incrementSlide = 0.0125f, sigFigs = 4)]
        public float suspensionOffset = 0f;

        /// <summary>
        /// The transform-local axis on which to move the suspension transform.  Defaults to y+ for 'up'.
        /// </summary>
        [KSPField]
        public Vector3 suspensionAxis = Vector3.up;

        private Vector3 defaultPos;
        private Transform suspensionTransform;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            suspensionTransform = part.transform.FindRecursive(suspensionName);
            defaultPos = suspensionTransform.localPosition;
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || wheel==null) { return; }
            if (suspensionTransform != null)
            {
                float scale = suspensionTransform.parent.localScale.y;
                float offset = (wheel.length - wheel.compressionDistance + suspensionOffset) / scale;
                suspensionTransform.localPosition = defaultPos - suspensionAxis * offset;
            }
        }

    }
}
