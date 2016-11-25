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

        /// <summary>
        /// Called directly after the wheel has been initialized in the flight scene.
        /// This is the first point at which the wheel and wheel transform fields are populated,
        /// and should be used to do any setup necessary prior to physics being enabled.
        /// </summary>
        internal virtual void postWheelCreated()
        {

        }

        /// <summary>
        /// Called after controller module has been initialized.  At this point the control modules state has been loaded, but no other cross-module state is guaranteed
        /// </summary>
        internal virtual void postControllerSetup()
        {

        }

        /// <summary>
        /// Called prior to updating wheel physics, only if wheel physics are enabled for that frame.
        /// </summary>
        internal virtual void preWheelPhysicsUpdate()
        {

        }

        /// <summary>
        /// Called after updating wheel physics, only if wheel physics are enabled for that frame
        /// </summary>
        internal virtual void postWheelPhysicsUpdate()
        {

        }

        /// <summary>
        /// Called during controlling modules Update() method, only if there is a valid wheel object
        /// </summary>
        internal virtual void preWheelFrameUpdate()
        {

        }

        internal virtual void onWheelConfigChanged(KSPWheelSubmodule module)
        {

        }

    }
}
