using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelAnimationRemoval : PartModule
    {
        [KSPField]
        public string animationName = string.Empty;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (string.IsNullOrEmpty(animationName)) { return; }
            Animation[] animations = part.transform.FindRecursive("model").gameObject.GetComponentsInChildren<Animation>(true);
            int len = animations.Length;
            for (int i = 0; i < len; i++)
            {
                animations[i].RemoveClip(animationName);
            }
        }

    }
}
