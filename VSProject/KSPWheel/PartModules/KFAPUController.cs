using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KFAPUController : ModuleResourceConverter
    {

        [KSPField]
        public string throttleEffect = string.Empty;

        [KSPField]
        public string startEffect = string.Empty;

        [KSPField]
        public string stopEffect = string.Empty;

        [KSPField]
        public string noFuelEffect = string.Empty;

        [KSPField(guiName = "Throttle", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 0.5f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public float throttle = 0f;

        [KSPField(guiName = "Target Charge Level", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 0.5f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public float target = 75f;

        [KSPField(guiName = "Auto Throttle", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(suppressEditorShipModified = true)]
        public bool autoThrottle = true;

        [KSPField(guiName = "Link To Main Throttle", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(suppressEditorShipModified = true)]
        public bool linkedThrottle = false;

        [KSPField(guiName = "Output", guiUnits = "EC/s", guiFormat = "F2", guiActive = true, guiActiveEditor = false)]
        public float energyOutput = 0f;

        [KSPField(guiName = "Mode", guiActive =true, guiActiveEditor = true, isPersistant = true)]
        public string modeDisplay = "Closed Cycle";

        [KSPField(isPersistant = true)]
        public bool closedMode = true;

        [SerializeField]
        private ResourceRatio closedRatio;
        [SerializeField]
        private ResourceRatio openRatio;

        [KSPEvent(guiName = "Toggle Mode", guiActive = true, guiActiveEditor = true)]
        public void toggleMode()
        {
            this.closedMode = !this.closedMode;
            updateRecipe();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (inputList.Count >= 3)
            {
                closedRatio = inputList[1];
                openRatio = inputList[2];
            }
            updateRecipe();
        }

        public override string GetInfo()
        {
            return base.GetInfo();
        }

        public void Update()
        {
            if (!string.IsNullOrEmpty(throttleEffect))
            {
                part.Effect(throttleEffect, IsActivated? throttle * 0.01f : 0f);
            }
        }

        public override void FixedUpdate()
        {            
            if (HighLogic.LoadedSceneIsFlight && IsActivated)
            {
                if (autoThrottle)
                {
                    PartResource pr = part.Resources.Get("ElectricCharge");
                    float ecp = pr.maxAmount==0? 1 : (float)(pr.amount / pr.maxAmount);
                    float t = 0f;
                    if (ecp < target * 0.01f)
                    {
                        t = 100f;
                    }
                    else
                    {
                        t = 0f;
                    }
                    throttle = Mathf.MoveTowards(throttle, t, Time.fixedDeltaTime * 100f);
                }
                else if (linkedThrottle)
                {
                    throttle = Mathf.MoveTowards(throttle, vessel.ctrlState.mainThrottle * 100f, Time.fixedDeltaTime * 100f);
                }
                this.EfficiencyBonus = throttle * 0.01f;
            }   
            base.FixedUpdate();
        }

        protected override void PostProcess(ConverterResults result, double deltaTime)
        {
            base.PostProcess(result, deltaTime);
            energyOutput = (float)(result.TimeFactor * Recipe.Outputs[0].Ratio) / (float)deltaTime;
        }

        private void updateRecipe()
        {
            modeDisplay = closedMode ? "Closed Cycle" : "Open Cycle";
            if (inputList.Count >= 3)
            {
                inputList.RemoveAt(1);
                inputList.RemoveAt(1);
            }
            else
            {
                inputList.RemoveAt(1);
            }
            inputList.Add(closedMode ? closedRatio : openRatio);
            this._recipe = null;
            this._recipe = this.LoadRecipe();
        }

    }
}
