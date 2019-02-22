using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{

    public class KSPWheelAdjustableDeployment : KSPWheelSubmodule
    {

        /*
        This module is intended to provide a user-adjustable transform movement
        capability to an otherwise non-animated transform within the model.

        It will interface with the KSPWheelDeployment module to allow for the
        transform animation to be linked to a standard animation deployment setup,
        so as to ensure that the user-controlled portions also deploy/retract along
        with the main animation.  This ability can be enabled on a per-transform
        or per-module basis, and is optional.  If not linked to an animation module,
        the user controls will be available at all times, and may be controlled only
        by the module's float control.

        This module must be positioned in the config file AFTER any KSPWheelDeployment
        or KSPWheelSidedModel modules.

        Each module will expose a single float control to allow for user adjustment
        of its controlled transforms.

        Each module may control multiple transforms, of different names, and even
        with different types of transformation (rotation, translate, scale).  These
        will be specified through a multi-node config setup in the part module config
        within the part config.

        Each transform configuration entry will specify the type of manipulation that
        it affects (rot/tran/scale), and will specify in LOCAL SPACE the extents of
        movement compared to the base model.  When the model is initially loaded by
        the part prefab, the starting data for the specified transform will be cached,
        and the users selected values will be applied from that cached starting state.

            TODO -- bloody hell, apparently when parts are cloned in the editor...
            TODO -- none of the inner class values are serialized properly
            TODO -- so... somehow need to reset the cloned transforms into prefab state, or
            TODO -- otherwise persist the control values to 'rewind' the controls back
            TODO -- to a known 'default' value
        */

        [KSPField]
        public bool useDeployModule = false;

        [KSPField]
        public string controlName = "User Control";

        /// <summary>
        /// The current value of the deployment, in the 0-1 range.
        /// 0 = At default position/rotation/scale
        /// 1 = At config-specified 'end-point' of pos/rot/scale (may be +/- compared to defaults, in any/all axis)
        /// </summary>
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiUnits = "%"),
         UI_FloatEdit(minValue =0, maxValue =1, incrementLarge = 0.25f, incrementSmall = 0.05f, incrementSlide = 0.01f, suppressEditorShipModified = true, sigFigs = 2)]
        public float ControlValue = 0f;

        [Persistent]
        public string configNodeData;

        private TransformData[] transformData;
        private bool initialized = false;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (string.IsNullOrEmpty(configNodeData)) { configNodeData = node.ToString(); }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            initialize();

            Fields[nameof(ControlValue)].guiName = controlName;
            Callback<BaseField, object> fieldChanged = (a, b) => 
            {
                this.symmetryUpdate(m => 
                {
                    if (m != this) { m.ControlValue = this.ControlValue; }
                    int len = m.transformData.Length;
                    for (int i = 0; i < len; i++)
                    {
                        m.transformData[i].onUserControlUpdated(m.ControlValue);
                    }
                });
            };
            Fields[nameof(ControlValue)].uiControlEditor.onFieldChanged = fieldChanged;
            Fields[nameof(ControlValue)].uiControlFlight.onFieldChanged = fieldChanged;
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
        }

        public override void OnIconCreate()
        {
            base.OnIconCreate();
        }

        private void initialize()
        {
            if (initialized) { return; }
            initialized = true;
            ConfigNode node = ConfigNode.Parse(configNodeData).nodes[0];
            ConfigNode[] trNodes = node.GetNodes("TRANSFORM");
            int len = trNodes.Length;
            transformData = new TransformData[len];
            for (int i = 0; i < len; i++)
            {
                transformData[i] = new TransformData(this, trNodes[i]);
                transformData[i].initializeTransform(!(HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor));
                transformData[i].onAnimationUpdated(controller.deployAnimationTime);
                transformData[i].onUserControlUpdated(ControlValue);
            }
        }

        internal override void onDeployTimeUpdated(float value)
        {
            base.onDeployTimeUpdated(value);
            if (transformData != null)
            {
                int len = transformData.Length;
                for (int i = 0; i < len; i++)
                {
                    transformData[i].onAnimationUpdated(useDeployModule? value : 1.0f);
                }
            }
        }

        /// <summary>
        /// Container class for the parsed config information for a single transform.
        /// </summary>
        public class TransformData
        {

            /// <summary>
            /// Transform name/path.
            /// Config file value.
            /// </summary>
            public string transformName { get; private set; }

            /// <summary>
            /// Endpoint of local position movement.
            /// Config value.
            /// </summary>
            public Vector3 localPosition { get; private set; }

            /// <summary>
            /// Endpoint of local scale adjustment.
            /// Config value.
            /// </summary>
            public Vector3 localScale { get; private set; }

            /// <summary>
            /// Endpoint of local rotation adjustment.
            /// Config value.
            /// </summary>
            public Vector3 localRotation { get; private set; }

            /// <summary>
            /// The controlled transform.  Will be null until explicitly initialized by the initialization method.
            /// </summary>
            public Transform Transform { get; private set; }

            /// <summary>
            /// The original/base local position of the transform.
            /// Cached from the transform on model creation.
            /// </summary>
            public Vector3 DefaultLocalPosition { get; private set; }

            /// <summary>
            /// The original/base local scale of the transform.
            /// Cached from the transform on model creation.
            /// </summary>
            public Vector3 DefaultLocalScale { get; private set; }

            /// <summary>
            /// The original/base local rotation of the transform.
            /// Cached from the transform on model creation.
            /// </summary>
            public Quaternion DefaultLocalRotation { get; private set; }

            /// <summary>
            /// Parent module, for access to Part, transforms, etc.
            /// </summary>
            private KSPWheelAdjustableDeployment parent { get; set; }

            /// <summary>
            /// Cached value for animation control input, defaults to 1.0
            /// </summary>
            private float animationControlValue { get; set; } = 1.0f;

            /// <summary>
            /// Cached value for user control input
            /// </summary>
            private float userControlValue { get; set; } = 0f;

            /// <summary>
            /// Constructor, providing config node with transform name data, and reference to parent module for access to Part/etc.
            /// </summary>
            /// <param name="node"></param>
            public TransformData(KSPWheelAdjustableDeployment parent, ConfigNode node)
            {
                this.parent = parent;
                this.transformName = node.GetStringValue(nameof(transformName));
                if (string.IsNullOrEmpty(transformName)) { MonoBehaviour.print("ERROR: Tranform name for adjustable deployment was invalid"); }
                this.localPosition = node.GetVector3(nameof(localPosition), Vector3.zero);
                this.localScale = node.GetVector3(nameof(localScale), Vector3.zero);
                this.localRotation = node.GetVector3(nameof(localRotation), Vector3.zero);
            }

            /// <summary>
            /// Load the 'transform' and grab default values for pos/rot/scale
            /// </summary>
            /// <param name="prefab"></param>
            public void initializeTransform(bool prefab)
            {
                this.Transform = parent.part.transform.FindRecursive("model").FindRecursive(transformName);
                if (this.Transform == null)
                {
                    MonoBehaviour.print("ERROR: Could not locate transform for name: " + transformName);
                    return;
                }
                DefaultLocalPosition = Transform.localPosition;
                DefaultLocalScale = Transform.localScale;
                DefaultLocalRotation = Transform.localRotation;
            }

            /// <summary>
            /// Load any persistent data from the input string.  TODO
            /// </summary>
            /// <param name="rawData"></param>
            public void loadPersistentData(string rawData)
            {

            }

            /// <summary>
            /// Return a string containing the persistent data for this transform.  TODO
            /// </summary>
            /// <returns></returns>
            public string savePersistentData()
            {
                return string.Empty;
            }

            /// <summary>
            /// Callback for when user GUI control value is changed (or reloaded)
            /// </summary>
            /// <param name="newValue"></param>
            public void onUserControlUpdated(float newValue)
            {
                if (newValue != userControlValue)
                {
                    userControlValue = newValue;
                    updateTransform();
                }
            }

            /// <summary>
            /// Callback for when animation time value is changed (or reloaded)
            /// </summary>
            /// <param name="newValue"></param>
            public void onAnimationUpdated(float newValue)
            {
                if (newValue != animationControlValue)
                {
                    animationControlValue = newValue;
                    updateTransform();
                }
            }

            /// <summary>
            /// Internal function to update the transform location/rotation/scale, in that order.
            /// Values are only updated if the config specified values are non-zero.
            /// </summary>
            private void updateTransform()
            {
                float compositeValue = userControlValue * animationControlValue;
                Transform.localPosition = DefaultLocalPosition;
                Transform.localRotation = DefaultLocalRotation;
                Transform.localScale = DefaultLocalScale;
                if (localPosition != Vector3.zero)
                {
                    Transform.localPosition += localPosition * compositeValue;
                }
                if (localRotation != Vector3.zero)
                {
                    Transform.Rotate(localRotation * compositeValue, Space.Self);
                }
                if (localScale != Vector3.zero)
                {
                    Transform.localScale += localScale * compositeValue;
                }
            }

        }

    }

}
