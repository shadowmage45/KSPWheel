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

        /// <summary>
        /// Secondary wheel indexes form compression distance averaging
        /// CSV list, defaults to none
        /// </summary>
        [KSPField]
        public string secondaryWheels = string.Empty;

        [KSPField]
        public float retractedPosition = 1f;

        [KSPField]
        public float deployStart = 0.5f;

        [KSPField]
        public float deployEnd = 0.7f;

        [KSPField]
        public bool allowLockedSuspension = false;

        [KSPField(guiName = "Lock Suspension", guiActive = false, guiActiveEditor = false, isPersistant = true),
         UI_Toggle(enabledText = "Locked", disabledText = "Free", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool lockSuspension = false;

        public Vector3 defaultPos;
        public Transform suspensionTransform;
        private float deployedPosition = 1f;
        private GameObject lockedSuspensionObject;
        private CapsuleCollider lockedSuspensionCollider;
        private int[] secondaryWheelInd = null;

        private void suspensionLockChanged(BaseField field, System.Object obj)
        {
            if (lockedSuspensionCollider != null)
            {
                if (lockSuspension)
                {
                    lockedSuspensionCollider.center = Vector3.zero;
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
            if (!string.IsNullOrEmpty(secondaryWheels))
            {
                string[] sp = secondaryWheels.Split(',');
                int len = sp.Length;
                secondaryWheelInd = new int[len];
                for (int i = 0; i < len; i++)
                {
                    secondaryWheelInd[i] = int.Parse(sp[i].Trim());
                }
            }
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                preWheelFrameUpdate();
            }
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            if (suspensionTransform == null)
            {
                suspensionTransform = part.transform.FindChildren(suspensionName)[wheelData.indexInDuplicates];
                if (suspensionTransform == null)
                {
                    MonoBehaviour.print("ERROR: Suspension transform was null for name: " + suspensionName);
                    MonoBehaviour.print("Model Hierarchy: ");
                    Utils.printHierarchy(part.gameObject);
                }
                defaultPos = suspensionTransform.localPosition;
            }
            if (vessel != null && controller.wheelState == KSPWheelState.DEPLOYED)
            {
                vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, true);
            }
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (suspensionTransform != null && wheel!=null)
            {
                float offset = 0;
                if (controller.wheelState == KSPWheelState.DEPLOYED)
                {
                    offset = wheel.length - wheel.compressionDistance;
                    if (secondaryWheelInd != null)
                    {
                        int len = secondaryWheelInd.Length;
                        KSPWheelBase.KSPWheelData sec;
                        for (int i = 0; i < len; i++)
                        {
                            sec = controller.wheelData[secondaryWheelInd[i]];
                            offset += sec.wheel.length - sec.wheel.compressionDistance;
                        }
                        offset /= len + 1;
                    }
                }
                else if (controller.wheelState == KSPWheelState.BROKEN)
                {
                    offset = 0f;
                }
                else//retracting or deploying, find and set to proper position
                {
                    float animPos = controller.deployAnimationTime;
                    if (animPos < deployStart)
                    {
                        offset = retractedPosition * wheel.length;
                    }
                    else if (animPos < deployEnd)
                    {
                        float interval = deployEnd - deployStart;//length of interval
                        if (interval > 0)
                        {
                            float adjustment = deployedPosition - retractedPosition;
                            float time = (animPos - deployStart) / interval;//0-1 position within adjustment range
                            float pos = retractedPosition + time * adjustment;
                            offset = pos * wheel.length;
                        }
                    }
                    else
                    {
                        offset = deployedPosition * wheel.length;
                    }
                }
                offset += suspensionOffset * part.rescaleFactor * controller.scale;
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
                    lockedSuspensionCollider.center = Vector3.zero;
                }
                float destY = -wheel.length * 1.01f;
                float t = lockedSuspensionCollider.center.y;
                if (t != destY)
                {
                    t = Mathf.MoveTowards(t, destY, wheel.length * Time.fixedDeltaTime * 0.5f);
                    Vector3 pos = new Vector3(0, t, 0);
                    lockedSuspensionCollider.center = pos;
                }
            }
        }

        internal override void onStateChanged(KSPWheelState oldState, KSPWheelState newState)
        {
            base.onStateChanged(oldState, newState);
            if (newState == KSPWheelState.RETRACTING && oldState == KSPWheelState.DEPLOYED && wheel != null)
            {
                float comp = wheel.length - wheel.compressionDistance;
                deployedPosition = comp / wheel.length;
            }
            else { deployedPosition = 1f; }
        }
    }
}
