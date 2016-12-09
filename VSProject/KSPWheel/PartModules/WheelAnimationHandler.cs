using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    /// <summary>
    /// Container class for handling animation states; separated from PartModule class to cut down on the clutter and enforce some encapsulation
    /// </summary>
    public class WheelAnimationHandler
    {
        private readonly KSPWheelDeployment module;
        private KSPWheelState currentAnimState;
        private AnimationData animationData;
        private float animTime = 0f;
        private AnimationData[] secondaryAnimationData;

        public WheelAnimationHandler(KSPWheelDeployment module, string animationName, float animationSpeed, int animationLayer, KSPWheelState initialState)
        {
            this.module = module;
            this.currentAnimState = initialState;
            this.animationData = new AnimationData(module.part, animationName, animationSpeed, animationLayer);
        }

        public void loadSecondaryAnimations(ConfigNode[] animNodes)
        {
            int len = animNodes.Length;
            secondaryAnimationData = new AnimationData[len];
            for (int i = 0; i < len; i++)
            {
                secondaryAnimationData[i] = new AnimationData(module.part, animNodes[i]);
            }
        }
        
        /// <summary>
        /// Should be called every Update() frame from PartModule; updates current internal 'playing' state and issues callback to PartModule when animation state changes (transition from deploying to deployed, retracting to retracted)
        /// </summary>
        public void updateAnimationState()
        {
            animTime = 0f;
            float time;
            if (currentAnimState == KSPWheelState.RETRACTING || currentAnimState == KSPWheelState.DEPLOYING)
            {
                bool playing = false;
                int len = animationData.anims.Length;
                for (int i = 0; i < len && !playing; i++)
                {
                    if (animationData.anims[i][animationData.animationName].enabled)
                    {
                        playing = true;
                        time = animationData.anims[i][animationData.animationName].normalizedTime;
                        if (time > animTime) { animTime = time; }
                    }
                }
                len = secondaryAnimationData.Length;
                int len2;
                for (int i = 0; i < len; i++)
                {
                    len2 = secondaryAnimationData[i].anims.Length;
                    for (int k = 0; k < len2; k++)
                    {
                        if (secondaryAnimationData[i].anims[k].enabled)
                        {
                            playing = true;
                        }
                    }
                }
                //if no longer playing, set the new animation state and inform the callback of the change
                if (!playing)
                {
                    KSPWheelState newState = currentAnimState == KSPWheelState.RETRACTING ? KSPWheelState.RETRACTED : KSPWheelState.DEPLOYED;
                    setToAnimationState(newState, true);
                }
            }
        }

        public float animationTime
        {
            get { return animTime; }
        }

        public void setToAnimationState(KSPWheelState state, bool callback)
        {
            switch (state)
            {
                case KSPWheelState.RETRACTING:
                    {
                        setAnimSpeed(-1f);
                        if (currentAnimState == KSPWheelState.DEPLOYED)//enforce play backwards from end
                        {
                            setAnimTime(1f);
                        }
                        playAnimation();
                        break;
                    }
                case KSPWheelState.DEPLOYING:
                    {
                        setAnimSpeed(1f);
                        if (currentAnimState == KSPWheelState.RETRACTED)//enforce play forwards from beginning
                        {
                            setAnimTime(0f);
                        }
                        playAnimation();
                        break;
                    }
                case KSPWheelState.DEPLOYED:
                    {
                        setAnimTime(1);
                        setAnimSpeed(1);
                        playAnimation();
                        break;
                    }
                case KSPWheelState.RETRACTED:
                    {
                        setAnimTime(0);
                        setAnimSpeed(-1);
                        playAnimation();
                        break;
                    }
                case KSPWheelState.BROKEN:
                    {
                        break;
                    }
            }

            this.currentAnimState = state;
            if (callback) { module.onAnimationStateChanged(state); }
        }

        private void playAnimation()
        {
            animationData.playAnimation();
            int len = secondaryAnimationData.Length;
            for (int i = 0; i < len; i++)
            {
                secondaryAnimationData[i].playAnimation();
            }
        }

        private void stopAnimation()
        {
            animationData.stopAnimation();
            int len = secondaryAnimationData.Length;
            for (int i = 0; i < len; i++)
            {
                secondaryAnimationData[i].stopAnimation();
            }
        }

        private void setAnimTime(float time)
        {
            animationData.setAnimTime(time);
            int len = secondaryAnimationData.Length;
            for (int i = 0; i < len; i++)
            {
                secondaryAnimationData[i].setAnimTime(time);
            }
        }

        private void setAnimSpeed(float speed)
        {
            animationData.setAnimSpeed(speed);
            int len = secondaryAnimationData.Length;
            for (int i = 0; i < len; i++)
            {
                secondaryAnimationData[i].setAnimSpeed(speed);
            }
        }
    }

    public class AnimationData
    {
        public readonly Animation[] anims;
        public readonly String animationName;
        public readonly float animationSpeed = 1;
        public readonly int animationLayer = 1;

        public AnimationData(Part part, string name, float speed, int layer)
        {
            animationName = name;
            animationSpeed = speed;
            animationLayer = layer;
            anims = setupAnimation(part, name, speed, layer);
        }

        public AnimationData(Part part, ConfigNode node)
        {
            string name = node.GetStringValue("name");
            float speed = node.GetFloatValue("speed");
            int layer = node.GetIntValue("layer");
            animationName = name;
            animationSpeed = speed;
            animationLayer = layer;
            anims = setupAnimation(part, name, speed, layer);
        }

        private Animation[] setupAnimation(Part part, string name, float speed, int layer)
        {
            Animation[] animsBase = part.gameObject.GetComponentsInChildren<Animation>(true);
            List<Animation> al = new List<Animation>();
            foreach (Animation a in animsBase)
            {
                AnimationClip c = a.GetClip(animationName);
                if (c != null)
                {
                    al.Add(a);
                }
            }
            Animation[] anims = al.ToArray();
            if (this.anims == null || this.anims.Length == 0)
            {
                MonoBehaviour.print("ERROR: No animations found for animation name: " + animationName);
            }
            foreach (Animation a in this.anims)
            {
                a[animationName].layer = animationLayer;
                a[animationName].wrapMode = WrapMode.Once;
                a.wrapMode = WrapMode.Once;
            }
            return anims;
        }

        public void playAnimation()
        {
            int len = anims.Length;
            for (int i = 0; i < len; i++)
            {
                anims[i].Play(animationName);
            }
        }

        public void stopAnimation()
        {
            int len = anims.Length;
            for (int i = 0; i < len; i++)
            {
                anims[i][animationName].speed = 0f;
                anims[i].Stop(animationName);
            }
        }

        public void setAnimTime(float time)
        {
            int len = anims.Length;
            for (int i = 0; i < len; i++)
            {
                anims[i][animationName].normalizedTime = time;
                anims[i].Sample();
            }
        }

        public void setAnimSpeed(float speed)
        {
            int len = anims.Length;
            for (int i = 0; i < len; i++)
            {
                anims[i][animationName].speed = speed * animationSpeed;
            }
        }
    }
}
