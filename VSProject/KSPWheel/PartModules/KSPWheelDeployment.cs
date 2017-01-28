using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelDeployment : KSPWheelSubmodule, IMultipleDragCube
    {

        [KSPField]
        public string animationName = string.Empty;

        [KSPField]
        public float animationSpeed = 1;

        [KSPField]
        public int animationLayer = 1;

        [KSPField]
        public string tempColliderName = "deployTgt";

        [KSPField]
        public float tempColliderOffset = 0f;

        [KSPField]
        public float tempColliderRadius = 0.1f;

        [KSPField]
        public string deployEffect = string.Empty;

        [KSPField]
        public string deployedEffect = string.Empty;

        [KSPField]
        public string retractEffect = string.Empty;

        [KSPField]
        public string retractedEffect = string.Empty;

        [KSPField]
        public bool updateDragCubes = true;

        [KSPField]
        public bool oneShotAnimation = false;

        [KSPField(isPersistant = true)]
        public bool oneShotTriggered = false;

        [KSPField]
        public bool invertAnimation = false;

        [Persistent]
        public string configNodeData = String.Empty;

        private CapsuleCollider collider;
        private Transform tempColliderTransform;
        private WheelAnimationHandler animationControl;
        private ModuleLight lightModule;

        public bool IsMultipleCubesActive
        {
            get
            {
                return updateDragCubes;
            }
        }

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

        public void deploy()
        {
            if (controller == null) { return; }
            if (controller.wheelState == KSPWheelState.RETRACTED || controller.wheelState == KSPWheelState.RETRACTING) { toggleDeploy(); }
        }

        public void retract()
        {
            if (controller == null) { return; }
            if (controller.wheelState == KSPWheelState.DEPLOYED || controller.wheelState == KSPWheelState.DEPLOYING) { toggleDeploy(); }
        }

        private void toggleDeploy()
        {
            if (animationControl == null)
            {
                MonoBehaviour.print("Animation control is null!");
                return;
            }
            if (oneShotAnimation && oneShotTriggered) { return; }
            if (controller.wheelState == KSPWheelState.DEPLOYED || controller.wheelState == KSPWheelState.DEPLOYING)
            {
                changeWheelState(KSPWheelState.RETRACTING);
                animationControl.setToAnimationState(controller.wheelState, false);
                if (collider != null)
                {
                    float totalHeight = wheel.length + wheel.radius * 2 - wheel.compressionDistance;
                    collider.height = totalHeight;
                    collider.center = new Vector3(0, -collider.height * 0.5f + wheel.radius, 0);
                    collider.enabled = true;
                }
                if (!string.IsNullOrEmpty(retractEffect)) { part.Effect(retractEffect, 1f); }
                if (!string.IsNullOrEmpty(retractedEffect)) { part.Effect(retractedEffect, 0f); }
                if (!string.IsNullOrEmpty(deployEffect)) { part.Effect(deployEffect, 0f); }
                if (!string.IsNullOrEmpty(deployedEffect)) { part.Effect(deployedEffect, 0f); }
                oneShotTriggered = oneShotTriggered || HighLogic.LoadedSceneIsFlight;
            }
            else if (controller.wheelState == KSPWheelState.RETRACTED || controller.wheelState == KSPWheelState.RETRACTING)
            {
                changeWheelState(KSPWheelState.DEPLOYING);
                animationControl.setToAnimationState(controller.wheelState, false);
                if (collider != null)
                {
                    float totalHeight = wheel.length + wheel.radius * 2;
                    collider.height = totalHeight;
                    collider.center = new Vector3(0, -collider.height * 0.5f + wheel.radius, 0);
                    collider.enabled = true;
                }
                if (!string.IsNullOrEmpty(retractEffect)) { part.Effect(retractEffect, 0f); }
                if (!string.IsNullOrEmpty(retractedEffect)) { part.Effect(retractedEffect, 0f); }
                if (!string.IsNullOrEmpty(deployEffect)) { part.Effect(deployEffect, 1f); }
                if (!string.IsNullOrEmpty(deployedEffect)) { part.Effect(deployedEffect, 0f); }
                oneShotTriggered = oneShotTriggered || HighLogic.LoadedSceneIsFlight;
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            if (string.IsNullOrEmpty(configNodeData))
            {
                configNodeData = node.ToString();
            }
            base.OnLoad(node);
        }

        public void Update()
        {
            if (animationControl != null)
            {
                animationControl.updateAnimationState();
                updateDragCube();
                if (controller != null)
                {
                    float time = animationControl.animationTime;
                    if (invertAnimation) { time = 1 - time; }
                    controller.deployAnimationTime = time;
                }
            }
        }

        private void setupAnimationController()
        {
            animationControl = new WheelAnimationHandler(this, animationName, animationSpeed, animationLayer, controller.wheelState, invertAnimation);
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
            animationControl.setToAnimationState(controller.wheelState, false);
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            setupAnimationController();
            lightModule = part.GetComponent<ModuleLight>();
            if (lightModule != null && controller.wheelState == KSPWheelState.DEPLOYED)
            {
                lightModule.LightsOn();
            }
            if (vessel != null)
            {
                vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, controller.wheelState == KSPWheelState.DEPLOYED);
            }
            tempColliderTransform = part.transform.FindRecursive(tempColliderName);
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            if (HighLogic.LoadedSceneIsEditor) { return; }
            if (tempColliderTransform != null)
            {
                GameObject standInCollider = new GameObject("KSPWheelTempCollider");
                //nest it to the wheel collider, with y+ orientation
                standInCollider.transform.NestToParent(tempColliderTransform);
                Vector3 pos = standInCollider.transform.localPosition;
                pos.y += tempColliderOffset * part.rescaleFactor;
                standInCollider.transform.localPosition = pos;
                standInCollider.layer = 26;
                collider = standInCollider.AddComponent<CapsuleCollider>();
                collider.radius = tempColliderRadius;
                collider.height = wheel.length + wheel.radius * 2;
                collider.center = new Vector3(0, -collider.height * 0.5f + wheel.radius, 0);
                collider.enabled = controller.wheelState == KSPWheelState.RETRACTING || controller.wheelState == KSPWheelState.DEPLOYING;
                CollisionManager.IgnoreCollidersOnVessel(vessel, collider);
            }
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            if (collider != null)
            {
                collider.enabled = controller.wheelState == KSPWheelState.RETRACTING || controller.wheelState==KSPWheelState.DEPLOYING;
            }
        }

        /// <summary>
        /// Callback from animationControl for when an animation transitions from one state to another
        /// </summary>
        /// <param name="state"></param>
        public void onAnimationStateChanged(KSPWheelState state)
        {
            changeWheelState(state);
            if (state == KSPWheelState.RETRACTED)
            {
                //TODO reset suspension and steering transforms to neutral?
                if (lightModule != null) { lightModule.LightsOff(); }
                if (!string.IsNullOrEmpty(retractEffect)) { part.Effect(retractEffect, 0f); }
                if (!string.IsNullOrEmpty(retractedEffect)) { part.Effect(retractedEffect, 1f); }
                if (!string.IsNullOrEmpty(deployEffect)) { part.Effect(deployEffect, 0f); }
                if (!string.IsNullOrEmpty(deployedEffect)) { part.Effect(deployedEffect, 0f); }
            }
            else if (state == KSPWheelState.DEPLOYED)
            {
                if (lightModule != null) { lightModule.LightsOn(); }
                if (!string.IsNullOrEmpty(retractEffect)) { part.Effect(retractEffect, 0f); }
                if (!string.IsNullOrEmpty(retractedEffect)) { part.Effect(retractedEffect, 0f); }
                if (!string.IsNullOrEmpty(deployEffect)) { part.Effect(deployEffect, 0f); }
                if (!string.IsNullOrEmpty(deployedEffect)) { part.Effect(deployedEffect, 1f); }
            }
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
            }
        }

        private void updateDragCube()
        {
            if (!updateDragCubes) { return; }
            float time = animationControl.animationTime;
            part.DragCubes.SetCubeWeight("Retracted", 1f - time);
            part.DragCubes.SetCubeWeight("Deployed", time);
        }

        public string[] GetDragCubeNames()
        {
            if (!updateDragCubes) { return new String[] { "Default" }; }
            return new String[] { "Retracted", "Deployed" };
        }

        public void AssumeDragCubePosition(string name)
        {
            if (animationControl == null || !updateDragCubes) { return; }
            switch (name)
            {
                case "Retracted":
                    animationControl.setToAnimationState(KSPWheelState.RETRACTED, false);
                    break;
                case "Deployed":
                    animationControl.setToAnimationState(KSPWheelState.DEPLOYED, false);
                    break;
                default:
                    break;
            }
        }

        public bool UsesProceduralDragCubes()
        {
            return false;
        }
    }
}
