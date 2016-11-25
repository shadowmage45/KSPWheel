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
        [KSPField]
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
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            suspensionTransform = part.transform.FindRecursive(suspensionName);
            if (suspensionTransform == null)
            {
                MonoBehaviour.print("ERROR: Suspension transform was null for name: " + suspensionName);
                MonoBehaviour.print("Model Hierarchy: ");
                Utils.printHierarchy(part.gameObject);
            }
            defaultPos = suspensionTransform.localPosition;
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (controller.wheelState == KSPWheelState.DEPLOYED && suspensionTransform != null)
            {
                float scale = suspensionTransform.parent.localScale.y;
                float offset = (wheel.length - wheel.compressionDistance + suspensionOffset);
                Vector3 o = suspensionTransform.TransformDirection(suspensionAxis);
                suspensionTransform.localPosition = defaultPos;
                suspensionTransform.position -= o * offset;
            }
        }

    }
}
