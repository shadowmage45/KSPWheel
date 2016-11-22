using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelDeployment : KSPWheelSubmodule
    {

        #region REGION - Animation handling

        [KSPField]
        public string animationName = String.Empty;
        [KSPField]
        public float animationSpeed = 1;
        [KSPField]
        public int animationLayer = 1;

        #endregion

        private WheelAnimationHandler animationControl;
        private ModuleLight lightModule;
        
        [KSPAction("Toggle Gear", KSPActionGroup.Gear)]
        public void toggleGearAction(KSPActionParam param)
        {
            if (param.type == KSPActionType.Activate) { deploy(); }
            else if (param.type == KSPActionType.Deactivate) { retract(); }
        }

        [KSPEvent(guiName = "Toggle Gear", guiActive = true, guiActiveEditor = true)]
        public void toggleGearEvent()
        {
            toggleDeploy();
        }

        private void deploy()
        {
            if (controller.wheelState == KSPWheelState.RETRACTED || controller.wheelState == KSPWheelState.RETRACTING) { toggleDeploy(); }
        }

        private void retract()
        {
            if (controller.wheelState == KSPWheelState.DEPLOYED || controller.wheelState == KSPWheelState.DEPLOYING) { toggleDeploy(); }
        }

        private void toggleDeploy()
        {
            if (animationControl == null)
            {
                MonoBehaviour.print("Animation control is null!");
                return;
            }
            if (controller.wheelState == KSPWheelState.DEPLOYED || controller.wheelState == KSPWheelState.DEPLOYING)
            {
                controller.wheelState = KSPWheelState.RETRACTING;
                animationControl.setToAnimationState(controller.wheelState, false);
            }
            else if (controller.wheelState == KSPWheelState.RETRACTED || controller.wheelState == KSPWheelState.RETRACTING)
            {
                controller.wheelState = KSPWheelState.DEPLOYING;
                animationControl.setToAnimationState(controller.wheelState, false);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Events["toggleGearEvent"].active = animationControl != null;
            Actions["toggleGearAction"].active = animationControl != null;
        }

        public void Start()
        {
            animationControl.setToAnimationState(controller.wheelState, false);
            lightModule = part.GetComponent<ModuleLight>();
            if (lightModule != null && controller.wheelState == KSPWheelState.DEPLOYED)
            {
                lightModule.LightsOn();
            }
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            animationControl = new WheelAnimationHandler(this, animationName, animationSpeed, animationLayer, controller.wheelState);
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            animationControl.updateAnimationState();
        }

        /// <summary>
        /// Callback from animationControl for when an animation transitions from one state to another
        /// </summary>
        /// <param name="state"></param>
        public void onAnimationStateChanged(KSPWheelState state)
        {
            wheelState = state;
            if (state == KSPWheelState.RETRACTED)
            {
                //TODO reset suspension and steering transforms to neutral?
                if (lightModule != null) { lightModule.LightsOff(); }
            }
            else if (state == KSPWheelState.DEPLOYED)
            {
                if (lightModule != null) { lightModule.LightsOn(); }
            }
        }

    }
}
