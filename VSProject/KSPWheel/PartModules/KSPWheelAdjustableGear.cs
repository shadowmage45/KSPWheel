using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelAdjustableGear : KSPWheelSubmodule
    {

        [KSPField]
        public string strutRotatorName = string.Empty;
        [KSPField]
        public string strutName = string.Empty;
        [KSPField]
        public string wheelName = string.Empty;
        [KSPField]
        public string wheelRotatorName = string.Empty;
        [KSPField]
        public string leftDoorName = string.Empty;
        [KSPField]
        public string rightDoorName = string.Empty;
        [KSPField]
        public string rearDoorName = string.Empty;
        [KSPField]
        public string rearDoorRotatorName = string.Empty;
        [KSPField]
        public float strutRotationMax = 30f;
        [KSPField]
        public float wheelRotationMax = 30f;
        [KSPField]
        public float strutRotationRetracted = 88f;
        [KSPField]
        public float wheelRotationRetracted = 95f;
        [KSPField]
        public float wheelRotatorRotationRetracted = 0f;
        [KSPField]
        public float suspensionAdjustmentRange = 0.3f;
        [KSPField]
        public float suspensionOffsetDistance = 0.175f;
        [KSPField]
        public bool allowFlip = false;

        [KSPField(guiName = "Strut Angle", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = -30f, maxValue = 30f, stepIncrement = 0.1f, suppressEditorShipModified = true)]
        public float strutRotation = 0f;
        
        [KSPField(guiName = "Wheel Angle", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = -30f, maxValue = 30f, stepIncrement = 0.1f, suppressEditorShipModified = true)]
        public float wheelRotation = 0f;

        [KSPField(guiName = "Suspension Length", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0f, maxValue = 1f, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float suspensionLength = 0.25f;

        [KSPField(guiName = "Flip Wheel", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Left", disabledText = "Right")]
        public bool flipWheel = false;

        [KSPField(isPersistant = true)]
        public int animationState = 0;

        public Transform strutRotatorTransform;
        public Transform strutTransform;
        public Transform carriageTransform;
        public Transform carriageRotatorTransform;
        public Transform leftDoorTransform;
        public Transform rightDoorTransform;
        public Transform rearDoorTransform;
        public Transform rearDoorRotatorTransform;
        public Quaternion strutRotatorDefaultRotation;
        public Quaternion strutDefaultRotation;
        public Quaternion wheelDefaultRotation;
        public Quaternion wheelRotatorDefaultRotation;
        public Quaternion rightDoorDefaultRotation;
        public Quaternion leftDoorDefaultRotation;
        public Quaternion rearDoorDefaultRotation;
        public Quaternion rearDoorRotatorDefaultRotation;
        public Vector3 wheelDefaultPos;

        private CapsuleCollider standInCollider;

        private KSPWheelSuspension suspensionModule;

        private float mainAnimTime = 0f;

        private bool initialized = false;

        private void suspensionLengthUpdated(BaseField field, System.Object obj)
        {
            this.symmetryUpdate(m =>
            {
                m.suspensionLength = suspensionLength;
                m.updateSuspensionLength();
            });
        }

        private void toggleWheelFlip(BaseField field, System.Object obj)
        {
            this.symmetryUpdate(m => 
            {
                m.flipWheel = flipWheel;                
                if (m.part.symMethod == SymmetryMethod.Mirror && m != this)
                {
                    m.flipWheel = !m.flipWheel;
                }
                m.setupFlippedState();
            });
        }

        private void wheelAnglesUpdated(BaseField field, System.Object obj)
        {
            this.symmetryUpdate(m => 
            {
                m.strutRotation = strutRotation;
                m.wheelRotation = wheelRotation;
                if (m.part.symMethod == SymmetryMethod.Mirror && m != this)
                {
                    m.strutRotation = -m.strutRotation;
                    if (!m.allowFlip || m.carriageRotatorTransform == null)
                    {
                        m.wheelRotation = -m.wheelRotation;
                    }
                }
                if (m.controller.wheelState == KSPWheelState.DEPLOYED)
                {
                    m.strutRotatorTransform.localRotation = m.strutRotatorDefaultRotation;
                    m.carriageTransform.localRotation = m.wheelDefaultRotation;
                    m.strutRotatorTransform.Rotate(0, 0, m.strutRotation, Space.Self);
                    m.carriageTransform.Rotate(0, 0, m.wheelRotation, Space.Self);
                }
            });
        }

        [KSPAction(actionGroup = KSPActionGroup.Gear, guiName = "Toggle Gear", requireFullControl = false)]
        public void deployAction(KSPActionParam param)
        {
            if (controller == null) { return; }//unpossible
            if (param.type == KSPActionType.Activate)
            {
                switch (controller.wheelState)
                {
                    case KSPWheelState.RETRACTED:
                        changeWheelState(KSPWheelState.DEPLOYING);
                        break;
                    case KSPWheelState.RETRACTING:
                        changeWheelState(KSPWheelState.DEPLOYING);
                        break;
                    default:
                        break;
                }
            }
            else//if param.type==KSPActionType.Deactivate
            {
                switch (controller.wheelState)
                {
                    case KSPWheelState.DEPLOYED:
                        changeWheelState(KSPWheelState.RETRACTING);
                        break;
                    case KSPWheelState.DEPLOYING:
                        changeWheelState(KSPWheelState.RETRACTING);
                        break;
                    default:
                        break;
                }
            }
            updateDeploymentState(true);
        }

        [KSPEvent(guiName = "Toggle Gear", guiActive = true, guiActiveEditor = true)]
        public void deployEvent()
        {
            if (controller == null) { return; }//unpossible
            switch (controller.wheelState)
            {
                case KSPWheelState.RETRACTED:
                    changeWheelState(KSPWheelState.DEPLOYING);
                    break;
                case KSPWheelState.RETRACTING:
                    changeWheelState(KSPWheelState.DEPLOYING);
                    break;
                case KSPWheelState.DEPLOYED:
                    changeWheelState(KSPWheelState.RETRACTING);
                    break;
                case KSPWheelState.DEPLOYING:
                    changeWheelState(KSPWheelState.RETRACTING);
                    break;
                case KSPWheelState.BROKEN:
                    break;
                default:
                    break;
            }
            updateDeploymentState(true);
        }

        [KSPEvent(guiName = "Align Wheel To Ground", guiActiveEditor = true, guiActive = false)]
        public void alignToGround()
        {
            this.symmetryUpdate(m =>
            {
                Vector3 target = Vector3.up + m.carriageTransform.position;//one unit above the transform, in world-space in the editor
                Vector3 localTarget = m.carriageTransform.InverseTransformPoint(target);//one unit above the transform, as seen in local space
                //rotating around the local Z axis, so we only care about the x and y offsets
                //erm.. feed this into Mathf.Atan2 as a slope, to get the returned angle
                float angle = -Mathf.Atan2(localTarget.x, localTarget.y) * Mathf.Rad2Deg;
                m.wheelRotation = Mathf.Clamp(m.wheelRotation + angle, -m.wheelRotationMax, m.wheelRotationMax);//clamp it to the current wheel angle limits
                m.carriageTransform.localRotation = m.wheelDefaultRotation;
                m.carriageTransform.Rotate(0, 0, m.wheelRotation, Space.Self);
            });
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(strutRotation)].uiControlEditor.onFieldChanged = wheelAnglesUpdated;
            Fields[nameof(wheelRotation)].uiControlEditor.onFieldChanged = wheelAnglesUpdated;
            Fields[nameof(flipWheel)].uiControlEditor.onFieldChanged = toggleWheelFlip;
            Fields[nameof(suspensionLength)].uiControlEditor.onFieldChanged = suspensionLengthUpdated;
            UI_FloatRange wdgt = (UI_FloatRange)Fields[nameof(strutRotation)].uiControlEditor;
            if (wdgt != null)
            {
                wdgt.minValue = -strutRotationMax;
                wdgt.maxValue = strutRotationMax;
            }
            wdgt = (UI_FloatRange)Fields[nameof(wheelRotation)].uiControlEditor;
            if (wdgt != null)
            {
                wdgt.minValue = -wheelRotationMax;
                wdgt.maxValue = wheelRotationMax;                
            }
            setupCloneState();
            setupFlippedState();
        }

        private void setupCloneState()
        {
            if (part.symmetryCounterparts != null && part.symmetryCounterparts.Count > 0)
            {
                KSPWheelAdjustableGear g = part.symmetryCounterparts[0].GetComponent<KSPWheelAdjustableGear>();
                flipWheel = !g.flipWheel;
                strutRotation = -g.strutRotation;
                if (!allowFlip || carriageRotatorTransform == null)//when flipping is allowed, wheel rotation handled by the carriage transform rotation
                {
                    wheelRotation = -g.wheelRotation;
                }
            }
        }

        private void setupFlippedState()
        {
            float rot = flipWheel ? 180 : 0;
            if (carriageRotatorTransform != null)
            {
                carriageRotatorTransform.localRotation = wheelRotatorDefaultRotation;
                carriageRotatorTransform.Rotate(0, 0, rot, Space.Self);
            }
            if (rearDoorRotatorTransform != null)
            {
                rearDoorRotatorTransform.localRotation = rearDoorRotatorDefaultRotation;
                rearDoorRotatorTransform.Rotate(0, 0, rot, Space.Self);
            }
            if (controller.wheelState == KSPWheelState.DEPLOYED)
            {
                strutRotatorTransform.localRotation = strutRotatorDefaultRotation;
                carriageTransform.localRotation = wheelDefaultRotation;
                strutRotatorTransform.Rotate(0, 0, strutRotation, Space.Self);
                carriageTransform.Rotate(0, 0, wheelRotation, Space.Self);
            }
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                initialized = true;
                if (strutTransform == null)
                {
                    strutRotatorTransform = part.transform.FindRecursive(strutRotatorName);
                    strutTransform = part.transform.FindRecursive(strutName);
                    carriageTransform = part.transform.FindRecursive(wheelName);
                    strutRotatorDefaultRotation = strutRotatorTransform.localRotation;
                    strutDefaultRotation = strutTransform.localRotation;
                    wheelDefaultRotation = carriageTransform.localRotation;
                    if (!string.IsNullOrEmpty(wheelRotatorName))
                    {
                        carriageRotatorTransform = part.transform.FindRecursive(wheelRotatorName);
                        wheelRotatorDefaultRotation = carriageRotatorTransform.localRotation;
                    }
                    leftDoorTransform = part.transform.FindRecursive(leftDoorName);
                    rightDoorTransform = part.transform.FindRecursive(rightDoorName);
                    rearDoorTransform = part.transform.FindRecursive(rearDoorName);
                    leftDoorDefaultRotation = leftDoorTransform.localRotation;
                    rightDoorDefaultRotation = rightDoorTransform.localRotation;
                    rearDoorDefaultRotation = rearDoorTransform.localRotation;
                    if (!string.IsNullOrEmpty(rearDoorRotatorName))
                    {
                        rearDoorRotatorTransform = part.transform.FindRecursive(rearDoorRotatorName);
                        rearDoorRotatorDefaultRotation = rearDoorRotatorTransform.localRotation;
                    }
                }
                if (controller.wheelState == KSPWheelState.BROKEN)
                {
                    mainAnimTime = 1f;
                }
                else if (controller.wheelState == KSPWheelState.DEPLOYED || controller.wheelState == KSPWheelState.DEPLOYING)
                {
                    mainAnimTime = 1f;
                }
                else//retracted
                {
                    mainAnimTime = 0f;
                }
                updateAnimation(0.0f);
            }
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            suspensionModule = part.GetComponent<KSPWheelSuspension>();
            if (HighLogic.LoadedSceneIsFlight)
            {
                Quaternion rot = strutTransform.localRotation;
                strutTransform.localRotation = strutDefaultRotation;
                GameObject colObj = new GameObject("ALGStandInCollider");
                colObj.transform.parent = strutTransform;
                colObj.transform.position = wheel.transform.position;
                colObj.transform.localPosition += new Vector3(0, -suspensionLength * suspensionAdjustmentRange, 0);
                colObj.transform.localRotation = Quaternion.identity;
                standInCollider = colObj.AddComponent<CapsuleCollider>();
                standInCollider.radius = wheel.radius;
                standInCollider.height = wheel.radius * 2f + wheel.length;
                standInCollider.center = new Vector3(0, -standInCollider.height * 0.5f, 0);
                standInCollider.enabled = false;
                CollisionManager.IgnoreCollidersOnVessel(vessel, standInCollider);
                strutTransform.localRotation = rot;
            }
            updateDeploymentState(false);
            wheelDefaultPos = wheelTransform.localPosition;//TODO -- this will have problems when cloned in the editor; the position will have already been moved
            updateSuspensionLength();
        }

        public void Update()
        {
            if (!initialized || controller == null) { return; }
            KSPWheelState state = controller.wheelState;
            switch (state)
            {
                case KSPWheelState.RETRACTING:
                    updateAnimation(-0.25f);
                    break;
                case KSPWheelState.DEPLOYING:
                    updateAnimation(0.25f);
                    break;
                default:
                    break;
            }
            updateDeploymentState(false);
        }

        private void updateSuspensionLength()
        {
            if (suspensionModule != null)
            {
                float offset = (1f - Vector3.Dot(carriageTransform.up, strutTransform.up)) * -suspensionOffsetDistance;
                offset += suspensionLength * suspensionAdjustmentRange;
                suspensionModule.suspensionOffset = offset;
            }
            if (wheelTransform != null)
            {
                wheelTransform.localPosition = wheelDefaultPos;
                wheelTransform.position += -wheelTransform.up * suspensionLength * suspensionAdjustmentRange * part.rescaleFactor * controller.scale;
                if (wheelData != null && wheelData.bumpStopCollider != null)
                {
                    wheelData.bumpStopCollider.transform.position = wheelTransform.position;
                }
            }
        }

        private void updateDeploymentState(bool setup)
        {
            if (!HighLogic.LoadedSceneIsFlight || standInCollider == null) { return; }
            switch (controller.wheelState)
            {
                case KSPWheelState.RETRACTED:
                    standInCollider.enabled = false;
                    wheel.angularVelocity = 0f;
                    break;
                case KSPWheelState.RETRACTING:
                    if (setup)
                    {
                        standInCollider.radius = wheel.radius;
                        float h = wheel.radius * 2 + (wheel.length - wheel.compressionDistance);
                        standInCollider.height = h;
                        float o = -h * 0.5f + wheel.radius;
                        standInCollider.center = new Vector3(0, o, 0);
                    }
                    wheel.angularVelocity = 0f;
                    standInCollider.enabled = true;
                    break;
                case KSPWheelState.DEPLOYED:
                    standInCollider.enabled = false;
                    break;
                case KSPWheelState.DEPLOYING:
                    if (setup)
                    {
                        standInCollider.radius = wheel.radius;
                        standInCollider.height = wheel.radius * 2f + wheel.length;
                        standInCollider.center = new Vector3(0, -standInCollider.height * 0.5f + wheel.radius, 0);
                    }
                    wheel.angularVelocity = 0f;
                    standInCollider.enabled = true;
                    break;
                case KSPWheelState.BROKEN:
                    wheel.angularVelocity = 0f;
                    standInCollider.enabled = false;
                    break;
                default:
                    break;
            }
        }

        private void updateAnimation(float speed)
        {
            mainAnimTime += Time.deltaTime * speed;
            mainAnimTime = Mathf.Clamp01(mainAnimTime);
            controller.deployAnimationTime = mainAnimTime;
            float doorRot = 0f;
            float rearDoorRot = 0f;
            float strutRotX = 0f;
            float strutRotZ = 0f;
            float wheelRotX = 0f;
            float wheelRotZ = 0f;
            float wheelSecRot = 0f;
            float offsetTime = 0f;
            if (mainAnimTime <= 0)//full retracted
            {
                doorRot = 0f;
                rearDoorRot = 0f;
                strutRotX = strutRotationRetracted;
                strutRotZ = 0f;
                wheelRotX = wheelRotationRetracted;
                wheelRotZ = 0f;
                wheelSecRot = wheelRotatorRotationRetracted;
                changeWheelState(KSPWheelState.RETRACTED);
            }
            else if (mainAnimTime < 0.15f)//doors only (0 - 0.15)
            {
                float lerp = mainAnimTime / 0.15f;
                doorRot = Mathf.Lerp(0, 90, lerp);
                rearDoorRot = Mathf.Lerp(0, 90, lerp);
                strutRotX = strutRotationRetracted;
                strutRotZ = 0f;
                wheelRotX = wheelRotationRetracted;
                wheelRotZ = 0f;
                wheelSecRot = wheelRotatorRotationRetracted;
            }
            else if (mainAnimTime < 0.50f)//main only (0.15 - 0.50)
            {
                float lerp = (mainAnimTime - 0.15f) / 0.35f;
                doorRot = 90;
                rearDoorRot = 90;
                strutRotX = Mathf.Lerp(strutRotationRetracted, 0, lerp);
                strutRotZ = 0f;
                wheelRotX = Mathf.Lerp(wheelRotationRetracted, 0, lerp);
                wheelRotZ = 0f;
                wheelSecRot = wheelRotatorRotationRetracted;
            }
            else if (mainAnimTime < 0.85f)//main, secondary (0.50 - 0.85)
            {
                float lerp = (mainAnimTime - 0.50f) / 0.35f;
                rearDoorRot = 90;
                doorRot = 90;
                strutRotX = 0;
                strutRotZ = Mathf.Lerp(0, strutRotation, lerp);
                wheelRotX = 0;
                wheelRotZ = Mathf.Lerp(0, wheelRotation, lerp);
                wheelSecRot = Mathf.Lerp(wheelRotatorRotationRetracted, 0, lerp);
            }
            else if (mainAnimTime < 1f)//doors2 (0.85 - 1)
            {
                float lerp = (mainAnimTime - 0.85f) / 0.15f;
                rearDoorRot = 90;
                doorRot = Mathf.Lerp(90, 0, lerp);
                strutRotX = 0f;
                strutRotZ = strutRotation;
                wheelRotX = 0f;
                wheelRotZ = wheelRotation;
                wheelSecRot = 0f;
                offsetTime = lerp;
            }
            else //if (mainAnimTime >= 1f) // fully deployed
            {
                offsetTime = 1f;
                doorRot = 0;
                rearDoorRot = 90;
                strutRotX = 0f;
                strutRotZ = strutRotation;
                wheelRotX = 0f;
                wheelRotZ = wheelRotation;
                wheelSecRot = 0f;
                changeWheelState(KSPWheelState.DEPLOYED);
            }

            strutRotatorTransform.localRotation = strutRotatorDefaultRotation;
            strutTransform.localRotation = strutDefaultRotation;
            carriageTransform.localRotation = wheelDefaultRotation;
            rightDoorTransform.localRotation = rightDoorDefaultRotation;
            leftDoorTransform.localRotation = leftDoorDefaultRotation;
            rearDoorTransform.localRotation = rearDoorDefaultRotation;
            strutRotatorTransform.Rotate(0, 0, strutRotZ, Space.Self);
            strutTransform.Rotate(strutRotX, 0, 0, Space.Self);
            carriageTransform.Rotate(0, 0, wheelRotZ, Space.Self);
            carriageTransform.Rotate(wheelRotX, 0, 0, Space.Self);
            rightDoorTransform.Rotate(doorRot, 0, 0, Space.Self);
            leftDoorTransform.Rotate(doorRot, 0, 0, Space.Self);
            rearDoorTransform.Rotate(rearDoorRot, 0, 0, Space.Self);
            if (carriageRotatorTransform != null)
            {
                carriageRotatorTransform.localRotation = wheelRotatorDefaultRotation;
                if (flipWheel)
                {
                    wheelSecRot = -wheelSecRot;
                    wheelSecRot += 180f;
                }
                carriageRotatorTransform.Rotate(0, 0, wheelSecRot, Space.Self);
            }
            if (rearDoorRotatorTransform != null && flipWheel)
            {
                rearDoorRotatorTransform.localRotation = rearDoorRotatorDefaultRotation;
                rearDoorRotatorTransform.Rotate(0, 0, 180, Space.Self);
            }
            if (suspensionModule != null)
            {
                float offset = (1f - Vector3.Dot(carriageTransform.up, strutTransform.up)) * -suspensionOffsetDistance;
                offset += suspensionLength * suspensionAdjustmentRange;
                suspensionModule.suspensionOffset = offset * offsetTime;
            }
        }

    }
}
