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
        public string repulsorEffectTransform = string.Empty;

        [KSPField]
        public string repulsorSoundEffect = String.Empty;

        [KSPField]
        public string repulsorParticleTexture = "KSPWheel/Assets/particle";

        [KSPField]
        public bool showGUIHeight = true;

        private RepulsorParticles particles;
        
        private float curLen;

        private void repulsorToggled(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m => 
            {
                m.setRepulsorEnabled(repulsorEnabled);
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
                if (m.particles != null) { m.particles.setSpeed(m.repulsorHeight * m.maxHeight); }
            });
        }

        [KSPAction(guiName = "Toggle Repulsor Power")]
        public void repuslorPowerAction(KSPActionParam p)
        {
            setRepulsorEnabled(!repulsorEnabled);
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
            Fields[nameof(repulsorEnabled)].uiControlFlight.onFieldChanged = repulsorToggled;
            Fields[nameof(repulsorHeight)].uiControlFlight.onFieldChanged = Fields[nameof(repulsorHeight)].uiControlEditor.onFieldChanged = repulsorHeightUpdated;
            curLen = repulsorEnabled ? repulsorHeight : 0.0001f;
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            if (HighLogic.LoadedSceneIsEditor) { return; }
            //TODO adjust configs in dust module on repulsors to set min-speed to 0
            //dustModule = part.GetComponent<KSPWheelDustEffects>();
            //if (dustModule != null) { dustModule.minDustSpeed = 0.0f; }
            if (particles == null && !string.IsNullOrEmpty(repulsorEffectTransform))
            {
                GameObject root = part.transform.FindRecursive("model").FindRecursive(repulsorEffectTransform).gameObject;
                GameObject particlesRoot = new GameObject("RepulsorParticles");
                particlesRoot.transform.parent = root.transform;
                particlesRoot.transform.NestToParent(root.transform);
                particlesRoot.transform.Rotate(-90, 0, 0);
                
                Texture2D tex = GameDatabase.Instance.GetTexture(this.repulsorParticleTexture, false);
                Shader particleShader = Shader.Find("Particles/Additive (Soft)");

                Material material = new Material(particleShader);
                material.mainTexture = tex;

                particles = new RepulsorParticles(particlesRoot, material);
                particles.createParticles();
            }
            if (particles != null)
            {
                particles.setSpeed(repulsorHeight * maxHeight);
                particles.setEnabled(repulsorEnabled);
            }
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
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
            wheel.length = curLen * maxHeight;
            wheel.useSuspensionNormal = false;
            wheel.forceApplicationOffset = 1f;

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
                setRepulsorEnabled(false);
                //TODO - print to screen that there was a power failure in the repulsor
            }
            guiEnergyUse = ecPerSecond;
        }

        internal override void onUIControlsUpdated(bool show)
        {
            base.onUIControlsUpdated(show);
            Fields[nameof(repulsorHeight)].guiActive = Fields[nameof(repulsorHeight)].guiActiveEditor = show && showGUIHeight;
            Fields[nameof(repulsorEnabled)].guiActive = Fields[nameof(repulsorEnabled)].guiActiveEditor = show;
        }

        private void setRepulsorEnabled(bool enabled)
        {
            repulsorEnabled = enabled;
            if (particles != null)
            {
                particles.setSpeed(repulsorHeight*maxHeight);
                particles.setEnabled(enabled);
            }
        }

    }


    public class RepulsorParticles
    {

        public GameObject Parent { get; private set; }
        public Material ParticleMaterial { get; private set; }
        public ParticleSystem ParticleSystem { get; private set; }

        private static GameObject prefab { get; set; }

        public RepulsorParticles(GameObject parent, Material material)
        {
            Parent = parent;
            ParticleMaterial = material;
        }

        public void setEnabled(bool enabled)
        {
            if (enabled) { ParticleSystem.Play(); }
            else { ParticleSystem.Stop(); }
        }

        public void setSpeed(float repulsorHeight)
        {
            if (ParticleSystem != null)
            {

                float speed = repulsorHeight / 2.5f;

                float mainSpeed = speed;
                float velSpeed = speed * 2f;

                ParticleSystem.MainModule main = ParticleSystem.main;
                main.startSpeed = speed;

                ParticleSystem.VelocityOverLifetimeModule velocity = ParticleSystem.velocityOverLifetime;
                velocity.z = new ParticleSystem.MinMaxCurve(velSpeed);
            }
        }

        public void createParticles()
        {
            if (prefab == null)
            {
                prefab = KSPWheelParticleEffect.loadFromDisk("KerbalFoundries/Effects/KF-RepulsorEffect");
            }
            GameObject particle = GameObject.Instantiate(prefab);
            particle.transform.NestToParent(Parent.transform);
            particle.SetActive(true);
            ParticleSystem = particle.GetComponentInChildren<ParticleSystem>();
        }

    }

}
