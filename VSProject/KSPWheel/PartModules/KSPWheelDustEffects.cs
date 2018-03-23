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
        public string dustParticleTexture;// GameData/SQUAD/???  I think they are baked into the prefabs... will probably have to provide my own texture(s)

        [KSPField]
        public string waterParticleTexture;

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
        public float dustMinSize = 0.1f;

        [KSPField]
        public float dustMaxSize = 1f;

        [KSPField]
        public float dustMinEmission = 50f;

        [KSPField]
        public float dustMaxEmission = 500f;

        [KSPField]
        public float dustMinEnergy = 0.1f;

        [KSPField]
        public float dustMaxEnergy = 3f;

        private bool gamePaused = false;

        private bool dustEnabled = false;

        private float dustPower = 1f;

        private float colorUpdateTime = 1f;
        private float colorUpdateTimer = 0f;

        private ParticleSystem[] dustEmitters;
        private ParticleSystem[] waterEmitters;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            GameEvents.onGamePause.Add(new EventVoid.OnEvent(onGamePause));
            GameEvents.onGameUnpause.Add(new EventVoid.OnEvent(onGameUnpause));
            GameEvents.onFloatingOriginShift.Add(new EventData<Vector3d, Vector3d>.OnEvent(onOriginShift));
            dustPower = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wheelDustPower;
            dustEnabled = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wheelDustEffects;
            if (dustEnabled)
            {
                setupDustEmitters();
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
            }
            GameEvents.onGamePause.Remove(new EventVoid.OnEvent(onGamePause));
            GameEvents.onGameUnpause.Remove(new EventVoid.OnEvent(onGameUnpause));
            GameEvents.onFloatingOriginShift.Remove(new EventData<Vector3d, Vector3d>.OnEvent(onOriginShift));
        }

        private void onGamePause()
        {
            gamePaused = true;
            if (dustEmitters != null)
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
            if (!dustEnabled) { return; }
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
                waterEmitters[i].GetParticles(ps);
                len2 = waterEmitters[i].particleCount;
                for (int k = 0; k < len2; k++)
                {
                    Vector3 pos = ps[k].position;
                    pos += (n - o);
                    ps[k].position = pos;
                }
            }
            //if (dustEmitters == null) { return; }
            //Vector3 d = n - o;
            //Vector3 pos;
            //int len = dustEmitters.Length;
            //int len2;
            //Particle[] particles;
            //for (int i = 0; i < len; i++)
            //{
            //    particles = dustEmitters[i].particles;
            //    len2 = particles.Length;
            //    for (int k = 0; k < len2; k++)
            //    {
            //        pos = particles[k].position;
            //        pos = pos + d;
            //        particles[k].position = pos;
            //    }
            //    dustEmitters[i].particles = particles;

            //    particles = waterEmitters[i].particles;
            //    len2 = particles.Length;
            //    for (int k = 0; k < len2; k++)
            //    {
            //        pos = particles[k].position;
            //        pos = pos + d;
            //        particles[k].position = pos;
            //    }
            //    waterEmitters[i].particles = particles;
            //}
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            //if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready || wheel == null || gamePaused || controller.wheelState != KSPWheelState.DEPLOYED || !HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wheelDustEffects)
            //{
            //    return;
            //}
            //if (!setupEmitters)
            //{
            //    setupEmitters = true;
            //    setupDustEmitters();
            //}
            //updateDustEmission();
        }

        private void setupDustEmitters()
        {
            int len = controller.wheelData.Length;
            dustEmitters = new ParticleSystem[len];
            waterEmitters = new ParticleSystem[len];

            Texture2D dustParticleTexture = GameDatabase.Instance.GetTexture(this.dustParticleTexture, false);
            Texture2D waterParticleTexture = GameDatabase.Instance.GetTexture(this.waterParticleTexture, false);

            Shader particleShader = Shader.Find("Particles/Additive");

            Material dustMaterial = new Material(particleShader);
            dustMaterial.mainTexture = dustParticleTexture;

            Material waterMaterial = new Material(particleShader);
            waterMaterial.mainTexture = waterParticleTexture;

            for (int i = 0; i < len; i++)
            {
                GameObject dustParticleObject = new GameObject("DustEmitter");
                dustParticleObject.transform.parent = wheel.transform;
                dustParticleObject.transform.position = wheel.transform.position;
                dustParticleObject.transform.rotation = wheel.transform.rotation;

                ParticleSystem dustParticleSystem = dustParticleObject.AddComponent<ParticleSystem>();
                dustEmitters[i] = dustParticleSystem;
                dustEmitters[i].ext_setCoordinateSpace(ParticleSystemSimulationSpace.World);
                dustEmitters[i].ext_setMaterial(dustMaterial);

                GameObject waterParticleObject = new GameObject("WaterEmitter");
                waterParticleObject.transform.parent = wheel.transform;
                waterParticleObject.transform.position = wheel.transform.position;
                waterParticleObject.transform.rotation = wheel.transform.rotation;

                ParticleSystem waterParticleSystem = waterParticleObject.AddComponent<ParticleSystem>();
                waterEmitters[i] = waterParticleSystem;
                waterEmitters[i].ext_setCoordinateSpace(ParticleSystemSimulationSpace.World);
                waterEmitters[i].ext_setMaterial(waterMaterial);
            }


            ////one dust and one water effect emitter per wheel collider ..
            //int len = controller.wheelData.Length;
            //dustObjects = new GameObject[len];
            //dustEmitters = new ParticleEmitter[len];
            //dustAnimators = new ParticleAnimator[len];
            //waterObjects = new GameObject[len];
            //waterEmitters = new ParticleEmitter[len];
            //waterAnimators = new ParticleAnimator[len];
            //KSPWheelCollider wheel;
            //for (int i = 0; i < len; i++)
            //{
            //    wheel = controller.wheelData[i].wheel;
            //    //TODO -- clean up loading code; load once, clone many times
            //    dustObjects[i] = (GameObject)GameObject.Instantiate(Resources.Load(dustParticleEffect));
            //    dustObjects[i].transform.position = wheel.transform.position;
            //    dustObjects[i].transform.rotation = wheel.transform.rotation;
            //    dustEmitters[i] = dustObjects[i].GetComponent<ParticleEmitter>();
            //    dustAnimators[i] = dustEmitters[i].GetComponent<ParticleAnimator>();
            //    dustAnimators[i].colorAnimation = dustColorArray;
            //    dustAnimators[i].sizeGrow = 0.5f;
            //    dustEmitters[i].useWorldSpace = true;
            //    dustEmitters[i].localVelocity = Vector3.zero;
            //    dustEmitters[i].emit = false;

            //    waterObjects[i] = (GameObject)GameObject.Instantiate(Resources.Load(waterParticleEffect));
            //    waterObjects[i].transform.position = wheel.transform.position;
            //    waterObjects[i].transform.rotation = wheel.transform.rotation;
            //    waterEmitters[i] = waterObjects[i].GetComponent<ParticleEmitter>();
            //    waterAnimators[i] = waterEmitters[i].GetComponent<ParticleAnimator>();
            //    waterAnimators[i].colorAnimation = waterColorArray;
            //    waterAnimators[i].doesAnimateColor = true;
            //    waterAnimators[i].sizeGrow = 0.5f;
            //    waterEmitters[i].useWorldSpace = true;
            //    waterEmitters[i].localVelocity = Vector3.zero;
            //    waterEmitters[i].emit = false;
            //}
            //for (int i = 0; i < 5; i++)
            //{
            //    float percent = (1f - ((float)i / 5f));
            //    waterColorArray[i] = new Color(0.75f * percent, 0.75f * percent, 0.80f * percent, percent * 0.0125f);
            //}
        }

        private void updateDustEmission()
        {
            if (!dustEnabled) { return; }
            //TODO remove this per-tick component lookup; source say to cache the vessel->module map in a static map in the vessel-module class; add/remove by the start/etc methods on the vessel-module
            KSPWheelDustCamera cm = vessel.GetComponent<KSPWheelDustCamera>();
            updateColorArray(cm.cameraColor);

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
                    springForce = data.waterEffectSize;
                    mult = data.waterEffectForce * dustSpeedMult;
                    waterEmitters[i].transform.position = data.waterEffectPos;
                    waterEmitters[i].transform.rotation = wheel.transform.rotation;
                    if (mult > 0)
                    {
                        waterEmitters[i].ext_setVelocity(antiGravity * mult);
                        //waterEmitters[i].localVelocity = Vector3.up * mult;
                        waterEmitters[i].ext_setEmissionMinMax(dustMinEmission * dustPower, dustMaxEmission * dustPower);
                        //waterEmitters[i].minEmission = dustMinEmission * dustPower;
                        //waterEmitters[i].maxEmission = dustMaxEmission * dustPower;
                        waterEmitters[i].ext_setEnergyMinMax(dustMinEnergy * dustPower, dustMaxEnergy * mult * dustPower);
                        //waterEmitters[i].minEnergy = dustMinEnergy * dustPower;
                        //waterEmitters[i].maxEnergy = dustMaxEnergy * mult * dustPower;
                        waterEmitters[i].minSize = dustMinSize * springForce * dustPower;
                        waterEmitters[i].maxSize = dustMaxSize * springForce * dustPower;
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
                    //dustEmitters[i].ext_setEmissionEnable(true);
                    //waterEmitters[i].ext_setEmissionEnable(false);
                    //springForce = wheel.springForce * 0.1f * dustForceMult;
                    //speedForce = Mathf.Clamp(Mathf.Abs(wheel.wheelLocalVelocity.z) / maxDustSpeed, 0, 1);
                    //slipForce = Mathf.Clamp(Mathf.Abs(wheel.wheelLocalVelocity.x) / maxDustSpeed, 0, 1);
                    //mult = Mathf.Sqrt(speedForce * speedForce * dustSpeedMult + slipForce * slipForce * dustSlipMult);
                    //dustObjects[i].transform.position = wheel.worldHitPos;
                    //dustObjects[i].transform.rotation = wheel.transform.rotation;
                    //dustEmitters[i].localVelocity = Vector3.up * (speedForce + slipForce);
                    //dustEmitters[i].minEmission = dustMinEmission * dustPower;
                    //dustEmitters[i].maxEmission = dustMaxEmission * dustPower;
                    //dustEmitters[i].minEnergy = dustMinEnergy * dustPower;
                    //dustEmitters[i].maxEnergy = dustMaxEnergy * mult * dustPower;
                    //dustEmitters[i].minSize = dustMinSize * springForce * dustPower;
                    //dustEmitters[i].maxSize = dustMaxSize * springForce * dustPower;
                }
                else//not grounded
                {
                    dustEmitters[i].ext_setEmissionEnable(false);
                    waterEmitters[i].ext_setEmissionEnable(false);
                }
            }
        }

        /// <summary>
        /// Updates the color array used by the color animator for dust
        /// TODO add some randomization factors to each of the colors in the array, perhaps reducing color and alpha in the later array indices
        /// This will require instantiating the colors into the array originally as custom colors and then manually setting their RBGA values to avoid per-tick 'new' color allocations
        /// </summary>
        /// <param name="inputColor"></param>
        private void updateColorArray(Color inputColor)
        {
            //Color color = inputColor;
            //dustColorArray[0] = color;
            //dustColorArray[1] = color;
            //dustColorArray[2] = color;
            //dustColorArray[3] = color;
            //dustColorArray[4] = color;
            //int len = dustAnimators.Length;
            //for (int i = 0; i < len; i++)
            //{
            //    dustAnimators[i].colorAnimation = dustColorArray;
            //    waterAnimators[i].colorAnimation = waterColorArray;
            //}
        }
        
    }

    /// <summary>
    /// Yep, an entire set of extension methods, just to remove the retarded implementation that Unity used on particles.<para/>
    /// Probably terrible for performance, but fuck Unity and their asinine system designs.
    /// </summary>
    public static class ParticleSystemExtensions
    {

        public static void ext_setEmissionEnable(this ParticleSystem ps, bool value)
        {
            var bs = ps.emission;
            bs.enabled = value;
        }

        public static bool ext_getEmissionEnable(this ParticleSystem ps)
        {
            return ps.emission.enabled;
        }

        public static void ext_setEmissionMinMax(this ParticleSystem ps, float min, float max)
        {
            var bs = ps.emission.rateOverTime;
            bs.constantMin = min;
            bs.constantMax = max;
            bs.mode = ParticleSystemCurveMode.TwoConstants;
        }

        public static void ext_setEnergyMinMax(this ParticleSystem ps, float min, float max)
        {
            var bs = ps.main;
            bs.startLifetime = new ParticleSystem.MinMaxCurve(min, max);
        }

        public static void ext_setCoordinateSpace(this ParticleSystem ps, ParticleSystemSimulationSpace space)
        {
            var bs = ps.main;
            bs.simulationSpace = space;
        }

        public static ParticleSystemSimulationSpace ext_getCoordinateSpace(this ParticleSystem ps)
        {
            return ps.main.simulationSpace;
        }

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

        public static void ext_setSizeGrow(this ParticleSystem ps, float sg)
        {
            var bs = ps.sizeOverLifetime;
            bs.separateAxes = false;
            bs.x = sg;
        }

        public static void ext_setVelocity(this ParticleSystem ps, Vector3 vel)
        {
            var bs = ps.velocityOverLifetime;
            bs.enabled = true;
            bs.space = ParticleSystemSimulationSpace.World;
            bs.x = vel.x;
            bs.y = vel.y;
            bs.z = vel.z;
        }

        public static void ext_setInheritVelocity(this ParticleSystem ps, bool val)
        {
            var bs = ps.inheritVelocity;
            bs.enabled = val;
            bs.mode = ParticleSystemInheritVelocityMode.Initial;
        }

    }
}
