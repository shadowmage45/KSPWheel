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

        [KSPField(guiName = "Dust", guiActive = true),
         UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", suppressEditorShipModified = true)]
        public bool emittersEnabled = false;

        /// <summary>
        /// Below this value of spring force / spring loading, no contribution to emission will come from spring force
        /// </summary>
        [KSPField]
        public float minDustForce = 0.1f;

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

        [KSPField(guiName = "Min Size", guiActive = true),
         UI_FloatRange(minValue = 0f, maxValue = 10f, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float dustMinSize = 0.1f;

        [KSPField(guiName = "Max Size", guiActive = true),
         UI_FloatRange(minValue = 0f, maxValue = 10f, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float dustMaxSize = 3.5f;

        [KSPField(guiName = "Min Emit", guiActive = true),
         UI_FloatRange(minValue = 0f, maxValue = 10f, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float dustMinEmission = 0.1f;

        [KSPField(guiName = "Max Emit", guiActive = true),
         UI_FloatRange(minValue = 0f, maxValue = 20f, stepIncrement = 0.25f, suppressEditorShipModified = true)]
        public float dustMaxEmission = 20f;

        [KSPField(guiName = "Min Energy", guiActive = true),
         UI_FloatRange(minValue = 0f, maxValue = 10f, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float dustMinEnergy = 0.1f;

        [KSPField(guiName = "Max Energy", guiActive = true),
         UI_FloatRange(minValue = 0f, maxValue = 10f, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float dustMaxEnergy = 3f;

        [KSPField(guiName = "Particle System", guiActive = true),
         UI_Toggle(enabledText = "New", disabledText = "Old", suppressEditorShipModified = true)]
        public bool useParticleSystem = false;

        [KSPField(guiName = "Body", guiActive = true)]
        public string body = String.Empty;

        [KSPField(guiName = "Biome", guiActive = true)]
        public string biome = String.Empty;

        [KSPField(guiName = "Color", guiActive = true)]
        public string colorString = String.Empty;

        private Color[] colArr = new Color[5];
        private string prevBody = string.Empty;
        private string prevBiome = string.Empty;

        private float colorUpdateTime = 1f;
        private float colorUpdateTimer = 0f;

        private bool setupEmitters = false;
        private GameObject[] dustObjects;//one emitter per wheel collider on the part; so that tracks/compound parts still throw up dust on a per-wheel basis
        private ParticleEmitter[] dustEmitters;
        private ParticleAnimator[] dustAnimators;//no clue if these will even work in modern Unity versions, will investigate ParticleSystem instead...

        //TODO test out if stuff is compatible with Unity's new ParticleSystem setup
        private bool setupParticleSystem = false;

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
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready || wheel==null) { return; }
            bool enabled = HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wheelDustEffects;
            if (!enabled) { return; }
            if (controller.wheelState != KSPWheelState.DEPLOYED) { return; }
            if (!emittersEnabled) { return; }
            if (useParticleSystem)
            {
                //TODO test out if stuff is compatible with Unity's new ParticleSystem setup
            }
            else
            {
                if (!setupEmitters) { setupDustEmitters(); }
                updateDustEmission();
            }
        }

        private void setupDustEmitters()
        {
            setupEmitters = true;
            //one emitter per collider ..
            int len = controller.wheelData.Length;
            dustObjects = new GameObject[len];
            dustEmitters = new ParticleEmitter[len];
            dustAnimators = new ParticleAnimator[len];
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
                dustAnimators[i].colorAnimation = new Color[] { Color.white, Color.white, Color.white, Color.white, Color.white };
                dustEmitters[i].useWorldSpace = true;
                dustEmitters[i].localVelocity = Vector3.zero;
                dustEmitters[i].emit = false;
                //energy, emission, size, speed all updated per-frame
            }
        }

        private void updateDustEmission()
        {
            int len = dustObjects.Length;
            if (colorUpdateTimer <= 0)
            {
                body = vessel.mainBody.name;
                biome = ScienceUtil.GetExperimentBiome(vessel.mainBody, vessel.latitude, vessel.longitude);
                if (body != prevBody || biome != prevBiome)
                {
                    Color color = DustColors.getBodyColor(body, biome);
                    MonoBehaviour.print("updating color for: " + body + " : " + biome + " new color: "+color);
                    colArr[0] = color;
                    colArr[1] = color;
                    colArr[2] = color;
                    colArr[3] = color;
                    colArr[4] = color;
                    colorString = color.ToString();
                    for (int i = 0; i < len; i++)
                    {
                        dustAnimators[i].colorAnimation = colArr;
                    }
                }
                colorUpdateTimer = colorUpdateTime;
                prevBody = body;
                prevBiome = biome;
            }
            colorUpdateTimer -= Time.deltaTime;
            KSPWheelCollider wheel;
            float springForce = 1f;
            float speedForce = 1f;
            float slipForce = 1f;
            float mult = 0f;
            float maxLoad = 0f;
            for (int i = 0; i < len; i++)
            {
                wheel = controller.wheelData[i].wheel;
                maxLoad = controller.maxLoadRating * controller.wheelData[i].loadShare * Mathf.Pow(controller.scale * part.rescaleFactor, HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelScaleSettings>().wheelMaxLoadScalingPower);
                if (wheel.isGrounded && wheel.wheelLocalVelocity.magnitude > minDustSpeed)
                {
                    springForce = wheel.springForce * 0.1f / maxLoad;
                    speedForce = Mathf.Clamp(Mathf.Abs(wheel.wheelLocalVelocity.z) / maxDustSpeed, 0, 1);
                    slipForce = Mathf.Clamp(Mathf.Abs(wheel.wheelLocalVelocity.x) / maxDustSpeed, 0, 1);
                    mult = Mathf.Sqrt(springForce * springForce * dustForceMult + speedForce * speedForce * dustSpeedMult + slipForce * slipForce * dustSlipMult) * controller.scale;                    
                    dustObjects[i].transform.position = wheel.worldHitPos;
                    dustObjects[i].transform.rotation = wheel.transform.rotation;
                    dustEmitters[i].localVelocity = Vector3.up * (speedForce + slipForce);
                    dustEmitters[i].minEmission = dustMinEmission;
                    dustEmitters[i].maxEmission = dustMaxEmission * mult;
                    dustEmitters[i].minEnergy = dustMinEnergy;
                    dustEmitters[i].maxEnergy = dustMaxEnergy * mult;
                    dustEmitters[i].minSize = dustMinSize;
                    dustEmitters[i].maxSize = dustMaxSize * mult;
                    dustEmitters[i].Emit();
                }
            }
        }
    }

    //TODO -- this needs to be a KSPAddon with a game-database-loaded callback and MM database reloaded callbacks
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
            MonoBehaviour.print("body dust colors did not contain value for : " + body);
            return defaultColor;
        }

        public static void loadDustColors()
        {
            MonoBehaviour.print("Loading KSPWheel dust color maps!");
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
                MonoBehaviour.print("Processing body: "+bodyName);
                int len2 = bodyNode.nodes.Count;
                for (int k = 0; k < len2; k++)
                {
                    biomeNode = bodyNode.nodes[k];
                    biomeName = biomeNode.name;
                    color = ConfigNode.ParseColor(biomeNode.GetStringValue("Color", "0.75, 0.75, 0.75, 0.014"));
                    MonoBehaviour.print("Parsed color for biome: " + biomeName + " : " + color.ToString());
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
            MonoBehaviour.print("biome dust colors did not contain value for : " + name);
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
