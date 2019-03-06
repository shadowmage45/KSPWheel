using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelTracks : KSPWheelMotor
    {

        [KSPField(guiName = "Track Length Adjust", guiActive =false, guiActiveEditor = false)
         ,UI_FloatEdit(minValue = -100f, maxValue = 100f, incrementLarge = 5f, incrementSmall = 1f, incrementSlide = 0.1f)]
        public float trackLength = 10f;

        [KSPField]
        public int trackDir = 1;

        [KSPField]
        public int smrIndex = 0;

        [KSPField(guiName = "Invert Track Surface Direction", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(scene = UI_Scene.Editor, controlEnabled = true, requireFullControl = false, disabledText = "Standard", enabledText = "Inverted", affectSymCounterparts = UI_Scene.None, suppressEditorShipModified = true)]
        public bool invertTrackTexture = false;

        [KSPField(guiName = "Display Fwd Rotation", guiActive = false, guiActiveEditor = true, isPersistant = false),
         UI_Toggle(enabledText = "True", disabledText = "False", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public bool editorRotation = false;

        private float factorSum;
        private float[] shares;
        private SkinnedMeshRenderer smr;
        private Vector2 offset = Vector2.zero;
        private Material mat;
        private float trackVelocity = 0f;//velocity of the track surface, in m/s

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(invertTrackTexture)].uiControlEditor.onFieldChanged = invertTrackTextureClicked;
            Fields[nameof(trackLength)].guiActiveEditor = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().debugMode;
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            updateTrackTextureScale();
            if (HighLogic.LoadedSceneIsEditor) { return; }
            updateScaleValues();
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (mat != null && controller.wheelState==KSPWheelState.DEPLOYED)
            {
                offset.x += (-trackVelocity * Time.deltaTime * trackDir) / (trackLength * part.rescaleFactor * controller.scale) * (invertTrackTexture ? -1 : 1);
                mat.SetTextureOffset("_MainTex", offset);
                mat.SetTextureOffset("_BumpMap", offset);
            }
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (editorRotation && mat != null && controller.wheelState == KSPWheelState.DEPLOYED)
                {
                    float trackVelocity = invertMotor? -1 : 1;
                    offset.x += (-trackVelocity * Time.deltaTime * trackDir) / (trackLength * part.rescaleFactor * controller.scale) * (invertTrackTexture ? -1 : 1);
                    mat.SetTextureOffset("_MainTex", offset);
                    mat.SetTextureOffset("_BumpMap", offset);
                }
            }
        }

        protected void updateScaleValues()
        {
            if (this.wheel == null) { return; }//wheel not initialized

            KSPWheelCollider wheel;
            int len = controller.wheelData.Length;
            factorSum = 0;
            float[] factors = new float[len];
            shares = new float[len];
            for (int i = 0; i < len; i++)
            {
                wheel = controller.wheelData[i].wheel;
                factors[i] = wheel.momentOfInertia / wheel.radius;
                factorSum += factors[i];
            }
            for (int i = 0; i < len; i++)
            {
                shares[i] = factors[i] / factorSum;
            }
        }

        /// <summary>
        /// This method sets each wheel torque to that which would be needed
        /// exert the same linear velocity change on each wheel in the group.
        /// The moment of inertia and radius of each wheel determines a wheels share
        /// of the torque.
        /// </summary>
        protected override void updateMotor()
        {
            base.updateMotor();
            if (shares == null) { updateScaleValues(); }
            float totalSystemTorque = 0f;
            float totalBrakeTorque = this.wheel.brakeTorque;
            float totalMotorTorque = torqueOutput;
            KSPWheelCollider wheel;
            int len = controller.wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                wheel = controller.wheelData[i].wheel;
                totalSystemTorque += wheel.angularVelocity * wheel.momentOfInertia;
            }
            for (int i = 0; i < len; i++)
            {
                wheel = controller.wheelData[i].wheel;
                wheel.motorTorque = shares[i] * totalMotorTorque;
                wheel.brakeTorque = shares[i] * totalBrakeTorque;
                wheel.angularVelocity = shares[i] * totalSystemTorque / wheel.momentOfInertia;
            }
            trackVelocity = this.wheel.linearVelocity;
        }

        private void invertTrackTextureClicked(BaseField a, System.Object b)
        {
            updateTrackTextureScale();
        }

        private void updateTrackTextureScale()
        {
            SkinnedMeshRenderer[] smrs = part.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (smrs != null && smrs.Length > 0)
            {
                smr = part.GetComponentsInChildren<SkinnedMeshRenderer>()[smrIndex];
                if (smr != null)
                {
                    mat = smr.material;
                    if (mat != null)
                    {
                        Vector2 scaling = mat.mainTextureScale;
                        scaling.x = trackDir * (invertTrackTexture ? -1 : 1);
                        mat.SetTextureScale("_MainTex", scaling);
                        //special feature of TU shaders to flip the X coordinate of the normal map, fixes lighting issues with negative UV scaling
                        mat.SetFloat("_NormalFlipX", scaling.x);
                    }
                }
            }
        }

    }
}
