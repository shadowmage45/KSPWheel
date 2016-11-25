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

        private Transform wheelMeshTransform;

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            wheelMeshTransform = part.transform.FindRecursive(wheelMeshName);
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            wheelMeshTransform.Rotate(rotationAxis * wheel.perFrameRotation, Space.Self);
        }

    }

}
