using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelDustEffects : KSPWheelSubmodule
    {

        /// <summary>
        /// The name of the particle effect to use for dust creation
        /// </summary>
        [KSPField]
        public string dustParticleEffect = "Effects/fx_smokeTrail_light";

        /// <summary>
        /// The name of the particle effect to use for water-spray creation
        /// </summary>
        [KSPField]
        public string waterParticleEffect = "Effects/fx_smokeTrail_light";

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
        public float dustMaxSize = 3.5f;

        [KSPField]
        public float dustMinEmission = 0.1f;

        [KSPField]
        public float dustMaxEmission = 20f;

        [KSPField]
        public float dustMinEnergy = 0.1f;

        [KSPField]
        public float dustMaxEnergy = 3f;

        private bool gamePaused = false;

        private float dustPower = 1f;

        private Color[] dustColorArray = new Color[5] { Color.clear, Color.clear, Color.clear, Color.clear, Color.clear };
        private string prevBody = string.Empty;
        private string prevBiome = string.Empty;

        private Color[] waterColorArray = new Color[5] { Color.clear, Color.clear, Color.clear, Color.clear, Color.clear };

        private float colorUpdateTime = 1f;
        private float colorUpdateTimer = 0f;

        private bool setupEmitters = false;
        private GameObject[] dustObjects;//one emitter per wheel collider on the part; so that tracks/compound parts still throw up dust on a per-wheel basis
        private ParticleEmitter[] dustEmitters;
        private ParticleAnimator[] dustAnimators;//no clue if these will even work in modern Unity versions, will investigate ParticleSystem instead...
        private GameObject[] waterObjects;
        private ParticleEmitter[] waterEmitters;
        private ParticleAnimator[] waterAnimators;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            GameEvents.onGamePause.Add(new EventVoid.OnEvent(onGamePause));
            GameEvents.onGameUnpause.Add(new EventVoid.OnEvent(onGameUnpause));
            dustPower = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wheelDustPower;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (dustObjects != null)
            {
                dustEmitters = null;
                dustAnimators = null;
                int len = dustObjects.Length;
                for (int i = 0; i < len; i++)
                {
                    GameObject.Destroy(dustObjects[i]);
                }
            }
            GameEvents.onGamePause.Remove(new EventVoid.OnEvent(onGamePause));
            GameEvents.onGameUnpause.Remove(new EventVoid.OnEvent(onGameUnpause));
        }

        private void onGamePause()
        {
            gamePaused = true;
        }

        private void onGameUnpause()
        {
            gamePaused = false;
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready || wheel==null) { return; }
            if (controller.wheelState != KSPWheelState.DEPLOYED) { return; }
            bool enabled = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wheelDustEffects;
            if (!enabled) { return; }
            if (!setupEmitters) { setupDustEmitters(); }
            updateDustEmission();
        }

        private void setupDustEmitters()
        {
            setupEmitters = true;
            //one dust and one water effect emitter per wheel collider ..
            int len = controller.wheelData.Length;
            dustObjects = new GameObject[len];
            dustEmitters = new ParticleEmitter[len];
            dustAnimators = new ParticleAnimator[len];
            waterObjects = new GameObject[len];
            waterEmitters = new ParticleEmitter[len];
            waterAnimators = new ParticleAnimator[len];
            KSPWheelCollider wheel;
            for (int i = 0; i < len; i++)
            {
                wheel = controller.wheelData[i].wheel;
                //TODO -- clean up loading code; load once, clone many times
                dustObjects[i] = (GameObject)GameObject.Instantiate(Resources.Load(dustParticleEffect));
                dustObjects[i].transform.position = wheel.transform.position;
                dustObjects[i].transform.rotation = wheel.transform.rotation;
                dustEmitters[i] = dustObjects[i].GetComponent<ParticleEmitter>();
                dustAnimators[i] = dustEmitters[i].GetComponent<ParticleAnimator>();
                dustAnimators[i].colorAnimation = dustColorArray;
                dustEmitters[i].useWorldSpace = true;
                dustEmitters[i].localVelocity = Vector3.zero;
                dustEmitters[i].emit = false;

                waterObjects[i] = (GameObject)GameObject.Instantiate(Resources.Load(waterParticleEffect));
                waterObjects[i].transform.position = wheel.transform.position;
                waterObjects[i].transform.rotation = wheel.transform.rotation;
                waterEmitters[i] = waterObjects[i].GetComponent<ParticleEmitter>();
                waterAnimators[i] = waterEmitters[i].GetComponent<ParticleAnimator>();
                waterAnimators[i].colorAnimation = waterColorArray;
                waterEmitters[i].useWorldSpace = true;
                waterEmitters[i].localVelocity = Vector3.zero;
                waterEmitters[i].emit = false;
            }
            for (int i = 0; i < 5; i++)
            {
                float percent = (1f - ((float)i / 5f)) * 0.05f;
                waterColorArray[i] = new Color(0.8f, 0.8f, 0.95f, percent);
            }
        }

        private void updateDustEmission()
        {
            if (gamePaused) { return; }//game paused...
            int len = dustObjects.Length;
            if (HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wheelDustCamera)
            {
                //TODO remove this per-tick component lookup; source say to cache the vessel->module map in a static map in the vessel-module class; add/remove by the start/etc methods on the vessel-module
                KSPWheelDustCamera cm = vessel.GetComponent<KSPWheelDustCamera>();
                updateColorArray(cm.cameraColor);
            }
            else
            {
                if (colorUpdateTimer <= 0)
                {
                    string body = vessel.mainBody.name;
                    string biome = ScienceUtil.GetExperimentBiome(vessel.mainBody, vessel.latitude, vessel.longitude);
                    if (body != prevBody || biome != prevBiome)
                    {
                        updateColorArray(DustColors.getBodyColor(body, biome));
                    }
                    colorUpdateTimer = colorUpdateTime;
                    prevBody = body;
                    prevBiome = biome;
                }
                colorUpdateTimer -= Time.deltaTime;
            }
            if (dustPower <= 0)
            {
                //early out, no need to compute anything, dust is effectively disabled
                return;
            }
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
                if (data.waterMode)
                {
                    //TODO -- how to handle emission power
                    //should be mostly based on vessel speed, with a secondary term for wheel velocity
                    //but this needs to be handled a bit differently for repulsors, as they still need spring force
                    springForce = wheel.springForce * 0.1f * dustForceMult * 2f;
                    speedForce = Mathf.Clamp(Mathf.Abs(wheel.wheelLocalVelocity.z) / maxDustSpeed, 0, 1);
                    slipForce = Mathf.Clamp(Mathf.Abs(wheel.wheelLocalVelocity.x) / maxDustSpeed, 0, 1);
                    mult = Mathf.Sqrt(speedForce * speedForce * dustSpeedMult + slipForce * slipForce * dustSlipMult);
                    waterObjects[i].transform.position = wheel.worldHitPos;
                    waterObjects[i].transform.rotation = wheel.transform.rotation;
                    waterEmitters[i].localVelocity = Vector3.up * (speedForce + slipForce);
                    waterEmitters[i].minEmission = dustMinEmission * dustPower;
                    waterEmitters[i].maxEmission = dustMaxEmission * mult * dustPower;
                    waterEmitters[i].minEnergy = dustMinEnergy * dustPower;
                    waterEmitters[i].maxEnergy = dustMaxEnergy * mult * dustPower;
                    waterEmitters[i].minSize = dustMinSize * springForce * dustPower;
                    waterEmitters[i].maxSize = dustMaxSize * springForce * dustPower;
                    waterEmitters[i].Emit();
                }
                else if (wheel.isGrounded && wheel.wheelLocalVelocity.magnitude >= minDustSpeed)
                {
                    springForce = wheel.springForce * 0.1f * dustForceMult;
                    speedForce = Mathf.Clamp(Mathf.Abs(wheel.wheelLocalVelocity.z) / maxDustSpeed, 0, 1);
                    slipForce = Mathf.Clamp(Mathf.Abs(wheel.wheelLocalVelocity.x) / maxDustSpeed, 0, 1);
                    mult = Mathf.Sqrt(speedForce * speedForce * dustSpeedMult + slipForce * slipForce * dustSlipMult);
                    dustObjects[i].transform.position = wheel.worldHitPos;
                    dustObjects[i].transform.rotation = wheel.transform.rotation;
                    dustEmitters[i].localVelocity = Vector3.up * (speedForce + slipForce);
                    dustEmitters[i].minEmission = dustMinEmission * dustPower;
                    dustEmitters[i].maxEmission = dustMaxEmission * mult * dustPower;
                    dustEmitters[i].minEnergy = dustMinEnergy * dustPower;
                    dustEmitters[i].maxEnergy = dustMaxEnergy * mult * dustPower;
                    dustEmitters[i].minSize = dustMinSize * springForce * dustPower;
                    dustEmitters[i].maxSize = dustMaxSize * springForce * dustPower;
                    dustEmitters[i].Emit();
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
            Color color = inputColor;
            dustColorArray[0] = color;
            dustColorArray[1] = color;
            dustColorArray[2] = color;
            dustColorArray[3] = color;
            dustColorArray[4] = color;
            int len = dustAnimators.Length;
            for (int i = 0; i < len; i++)
            {
                dustAnimators[i].colorAnimation = dustColorArray;
                waterAnimators[i].colorAnimation = waterColorArray;
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class DustColors : MonoBehaviour
    {
        private static Dictionary<String, BodyDustColors> bodyDustColors = new Dictionary<string, BodyDustColors>();
        public static readonly Color defaultColor = new Color(0.75f, 0.75f, 0.75f, 0.014f);

        public void Start()
        {
            GameObject.DontDestroyOnLoad(this);
            GameEvents.OnGameDatabaseLoaded.Add(new EventVoid.OnEvent(gameDatabaseLoaded));
        }

        public void OnDestroy()
        {
            GameEvents.OnGameDatabaseLoaded.Remove(new EventVoid.OnEvent(gameDatabaseLoaded));
        }

        private void gameDatabaseLoaded()
        {
            loadDustColors();
        }

        public void ModuleManagerPostLoad()
        {
            loadDustColors();
        }

        public static Color getBodyColor(string body, string biome)
        {
            if (bodyDustColors.ContainsKey(body))
            {
                return bodyDustColors[body].getBiomeColor(biome);
            }
            return defaultColor;
        }

        public static void loadDustColors()
        {
            //TODO -- when would loading of the config values be appropriate outside of a module-manager context?
            //TODO -- can only really load them on game-database finished processing
            //TODO -- add a MM callback for -re-loading- of the color values
            bodyDustColors.Clear();
            ConfigNode[] potentialColorNodes = GameDatabase.Instance.GetConfigNodes("DustColorDefinitions");
            if (potentialColorNodes == null || potentialColorNodes.Length <= 0) { return; }
            ConfigNode bodyDustColorNode = potentialColorNodes[0];
            ConfigNode bodyNode;
            ConfigNode biomeNode;
            String bodyName, biomeName;
            Color color;
            int len = bodyDustColorNode.nodes.Count;
            for (int i = 0; i < len; i++)
            {
                bodyNode = bodyDustColorNode.nodes[i];
                bodyName = bodyNode.name;
                int len2 = bodyNode.nodes.Count;
                for (int k = 0; k < len2; k++)
                {
                    biomeNode = bodyNode.nodes[k];
                    biomeName = biomeNode.name;
                    color = ConfigNode.ParseColor(biomeNode.GetStringValue("Color", "0.75, 0.75, 0.75, 0.014"));
                    loadColor(bodyName, biomeName, color);
                }
            }
        }

        private static void loadColor(string body, string biome, Color color)
        {
            if (!bodyDustColors.ContainsKey(body))
            {
                bodyDustColors.Add(body, new BodyDustColors());
            }
            bodyDustColors[body].addColor(biome, color);
        }
    }

    public class BodyDustColors
    {
        private Dictionary<String, Color> biomeDustColors = new Dictionary<string, Color>();
        private Color bodyDefaultColor;

        public Color getBiomeColor(string name)
        {
            if (biomeDustColors.ContainsKey(name)) { return biomeDustColors[name]; }
            return bodyDefaultColor == null? DustColors.defaultColor : bodyDefaultColor;
        }

        public void addColor(string biomeName, Color color)
        {
            if (biomeName.ToLower().Equals("default"))
            {
                bodyDefaultColor = color;
                return;
            }
            if (!biomeDustColors.ContainsKey(biomeName))
            {
                biomeDustColors.Add(biomeName, color);
            }
        }
    }
}
