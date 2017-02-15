using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelAdjustableGear2 : KSPWheelSubmodule
    {

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

        [KSPField]
        public float minExtension = 0.25f;

        [KSPField]
        public float maxExtension = 0.5f;

        [KSPField]
        public float minSuspensionLength = 0.25f;

        [KSPField]
        public float maxSuspensionLength = 0.25f;

        [KSPField]
        public float minAngle = 0f;

        [KSPField]
        public float maxAngle = 30f;

        [KSPField]
        public float mainStrutRetractedAngle = 90f;

        [KSPField]
        public float secStrutRetractedAngle = 90f;

        [KSPField]
        public float wheelHousingRetractedAngle = 0f;

        [KSPField]
        public float wheelBogeyRetractedAngle = 0f;

        /// <summary>
        /// User-configured main strut angle in editor
        /// </summary>
        [KSPField(guiName = "Strut Angle", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = -30f, maxValue = 30f, stepIncrement = 0.1f, suppressEditorShipModified = true)]
        public float currentPrimaryAngle = 0f;

        /// <summary>
        /// Calculated secondary angle for wheel container to keep aligned with ground
        /// </summary>
        [KSPField(guiName = "Wheel Angle", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0f, maxValue = 60f, stepIncrement = 0.1f, suppressEditorShipModified = true)]
        public float currentSecondaryAngle = 0f;

        [KSPField(guiName = "Comp Test", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0f, maxValue = 1f, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float compTest = 1f;

        /// <summary>
        /// Has this part been marked as a clone?  Set during part symmetry setups.  Determines initial 'flipped' state if wheel can be flipped.
        /// Also determines the direction of movement for the user-selectable 'angle' controls.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isClone = false;

        /// <summary>
        /// Has user selected flipped wheel or set automatically from cloned state.  Determines wheel angle offset direction, wheel spin direction, others...
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isFlipped = false;

        private Transform suspensionContainer1;
        private Transform suspensionContainer2;
        private Transform suspensionTarget;
        private Transform wheelContainer;//used to position wheel collider according to model setup -AND- to rotate wheel for 'flip'
        private Transform mainStrut;
        private Transform secStrut;
        private Transform wheelHousing;
        private Transform wheelMesh;

        private Transform leftDoor;
        private Transform rightDoor;
        private Transform rearDoor;
        private Transform rearDoorFlip;

        /// <summary>
        /// Serialize these fields across parts (prefab -> editor; editor -> cloned)
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
        private Quaternion wheelHousingDefaultRotation;
        [SerializeField]
        private Vector3 secStrutDefaultPosition;

        [SerializeField]
        private Quaternion leftDoorDefaultRotation;
        [SerializeField]
        private Quaternion rightDoorDefaultRotation;
        [SerializeField]
        private Quaternion rearDoorDefaultRotation;
        [SerializeField]
        private Quaternion rearDoorFlipDefaultRotation;
                
        private float animationTime = 0f;
        private float animationSpeed = 1f;

        [KSPEvent(guiName = "Toggle Gear", guiActive = true, guiActiveEditor = true)]
        public void deploy()
        {
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
        }

        [KSPEvent(guiName = "Flip Gear", guiActive = true, guiActiveEditor = true)]
        public void flip()
        {
            isFlipped = !isFlipped;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            locateTransforms();
            updateAnimation(1.0f);
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
        }

        public void Update()
        {
            if (controller.wheelState == KSPWheelState.DEPLOYING) { updateAnimation(Time.deltaTime * animationSpeed); }
            else if (controller.wheelState == KSPWheelState.RETRACTING) { updateAnimation(Time.deltaTime * -animationSpeed); }
            else if (controller.wheelState == KSPWheelState.DEPLOYED)
            {
                updateAnimation(0f);//TODO....
                //TODO update wheel mesh rotation based on wheel rpm and flipped state
                //TODO update suspension transform layout based on wheel collider compression data
            }
        }

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
            float wheelDeployRot = 0f;
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
                strutAngleRot = currentPrimaryAngle * lrp;
                wheelAngleRot = currentSecondaryAngle * lrp;
                doorLeftRot = 90f;
                doorRightRot = 90f;
                doorRearRot = 90f + lrp * Mathf.Max(0, -currentPrimaryAngle);
                susTargetPos = lrp;
            }
            else if (animationTime < 1.0f)//last stage before fully deployed. close back end doors, lerp into user-configured positions
            {
                lrp = lerp(animationTime, 0.5f, 1f);
                mainStrutRot = 0f;
                secStrutRot = 0f;
                strutAngleRot = currentPrimaryAngle;
                wheelAngleRot = currentSecondaryAngle;
                doorLeftRot = (1 - lrp) * 90f;
                doorRightRot = (1 - lrp) * 90f;
                doorRearRot = 90f + Mathf.Max(0, -currentPrimaryAngle);
                susTargetPos = 1f;
            }
            else if (animationTime >= 1.0f)//fully deployed
            {
                animationTime = 1.0f;
                changeWheelState(KSPWheelState.DEPLOYED);
                mainStrutRot = 0f;
                strutAngleRot = currentPrimaryAngle;
                wheelAngleRot = currentSecondaryAngle;
                doorLeftRot = 0f;
                doorRightRot = 0f;
                doorRearRot = 90f + Mathf.Max(0, -currentPrimaryAngle);
            }

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
        }

        private float lerp(float time, float pStart, float pEnd)
        {
            float p = pEnd - pStart;
            float t = time - pStart;
            return p <= 0 ? 0 : t / p;
        }

        /**
         * Basic Overview of ALG rigging.
         * Important Transforms:
         * Suspension Upper Container
         *   Used to effect main strut angle setup
         * Wheel Container
         *   Contains wheel collider and suspension lower container
         *   Used to position wheel collider into its 'compressed' position.  Drives constraint handling for most of the rest of the transforms.
         * Suspension Lower Container
         *   Used by suspension module to position transforms according to wheel compression
         * Main Strut Upper
         *   Used to drive retract animation.  Most of the rest of the animation is handled through constraints
         *   
         * --------------------------------------------------------------------
         * Animation handling:
         * (Retract)
         * 1. Lerp main strut towards straight-downward extended position
         *    Lerp wheel alignment into straight-upwards extended position
         * 2. Open the 'main doors' to allow for main strut to retract
         * 3. Retract wheel extension from extended to retracted position
         * 4. Rotate main wheel strut from extended to retracted position
         *    Rotate wheel housing from extended to retracted position
         * 5. Close all doors
         * 
         * (Deploy)
         * 1.) Open all doors
         * 2.) Rotate main wheel strut from retracted to extended position
         *     Rotate wheel housing from retracted to extended position
         * 3.) Close main doors
         * 4.) Extend strut extension from retracted to user-set position
         * 5.) Rotate wheel from extended to user-set configuration.
         * 
         * --------------------------------------------------------------------
         * Full Time External Module Constraints:
         * DragBraceUpper -> DragBraceLower (locked look)
         * DragBraceLower -> DragBraceUpper (locked look)
         * WheelContainer -> WheelTarget (position)
         * SuspensionContainerLower -> SuspensionContainerUpper (locked look)
         * SuspensionContainerUpper -> SuspensionContainerLower (locked look)
         * 
         * --------------------------------------------------------------------
         * ALG Module Managed Constraints:
         * WheelContainer -> UserSetRotation (rotation)
         * SuspensionContainerLower -> compression from collider (position)
         * WheelMesh -> rpm-based rotation from collider (rotation)
         * 
         **/
    }
}
