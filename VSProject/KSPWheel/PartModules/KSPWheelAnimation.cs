using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelAnimation : KSPWheelSubmodule, WheelAnimationCallback
    {

        [KSPField]
        public string animationName = string.Empty;

        [KSPField]
        public float animationSpeed = 1;

        [KSPField]
        public int animationLayer = 1;

        [KSPField]
        public string effectName = string.Empty;

        [KSPField]
        public bool invertAnimation = false;

        /// <summary>
        /// true = 'wrap', false = 'ping-pong'
        /// </summary>
        [KSPField]
        public bool wrap = true;

        [KSPField(isPersistant = true)]
        public float animTime = 0f;

        [Persistent]
        public string configNodeData = String.Empty;

        private WheelAnimationHandler animationControl;

        public override void OnLoad(ConfigNode node)
        {
            if (string.IsNullOrEmpty(configNodeData))
            {
                configNodeData = node.ToString();
            }
            base.OnLoad(node);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
        }

        public void Update()
        {
            if (controller == null || wheel == null) { return; }
            if (controller.wheelState == KSPWheelState.DEPLOYED)
            {
                if (animationControl.state != KSPWheelState.DEPLOYED)
                {
                    animationControl.setToAnimationState(KSPWheelState.DEPLOYED, false);
                }
                //calculate the speed at which to play the animation
                float speed = 0f;
                int len = controller.wheelData.Length;
                for (int i = 0; i < len; i++)
                {
                    speed += Mathf.Abs(controller.wheelData[i].wheel.linearVelocity);
                }
                speed /= len;
                speed /= controller.maxSpeed;
                speed *= Mathf.Sign(wheel.rpm);
                if (invertAnimation) { speed = -speed; }
                animationControl.setAnimSpeedMult(speed);
                //update the effect for the current 'speed percent' value
                if (!string.IsNullOrEmpty(effectName))
                {
                    part.Effect(effectName, speed);
                }
                animationControl.updateAnimationState();
            }
            else
            {
                if (!string.IsNullOrEmpty(effectName)) { part.Effect(effectName, 0f); }
            }
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            setupAnimationController();
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
        }

        internal override void onStateChanged(KSPWheelState oldState, KSPWheelState newState)
        {
            base.onStateChanged(oldState, newState);
            if (newState == KSPWheelState.DEPLOYED)
            {
                if (animationControl.state != KSPWheelState.DEPLOYED)
                {
                    animationControl.setToAnimationState(KSPWheelState.DEPLOYED, false);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(effectName)) { part.Effect(effectName, 0f); }
                animationControl.setToAnimationState(KSPWheelState.RETRACTED, false);
            }
        }

        private void setupAnimationController()
        {
            animationControl = new WheelAnimationHandler(this, animationName, animationSpeed, animationLayer, controller.wheelState, false, wrap? WrapMode.Loop : WrapMode.PingPong);
            ConfigNode node = ConfigNode.Parse(configNodeData);
            if (node != null)
            {
                node = node.nodes[0];
                if (node != null)
                {
                    ConfigNode[] animNodes = node.GetNodes("ANIMATION");
                    animationControl.loadSecondaryAnimations(animNodes);
                }
            }
            animationControl.setToAnimationState(controller.wheelState==KSPWheelState.DEPLOYED?KSPWheelState.DEPLOYED : KSPWheelState.RETRACTED, false);
        }

        /// <summary>
        /// Callback from animationControl for when an animation transitions from one state to another
        /// </summary>
        /// <param name="state"></param>
        public void onAnimationStateChanged(KSPWheelState state)
        {
            if (state != KSPWheelState.DEPLOYING)
            {
                if (!string.IsNullOrEmpty(effectName)) { part.Effect(effectName, 0f); }
            }
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
            }
        }

    }
}
