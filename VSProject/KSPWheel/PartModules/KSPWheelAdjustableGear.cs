using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelAdjustableGear : KSPWheelSubmodule
    {

        #region REGION - Standard Part Config File Fields

        [KSPField]
        public string suspensionContainer1Name = string.Empty;

        [KSPField]
        public string suspensionContainer2Name = string.Empty;

        [KSPField]
        public string suspensionTargetName = string.Empty;

        [KSPField]
        public string suspensionRotatorName = string.Empty;

        [KSPField]
        public string wheelContainerName = string.Empty;

        [KSPField]
        public string wheelMeshName = string.Empty;

        [KSPField]
        public string rearDoorFlipName = string.Empty;

        [KSPField]
        public string rearDoorName = string.Empty;

        [KSPField]
        public string rightDoorName = string.Empty;

        [KSPField]
        public string leftDoorName = string.Empty;

        [KSPField]
        public string deployEffect = "DeployEffect";

        [KSPField]
        public string deployedEffect = "DeployedEffect";

        [KSPField]
        public string retractEffect = "RetractEffect";

        [KSPField]
        public string retractedEffect = "RetractedEffect";

        [KSPField]
        public bool allowFlip = false;

        [KSPField]
        public bool sideMode = false;

        /// <summary>
        /// User-selectable strut-extension.  Makes the entire gear 'longer'.
        /// </summary>
        [KSPField]
        public float maxExtension = 0.5f;

        /// <summary>
        /// User-selectable strut angle min/max (negative value used for min)
        /// </summary>
        [KSPField]
        public float minStrutAngle = -30f;

        /// <summary>
        /// User-selectable strut angle min/max (negative value used for min)
        /// </summary>
        [KSPField]
        public float maxStrutAngle = 30f;

        /// <summary>
        /// User-selectable maximum wheel angle.  Minimum = 0
        /// </summary>
        [KSPField]
        public float minWheelAngle = -60f;

        /// <summary>
        /// User-selectable maximum wheel angle.  Minimum = 0
        /// </summary>
        [KSPField]
        public float maxWheelAngle = 60f;

        [KSPField]
        public float maxSteeringAngle = 25f;

        /// <summary>
        /// Angular rotation of the main strut during the retract animation;  this is around the horizontal (X) axis, and rotates the main strut upwards into the housing.
        /// </summary>
        [KSPField]
        public float mainStrutRetractedAngle = 90f;

        /// <summary>
        /// Angular rotation of the secondary strut during retract animation; this is around the vertical (Y) axis, and rotates the wheel into the housing.
        /// </summary>
        [KSPField]
        public float secStrutRetractedAngle = 90f;

        /// <summary>
        /// Angular rotation of the wheel bogey during retract animation.  TODO
        /// </summary>
        [KSPField]
        public float wheelBogeyRetractedAngle = 0f;

        /// <summary>
        /// User-configured main strut angle in editor
        /// </summary>
        [KSPField(guiName = "Strut Angle", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = -30f, maxValue = 30f, stepIncrement = 0.1f, suppressEditorShipModified = true)]
        public float strutRotation = 0f;

        /// <summary>
        /// User-set secondary angle for wheel container.  Determines the axis along which the suspension operates.
        /// </summary>
        [KSPField(guiName = "Wheel Angle", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0f, maxValue = 60f, stepIncrement = 0.1f, suppressEditorShipModified = true)]
        public float wheelRotation = 0f;

        /// <summary>
        /// User-set strut extension value.  Makes the landing leg longer or shorter.  Does not effect suspension travel range.
        /// </summary>
        [KSPField(guiName = "Strut Extension", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0f, maxValue = 1, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float strutExtension = 0f;

        /// <summary>
        /// Temporary testing compression value -- TODO remove once module is finished being developed
        /// </summary>
        [KSPField(guiName = "Comp Test", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0f, maxValue = 1f, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float compTest = 1f;

        /// <summary>
        /// Current animation state/time.  Stored independently of animation state (which is stored in the base module)
        /// </summary>
        [KSPField(isPersistant = true)]
        public float animationTime = 1f;

        /// <summary>
        /// The speed of the animation. 1 = 1 second.  0.25 = 4 seconds. 0 = non-animated (infinite animation time). Lower values decrease playback speed; higher values increase it.
        /// </summary>
        [KSPField]
        public float animationSpeed = 0.25f;

        /// <summary>
        /// Has user selected flipped wheel or set automatically from cloned state.  Determines wheel angle offset direction, wheel spin direction, door opening directions, others...
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isFlipped = false;

        #endregion ENDREGION - Standard Part Config File Fields

        #region REGION - Private Working Variables

        /// <summary>
        /// Cached transforms for manipulation of the model
        /// </summary>
        private Transform suspensionContainer1;
        private Transform suspensionContainer2;
        private Transform suspensionTarget;
        private Transform suspensionRotator;
        private Transform wheelContainer;
        private Transform wheelMesh;

        private Transform leftDoor;
        private Transform rightDoor;
        private Transform rearDoor;
        private Transform rearDoorFlip;

        [SerializeField]
        private SphereCollider tempCollider;

        private KSPWheelSteering steering;
        

        /// <summary>
        /// Cached default orientations and locations for the above transforms
        /// Serialize these fields across parts (prefab -> editor; editor -> cloned)
        /// Fixes problems of cloned models taking on new default orientations/locations from the part they were cloned from
        /// </summary>
        [SerializeField]
        private bool initializedDefaultRotations;
        [SerializeField]
        private Quaternion sc1DefaultRotation;
        [SerializeField]
        private Quaternion sc2DefaultRotation;
        [SerializeField]
        private Quaternion wheelContainerDefaultRotation;
        [SerializeField]
        private Vector3 wheelContainerDefaultPosition;

        [SerializeField]
        private Quaternion leftDoorDefaultRotation;
        [SerializeField]
        private Quaternion rightDoorDefaultRotation;
        [SerializeField]
        private Quaternion rearDoorDefaultRotation;
        [SerializeField]
        private Quaternion rearDoorFlipDefaultRotation;

        #endregion ENDREGION - Private Working Variables

        #region REGION - GUI methods

        [KSPAction(actionGroup = KSPActionGroup.Gear, guiName = "Toggle Gear", requireFullControl = false)]
        public void deployAction(KSPActionParam param)
        {
            if (param.type == KSPActionType.Activate)
            {
                switch (controller.wheelState)
                {
                    case KSPWheelState.RETRACTED:
                        changeWheelState(KSPWheelState.DEPLOYING);
                        part.Effect(deployEffect, 1f);
                        break;
                    case KSPWheelState.RETRACTING:
                        changeWheelState(KSPWheelState.DEPLOYING);
                        part.Effect(deployEffect, 1f);
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
                        part.Effect(retractEffect, 1f);
                        break;
                    case KSPWheelState.DEPLOYING:
                        changeWheelState(KSPWheelState.RETRACTING);
                        part.Effect(retractEffect, 1f);
                        break;
                    default:
                        break;
                }
            }
        }

        [KSPEvent(guiName = "Toggle Gear", guiActive = true, guiActiveEditor = true)]
        public void deploy()
        {
            this.symmetryUpdate(m =>
            {
                switch (m.controller.wheelState)
                {
                    case KSPWheelState.RETRACTED:
                        m.changeWheelState(KSPWheelState.DEPLOYING);
                        part.Effect(deployEffect, 1f);
                        break;
                    case KSPWheelState.RETRACTING:
                        m.changeWheelState(KSPWheelState.DEPLOYING);
                        part.Effect(deployEffect, 1f);
                        break;
                    case KSPWheelState.DEPLOYED:
                        m.changeWheelState(KSPWheelState.RETRACTING);
                        part.Effect(retractEffect, 1f);
                        break;
                    case KSPWheelState.DEPLOYING:
                        m.changeWheelState(KSPWheelState.RETRACTING);
                        part.Effect(retractEffect, 1f);
                        break;
                    case KSPWheelState.BROKEN:
                        break;
                    default:
                        break;
                }
            });
        }

        [KSPEvent(guiName = "Flip Gear", guiActive = false, guiActiveEditor = true)]
        public void flip()
        {
            if (!allowFlip) { return; }
            isFlipped = !isFlipped;
            this.symmetryUpdate(m =>
            {
                if (m != this)
                {
                    m.isFlipped = !this.isFlipped;
                }
            });
        }

        [KSPEvent(guiName = "Align Wheel To Ground", guiActive = false, guiActiveEditor = true)]
        public void alignToGround()
        {
            Vector3 target = wheelContainer.position + Vector3.up;//one unit above the transform, in world-space in the editor
            Vector3 localTarget = wheelContainer.InverseTransformPoint(target);//one unit above the transform, as seen in local space
            //rotating around the local Z axis, so we only care about the x and y offsets
            //erm.. feed this into Mathf.Atan2 as a slope, to get the returned angle
            float angle = -Mathf.Atan2(localTarget.x, localTarget.y) * Mathf.Rad2Deg;
            if (isFlipped && sideMode) { angle = -angle; }
            wheelRotation = Mathf.Clamp(wheelRotation + angle, 0, maxWheelAngle);//clamp it to the current wheel angle limits
            this.symmetryUpdate(m =>
            {
                m.wheelRotation = wheelRotation;
            });
        }

        #endregion ENDREGION - GUI Methods

        #region REGION - Standard KSP/Unity Overrides

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            locateTransforms();
            Events[nameof(flip)].guiActiveEditor = allowFlip;
            this.updateUIFloatRangeControl(nameof(strutRotation), strutRotation, minStrutAngle, maxStrutAngle, 0.5f);
            this.updateUIFloatRangeControl(nameof(wheelRotation), wheelRotation, minWheelAngle, maxWheelAngle, 0.5f);
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            if (part.symmetryCounterparts != null && part.symmetryCounterparts.Count > 0)
            {
                //this must be a clone, or part is being reloaded
                //the 'symmetry counterpart' that exists should be the original part
                this.isFlipped = !part.symmetryCounterparts[0].GetComponent<KSPWheelAdjustableGear>().isFlipped;
            }
            if (HighLogic.LoadedSceneIsFlight && tempCollider == null)
            {
                tempCollider = new GameObject("StandInCollider").AddComponent<SphereCollider>();
                tempCollider.radius = wheel.radius;
                tempCollider.gameObject.layer = 26;
                tempCollider.transform.parent = suspensionTarget;
                tempCollider.transform.position = wheel.transform.position;
                tempCollider.gameObject.SetActive(HighLogic.LoadedSceneIsFlight && (controller.wheelState != KSPWheelState.DEPLOYED && controller.wheelState != KSPWheelState.BROKEN));
                CollisionManager.IgnoreCollidersOnVessel(vessel, tempCollider);
            }
            steering = part.GetComponent<KSPWheelSteering>();
            updateAnimation(0f);//force update the animation based on current time
        }

        public void Update()
        {
            base.preWheelFrameUpdate();
            float animTime = 0f;
            if (controller.wheelState == KSPWheelState.DEPLOYING)
            {
                animTime = Time.deltaTime * animationSpeed;
                part.Effect(retractEffect, 0f);
            }
            else if (controller.wheelState == KSPWheelState.RETRACTING)
            {
                animTime = Time.deltaTime * -animationSpeed;
                part.Effect(deployEffect, 0f);
            }
            else if (controller.wheelState == KSPWheelState.DEPLOYED)
            {
                wheelMesh.Rotate(wheel.perFrameRotation, 0, 0, Space.Self);
                part.Effect(deployEffect, 0f);
                part.Effect(retractEffect, 0f);
            }
            else
            {
                part.Effect(deployEffect, 0f);
                part.Effect(retractEffect, 0f);
            }
            updateAnimation(animTime);
            if (HighLogic.LoadedSceneIsFlight)
            {
                tempCollider.gameObject.SetActive(controller.wheelState == KSPWheelState.RETRACTING || controller.wheelState == KSPWheelState.DEPLOYING);
                if (steering != null)
                {
                    steering.maxSteeringAngle = Mathf.Abs(wheelRotation) < 0.25f ? maxSteeringAngle : 0;
                }
            }
        }

        #endregion ENDREGION - Standard KSP/Unity Overrides

        #region REGION - Custom Update Methods

        /// <summary>
        /// Locates all of the relevant transforms for this module, and sets up their default orientation/location cached values
        /// </summary>
        private void locateTransforms()
        {
            suspensionContainer1 = part.transform.FindRecursive(suspensionContainer1Name);
            suspensionContainer2 = part.transform.FindRecursive(suspensionContainer2Name);
            suspensionTarget = part.transform.FindRecursive(suspensionTargetName);
            suspensionRotator = part.transform.FindRecursive(suspensionRotatorName);
            wheelContainer = part.transform.FindRecursive(wheelContainerName);
            wheelMesh = part.transform.FindRecursive(wheelMeshName);

            leftDoor = part.transform.FindRecursive(leftDoorName);
            rightDoor = part.transform.FindRecursive(rightDoorName);
            rearDoor = part.transform.FindRecursive(rearDoorName);
            rearDoorFlip = part.transform.FindRecursive(rearDoorFlipName);

            if (!initializedDefaultRotations)
            {
                initializedDefaultRotations = true;
                sc1DefaultRotation = suspensionContainer1.localRotation;
                sc2DefaultRotation = suspensionContainer2.localRotation;
                wheelContainerDefaultRotation = wheelContainer.localRotation;
                wheelContainerDefaultPosition = wheelContainer.localPosition;

                leftDoorDefaultRotation = leftDoor.localRotation;
                rightDoorDefaultRotation = rightDoor.localRotation;
                rearDoorDefaultRotation = rearDoor.localRotation;
                rearDoorFlipDefaultRotation = rearDoorFlip.localRotation;
            }
        }

        private void updateAnimation(float dt)
        {
            animationTime += dt;
            float lrp = 0f;
            bool deployed = animationTime >= 1f;

            float mainStrutRot = 0f;
            float secStrutRot = 0f;
            float strutAngleRot = 0f;
            float wheelAngleRot = 0f;
            float doorLeftRot = 0f;
            float doorRightRot = 0f;
            float doorRearRot = 0f;
            float doorFlipRot = isFlipped && allowFlip ? 180f : 0f;
            float susTargetPos = 0f;
            float bogeyAngleRot = 0f;
            if (animationTime <= 0)//fully retracted, everything in retracted state
            {
                animationTime = 0f;
                if (controller.wheelState != KSPWheelState.RETRACTED)
                {
                    changeWheelState(KSPWheelState.RETRACTED);
                    part.Effect(retractedEffect);
                }
                mainStrutRot = mainStrutRetractedAngle;
                secStrutRot = secStrutRetractedAngle;
                bogeyAngleRot = wheelBogeyRetractedAngle;
                strutAngleRot = 0f;
                wheelAngleRot = 0f;
                doorLeftRot = 0f;
                doorRightRot = 0f;
                doorRearRot = 0f;
                susTargetPos = 0f;
            }
            else if (animationTime < 0.15f)//open doors
            {
                lrp = lerp(animationTime, 0, 0.15f);
                mainStrutRot = mainStrutRetractedAngle;
                secStrutRot = secStrutRetractedAngle;
                bogeyAngleRot = wheelBogeyRetractedAngle;
                strutAngleRot = 0f;
                wheelAngleRot = 0f;
                doorLeftRot = lrp * 90f;
                doorRightRot = lrp * 90f;
                doorRearRot = lrp * 90f;
                susTargetPos = 0f;
            }
            else if (animationTime < 0.4f)//main deploy animation for main strut and wheel rotation
            {
                lrp = lerp(animationTime, 0.15f, 0.4f);
                mainStrutRot = (1f - lrp) * mainStrutRetractedAngle;
                secStrutRot = secStrutRetractedAngle;
                bogeyAngleRot = wheelBogeyRetractedAngle;
                strutAngleRot = 0f;
                wheelAngleRot = 0f;
                doorLeftRot = 90f;
                doorRightRot = 90f;
                doorRearRot = 90f;
                susTargetPos = 0f;
            }
            else if (animationTime < 0.6f)//main deploy animation for main strut and wheel rotation
            {
                lrp = lerp(animationTime, 0.4f, 0.6f);
                mainStrutRot = 0;
                secStrutRot = (1f - lrp) * secStrutRetractedAngle;
                bogeyAngleRot = (1f - lrp) * wheelBogeyRetractedAngle;
                strutAngleRot = 0f;
                wheelAngleRot = 0f;
                doorLeftRot = 90f;
                doorRightRot = 90f;
                doorRearRot = 90f;
                susTargetPos = 0f;
            }
            else if (animationTime < 0.85f)//lerp into user-configured positions
            {
                lrp = lerp(animationTime, 0.6f, 0.85f);
                mainStrutRot = 0f;
                secStrutRot = 0f;
                bogeyAngleRot = 0f;
                strutAngleRot = strutRotation * lrp;
                wheelAngleRot = wheelRotation * lrp;
                doorLeftRot = (1 - lrp) * 90f;
                doorRightRot = (1 - lrp) * 90f;
                doorRearRot = 90f + (allowFlip ? lrp * Mathf.Max(0, -strutRotation) : 0);
                susTargetPos = lrp;
            }
            else if (animationTime < 1.0f)//last stage before fully deployed. close back end doors, lerp into user-configured positions
            {
                lrp = lerp(animationTime, 0.85f, 1f);
                mainStrutRot = 0f;
                secStrutRot = 0f;
                bogeyAngleRot = 0f;
                strutAngleRot = strutRotation;
                wheelAngleRot = wheelRotation;
                doorLeftRot = 0f;
                doorRightRot = 0f;
                doorRearRot = 90f + (allowFlip ? Mathf.Max(0, -strutRotation) : 0);
                susTargetPos = 1f;
            }
            else if (animationTime >= 1.0f)//fully deployed
            {
                animationTime = 1.0f;
                if (controller.wheelState != KSPWheelState.DEPLOYED)
                {
                    changeWheelState(KSPWheelState.DEPLOYED);
                    part.Effect(deployedEffect);
                }
                mainStrutRot = 0f;
                secStrutRot = 0f;
                bogeyAngleRot = 0f;
                strutAngleRot = strutRotation;
                wheelAngleRot = wheelRotation;
                doorLeftRot = 0f;
                doorRightRot = 0f;
                doorRearRot = 90f + (allowFlip ? Mathf.Max(0, -strutRotation) : 0);
                susTargetPos = 1f;
            }

            if (isFlipped && !sideMode)
            {
                strutAngleRot = -strutAngleRot;
                secStrutRot = -secStrutRot;
                bogeyAngleRot = -bogeyAngleRot;
            }
            if (sideMode && isFlipped)
            {
                wheelAngleRot = -wheelAngleRot;
            }

            leftDoor.localRotation = leftDoorDefaultRotation;
            rightDoor.localRotation = rightDoorDefaultRotation;
            rearDoor.localRotation = rearDoorDefaultRotation;
            rearDoorFlip.localRotation = rearDoorFlipDefaultRotation;
            //update door rotations from animation state
            leftDoor.Rotate(doorLeftRot, 0, 0, Space.Self);
            rightDoor.Rotate(doorRightRot, 0, 0, Space.Self);
            rearDoor.Rotate(doorRearRot, 0, 0, Space.Self);
            rearDoorFlip.Rotate(0, 0, doorFlipRot, Space.Self);

            suspensionContainer1.localRotation = sc1DefaultRotation;//user strut angle setting
            wheelContainer.localRotation = wheelContainerDefaultRotation;//user wheel angle setting
            wheelContainer.localPosition = wheelContainerDefaultPosition;//user strut extension setting
            wheelContainer.transform.position -= wheelContainer.up * controller.scale * strutExtension * maxExtension * susTargetPos;
            suspensionContainer1.Rotate(strutAngleRot, 0, 0, Space.Self);//user strut angle setting
            if (isFlipped)
            {
                wheelContainer.Rotate(0, 180, 0, Space.Self);
            }
            wheelContainer.Rotate(0, 0, wheelAngleRot, Space.Self);
            wheelContainer.Rotate(0, secStrutRot + wheel.steeringAngle, 0, Space.Self);
            Vector3 p2 = wheelContainer.position - wheelContainer.up * (HighLogic.LoadedSceneIsFlight ? (wheel.length - wheel.compressionDistance) : (wheel.length * (1 - compTest)));
            suspensionTarget.position = Vector3.Lerp(wheelContainer.position, p2, susTargetPos);
            suspensionTarget.rotation = wheelContainer.rotation;
            suspensionTarget.RotateAround(suspensionContainer1.position, sideMode? suspensionContainer1.right: suspensionContainer1.up, mainStrutRot);
            suspensionTarget.Rotate(-bogeyAngleRot, 0, 0, Space.Self);
            suspensionRotator.rotation = suspensionTarget.rotation;
            suspensionRotator.Rotate(bogeyAngleRot, 0, 0, Space.Self);
            suspensionRotator.LookAtLocked(suspensionContainer1.position, Vector3.up, Vector3.forward);

            suspensionContainer2.localRotation = sc2DefaultRotation;
            if (susTargetPos > 0)
            {
                suspensionContainer2.LookAtLocked(suspensionTarget.position, Vector3.back, Vector3.right);
            }
        }

        private float lerp(float time, float pStart, float pEnd)
        {
            float p = pEnd - pStart;
            float t = time - pStart;
            return p <= 0 ? 0 : t / p;
        }

        #endregion ENDREGION - Custom Update Methods

    }
}
