using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{

    [EffectDefinition("KSPWHEELFX")]
    public class KSPWheelParticleEffect : EffectBehaviour
    {

        /// <summary>
        /// The asset-bundle name to load
        /// </summary>
        [Persistent]
        public string modelName = string.Empty;

        [Persistent]
        public string textureName = string.Empty;

        [Persistent]
        public string transformName = string.Empty;

        //all other params taken from the particle effect itself
        //in the future, will add additional handling for more properties such as atm density

        /// <summary>
        /// All transforms of the given name.
        /// </summary>
        private List<Transform> transforms = new List<Transform>();

        private EffectData[] effectData;

        private static Dictionary<string, GameObject> modelPrefabs = new Dictionary<string, GameObject>();

        public override void OnInitialize()
        {
            MonoBehaviour.print("KSPWHEELFX-OnInitialize.  ModelName: "+modelName+" TransformName: "+transformName);
            hostPart.transform.FindRecursive("model").FindRecursiveMulti(transformName, transforms);
            GameObject model;
            if (!modelPrefabs.TryGetValue(modelName, out model))
            {
                model = loadFromDisk(modelName);
                modelPrefabs.Add(modelName, model);
            }
            int len = transforms.Count;
            effectData = new EffectData[len];
            for (int i = 0; i < len; i++)
            {
                effectData[i] = new EffectData(transforms[i], model);
            }
        }

        /// <summary>
        /// Called to 'start' the effect.  TODO -- also stops effect? (toggle?)
        /// </summary>
        public override void OnEvent()
        {
            //MonoBehaviour.print("KSPWHEELFX-OnEvent");

        }

        /// <summary>
        /// Called to play/stop the effect depending on input 'power'<para/>
        /// 0 = stop<para/>
        /// > 0 = play
        /// </summary>
        /// <param name="power"></param>
        public override void OnEvent(float power)
        {
            //MonoBehaviour.print("KSPWHEELFX-OnEvent: "+power);
            int len = effectData.Length;
            for (int i = 0; i < len; i++)
            {
                effectData[i].setPower(power);
            }
        }

        public override void OnEvent(int transformIdx)
        {
            OnEvent();
        }

        public override void OnEvent(float power, int transformIdx)
        {
            OnEvent(power);
        }

        public override void OnLoad(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);
            MonoBehaviour.print("KSPWHEELFX-OnLoad:\n" + node);
        }

        public override void OnSave(ConfigNode node)
        {
            ConfigNode.CreateConfigFromObject(this, node);
            MonoBehaviour.print("KSPWHEELFX-OnSave:\n" + node);
        }

        private GameObject loadFromDisk(string modelName)
        {
            MonoBehaviour.print("KSPWHEELFX-loadFromDisk: " + modelName);
            AssetBundle bundle = AssetBundle.LoadFromFile(KSPUtil.ApplicationRootPath + "GameData/" + modelName + ".kfx");
            if (bundle == null) { throw new NullReferenceException(); }
            string objName = bundle.GetAllAssetNames().FirstOrDefault(assetName => assetName.EndsWith("prefab"));
            GameObject model = (GameObject)bundle.LoadAsset(objName);
            bundle.Unload(false);
            return model;
        }

        public class EffectData
        {

            public Transform parentTransform;//parent object
            public Transform emitterTransform;//added at runtime
            public ParticleSystem particleSystem;
            public ParticleSystem.MainModule main;
            public ParticleSystem.EmissionModule emission;

            private float power=-1;

            public EffectData(Transform parent, GameObject model)
            {
                this.parentTransform = parent;
                this.emitterTransform = GameObject.Instantiate(model).transform;
                this.emitterTransform.parent = this.parentTransform;
                this.emitterTransform.localRotation = Quaternion.identity;
                this.emitterTransform.localPosition = Vector3.zero;
                this.emitterTransform.localScale = Vector3.one;
                this.particleSystem = this.emitterTransform.gameObject.GetComponentInChildren<ParticleSystem>();
                this.main = particleSystem.main;
                this.emission = particleSystem.emission;
            }

            public void setPower(float val)
            {

                if (val != power)
                {
                    if (val <= 0)
                    {
                        val = 0;
                        //disable
                        emission.enabled = false;
                    }
                    else
                    {
                        //if was previously disabled
                        if (power <= 0)
                        {
                            //enable
                            emission.enabled = true;
                        }

                        //apply stats scaling
                        //what stats should be scaled with power?
                        emission.rateOverTimeMultiplier = power;
                    }
                    power = val;
                }

            }

            public void Destroy ()
            {
                GameObject.Destroy(emitterTransform.gameObject);
            }

        }

    }

}
