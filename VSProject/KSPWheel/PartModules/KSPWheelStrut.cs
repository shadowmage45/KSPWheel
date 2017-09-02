using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPWheel
{
    public class KSPWheelStrut : CompoundParts.CompoundPartModule
    {
        /// <summary>
        /// Cached/persistent saved value of the second end-point of this strut, in the local space of this part
        /// (which does not alter part orientation for model-orientation changes)
        /// </summary>
        [KSPField]
        public Vector3 endVectorLocal = Vector3.zero;

        public override void OnTargetSet(Part target)
        {
            //TODO
            Vector3 vector = base.compoundPart.transform.TransformPoint(base.compoundPart.targetPosition);
            throw new NotImplementedException();
        }

        public override void OnTargetLost()
        {
            //TODO
            throw new NotImplementedException();
        }

    }
}
