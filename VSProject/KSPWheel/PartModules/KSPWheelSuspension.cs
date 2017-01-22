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

        [KSPField]
        public bool allowLockedSuspension = false;

        [KSPField(guiName = "Lock Suspension", guiActive = false, guiActiveEditor = false, isPersistant = true),
         UI_Toggle(enabledText = "Locked", disabledText = "Free", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool lockSuspension = false;

        public Vector3 defaultPos;
        public Transform suspensionTransform;
        private GameObject lockedSuspensionObject;
        private CapsuleCollider lockedSuspensionCollider;
        private Vector3 lockedPos = Vector3.zero;

        private void suspensionLockChanged(BaseField field, System.Object obj)
        {
            if (lockedSuspensionCollider != null)
            {
                if (lockSuspension)
                {
                    lockedPos = Vector3.zero;
                    lockedSuspensionCollider.center = lockedPos;
                    lockedSuspensionCollider.enabled = controller.wheelState == KSPWheelState.DEPLOYED;
                }
                else if (!lockSuspension)
                {
                    lockedSuspensionCollider.enabled = false;
                }
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(lockSuspension)].guiActive = allowLockedSuspension;
            Fields[nameof(lockSuspension)].guiActiveEditor = false;
            Fields[nameof(lockSuspension)].uiControlFlight.onFieldChanged = suspensionLockChanged;
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor && controller != null && wheel != null && suspensionTransform != null)
            {
                float scale = part.rescaleFactor * controller.scale;
                float offset = suspensionOffset * scale + (controller.wheelState == KSPWheelState.DEPLOYED? wheel.length * 0.5f : 0f);
                Vector3 o = suspensionTransform.TransformDirection(suspensionAxis);
                suspensionTransform.localPosition = defaultPos;
                suspensionTransform.position -= o * offset;
            }
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            if (suspensionTransform == null)
            {
                suspensionTransform = part.transform.FindRecursive(suspensionName);
                if (suspensionTransform == null)
                {
                    MonoBehaviour.print("ERROR: Suspension transform was null for name: " + suspensionName);
                    MonoBehaviour.print("Model Hierarchy: ");
                    Utils.printHierarchy(part.gameObject);
                }
                defaultPos = suspensionTransform.localPosition;
            }
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (suspensionTransform != null)
            {
                float offset = 0;
                if (controller.wheelState == KSPWheelState.DEPLOYED)
                {
                    offset = (wheel.length - wheel.compressionDistance + (suspensionOffset * part.rescaleFactor * controller.scale));
                }
                else if (controller.wheelState == KSPWheelState.BROKEN)
                {
                    offset = suspensionOffset * part.rescaleFactor * controller.scale;
                }
                Vector3 o = suspensionTransform.TransformDirection(suspensionAxis);
                suspensionTransform.localPosition = defaultPos;
                suspensionTransform.position -= o * offset;
            }
            if (lockedSuspensionCollider != null)
            {
                lockedSuspensionCollider.enabled = controller.wheelState == KSPWheelState.DEPLOYED && lockSuspension;
            }
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            if (lockSuspension)
            {
                if (lockedSuspensionObject == null)
                {
                    lockedSuspensionObject = new GameObject("KSPWheelLockedSuspension");
                    lockedSuspensionObject.transform.NestToParent(wheelTransform);
                    lockedSuspensionObject.layer = 26;
                    lockedSuspensionCollider = lockedSuspensionObject.AddComponent<CapsuleCollider>();
                    lockedSuspensionCollider.radius = wheel.radius;
                    lockedSuspensionCollider.height = wheel.radius * 2;
                    lockedPos.y = -(wheel.length - wheel.compressionDistance);
                    lockedSuspensionCollider.center = lockedPos;
                }
                float extSpeed = 0.05f*Time.fixedDeltaTime;
                float d = Mathf.Abs(lockedPos.y) - wheel.length * 0.95f;
                if (d < -extSpeed) { d = -extSpeed; }
                if (d != 0)
                {
                    lockedPos.y += d;
                    lockedSuspensionCollider.center = lockedPos;
                }
            }
        }
    }
}
