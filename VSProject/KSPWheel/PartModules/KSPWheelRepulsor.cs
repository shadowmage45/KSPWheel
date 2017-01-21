using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelRepulsor : KSPWheelSubmodule
    {

        [KSPField(guiName = "Repulsor Height", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.1f, maxValue = 1, stepIncrement = 0.01f, suppressEditorShipModified = true)]
        public float repulsorHeight = 1f;

        [KSPField(guiName = "Repuslor Power", guiActiveEditor = false, guiActive = true),
         UI_Toggle(enabledText ="On", disabledText ="Off", suppressEditorShipModified = true)]
        public bool repulsorEnabled = true;

        [KSPField(guiName = "Energy Use", guiActive = true, guiUnits = "EC/s")]
        public float guiEnergyUse = 0f;

        [KSPField(guiName = "Force Application", guiActiveEditor = false, guiActive = true),
         UI_Toggle(enabledText = "Offset", disabledText = "Standard", suppressEditorShipModified = true)]
        public bool forcePointOffset = true;

        [KSPField(guiName = "Force Axis", guiActiveEditor = false, guiActive = false),
         UI_Toggle(enabledText = "Suspension", disabledText = "HitNormal", suppressEditorShipModified = true)]
        public bool suspensionNormal = false;

        [KSPField]
        public float easeTimeMult = 0.5f;

        /// <summary>
        /// EC/s * tons of weight supported
        /// </summary>
        [KSPField]
        public float energyUse = 1f;

        [KSPField]
        public float animSpeed = 0.1f;

        [KSPField]
        public int animAxis = 1;

        [KSPField]
        public bool gimbaled = false;

        [KSPField]
        public string gimbalName = String.Empty;

        [KSPField]
        public string gridName = String.Empty;

        [KSPField(guiName = "Light L", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.1f, maxValue = 1, stepIncrement = 0.01f, suppressEditorShipModified = true)]
        public float lightLength = 10f;

        [KSPField(guiName = "Light A", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.1f, maxValue = 1, stepIncrement = 0.01f, suppressEditorShipModified = true)]
        public float lightAngle = 30f;

        [KSPField(guiName = "Light I", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.1f, maxValue = 1, stepIncrement = 0.01f, suppressEditorShipModified = true)]
        public float lightIntensity = 1f;

        private KSPWheelDustEffects dustModule;

        private Transform gimbalTransform;

        private Material gridMaterial;
        private Vector2 offset = Vector2.zero;

        private float curLen;
        private float destLen;
        private Vector3 oceanHitPos;

        private void repulsorToggled(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m => 
            {
                m.repulsorEnabled = repulsorEnabled;
                if (m.repulsorEnabled)
                {
                    m.controller.wheelState = KSPWheelState.DEPLOYED;
                    m.controller.springEaseMult = 0f;
                }
                else
                {
                    //handled by per-tick updating
                }
            });
        }

        private void repulsorHeightUpdated(BaseField field, System.Object ob)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.repulsorHeight = repulsorHeight;
                destLen = m.repulsorHeight;
            });
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!string.IsNullOrEmpty(gimbalName)) { gimbalTransform = part.transform.FindRecursive(gimbalName); }
            Fields[nameof(repulsorEnabled)].uiControlFlight.onFieldChanged = repulsorToggled;
            Fields[nameof(repulsorHeight)].uiControlFlight.onFieldChanged = Fields[nameof(repulsorHeight)].uiControlEditor.onFieldChanged = repulsorHeightUpdated;
            curLen = destLen = repulsorHeight;
            if (!string.IsNullOrEmpty(gridName))
            {
                Transform gridMesh = part.transform.FindRecursive(gridName);
                if (gridMesh != null)
                {
                    Renderer rend = gridMesh.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        gridMaterial = rend.material;
                        MonoBehaviour.print("Found grid object/mat: " + gridMesh + " :: " + gridMaterial);
                    }
                }
            }
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            dustModule = part.GetComponent<KSPWheelDustEffects>();
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();

            if (gridMaterial != null)
            {
                //TODO update texture offsets for grid texture animation
                if (animAxis == 0)
                {
                    offset.x += animSpeed * Time.deltaTime * guiEnergyUse * 0.25f;
                }
                else
                {
                    offset.y += animSpeed * Time.deltaTime * guiEnergyUse * 0.25f;
                }
                gridMaterial.SetTextureOffset("_MainTex", offset);
                gridMaterial.SetTextureOffset("_BumpMap", offset);
                gridMaterial.SetTextureOffset("_Emissive", offset);
            }
        }

        internal override void preWheelSuspensionCalc()
        {
            base.preWheelSuspensionCalc();
            //update repulsor 'length' stats
            wheel.length = curLen * 5f;
            curLen = Mathf.MoveTowards(curLen, destLen, 1 * Time.fixedDeltaTime);
            wheel.useSuspensionNormal = suspensionNormal;
            wheel.forceApplicationOffset = forcePointOffset ? 1f : 0f;
            wheel.useExternalHit = false;
            if (dustModule != null) { dustModule.waterMode = false; }
            if (vessel.mainBody.ocean)
            {
                float alt = FlightGlobals.getAltitudeAtPos(wheel.transform.position);
                float susLen = wheel.length + wheel.radius;
                if (alt > susLen)//impossible that wheel contacted surface regardless of orientation
                {
                    return;
                }
                Vector3 surfaceNormal = vessel.mainBody.GetSurfaceNVector(vessel.latitude, vessel.longitude);
                Vector3 pointOnSurface = wheel.transform.position - alt * surfaceNormal;
                Vector3 oceanHitPos = Vector3.zero;
                if (Utils.rayPlaneIntersect(wheel.transform.position, -wheel.transform.up, pointOnSurface, surfaceNormal, out oceanHitPos))
                {
                    RaycastHit hit;
                    if (Physics.Raycast(wheel.transform.position, -wheel.transform.up, out hit, susLen, controller.raycastMask))
                    {
                        float oceanDistance = (wheel.transform.position - oceanHitPos).magnitude;
                        if (hit.distance < oceanDistance)
                        {
                            return;
                        }
                    }
                    if (alt < 0)//underwater... should probably turn off?
                    {
                        MonoBehaviour.print("UNDERWATER -- TODO");
                        //TODO .... 
                    }
                    else
                    {
                        wheel.useExternalHit = true;
                        wheel.externalHitPoint = oceanHitPos;
                        wheel.externalHitNormal = surfaceNormal;
                        if (dustModule != null) { dustModule.waterMode = true; }
                    }
                }
            }
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();

            if (repulsorEnabled && controller.springEaseMult < 1)
            {
                controller.springEaseMult = Mathf.Clamp01(controller.springEaseMult + Time.fixedDeltaTime * easeTimeMult);
            }
            else if (!repulsorEnabled && controller.springEaseMult > 0)
            {
                controller.springEaseMult = Mathf.Clamp01(controller.springEaseMult - Time.fixedDeltaTime * easeTimeMult);
                if (controller.springEaseMult <= 0)
                {
                    controller.wheelState = KSPWheelState.RETRACTED;
                }
            }
            float ecPerSecond = wheel.springForce * 0.1f * controller.springEaseMult * energyUse;
            float ecPerTick = ecPerSecond * Time.fixedDeltaTime;
            float used = part.RequestResource("ElectricCharge", ecPerTick);
            if (used < ecPerTick)
            {
                repulsorEnabled = false;
                //TODO - print to screen that there was a power failure in the repulsor
            }
            guiEnergyUse = ecPerSecond;
        }

        internal override void onUIControlsUpdated(bool show)
        {
            base.onUIControlsUpdated(show);
            Fields[nameof(repulsorHeight)].guiActive = Fields[nameof(repulsorHeight)].guiActiveEditor = show;
            Fields[nameof(repulsorEnabled)].guiActive = show;
            Fields[nameof(repulsorEnabled)].guiActive = Fields[nameof(repulsorEnabled)].guiActiveEditor = show;
        }

    }
}
