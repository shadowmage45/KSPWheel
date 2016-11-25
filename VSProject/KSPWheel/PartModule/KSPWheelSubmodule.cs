using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelSubmodule : PartModule
    {

        /// <summary>
        /// The module index in the set of KSPWheelBase to use (skip any non-wheel-base modules when counting indices).
        /// </summary>
        [KSPField]
        public int baseModuleIndex = 0;

        protected KSPWheelBase controller;
        protected Transform wheelTransform;
        protected KSPWheelCollider wheel;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            KSPWheelBase[] bases = part.GetComponents<KSPWheelBase>();
            controller = bases[baseModuleIndex];
            if (controller == null)
            {
                throw new NullReferenceException("ERROR: Could not locate KSPWheelBase controller module for wheel system.");
            }
            controller.addSubmodule(this);
            postControllerSetup();
        }

        public virtual void OnDestroy()
        {
            if (controller != null)
            {
                controller.removeSubmodule(this);
            }
        }

        internal void onWheelCreated(Transform transform, KSPWheelCollider collider)
        {
            this.wheelTransform = transform;
            this.wheel = collider;
            postWheelCreated();
        }

        internal virtual void postWheelCreated()
        {

        }

        internal virtual void postControllerSetup()
        {

        }

        internal virtual void preWheelPhysicsUpdate()
        {

        }

        internal virtual void postWheelPhysicsUpdate()
        {

        }

        internal virtual void preWheelFrameUpdate()
        {

        }

        internal virtual void onWheelConfigChanged(KSPWheelSubmodule module)
        {

        }

    }
}
