using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelDustEffects : KSPWheelSubmodule
    {
        [KSPField]
        public string dustParticleTexture = "KSPWheel/Assets/particle";

        [KSPField]
        public string waterParticleTexture = "KSPWheel/Assets/particle";

        /// <summary>
        /// Below this value of linear speed no contribution to emission will come from wheel speed/rpm
        /// </summary>
        [KSPField]
        public float minDustSpeed = 0.1f;

        /// <summary>
        /// Above this speed, velocity makes no further contribution to dust output
        /// </summary>
        [KSPField]
        public float maxDustSpeed = 20f;

        /// <summary>
        /// Multiplier to dust emission contribution from spring force
        /// </summary>
        [KSPField]
        public float dustForceMult = 1f;

        /// <summary>
        /// Multiplier to dust emission contribution from wheel velocity/rpm
        /// </summary>
        [KSPField]
        public float dustSpeedMult = 1f;

        /// <summary>
        /// Multiplier to dust emission contribution from wheel slip
        /// </summary>
        [KSPField]
        public float dustSlipMult = 1f;

        [KSPField]
        public float dustMinSize = 8f;

        [KSPField]
        public float dustMaxSize = 12f;

        [KSPField]
        public float dustMinEmission = 8f;

        [KSPField]
        public float dustMaxEmission = 12f;

        [KSPField]
        public float dustMinEnergy = 0.1f;

        [KSPField]
        public float dustMaxEnergy = 3f;

        private bool gamePaused = false;

        private bool dustEnabled = false;

        private bool setupEmitters = false;

        private float dustPower = 1f;

        private ParticleSystem[] dustEmitters;
        private ParticleSystem[] waterEmitters;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            dustEnabled = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wheelDustEffects;
            if (dustEnabled)
            {
                dustPower = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wheelDustPower;
                GameEvents.onGamePause.Add(new EventVoid.OnEvent(onGamePause));
                GameEvents.onGameUnpause.Add(new EventVoid.OnEvent(onGameUnpause));
                GameEvents.onFloatingOriginShift.Add(new EventData<Vector3d, Vector3d>.OnEvent(onOriginShift));
                //controller will likely always be null at this point, but... w/e
                if (controller != null && controller.wheelData != null && wheel != null)
                {
                    setupDustEmitters();
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (dustEnabled)
            {
                int len = dustEmitters.Length;
                for (int i = 0; i < len; i++)
                {
                    GameObject.Destroy(dustEmitters[i].gameObject);
                    GameObject.Destroy(waterEmitters[i].gameObject);
                }
                dustEmitters = null;
                waterEmitters = null;
                GameEvents.onGamePause.Remove(new EventVoid.OnEvent(onGamePause));
                GameEvents.onGameUnpause.Remove(new EventVoid.OnEvent(onGameUnpause));
                GameEvents.onFloatingOriginShift.Remove(new EventData<Vector3d, Vector3d>.OnEvent(onOriginShift));
            }
        }

        private void onGamePause()
        {
            gamePaused = true;
            if (setupEmitters)
            {
                int len = dustEmitters.Length;
                for (int i = 0; i < len; i++)
                {
                    dustEmitters[i].ext_setEmissionEnable(false);
                    waterEmitters[i].ext_setEmissionEnable(false);
                }
            }
        }

        private void onGameUnpause()
        {
            gamePaused = false;
        }

        private void onOriginShift(Vector3d o, Vector3d n)
        {
            if (!dustEnabled) { return; }//should not happen, event is not subscribed unless dust was enabled during onStart
            if (!setupEmitters || dustEmitters == null || waterEmitters == null) { return; }//may happen on inactive vessels
            int len = dustEmitters.Length;
            for (int i = 0; i < len; i++)
            {
                ParticleSystem.Particle[] ps = new ParticleSystem.Particle[dustEmitters[i].main.maxParticles];
                dustEmitters[i].GetParticles(ps);
                int len2 = dustEmitters[i].particleCount;
                for (int k = 0; k < len2; k++)
                {
                    Vector3 pos = ps[k].position;
                    pos += (n - o);
                    ps[k].position = pos;
                }
                dustEmitters[i].SetParticles(ps, len2);

                waterEmitters[i].GetParticles(ps);
                len2 = waterEmitters[i].particleCount;
                for (int k = 0; k < len2; k++)
                {
                    Vector3 pos = ps[k].position;
                    pos += (n - o);
                    ps[k].position = pos;
                }
                waterEmitters[i].SetParticles(ps, len2);
            }
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (!dustEnabled || !HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready || wheel == null || gamePaused || controller.wheelState != KSPWheelState.DEPLOYED)
            {
                return;
            }
            if (!setupEmitters)
            {
                setupEmitters = true;
                setupDustEmitters();
            }
            updateDustEmission();
        }

        private void setupDustEmitters()
        {
            int len = controller.wheelData.Length;
            dustEmitters = new ParticleSystem[len];
            waterEmitters = new ParticleSystem[len];

            Texture2D dustParticleTexture = GameDatabase.Instance.GetTexture(this.dustParticleTexture, false);
            Texture2D waterParticleTexture = GameDatabase.Instance.GetTexture(this.waterParticleTexture, false);

            Shader particleShader = Shader.Find("Particles/Additive (Soft)");

            Material dustMaterial = new Material(particleShader);
            dustMaterial.mainTexture = dustParticleTexture;

            Material waterMaterial = new Material(particleShader);
            waterMaterial.mainTexture = waterParticleTexture;

            for (int i = 0; i < len; i++)
            {
                //setup dust particle emitter, including all persistent parameters
                GameObject dustParticleObject = new GameObject("DustEmitter");
                dustParticleObject.transform.parent = controller.wheelData[i].wheel.transform;
                dustParticleObject.transform.position = controller.wheelData[i].wheel.transform.position;
                dustParticleObject.transform.rotation = controller.wheelData[i].wheel.transform.rotation;
                ParticleSystem dustParticleSystem = dustParticleObject.AddComponent<ParticleSystem>();
                dustEmitters[i] = dustParticleSystem;
                dustEmitters[i].ext_setMaterial(dustMaterial);
                dustEmitters[i].ext_setCoordinateSpace(ParticleSystemSimulationSpace.World);
                dustEmitters[i].ext_setInheritVelocity(true);
                dustEmitters[i].ext_setNoiseEnabled(true);
                dustEmitters[i].ext_setCone(10);
                dustEmitters[i].ext_setSpeed(0f, 0f);
                dustEmitters[i].ext_setSize(8f, 12f);
                dustEmitters[i].ext_setSizeGrow(0.25f, 1f);
                dustEmitters[i].ext_setupColorGradient();
                dustEmitters[i].ext_setColor(Color.white);
                dustEmitters[i].ext_setEmissionEnable(false);

                //setup water particle emitter, including all persistent parameters
                GameObject waterParticleObject = new GameObject("WaterEmitter");
                waterParticleObject.transform.parent = wheel.transform;
                waterParticleObject.transform.position = wheel.transform.position;
                waterParticleObject.transform.rotation = wheel.transform.rotation;
                ParticleSystem waterParticleSystem = waterParticleObject.AddComponent<ParticleSystem>();
                waterEmitters[i] = waterParticleSystem;
                waterEmitters[i].ext_setMaterial(waterMaterial);
                waterEmitters[i].ext_setCoordinateSpace(ParticleSystemSimulationSpace.World);
                waterEmitters[i].ext_setInheritVelocity(true);
                waterEmitters[i].ext_setNoiseEnabled(true);
                waterEmitters[i].ext_setCone(10);
                waterEmitters[i].ext_setSpeed(0f, 0f);
                waterEmitters[i].ext_setSize(8f, 12f);
                waterEmitters[i].ext_setSizeGrow(0.25f, 1f);
                waterEmitters[i].ext_setupColorGradient();
                waterEmitters[i].ext_setColor(Color.white);
                waterEmitters[i].ext_setEmissionEnable(false);
            }
        }

        private void updateDustEmission()
        {
            if (!dustEnabled) { return; }
            //TODO remove this per-tick component lookup; source say to cache the vessel->module map in a static map in the vessel-module class; add/remove by the start/etc methods on the vessel-module
            KSPWheelDustCamera cm = vessel.GetComponent<KSPWheelDustCamera>();
            Color color = cm.cameraColor;

            int len = dustEmitters.Length;

            Vector3 antiGravity = (-vessel.gravityForPos).normalized;

            KSPWheelCollider wheel;
            KSPWheelBase.KSPWheelData data;
            float springForce = 1f;
            float speedForce = 1f;
            float slipForce = 1f;
            float mult = 0f;
            for (int i = 0; i < len; i++)
            {
                data = controller.wheelData[i];
                wheel = data.wheel;
                if (dustPower <= 0)//dust disabled via game settings, really should never branch to this, but might if someone set dust to enabled, with dust power at zero
                {
                    dustEmitters[i].ext_setEmissionEnable(false);
                    waterEmitters[i].ext_setEmissionEnable(false);
                }
                else if (data.waterMode)
                {
                    waterEmitters[i].ext_setColor(color);
                    springForce = data.waterEffectSize * 0.25f;//0.25f mult is correction for changes between unity 5.4 / 2017.1.3
                    mult = data.waterEffectForce * dustSpeedMult;
                    waterEmitters[i].transform.position = data.waterEffectPos;
                    waterEmitters[i].transform.rotation = wheel.transform.rotation;
                    waterEmitters[i].transform.LookAt(waterEmitters[i].transform.position + antiGravity, Vector3.forward);
                    if (mult > 0)
                    {
                        waterEmitters[i].ext_setVelocity(antiGravity * mult, Vector3.zero);
                        waterEmitters[i].ext_setEmissionMinMax(dustMinEmission * dustPower, dustMaxEmission * dustPower);
                        waterEmitters[i].ext_setSize(dustMinSize * springForce * dustPower, dustMaxSize * springForce * dustPower);
                        waterEmitters[i].ext_setEmissionEnable(true);
                    }
                    else
                    {
                        waterEmitters[i].ext_setEmissionEnable(false);
                    }
                    dustEmitters[i].ext_setEmissionEnable(false);
                }
                else if (wheel.isGrounded && wheel.wheelLocalVelocity.magnitude >= minDustSpeed)
                {
                    dustEmitters[i].ext_setColor(color);
                    springForce = wheel.springForce * 0.025f * dustForceMult;
                    speedForce = Mathf.Clamp(Mathf.Abs(wheel.wheelLocalVelocity.z) / maxDustSpeed, 0, 1);
                    slipForce = Mathf.Clamp(Mathf.Abs(wheel.wheelLocalVelocity.x) / maxDustSpeed, 0, 1);
                    mult = Mathf.Sqrt(speedForce * speedForce * dustSpeedMult + slipForce * slipForce * dustSlipMult);
                    dustEmitters[i].transform.position = wheel.worldHitPos;
                    dustEmitters[i].transform.LookAt(dustEmitters[i].transform.position + antiGravity, Vector3.forward);
                    dustEmitters[i].ext_setVelocity(antiGravity * (speedForce + slipForce), Vector3.zero);
                    dustEmitters[i].ext_setEmissionMinMax(dustMinEmission * dustPower * mult, dustMaxEmission * dustPower * mult);
                    dustEmitters[i].ext_setSize(dustMinSize * springForce * dustPower, dustMaxSize * springForce * dustPower);
                    dustEmitters[i].ext_setEmissionEnable(true);
                    waterEmitters[i].ext_setEmissionEnable(false);
                }
                else//not grounded or wheel velocity is below threshold
                {
                    dustEmitters[i].ext_setEmissionEnable(false);
                    waterEmitters[i].ext_setEmissionEnable(false);
                }
            }
        }
        
    }

    /// <summary>
    /// Wrappers around the terrible particle system module implementation from Unity.<para/>
    /// Gives a slightly cleaner interface to set common particle emitter properties.
    /// </summary>
    public static class ParticleSystemExtensions
    {

        /// <summary>
        /// Extension method to enable/disable emission.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="value"></param>
        public static void ext_setEmissionEnable(this ParticleSystem ps, bool value)
        {
            var bs = ps.emission;
            bs.enabled = value;
        }

        /// <summary>
        /// Extension method to return the current emission enabled status
        /// </summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static bool ext_getEmissionEnable(this ParticleSystem ps)
        {
            return ps.emission.enabled;
        }

        /// <summary>
        /// Extension method to set the emission output of the emitter (# of particles/second).  A random value between min and max is used.  If min==max, a single constant value is used.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static void ext_setEmissionMinMax(this ParticleSystem ps, float min, float max)
        {
            var bs = ps.emission;
            bs.rateOverTime = new ParticleSystem.MinMaxCurve(min, max);
        }

        /// <summary>
        /// Extension method to set the size of the particles.  A random value between min and max is used.  If min==max, a single constant value is used.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static void ext_setSize(this ParticleSystem ps, float min, float max)
        {
            var bs = ps.main;
            bs.startSize = new ParticleSystem.MinMaxCurve(min, max);
        }

        /// <summary>
        /// Extension method to set the starting velocity of the particles.  A random value between min and max is used.  If min==max, a single constant value is used.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static void ext_setSpeed(this ParticleSystem ps, float min, float max)
        {
            var bs = ps.main;
            bs.startSpeed = (min == max ? new ParticleSystem.MinMaxCurve(min) : new ParticleSystem.MinMaxCurve(min, max));
        }

        /// <summary>
        /// Extension method to set the main/primary/start color.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="a"></param>
        public static void ext_setColor(this ParticleSystem ps, Color a)
        {
            var bs = ps.main;
            bs.startColor = a;//explicitly set it to a constant color, the input color, (a)
        }

        /// <summary>
        /// Extension method to set the emitter to 'cone' emission style, with the specified cone angle
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="angle"></param>
        public static void ext_setCone(this ParticleSystem ps, float angle)
        {
            var bs = ps.shape;
            bs.shapeType = ParticleSystemShapeType.Cone;
            bs.angle = angle;
        }

        /// <summary>
        /// Extension method to enable/disable the 'noise' module.  Does not change any of the noise parameters.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="value"></param>
        public static void ext_setNoiseEnabled(this ParticleSystem ps, bool value)
        {
            var bs = ps.noise;
            bs.enabled = true;
        }

        /// <summary>
        /// Extension method to set the particle lifetime.  Uses a random value between min and max.  If min==max, a sigle constant value is used.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static void ext_setLifetime(this ParticleSystem ps, float min, float max)
        {
            var bs = ps.main;
            bs.startLifetime = (min == max ? new ParticleSystem.MinMaxCurve(min) : new ParticleSystem.MinMaxCurve(min, max));
        }

        /// <summary>
        /// Extension method to set the particle system to use the specified coordinate system.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="space"></param>
        public static void ext_setCoordinateSpace(this ParticleSystem ps, ParticleSystemSimulationSpace space)
        {
            var bs = ps.main;
            bs.simulationSpace = space;
        }

        public static ParticleSystemSimulationSpace ext_getCoordinateSpace(this ParticleSystem ps)
        {
            return ps.main.simulationSpace;
        }

        /// <summary>
        /// Extension method to set the material used by the particle system renderer.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="mat"></param>
        public static void ext_setMaterial(this ParticleSystem ps, Material mat)
        {
            var bs = ps.gameObject.GetComponent<ParticleSystemRenderer>();
            bs.material = mat;
        }

        public static Material ext_getMaterial(this ParticleSystem ps)
        {
            var bs = ps.gameObject.GetComponent<ParticleSystemRenderer>();
            return bs.material;
        }

        /// <summary>
        /// Extension method to set the rendering type of the particle emitter (billboard/etc).
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="mode"></param>
        public static void ext_setRenderMode(this ParticleSystem ps, ParticleSystemRenderMode mode)
        {
            var bs = ps.gameObject.GetComponent<ParticleSystemRenderer>();
            bs.renderMode = mode;
        }

        public static ParticleSystemRenderMode ext_getRenderMode(this ParticleSystem ps)
        {
            var bs = ps.gameObject.GetComponent<ParticleSystemRenderer>();
            return bs.renderMode;
        }

        /// <summary>
        /// Extension method to set the size-grow parameter of the particles.<para/>
        /// Min = size multiplier at start of lifetime, Max = size multiplier at end of lifetime.<para/>
        /// These function as multipliers against the basic 'start size' parameter.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static void ext_setSizeGrow(this ParticleSystem ps, float min, float max)
        {
            AnimationCurve c = new AnimationCurve();
            c.AddKey(0, min);
            c.AddKey(1, max);
            var bs = ps.sizeOverLifetime;
            bs.separateAxes = false;
            bs.x = new ParticleSystem.MinMaxCurve(1, c);
        }

        /// <summary>
        /// Sets up a predefined color gradient for dust particles.<para/>
        /// Starts with no alpha (transparent), quickly lerps to low alpha (0.15), and tapers off slowly back to no alpha.
        /// </summary>
        /// <param name="ps"></param>
        public static void ext_setupColorGradient(this ParticleSystem ps)
        {
            Gradient gr = new Gradient();
            //gr.mode = GradientMode.Blend;
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 0f;
            alphaKeys[1].time = 0.15f;
            alphaKeys[1].alpha = 0.25f;
            alphaKeys[2].time = 1f;
            alphaKeys[2].alpha = 0f;

            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0].color = Color.white;
            colorKeys[0].time = 0f;
            colorKeys[1].color = Color.black;
            colorKeys[1].time = 1f;

            gr.SetKeys(colorKeys, alphaKeys);

            var bs = ps.colorOverLifetime;
            bs.enabled = true;

            bs.color = new ParticleSystem.MinMaxGradient(gr);
        }

        /// <summary>
        /// Extension method to set the world-space velocity-over-lifetime of the particles, given a start and end velocity.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="velStart"></param>
        public static void ext_setVelocity(this ParticleSystem ps, Vector3 velStart, Vector3 velEnd)
        {
            var bs = ps.velocityOverLifetime;
            bs.enabled = true;
            bs.space = ParticleSystemSimulationSpace.World;
            bs.x = velStart.x;
            bs.y = velStart.y;
            bs.z = velStart.z;
        }

        /// <summary>
        /// Extension method to enable/disable 'inherit velocity' on the particle system.<para/>
        /// Uses world-space velocity inheritance when enabled.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="val"></param>
        public static void ext_setInheritVelocity(this ParticleSystem ps, bool val)
        {
            var bs = ps.inheritVelocity;
            bs.enabled = val;
            bs.mode = ParticleSystemInheritVelocityMode.Initial;
        }

    }

}
