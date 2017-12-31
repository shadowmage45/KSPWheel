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
        private readonly KSPWheelSubmodule module;
        private readonly WheelAnimationCallback callback;
        private KSPWheelState currentAnimState;
        List<AnimationData> animationData = new List<AnimationData>();
        private float animTime = 0f;
        private bool invertAnimation;
        private WrapMode wrapMode;

        internal KSPWheelState state { get { return currentAnimState; } }

        public WheelAnimationHandler(KSPWheelSubmodule module, string animationName, float animationSpeed, int animationLayer, KSPWheelState initialState, bool invertAnimation, WrapMode wrapMode)
        {
            this.module = module;
            callback = (WheelAnimationCallback)module;//dirty, but whatever...
            this.currentAnimState = initialState;
            this.invertAnimation = invertAnimation;
            this.wrapMode = wrapMode;
            this.animationData.Add(new AnimationData(module.part, animationName, animationSpeed, animationLayer, wrapMode));
        }

        public void loadSecondaryAnimations(ConfigNode[] animNodes)
        {
            int len = animNodes.Length;
            for (int i = 0; i < len; i++)
            {
                animationData.Add(new AnimationData(module.part, animNodes[i]));
            }
        }
        
        /// <summary>
        /// Should be called every Update() frame from PartModule; updates current internal 'playing' state and issues callback to PartModule when animation state changes (transition from deploying to deployed, retracting to retracted)
        /// </summary>
        public void updateAnimationState()
        {
            animTime = 0f;
            if (currentAnimState == KSPWheelState.RETRACTING || currentAnimState == KSPWheelState.DEPLOYING)
            {
                bool playing = false;
                int len = animationData.Count;
                AnimationData data;
                for (int i = 0; i < len; i++)
                {
                    data = animationData[i];
                    if (data.updateAnimations()) { playing = true; }
                    if (data.time > animTime) { animTime = data.time; }
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
                        setAnimSpeedMult(-1f);
                        if (currentAnimState == KSPWheelState.DEPLOYED)//enforce play backwards from end
                        {
                            setAnimTime(1f);
                        }
                        playAnimation();
                        break;
                    }
                case KSPWheelState.DEPLOYING:
                    {
                        setAnimSpeedMult(1f);
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
                        setAnimSpeedMult(1);
                        playAnimation();
                        break;
                    }
                case KSPWheelState.RETRACTED:
                    {
                        setAnimTime(0);
                        setAnimSpeedMult(-1);
                        playAnimation();
                        if (wrapMode != WrapMode.Once)
                        {
                            stopAnimation();
                        }
                        break;
                    }
                case KSPWheelState.BROKEN:
                    {
                        break;
                    }
            }

            this.currentAnimState = state;
            if (callback) { this.callback.onAnimationStateChanged(state); }
        }

        private void playAnimation()
        {
            int len = animationData.Count;
            for (int i = 0; i < len; i++)
            {
                animationData[i].playAnimation();
            }
        }

        private void stopAnimation()
        {
            int len = animationData.Count;
            for (int i = 0; i < len; i++)
            {
                animationData[i].stopAnimation();
            }
        }

        /// <summary>
        /// Set the animation to the specified normalized time.
        /// Performs an sample() operation to set transforms to the new state.
        /// </summary>
        /// <param name="time"></param>
        internal void setAnimTime(float time)
        {
            if (invertAnimation)
            {
                time = 1f - time;
            }
            int len = animationData.Count;
            for (int i = 0; i < len; i++)
            {
                animationData[i].setAnimTime(time);
            }
        }

        internal void setAnimSpeedMult(float speed)
        {
            if (invertAnimation)
            {
                speed *= -1;
            }
            int len = animationData.Count;
            for (int i = 0; i < len; i++)
            {
                animationData[i].setAnimSpeedMultiplier(speed);
            }
        }

        internal void setAnimSpeedBase(float speed)
        {
            if (invertAnimation)
            {
                speed *= -1;
            }
            int len = animationData.Count;
            for (int i = 0; i < len; i++)
            {
                animationData[i].setAnimSpeedBase(speed);
            }
        }

        internal void sample()
        {
            int len = animationData.Count;
            for (int i = 0; i < len; i++)
            {
                animationData[i].sample();
            }
        }

    }

    public interface WheelAnimationCallback
    {
        void onAnimationStateChanged(KSPWheelState state);
    }

    public class AnimationData
    {
        public readonly Animation[] anims;
        public readonly String animationName;
        private float animationSpeed = 1;
        private float speedMult = 1;
        public readonly int animationLayer = 1;
        public readonly WrapMode wrapMode = WrapMode.Once;

        public float time = 0f;

        public AnimationData(Part part, string name, float speed, int layer, WrapMode wrap)
        {
            animationName = name;
            animationSpeed = speed;
            animationLayer = layer;
            wrapMode = wrap;
            anims = setupAnimation(part, name, speed, layer, wrap);
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

        private Animation[] setupAnimation(Part part, string name, float speed, int layer, WrapMode wrapMode = WrapMode.Once)
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
            if (anims == null || anims.Length == 0)
            {
                MonoBehaviour.print("ERROR: No animations found for animation name: " + animationName);
            }
            foreach (Animation a in anims)
            {
                a[animationName].layer = animationLayer;
                a[animationName].wrapMode = wrapMode;
            }
            return anims;
        }

        public bool updateAnimations()
        {
            bool playing = false;
            time = 0f;
            int len = anims.Length;
            AnimationState state;
            for (int i = 0; i < len; i++)
            {
                state = anims[i][animationName];
                if (state.normalizedTime > time) { time = state.normalizedTime; }
                if (state.enabled) { playing = true; }
            }
            return playing;
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
                float time = anims[i][animationName].normalizedTime;
                anims[i].Stop(animationName);
                anims[i][animationName].normalizedTime = time;
                anims[i].Sample();
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

        public void setAnimSpeedMultiplier(float speed)
        {
            speedMult = speed;
            int len = anims.Length;
            float totalSpeed = speedMult * animationSpeed;
            for (int i = 0; i < len; i++)
            {
                anims[i][animationName].speed = totalSpeed;
            }
        }

        public void setAnimSpeedBase(float speed)
        {
            animationSpeed = speed;
            setAnimSpeedMultiplier(speedMult);
        }

        public void sample()
        {
            int len = anims.Length;
            for (int i = 0; i < len; i++)
            {
                anims[i].Sample();
            }
        }

    }
}
