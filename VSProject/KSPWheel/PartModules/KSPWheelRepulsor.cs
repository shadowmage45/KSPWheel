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

        [KSPField(guiName = "Repuslor Power", guiActiveEditor = true, guiActive = true, isPersistant = true),
         UI_Toggle(enabledText ="On", disabledText ="Off", suppressEditorShipModified = true)]
        public bool repulsorEnabled = true;

        [KSPField(guiName = "Energy Use", guiActive = true, guiUnits = "EC/s")]
        public float guiEnergyUse = 0f;

        [KSPField(guiName = "Force Application", guiActiveEditor = false, guiActive = false),
         UI_Toggle(enabledText = "Offset", disabledText = "Standard", suppressEditorShipModified = true)]
        public bool forcePointOffset = true;

        [KSPField(guiName = "Force Axis", guiActiveEditor = false, guiActive = false),
         UI_Toggle(enabledText = "Suspension", disabledText = "HitNormal", suppressEditorShipModified = true)]
        public bool suspensionNormal = false;

        [KSPField]
        public float easeTimeMult = 0.25f;

        /// <summary>
        /// EC/s * tons of weight supported
        /// </summary>
        [KSPField]
        public float energyUse = 1f;

        [KSPField]
        public float maxHeight = 5f;

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

        [KSPField]
        public string repulsorSoundEffect = String.Empty;

        private Transform gimbalTransform;

        private Material gridMaterial;
        private Vector2 offset = Vector2.zero;

        private float curLen;

        private void repulsorToggled(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m => 
            {
                m.repulsorEnabled = repulsorEnabled;
                if (m.repulsorEnabled)
                {
                    m.changeWheelState(KSPWheelState.DEPLOYED);
                    m.curLen = 0.0001f;
                }
            });
        }

        private void repulsorHeightUpdated(BaseField field, System.Object ob)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.repulsorHeight = repulsorHeight;
            });
        }

        [KSPAction(guiName = "Toggle Repulsor Power")]
        public void repuslorPowerAction(KSPActionParam p)
        {
            repulsorEnabled = !repulsorEnabled;
            if (repulsorEnabled)
            {
                changeWheelState(KSPWheelState.DEPLOYED);
                curLen = 0.0001f;
            }
        }

        [KSPAction(guiName = "Repulsor Power 20%")]
        public void repuslorHeight20Action(KSPActionParam p)
        {
            repulsorHeight = 0.20f;
        }

        [KSPAction(guiName = "Repulsor Power 40%")]
        public void repuslorHeight40Action(KSPActionParam p)
        {
            repulsorHeight = 0.40f;
        }

        [KSPAction(guiName = "Repulsor Power 60%")]
        public void repuslorHeight60Action(KSPActionParam p)
        {
            repulsorHeight = 0.60f;
        }

        [KSPAction(guiName = "Repulsor Power 80%")]
        public void repuslorHeight80Action(KSPActionParam p)
        {
            repulsorHeight = 0.80f;
        }

        [KSPAction(guiName = "Repulsor Power 100%")]
        public void repuslorHeight100Action(KSPActionParam p)
        {
            repulsorHeight = 1.00f;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!string.IsNullOrEmpty(gimbalName)) { gimbalTransform = part.transform.FindRecursive(gimbalName); }
            Fields[nameof(repulsorEnabled)].uiControlFlight.onFieldChanged = repulsorToggled;
            Fields[nameof(repulsorHeight)].uiControlFlight.onFieldChanged = Fields[nameof(repulsorHeight)].uiControlEditor.onFieldChanged = repulsorHeightUpdated;
            curLen = repulsorEnabled ? repulsorHeight : 0.0001f;
            if (!string.IsNullOrEmpty(gridName) && HighLogic.LoadedSceneIsFlight)
            {
                Transform gridMesh = part.transform.FindRecursive(gridName);
                if (gridMesh != null)
                {
                    Renderer rend = gridMesh.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        //TODO -- grabbing a material reference from an object creates a -new- allocation somewhere in the process
                        //can shared material be used in this case, as we are only grabbing the material in the flight scene? or is that shared material still 'shared' with the prefab and icon parts?
                        gridMaterial = rend.material;
                    }
                }
            }
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            if (HighLogic.LoadedSceneIsEditor) { return; }
            //TODO adjust configs in dust module on repulsors to set min-speed to 0
            //dustModule = part.GetComponent<KSPWheelDustEffects>();
            //if (dustModule != null) { dustModule.minDustSpeed = 0.0f; }
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
            if (!string.IsNullOrEmpty(repulsorSoundEffect))
            {
                part.Effect(repulsorSoundEffect, Time.deltaTime * guiEnergyUse);
            }
        }

        internal override void preWheelSuspensionCalc()
        {
            base.preWheelSuspensionCalc();
            //update repulsor 'length' stats
            wheelData.waterMode = false;
            if (!repulsorEnabled)
            {
                curLen = Mathf.MoveTowards(curLen, 0.001f, 0.25f * Time.fixedDeltaTime);
                if (curLen <= 0.001f)
                {
                    changeWheelState(KSPWheelState.RETRACTED);
                }
            }
            else if (repulsorEnabled)
            {
                curLen = Mathf.MoveTowards(curLen, repulsorHeight, 0.5f * Time.fixedDeltaTime);
            }
            if (gimbaled && gimbalTransform!=null) { gimbalTransform.LookAt(vessel.mainBody.transform.position); }
            wheel.length = curLen * maxHeight;
            wheel.useSuspensionNormal = suspensionNormal;
            wheel.forceApplicationOffset = forcePointOffset ? 1f : 0f;

            //repulsor water handling code
            wheel.useExternalHit = false;
            if (vessel.mainBody.ocean)
            {
                Vector3 rayStartPos = wheel.transform.position - wheel.transform.up * wheel.radius;
                Vector3 oceanHitPos = Vector3.zero;
                float alt = FlightGlobals.getAltitudeAtPos(rayStartPos);
                float length = wheel.length;
                if (alt > length)//impossible that wheel contacted surface regardless of orientation
                {
                    return;
                }
                Vector3 surfaceNormal = vessel.mainBody.GetSurfaceNVector(vessel.latitude, vessel.longitude);

                float surfaceWheelDot = Vector3.Dot(surfaceNormal, wheel.transform.up);
                //upside down, or otherwise impossible to contact the surface of the ocean
                if (surfaceWheelDot <= 0)
                {
                    return;
                }

                //special handling for if underwater
                if (alt < 0)
                {
                    //use a base of 0.5 length, adjust by inverse of dot, so that at max angle force is near zero.  This gives a smooth response when uprighting an inverted repulsor.
                    oceanHitPos = rayStartPos - wheel.transform.up * (length * 0.5f + length * 0.5f * (1f - surfaceWheelDot));
                    wheel.useExternalHit = true;
                    wheel.externalHitPoint = oceanHitPos;
                    wheel.externalHitNormal = surfaceNormal;
                    wheelData.waterMode = false;
                    return;
                }

                //point on the surface directly below the origin of the ray (below as defined by the surface normal), used for defining the plane of the ocean, below
                Vector3 pointOnSurface = rayStartPos - alt * surfaceNormal;
                //first check to see if there was any contact with the plane of the ocean (there will be), and get the hit position
                if (Utils.rayPlaneIntersect(rayStartPos, -wheel.transform.up, pointOnSurface, surfaceNormal, out oceanHitPos))
                {
                    //check distance to the contact point; may be outside of suspension range at this point
                    float oceanDistance = (rayStartPos - oceanHitPos).magnitude;
                    if (oceanDistance <= 0 || oceanDistance > length)//not within valid hit range, either zero distance, or beyond repulsor range
                    {
                        return;
                    }
                    //check to see if there is ground closer than the ocean surface, if so, use that
                    //could possibly check radar altitude prior to ocean intersect, but this gives a more precise altitude for the orientation of the wheel
                    RaycastHit hit;
                    bool groundHit = false;
                    if (groundHit = Physics.Raycast(rayStartPos, -wheel.transform.up, out hit, length, controller.raycastMask))
                    {
                        if (hit.distance < oceanDistance)
                        {
                            return;
                        }
                    }
                    //if very close to the surface, use a point halfway on suspension compression for hit point
                    //this limits force output when rising out of the water to the maximum from the underwater code
                    if (oceanDistance < length * 0.5f)
                    {
                        if (groundHit && hit.distance < length * 0.5f)//use the ground contact if it is closer
                        {
                            return;
                        }
                        oceanHitPos = rayStartPos - wheel.transform.up * length * 0.5f;
                        wheel.useExternalHit = true;
                        wheel.externalHitPoint = oceanHitPos;
                        wheel.externalHitNormal = surfaceNormal;
                        wheelData.waterMode = true;
                    }
                    else//use the surface of the ocean itself for the hit position
                    {
                        wheel.useExternalHit = true;
                        wheel.externalHitPoint = oceanHitPos;
                        wheel.externalHitNormal = surfaceNormal;
                        wheelData.waterMode = true;
                    }
                    wheelData.waterEffectPos = oceanHitPos;
                    wheelData.waterEffectSize = wheel.springForce * 0.1f;
                    wheelData.waterEffectForce = Mathf.Clamp(wheel.wheelLocalVelocity.magnitude, 0, 40f) / 40f;
                }
            }
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            float ecPerSecond = wheel.springForce * 0.1f * energyUse;
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
