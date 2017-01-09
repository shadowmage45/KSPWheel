using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelScaling : KSPWheelSubmodule, IPartCostModifier, IPartMassModifier
    {

        [KSPField]
        public float minScale = 0.1f;

        [KSPField]
        public float maxScale = 40f;

        [KSPField(guiName = "Scale", guiActive = false, guiActiveEditor = true, isPersistant = true, guiUnits = "x"),
         UI_FloatEdit(suppressEditorShipModified = true, minValue = 0.1f, maxValue = 40f, incrementLarge = 1f, incrementSmall = 0.25f, incrementSlide = 0.01f)]
        public float scale = 1f;

        [KSPField]
        public float wheelMassScalingPower = 3f;

        [KSPField]
        public float wheelMaxSpeedScalingPower = 3f;

        [KSPField]
        public float wheelMaxLoadScalingPower = 3f;

        [KSPField]
        public float rollingResistanceScalingPower = 3f;

        [KSPField]
        public float partMassScalingPower = 3f;

        [KSPField]
        public float partCostScalingPower = 3f;

        [KSPField]
        public float motorTorqueScalingPower = 3f;

        [KSPField]
        public float motorPowerScalingPower = 3f;

        [KSPField]
        public float motorMaxRPMScalingPower = 3f;
        
        private bool initialized = false;

        private void onScaleAdjusted(BaseField field, System.Object obj)
        {
            setScale(scale);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            initialize();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(scale)].uiControlEditor.onFieldChanged = onScaleAdjusted;
            initialize();
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            return -defaultCost + Mathf.Pow(defaultCost, partCostScalingPower);
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            return -defaultMass + Mathf.Pow(defaultMass, partMassScalingPower);
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        internal override void postControllerSetup()
        {
            base.postControllerSetup();
            controller.onScaleUpdated(this);
        }

        private void initialize()
        {
            if (initialized) { return; }
            initialized = true;
            setScale(scale);
        }

        private void setScale(float newScale)
        {
            Vector3 scale = new Vector3(newScale, newScale, newScale);
            Transform modelRoot = part.transform.FindRecursive("model");
            foreach(Transform child in modelRoot)
            {
                child.localScale = scale;
            }
            if (controller != null)
            {
                controller.onScaleUpdated(this);
            }
        }
    }
}
