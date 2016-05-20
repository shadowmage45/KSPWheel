using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    public class WheelAnimationHandler
    {
        private readonly KSPWheelModule module;
        private KSPWheelState currentAnimState;        
        private AnimationData animationData;

        public WheelAnimationHandler(KSPWheelModule module, string animationName, float animationSpeed, int animationLayer, KSPWheelState initialState)
        {
            this.module = module;
            this.currentAnimState = initialState;
            this.animationData = new AnimationData(module.part, animationName, animationSpeed, animationLayer);
        }
        
        public void updateAnimationState()
        {
            if (currentAnimState == KSPWheelState.RETRACTING || currentAnimState == KSPWheelState.DEPLOYING)
            {
                bool playing = false;
                int len = animationData.anims.Length;
                for (int i = 0; i < len && !playing; i++)
                {
                    if (animationData.anims[i][animationData.animationName].enabled)
                    {
                        playing = true;
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
        }

        private void stopAnimation()
        {
            animationData.stopAnimation();
        }

        private void setAnimTime(float time)
        {
            animationData.setAnimTime(time);
        }

        private void setAnimSpeed(float speed)
        {
            animationData.setAnimSpeed(speed);
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
            anims = al.ToArray();
            if (anims == null || anims.Length == 0)
            {
                MonoBehaviour.print("ERROR: No animations found for animation name: " + animationName);
            }
            foreach (Animation a in anims)
            {
                a[animationName].layer = animationLayer;
                a[animationName].wrapMode = WrapMode.Once;
                a.wrapMode = WrapMode.Once;
            }
        }

        public void playAnimation()
        {
            foreach (Animation a in anims)
            {
                a.Play(animationName);
            }
        }

        public void stopAnimation()
        {
            foreach (Animation a in anims)
            {
                a[animationName].speed = 0f;
                a.Stop(animationName);
            }
        }

        public void setAnimTime(float time)
        {
            foreach (Animation a in anims)
            {
                a[animationName].normalizedTime = time;
            }
        }

        public void setAnimSpeed(float speed)
        {
            foreach (Animation a in anims)
            {
                a[animationName].speed = speed * animationSpeed;
            }
        }
    }
}
