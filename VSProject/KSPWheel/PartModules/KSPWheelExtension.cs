using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelExtension : KSPWheelSubmodule
    {

        /// <summary>
        /// The name of the transform to mainpulate.  If multiple transforms are present, 'indexInDuplicates' may be specified to manipulate only a single one (optional)
        /// </summary>
        [KSPField]
        public string transformName;

        /// <summary>
        /// If less than 0, effect will be applied to all transforms of the given name<para/>
        /// If >=0, effect will be only applied to a single transform from within any duplicate named transforms.
        /// </summary>
        [KSPField]
        public int indexInDuplicates = -1;

        /// <summary>
        /// The value to use when in fully retracted state
        /// </summary>
        [KSPField]
        public Vector3 retractedValue = Vector3.zero;

        /// <summary>
        /// The value to use when in fully deployed (but non-extended) state
        /// </summary>
        [KSPField]
        public Vector3 deployedValue = Vector3.zero;

        /// <summary>
        /// The value to use when in fully deployed and fully extended state
        /// </summary>
        [KSPField]
        public Vector3 extendedValue = Vector3.zero;

        /// <summary>
        /// The start time in the deployment animation at which the transform will start moving from retracted to deployed positions
        /// </summary>
        [KSPField]
        public float deployStartTime = 0f;

        /// <summary>
        /// The time in the deployment animation at which the transform will be at its deployed position
        /// </summary>
        [KSPField]
        public float deployEndTime = 0f;

        /// <summary>
        /// True/false value on if the constraint should be applied as a translation or rotation.  False = translation, true = rotation.
        /// </summary>
        [KSPField]
        public bool rotation = true;

        /// <summary>
        /// The user-configurable extension limit.
        /// </summary>
        [KSPField(guiName = "Extension Limit", guiActive = true, guiActiveEditor = true, isPersistant = true, guiUnits = "%"),
         UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 0.5f)]
        public float currentValue = 0f;

        /// <summary>
        /// The list of transforms to be manipulated.  Populated during post-wheel-created method callback from control module.
        /// </summary>
        public Transform[] transforms;

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            Transform[] trs = part.transform.FindChildren(transformName);
            if (indexInDuplicates >= 0)
            {
                trs = new Transform[] { trs[indexInDuplicates] };//use singular transform as specified by index-in-duplicates config value
            }
            else
            {
                transforms = trs;
            }
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            setInterpolateDeployPosition(controller.deployAnimationTime);
        }

        private void setInterpolateDeployPosition(float deployTime)
        {
            if (deployTime < deployStartTime)
            {
                setPosition(0, 0);//not deployed or extended
            }
            else if (deployTime < deployEndTime)
            {
                float range = deployEndTime - deployStartTime;
                float t = deployTime - deployStartTime;
                float percent = t / range;
                setPosition(percent, 0);//partially deployed, not extended
            }
            else if (deployTime < 1)
            {
                float range = 1f - deployEndTime;
                float t = deployTime - deployEndTime;
                float percent = t / range;
                setPosition(1f, percent * currentValue * 0.01f);//fully deployed, partially extended
            }
            else
            {
                setPosition(1f, currentValue * 0.01f);//fully deployed, fully extended
            }
        }

        private void setPosition(float percentDeployed, float percentExtended)
        {
            Vector3 val = Vector3.zero;
            if (percentDeployed < 1)
            {
                val = Vector3.Lerp(retractedValue, deployedValue, percentDeployed);
            }
            else
            {
                val = Vector3.Lerp(deployedValue, extendedValue, percentExtended);
            }

            Transform tr = null;
            int len = transforms.Length;
            for (int i = 0; i < len; i++)
            {
                tr = transforms[i];
                if (rotation)
                {
                    tr.localRotation = Quaternion.Euler(val);//TODO cache the quat?
                }
                else
                {
                    tr.localPosition = val;
                }
            }
        }

    }
}
