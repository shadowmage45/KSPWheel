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

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            wheelMeshTransform = part.transform.FindRecursive(wheelMeshName);
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (HighLogic.LoadedSceneIsEditor && editorRotation)
            {
                wheelMeshTransform.Rotate(rotationAxis * 72 * Time.deltaTime, Space.Self);//72 deg/sec rotation speed
            }
            wheelMeshTransform.Rotate(rotationAxis * wheel.perFrameRotation, Space.Self);
        }

    }

}
