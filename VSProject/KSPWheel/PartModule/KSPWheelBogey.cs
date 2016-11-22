using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelBogey : KSPWheelSubmodule
    {

        [KSPField]
        public string bogeyName;

        [KSPField]
        public Vector3 bogeyUpAxis = Vector3.up;

        [KSPField]
        public Vector3 bogeyFwdAxis = Vector3.forward;

        private Transform bogeyTransform;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            bogeyTransform = part.transform.FindRecursive(bogeyName);
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            //TODO update bogey orientation

            //the 'up' direction of the contacted surface
            Vector3 normal = wheel.contactNormal;
            normal = bogeyTransform.InverseTransformDirection(normal);
            float dot = Vector3.Dot(normal, bogeyUpAxis);
            float angle = Mathf.Acos(dot);
            //now how to know which way to rotate?
        }

    }
}
