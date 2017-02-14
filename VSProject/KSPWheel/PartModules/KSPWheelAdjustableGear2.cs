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
        public string suspensionUpperContainerName = string.Empty;

        [KSPField]
        public string suspensionLowerContainerName = string.Empty;

        [KSPField]
        public string wheelContainerName = string.Empty;

        [KSPField]
        public string rearDoorName = string.Empty;

        [KSPField]
        public string rightDoorName = string.Empty;

        [KSPField]
        public string leftDoorName = string.Empty;

        [KSPField]
        public string mainStrutName = string.Empty;

        [KSPField]
        public string wheelHousingName = string.Empty;

        [KSPField]
        public string wheelBogeyName = string.Empty;

        [KSPField]
        public string wheelMeshName = string.Empty;

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
        public float mainStrutRetractedAngle = 0f;

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
         UI_FloatRange(minValue = -30f, maxValue = 30f, stepIncrement = 0.1f, suppressEditorShipModified = true)]
        public float currentSecondaryAngle = 0f;

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

        private Transform suspensionUpperContainer;
        private Transform suspensionLowerContainer;
        private Transform wheelContainer;//used to position wheel collider according to model setup -AND- to rotate wheel for 'flip'
        private Transform mainStrut;
        private Transform wheelHousing;
        private Transform wheelMesh;

        /// <summary>
        /// Serialize these fields across parts (prefab -> editor; editor -> cloned)
        /// </summary>
        [SerializeField]
        private bool initializedDefaultRotations;
        [SerializeField]
        private Quaternion mainStrutDefaultRotation;
        [SerializeField]
        private Quaternion wheelHousingDefaultRotation;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            locateTransforms();
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
        }

        private void locateTransforms()
        {
            suspensionUpperContainer = part.transform.FindRecursive(suspensionUpperContainerName);
            suspensionLowerContainer = part.transform.FindRecursive(suspensionLowerContainerName);
            wheelContainer = part.transform.FindRecursive(wheelContainerName);
            mainStrut = part.transform.FindRecursive(mainStrutName);
            wheelHousing = part.transform.FindRecursive(wheelHousingName);
            wheelMesh = part.transform.FindRecursive(wheelMeshName);
            if (!initializedDefaultRotations)
            {
                initializedDefaultRotations = true;
                mainStrutDefaultRotation = mainStrut.localRotation;
                wheelHousingDefaultRotation = wheelHousing.localRotation;
            }
        }

        private void positionTransforms()
        {

        }

        private void updateAnimation()
        {

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
