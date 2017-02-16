using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelAdjustableGear2 : KSPWheelSubmodule
    {

        #region REGION - Standard Part Config File Fields

        [KSPField]
        public string suspensionContainer1Name = string.Empty;

        [KSPField]
        public string suspensionContainer2Name = string.Empty;

        [KSPField]
        public string suspensionTargetName = string.Empty;

        [KSPField]
        public string wheelContainerName = string.Empty;

        [KSPField]
        public string mainStrutName = string.Empty;

        [KSPField]
        public string secStrutName = string.Empty;

        [KSPField]
        public string wheelHousingName = string.Empty;

        [KSPField]
        public string wheelBogeyName = string.Empty;

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
        public bool canFlip = false;

        /// <summary>
        /// User-selectable strut-extension.  Makes the entire gear 'longer'.
        /// </summary>
        [KSPField]
        public float maxExtension = 0.5f;

        /// <summary>
        /// User-selectable strut angle min/max (negative value used for min)
        /// </summary>
        [KSPField]
        public float maxStrutAngle = 30f;

        /// <summary>
        /// User-selectable maximum wheel angle.  Minimum = 0
        /// </summary>
        [KSPField]
        public float maxWheelAngle = 60f;

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
        /// Angular rotation of the wheel housing during retract animation.  TODO
        /// </summary>
        [KSPField]
        public float wheelHousingRetractedAngle = 0f;

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
        public float animationTime = 0f;

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
        private Transform wheelContainer;
        private Transform mainStrut;
        private Transform secStrut;
        private Transform wheelHousing;
        private Transform wheelMesh;

        private Transform leftDoor;
        private Transform rightDoor;
        private Transform rearDoor;
        private Transform rearDoorFlip;

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
        private Quaternion mainStrutDefaultRotation;
        [SerializeField]
        private Quaternion secStrutDefaultRotation;
        [SerializeField]
        private Vector3 secStrutDefaultPosition;
        [SerializeField]
        private Quaternion wheelHousingDefaultRotation;

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
                        break;
                    case KSPWheelState.RETRACTING:
                        m.changeWheelState(KSPWheelState.DEPLOYING);
                        break;
                    case KSPWheelState.DEPLOYED:
                        m.changeWheelState(KSPWheelState.RETRACTING);
                        break;
                    case KSPWheelState.DEPLOYING:
                        m.changeWheelState(KSPWheelState.RETRACTING);
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
            this.symmetryUpdate(m =>
            {
                Vector3 target = m.wheelContainer.position + Vector3.up;//one unit above the transform, in world-space in the editor
                Vector3 localTarget = m.wheelContainer.InverseTransformPoint(target);//one unit above the transform, as seen in local space
                //rotating around the local Z axis, so we only care about the x and y offsets
                //erm.. feed this into Mathf.Atan2 as a slope, to get the returned angle
                float angle = -Mathf.Atan2(localTarget.x, localTarget.y) * Mathf.Rad2Deg;
                m.wheelRotation = Mathf.Clamp(m.wheelRotation + angle, 0, m.maxWheelAngle);//clamp it to the current wheel angle limits
            });
        }

        #endregion ENDREGION - GUI Methods

        #region REGION - Standard KSP/Unity Overrides

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            locateTransforms();
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            if (canFlip && part.symmetryCounterparts != null && part.symmetryCounterparts.Count > 0)
            {
                //this must be a clone, or part is being reloaded
                //the 'symmetry counterpart' that exists should be the original part
                this.isFlipped = !part.symmetryCounterparts[0].GetComponent<KSPWheelAdjustableGear2>().isFlipped;
            }
            updateAnimation(0f);//force update the animation based on current time
        }

        public void Update()
        {
            base.preWheelFrameUpdate();
            if (controller.wheelState == KSPWheelState.DEPLOYING) { updateAnimation(Time.deltaTime * animationSpeed); }
            else if (controller.wheelState == KSPWheelState.RETRACTING) { updateAnimation(Time.deltaTime * -animationSpeed); }
            else if (controller.wheelState == KSPWheelState.DEPLOYED)
            {
                updateAnimation(0f);//TODO -- clean this up; only adjust suspension mesh stuff during normal deployed operation
                wheelMesh.Rotate(0, wheel.perFrameRotation, 0, Space.Self);
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
            wheelContainer = part.transform.FindRecursive(wheelContainerName);
            mainStrut = part.transform.FindRecursive(mainStrutName);
            secStrut = part.transform.FindRecursive(secStrutName);
            wheelHousing = part.transform.FindRecursive(wheelHousingName);
            wheelMesh = part.transform.FindRecursive(wheelMeshName);
            leftDoor = part.transform.FindRecursive(leftDoorName);
            rightDoor = part.transform.FindRecursive(rightDoorName);
            rearDoor = part.transform.FindRecursive(rearDoorName);
            rearDoorFlip = part.transform.FindRecursive(rearDoorFlipName);
            MonoBehaviour.print(suspensionContainer1 + " :: " + suspensionContainer2 + " :: " + suspensionTarget + " :: " + wheelContainer + " :: " + mainStrut + " :: " + secStrut + " :: " + wheelHousing + " :: " + wheelMesh + " :: " + leftDoor + " :: " + rightDoor + " :: " + rearDoor + " :: " + rearDoorFlip);

            if (!initializedDefaultRotations)
            {
                initializedDefaultRotations = true;
                sc1DefaultRotation = suspensionContainer1.localRotation;
                sc2DefaultRotation = suspensionContainer2.localRotation;
                wheelContainerDefaultRotation = wheelContainer.localRotation;
                mainStrutDefaultRotation = mainStrut.localRotation;                
                secStrutDefaultRotation = secStrut.localRotation;
                secStrutDefaultPosition = secStrut.localPosition;
                wheelHousingDefaultRotation = wheelHousing.localRotation;
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

            //set all transforms to default orientations/positions
            suspensionContainer1.localRotation = sc1DefaultRotation;
            suspensionContainer2.localRotation = sc2DefaultRotation;
            suspensionTarget.position = wheelContainer.position;
            wheelContainer.localRotation = wheelContainerDefaultRotation;
            mainStrut.localRotation = mainStrutDefaultRotation;
            secStrut.localRotation = secStrutDefaultRotation;
            wheelHousing.localRotation = wheelHousingDefaultRotation;
            leftDoor.localRotation = leftDoorDefaultRotation;
            rightDoor.localRotation = rightDoorDefaultRotation;
            rearDoor.localRotation = rearDoorDefaultRotation;
            rearDoorFlip.localRotation = rearDoorFlipDefaultRotation;

            float mainStrutRot = 0f;
            float secStrutRot = 0f;
            float strutAngleRot = 0f;
            float wheelAngleRot = 0f;
            float doorLeftRot = 0f;
            float doorRightRot = 0f;
            float doorRearRot = 0f;
            float doorFlipRot = isFlipped ? 180f : 0f;
            float susTargetPos = 0f;
            if (animationTime <= 0)//fully retracted, everything in retracted state
            {
                animationTime = 0f;
                changeWheelState(KSPWheelState.RETRACTED);
                mainStrutRot = mainStrutRetractedAngle;
                secStrutRot = secStrutRetractedAngle;
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
                strutAngleRot = 0f;
                wheelAngleRot = 0f;
                doorLeftRot = lrp * 90f;
                doorRightRot = lrp * 90f;
                doorRearRot = lrp * 90f;
                susTargetPos = 0f;
            }
            else if (animationTime < 0.5f)//main deploy animation
            {
                lrp = lerp(animationTime, 0.15f, 0.5f);
                mainStrutRot = (1f - lrp) * mainStrutRetractedAngle;
                secStrutRot = (1f - lrp) * secStrutRetractedAngle;
                strutAngleRot = 0f;
                wheelAngleRot = 0f;
                doorLeftRot = 90f;
                doorRightRot = 90f;
                doorRearRot = 90f;
                susTargetPos = 0f;
            }
            else if (animationTime < 0.85f)//lerp into user-configured positions
            {
                lrp = lerp(animationTime, 0.5f, 0.85f);
                mainStrutRot = 0f;
                secStrutRot = 0f;
                strutAngleRot = strutRotation * lrp;
                wheelAngleRot = wheelRotation * lrp;
                doorLeftRot = 90f;
                doorRightRot = 90f;
                doorRearRot = 90f + lrp * Mathf.Max(0, -strutRotation);
                susTargetPos = lrp;
            }
            else if (animationTime < 1.0f)//last stage before fully deployed. close back end doors, lerp into user-configured positions
            {
                lrp = lerp(animationTime, 0.5f, 1f);
                mainStrutRot = 0f;
                secStrutRot = 0f;
                strutAngleRot = strutRotation;
                wheelAngleRot = wheelRotation;
                doorLeftRot = (1 - lrp) * 90f;
                doorRightRot = (1 - lrp) * 90f;
                doorRearRot = 90f + Mathf.Max(0, -strutRotation);
                susTargetPos = 1f;
            }
            else if (animationTime >= 1.0f)//fully deployed
            {
                animationTime = 1.0f;
                changeWheelState(KSPWheelState.DEPLOYED);
                mainStrutRot = 0f;
                strutAngleRot = strutRotation;
                wheelAngleRot = wheelRotation;
                doorLeftRot = 0f;
                doorRightRot = 0f;
                doorRearRot = 90f + Mathf.Max(0, -strutRotation);
            }

            //update ordering
            //sc1 - 'strut-angle' if deployed
            //suspension target - wheel compression if deployed
            //sc2 - aim at target if deployed, else keep default rotation
            //mainStrut - deploy angle if deploying, else 0
            //secStrut - deploy angle if deploying, else aim at main strut
            //wheelContainer - 'wheel-angle' if deployed
            //wheelHousing - deploy angle if deploying, else match wheel container angle
            /*            
            hierarchy
            0 doors
            0 sc1
            1 |--sc2
            2 |  |---mainStrut
            3 |      |---secStrut
            4 |          |---wheelHousing
            5 |              |---wheelMesh
            1 |--wheelContainer
            2    |---suspensionTarget
            2    |---wheelCollider
            */

            if (isFlipped)
            {
                strutAngleRot = -strutAngleRot;
                secStrutRot = -secStrutRot;
            }

            //update door rotations from animation state
            leftDoor.Rotate(-doorLeftRot, 0, 0, Space.Self);
            rightDoor.Rotate(doorRightRot, 0, 0, Space.Self);
            rearDoor.Rotate(doorRearRot, 0, 0, Space.Self);
            rearDoorFlip.Rotate(0, 0, doorFlipRot, Space.Self);

            //suspension container1
            //it is responsible for the user-selected 'strut-angle' rotation; it rotates all other transforms
            suspensionContainer1.Rotate(0, -strutAngleRot, 0, Space.Self);

            if (deployed) //transforms managed by suspension parameters
            {
                if (isFlipped)
                {
                    wheelContainer.Rotate(0, 180, 0, Space.Self);
                }
                wheelContainer.Rotate(0, 0, wheelAngleRot, Space.Self);
                suspensionTarget.position = wheelContainer.position - suspensionTarget.up * (wheel.length - wheel.compressionDistance);
                suspensionContainer2.LookAtLocked(suspensionTarget.position, Vector3.back, Vector3.up);
                secStrut.position = suspensionTarget.position;
                secStrut.LookAtLocked(mainStrut.position, Vector3.up, Vector3.right);
                wheelHousing.rotation = wheelContainer.rotation;
            }
            else //transforms managed by deploy animation
            {
                wheelContainer.Rotate(0, 0, wheelAngleRot, Space.Self);
                //TODO -- this actually needs to lerp from strut to wheel-container positions based on the susTargetPos...
                suspensionTarget.position = secStrut.position - secStrut.up * (wheel.length * susTargetPos);
                mainStrut.Rotate(mainStrutRot, 0, 0, Space.Self);
                secStrut.Rotate(0, secStrutRot, 0, Space.Self);
                wheelHousing.localRotation = wheelHousingDefaultRotation;
                if (isFlipped)
                {
                    wheelHousing.Rotate(0, 180, 0, Space.Self);
                }
                wheelHousing.Rotate(0, 0, wheelAngleRot, Space.Self);
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
