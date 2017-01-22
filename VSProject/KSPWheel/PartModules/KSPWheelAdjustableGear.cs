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
        public float minSuspensionLength = 0.1f;
        [KSPField]
        public float maxSuspensionLength = 0.5f;
        [KSPField]
        public float suspensionOffsetDistance = 0.175f;
        [KSPField]
        public bool allowFlip = false;

        [KSPField(guiName = "Strut Angle", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = -30f, maxValue = 30f, stepIncrement = 0.1f, suppressEditorShipModified = true)]
        public float strutRotation = 0f;
        
        [KSPField(guiName = "Strut Angle", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = -30f, maxValue = 30f, stepIncrement = 0.1f, suppressEditorShipModified = true)]
        public float wheelRotation = 0f;

        [KSPField(guiName = "Suspension Length", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0f, maxValue = 1f, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float suspensionLength = 0.5f;

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

        private CapsuleCollider standInCollider;

        private KSPWheelSuspension suspensionModule;

        private float mainAnimTime = 0f;

        private bool initialized = false;

        private void toggleWheelFlip(BaseField field, System.Object obj)
        {
            this.symmetryUpdate(m => 
            {
                m.flipWheel = flipWheel;                
                if (m.part.symMethod == SymmetryMethod.Mirror && m != this)
                {
                    m.flipWheel = !m.flipWheel;
                }
                float rot = m.flipWheel ? 180 : 0;
                if (m.carriageRotatorTransform != null)
                {
                    m.carriageRotatorTransform.localRotation = m.wheelRotatorDefaultRotation;
                    m.carriageRotatorTransform.Rotate(0, 0, rot, Space.Self);
                }
                if (m.rearDoorRotatorTransform != null)
                {
                    m.rearDoorRotatorTransform.localRotation = m.rearDoorRotatorDefaultRotation;
                    m.rearDoorRotatorTransform.Rotate(0, 0, rot, Space.Self);
                }
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
                        controller.wheelState = KSPWheelState.DEPLOYING;
                        break;
                    case KSPWheelState.RETRACTING:
                        controller.wheelState = KSPWheelState.DEPLOYING;
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
                        controller.wheelState = KSPWheelState.RETRACTING;
                        break;
                    case KSPWheelState.DEPLOYING:
                        controller.wheelState = KSPWheelState.RETRACTING;
                        break;
                    default:
                        break;
                }
            }
            updateStandInCollider(true);
        }

        [KSPEvent(guiName = "Toggle Gear", guiActive = true, guiActiveEditor = true)]
        public void deployEvent()
        {
            if (controller == null) { return; }//unpossible
            switch (controller.wheelState)
            {
                case KSPWheelState.RETRACTED:
                    controller.wheelState = KSPWheelState.DEPLOYING;
                    break;
                case KSPWheelState.RETRACTING:
                    controller.wheelState = KSPWheelState.DEPLOYING;
                    break;
                case KSPWheelState.DEPLOYED:
                    controller.wheelState = KSPWheelState.RETRACTING;
                    break;
                case KSPWheelState.DEPLOYING:
                    controller.wheelState = KSPWheelState.RETRACTING;
                    break;
                case KSPWheelState.BROKEN:
                    break;
                default:
                    break;
            }
            updateStandInCollider(true);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(strutRotation)].uiControlEditor.onFieldChanged = wheelAnglesUpdated;
            Fields[nameof(wheelRotation)].uiControlEditor.onFieldChanged = wheelAnglesUpdated;
            Fields[nameof(flipWheel)].uiControlEditor.onFieldChanged = toggleWheelFlip;
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
                setupInitialState();
            }
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            suspensionModule = part.GetComponent<KSPWheelSuspension>();
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameObject colObj = new GameObject("ALGStandInCollider");
                colObj.transform.parent = strutTransform;
                colObj.transform.position = wheel.transform.position;
                colObj.transform.localRotation = Quaternion.identity;
                standInCollider = colObj.AddComponent<CapsuleCollider>();
                standInCollider.radius = wheel.radius;
                standInCollider.height = wheel.radius * 2f + wheel.length;
                standInCollider.center = new Vector3(0, -standInCollider.height * 0.5f, 0);
                standInCollider.enabled = false;
                CollisionManager.IgnoreCollidersOnVessel(vessel, standInCollider);
            }
            updateStandInCollider(false);
        }

        private void setupInitialState()
        {
            bool deployed = false;
            bool broken = false;
            switch (controller.wheelState)
            {
                case KSPWheelState.RETRACTED:                    
                    break;
                case KSPWheelState.RETRACTING:
                    break;
                case KSPWheelState.DEPLOYED:
                    deployed = true;
                    break;
                case KSPWheelState.DEPLOYING:
                    deployed = true;
                    break;
                case KSPWheelState.BROKEN:
                    broken = true;
                    break;
                default:
                    break;
            }
            if (broken)
            {
                mainAnimTime = 1f;
                controller.wheelState = KSPWheelState.BROKEN;
            }
            else if (deployed)
            {
                mainAnimTime = 1f;
                controller.wheelState = KSPWheelState.DEPLOYED;
            }
            else//retracted
            {
                mainAnimTime = 0f;
                controller.wheelState = KSPWheelState.RETRACTED;
            }
            updateAnimation(0.0f);
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
            updateStandInCollider(false);
            if (suspensionModule != null)
            {                
                suspensionModule.suspensionOffset = (1f - Vector3.Dot(carriageTransform.up, strutTransform.up)) * -suspensionOffsetDistance;
            }
            if (wheel != null)
            {
                wheel.length = part.rescaleFactor * controller.scale * (minSuspensionLength + (suspensionLength * (maxSuspensionLength - minSuspensionLength)));
            }
        }

        private void updateStandInCollider(bool setup)
        {
            if (!HighLogic.LoadedSceneIsFlight || standInCollider == null) { return; }
            switch (controller.wheelState)
            {
                case KSPWheelState.RETRACTED:
                    standInCollider.enabled = false;
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
                    standInCollider.enabled = true;
                    break;
                case KSPWheelState.BROKEN:
                    standInCollider.enabled = false;
                    break;
                default:
                    break;
            }
        }

        private void updateAnimation(float speed)
        {
            mainAnimTime += Time.deltaTime * speed;
            float doorRot = 0f;
            float rearDoorRot = 0f;
            float strutRotX = 0f;
            float strutRotZ = 0f;
            float wheelRotX = 0f;
            float wheelRotZ = 0f;
            float wheelSecRot = 0f;
            if (mainAnimTime <= 0)//full retracted
            {
                doorRot = 0f;
                rearDoorRot = 0f;
                strutRotX = strutRotationRetracted;
                strutRotZ = 0f;
                wheelRotX = wheelRotationRetracted;
                wheelRotZ = 0f;
                wheelSecRot = wheelRotatorRotationRetracted;
                controller.wheelState = KSPWheelState.RETRACTED;
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
                strutRotX = Mathf.Lerp(strutRotationRetracted, 0, lerp * 0.5f);
                strutRotZ = 0f;
                wheelRotX = Mathf.Lerp(wheelRotationRetracted, 0, lerp * 0.5f);
                wheelRotZ = 0f;
                wheelSecRot = Mathf.Lerp(wheelRotatorRotationRetracted, 0, lerp * 0.5f);
            }
            else if (mainAnimTime < 0.85f)//main, secondary (0.50 - 0.85)
            {
                float lerp = (mainAnimTime - 0.50f) / 0.35f;
                rearDoorRot = 90;
                doorRot = 90;
                strutRotX = Mathf.Lerp(strutRotationRetracted, 0, lerp * 0.5f + 0.5f);
                strutRotZ = Mathf.Lerp(0, strutRotation, lerp);
                wheelRotX = Mathf.Lerp(wheelRotationRetracted, 0, lerp * 0.5f + 0.5f);
                wheelRotZ = Mathf.Lerp(0, wheelRotation, lerp);
                wheelSecRot = Mathf.Lerp(wheelRotatorRotationRetracted, 0, lerp * 0.5f + 0.5f);
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
            }
            else //if (mainAnimTime >= 1f) // fully deployed
            {
                doorRot = 0;
                rearDoorRot = 90;
                strutRotX = 0f;
                strutRotZ = strutRotation;
                wheelRotX = 0f;
                wheelRotZ = wheelRotation;
                wheelSecRot = 0f;
                controller.wheelState = KSPWheelState.DEPLOYED;
                //TODO door rotation set
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
        }

    }
}
