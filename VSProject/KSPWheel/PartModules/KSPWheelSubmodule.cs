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

        /// <summary>
        /// The index of the wheel that this module controls, in case multiple wheel colliders exist on the part
        /// </summary>
        [KSPField]
        public int wheelIndex = 0;

        internal KSPWheelBase controller;
        internal Transform wheelTransform;
        internal KSPWheelBase.KSPWheelData wheelData;
        internal KSPWheelCollider wheel;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            //this only runs during prefab creation -- 
            //initialize controller on prefab parts, so that the get-module-info callbacks can function properly
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
            {
                initializeController();
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (controller != null) { return; }
            //this runs in the editor and flight-scenes, to fully initialize the part
            initializeController();
        }

        /// <summary>
        /// Internal call to set up references to the controller module, and to start any initialization operations needed by the submodule.
        /// </summary>
        private void initializeController()
        {
            KSPWheelBase[] bases = part.GetComponents<KSPWheelBase>();
            controller = bases[baseModuleIndex];
            if (controller == null)
            {
                throw new NullReferenceException("ERROR: Could not locate KSPWheelBase controller module for wheel system.");
            }
            controller.addSubmodule(this);
            wheelData = controller.wheelData[wheelIndex];
            postControllerSetup();
            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                onUIControlsUpdated(controller.showControls);
            }            
        }

        public virtual void OnDestroy()
        {
            if (controller != null)
            {
                controller.removeSubmodule(this);
            }
        }

        /// <summary>
        /// Callback from the controller when the wheel data has been created.  If called outside of the flight-scene, the wheel-collider will not be
        /// fully initialized, as it requires a RigidBody, which is only created in flight scene.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="collider"></param>
        internal void onWheelCreated(Transform transform, KSPWheelCollider collider)
        {
            this.wheelTransform = transform;
            this.wheel = collider;
            postWheelCreated();
        }

        /// <summary>
        /// Called directly after the wheel has been initialized in the flight or editor scenes.
        /// This is the first point at which the wheel and wheel transform fields are populated,
        /// and should be used to do any setup necessary prior to physics being enabled.<para/>
        /// Physics updates will only be enabled in the flight scene, though the wheel data instance
        /// will be populated in the editor scene as well.
        /// </summary>
        internal virtual void postWheelCreated()
        {

        }

        /// <summary>
        /// Called after controller module has been initialized.  At this point the control modules state has been loaded, but no other cross-module state is guaranteed.
        /// May be called from either OnLoad() or OnStart(), whichever is called first.<para/>
        /// Will be called during prefab loading, and implementing classes must guard against access to anything that is not yet accessible (game-state data / game-settings).
        /// </summary>
        internal virtual void postControllerSetup()
        {

        }

        /// <summary>
        /// Called prior to calculating wheel suspension.  This is the chance to set external hit properties in the wheel collider (used by repulsors)
        /// </summary>
        internal virtual void preWheelSuspensionCalc()
        {

        }

        /// <summary>
        /// Called prior to updating wheel physics (but after suspension hit calculation), only if wheel physics are enabled for that frame.
        /// </summary>
        internal virtual void preWheelPhysicsUpdate()
        {

        }

        /// <summary>
        /// Called after updating wheel physics (after physics forces have been applied), only if wheel physics are enabled for that frame
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

        /// <summary>
        /// Called from the base-module whenever the 'toggle UI visibility' button is pressed.<para/>
        /// Implementing classes should show or hide their UI controls based on the value passed into the 'show' variable.
        /// </summary>
        /// <param name="show"></param>
        internal virtual void onUIControlsUpdated(bool show)
        {

        }

        /// <summary>
        /// Called whenever the parts model scale is changed.
        /// Implementing classes should update any internal values as needed for the new scale (which may be queried from the controller)
        /// </summary>
        internal virtual void onScaleUpdated()
        {

        }

        /// <summary>
        /// Called whenever the wheels deployment state / broken state is changed.
        /// Implementing classes should use this callback to update any internal values for the new state.
        /// </summary>
        /// <param name="oldState"></param>
        /// <param name="newState"></param>
        internal virtual void onStateChanged(KSPWheelState oldState, KSPWheelState newState)
        {

        }

        /// <summary>
        /// Implementing classes should return a string containing module-specific information.  This information will be grouped together
        /// and displayed in the editor part-list tooltip module description.<para/>
        /// Each line should contain one line of information, and may contain formatting data as used by KSP (unknown).
        /// </summary>
        /// <returns></returns>
        internal virtual string getModuleInfo()
        {
            return string.Empty;
        }

        /// <summary>
        /// Callback for when a part is being deployed and the deployment time has changed.  To be used for modules that require knowledge of an in-progress deployment.
        /// Implementing classes should use this callback to update any internal state needed for the passed in deployment value.<para/>
        /// Values will range from zero (0) for undeployed/stowed, to one (1) for fully deployed/extended.
        /// </summary>
        /// <param name="value"></param>
        internal virtual void onDeployTimeUpdated(float value)
        {

        }

        /// <summary>
        /// Internal utility method for submodules to call when they need to request a state change from the controller.  This will cause the controller to
        /// update its cached state values, and will then issue state-changed callbacks to all submodules.
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="selfCallback"></param>
        internal void changeWheelState(KSPWheelState newState, bool selfCallback = false)
        {
            if (controller != null)
            {
                controller.changeWheelState(newState, this, selfCallback);
            }
        }

    }
}
