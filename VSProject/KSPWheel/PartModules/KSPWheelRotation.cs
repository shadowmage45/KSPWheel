using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{

    public class KSPWheelRotation : KSPWheelSubmodule
    {

        [KSPField]
        public string wheelMeshName;

        [KSPField]
        public Vector3 rotationAxis = Vector3.left;

        [KSPField(guiName = "Display Fwd Rotation", guiActive = false, guiActiveEditor = true, isPersistant = false),
         UI_Toggle(enabledText = "True", disabledText = "False", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public bool editorRotation = false;

        private Transform wheelMeshTransform;
        private KSPWheelMotor motor;

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            wheelMeshTransform = part.transform.FindRecursive(wheelMeshName);
            motor = part.transform.GetComponent<KSPWheelMotor>();
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (HighLogic.LoadedSceneIsEditor && editorRotation)
            {
                float invert = motor == null ? 1 : motor.invertMotor ? -1 : 1;
                wheelMeshTransform.Rotate(rotationAxis * 72 * Time.deltaTime * invert, Space.Self);//72 deg/sec rotation speed
            }
            wheelMeshTransform.Rotate(rotationAxis * wheel.perFrameRotation, Space.Self);
        }

    }

}
