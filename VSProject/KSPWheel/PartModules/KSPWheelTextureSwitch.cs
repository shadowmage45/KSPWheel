using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;

namespace KSPWheel.PartModules
{

    public class KSPWheelTextureSwitch : PartModule
    {

        [KSPField(isPersistant = true, guiName = "Texture", guiActive = false, guiActiveEditor = false),
         UI_ChooseOption(suppressEditorShipModified = true)]
        public string currentTextureSet = string.Empty;

        [Persistent]
        public string configNodeData;

        private bool initialized = false;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (string.IsNullOrEmpty(configNodeData)) { configNodeData = node.ToString(); }
            initialize();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            initialize();
            Fields[nameof(currentTextureSet)].uiControlEditor.onFieldChanged = delegate (BaseField a, System.Object b)
            {
                updateTextureSet();
            };
            ConfigNode node = ConfigNode.Parse(configNodeData).nodes[0];
            ConfigNode[] setNodes = node.GetNodes("TEXTURESET");
            string[] names = TextureUtils.getTextureSetNames(setNodes);
            string[] titles = TextureUtils.getTextureSetTitles(setNodes);
            UI_ChooseOption uco = (UI_ChooseOption)Fields[nameof(currentTextureSet)].uiControlEditor;
            uco.options = names;
            uco.display = titles;
        }

        public void initialize()
        {
            if (initialized) { return; }
            initialized = true;
            updateTextureSet();
        }

        public void onPartGeometryChanged()
        {
            updateTextureSet();
        }

        public void updateTextureSet()
        {
            TextureSet s = TextureUtils.getTextureSet(currentTextureSet);
            s.enable(part.transform.FindRecursive("model").gameObject);
        }

    }
    
    public class TextureSet
    {
        public readonly String name;
        public readonly string title;
        public readonly TextureSetMaterialData[] textureData;

        public TextureSet(ConfigNode node)
        {
            name = node.GetStringValue("name");
            title = node.GetStringValue("title", name);
            ConfigNode[] texNodes = node.GetNodes("TEXTURE");
            int len = texNodes.Length;
            textureData = new TextureSetMaterialData[len];
            for (int i = 0; i < len; i++)
            {
                textureData[i] = new TextureSetMaterialData(texNodes[i]);
            }
        }

        public void enable(GameObject root)
        {
            foreach (TextureSetMaterialData mtd in textureData)
            {
                mtd.enable(root);
            }
        }

        public static TextureSet[] parse(ConfigNode[] nodes)
        {
            int len = nodes.Length;
            TextureSet[] sets = new TextureSet[len];
            for (int i = 0; i < len; i++)
            {
                sets[i] = new TextureSet(nodes[i]);
            }
            return sets;
        }

        /// <summary>
        /// Loads full texture sets from a config node containing only the set name
        /// the full set is loaded from the global set of SSTU_TEXTURESETs
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public static TextureSet[] loadGlobalTextureSets(ConfigNode[] nodes)
        {
            int len = nodes.Length;
            TextureSet[] sets = new TextureSet[len];
            for (int i = 0; i < len; i++)
            {
                sets[i] = getGlobalTextureSet(nodes[i].GetStringValue("name"));
            }
            return sets;
        }

        /// <summary>
        /// Retrieve a single SSTU_TEXTURESET by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TextureSet getGlobalTextureSet(string name)
        {
            return TextureUtils.getTextureSet(name);
        }

    }

    public class TextureSetMaterialData
    {
        public readonly String shader;
        public readonly String[] meshNames;
        public readonly String[] excludedMeshes;
        public readonly ShaderProperty[] props;

        public TextureSetMaterialData(ConfigNode node)
        {
            shader = node.GetStringValue("shader");
            meshNames = node.GetStringValues("mesh");
            excludedMeshes = node.GetStringValues("excludeMesh");
            props = ShaderProperty.parse(node);
        }

        public void enable(GameObject root)
        {
            TextureUtils.updateModelMaterial(root.transform, excludedMeshes, meshNames, shader, props);
        }

        public void enable(Material mat)
        {
            TextureUtils.updateMaterial(mat, shader, props);
        }

        public string getPropertyValue(string name)
        {
            ShaderProperty prop = Array.Find(props, m => m.name == name);
            if (prop == null) { return string.Empty; }
            if (prop is ShaderPropertyTexture) { return ((ShaderPropertyTexture)prop).textureName; }
            if (prop is ShaderPropertyFloat) { return ((ShaderPropertyFloat)prop).val.ToString(); }
            if (prop is ShaderPropertyColor) { return ((ShaderPropertyColor)prop).color.ToString(); }
            return string.Empty;
        }

        public Material createMaterial(string name)
        {
            string shdName = string.IsNullOrEmpty(this.shader) ? "KSP/Diffuse" : this.shader;
            Shader shd = Shader.Find(shdName);
            Material mat = new Material(shd);
            mat.name = name;
            TextureUtils.updateMaterialProperties(mat, props);
            return mat;
        }
    }

    public abstract class ShaderProperty
    {
        public readonly string name;

        public static ShaderProperty[] parse(ConfigNode node)
        {
            List<ShaderProperty> props = new List<ShaderProperty>();
            //direct property nodes
            ConfigNode[] propNodes = node.GetNodes("PROPERTY");
            int len = propNodes.Length;
            for (int i = 0; i < len; i++)
            {
                if (propNodes[i].HasValue("texture"))
                {
                    props.Add(new ShaderPropertyTexture(propNodes[i]));
                }
                else if (propNodes[i].HasValue("color"))
                {
                    props.Add(new ShaderPropertyColor(propNodes[i]));
                }
                else if (propNodes[i].HasValue("float"))
                {
                    props.Add(new ShaderPropertyFloat(propNodes[i]));
                }
            }
            //simply/lazy texture assignments
            string[] textures = node.GetStringValues("texture");
            len = textures.Length;
            string[] splits;
            string name, tex;
            bool main, nrm;
            for (int i = 0; i < len; i++)
            {
                splits = textures[i].Split(',');
                name = splits[0].Trim();
                tex = splits[1].Trim();
                main = splits[0] == "_MainTex";
                nrm = splits[0] == "_BumpMap";
                props.Add(new ShaderPropertyTexture(name, tex, main, nrm));
            }
            return props.ToArray();
        }

        protected ShaderProperty(ConfigNode node)
        {
            this.name = node.GetStringValue("name");
        }

        protected ShaderProperty(string name)
        {
            this.name = name;
        }

        public void apply(Material mat)
        {
            applyInternal(mat);
        }

        protected abstract void applyInternal(Material mat);

        //protected abstract string getStringValue();

        protected bool checkApply(Material mat)
        {
            if (mat.HasProperty(name))
            {
                return true;
            }
            else
            {
                MonoBehaviour.print("Shader: " + mat.shader + " did not have property: " + name);
            }
            return false;
        }

    }

    public class ShaderPropertyColor : ShaderProperty
    {
        public readonly Color color;

        public ShaderPropertyColor(ConfigNode node) : base(node)
        {            
            color = node.GetColorValue("color");
        }

        public ShaderPropertyColor(string name, Color color) : base(name)
        {
            this.color = color;
        }

        protected override void applyInternal(Material mat)
        {
            mat.SetColor(name, color);
        }
    }

    public class ShaderPropertyFloat : ShaderProperty
    {
        public readonly float val;

        public ShaderPropertyFloat(ConfigNode node) : base(node)
        {
            val = node.GetFloatValue("float");
        }

        public ShaderPropertyFloat(string name, float val) : base(name)
        {
            this.val = val;
        }

        protected override void applyInternal(Material mat)
        {
            if (checkApply(mat))
            {
                mat.SetFloat(name, val);
            }
        }
    }

    public class ShaderPropertyTexture : ShaderProperty
    {
        public readonly string textureName;
        public readonly bool main;
        public readonly bool normal;

        public ShaderPropertyTexture(ConfigNode node) : base(node)
        {
            textureName = node.GetStringValue("texture");
            main = node.GetBoolValue("main");
            normal = node.GetBoolValue("normal");
        }

        public ShaderPropertyTexture(string name, string texture, bool main, bool normal) : base(name)
        {
            this.textureName = texture;
            this.main = main;
            this.normal = normal;
        }

        protected override void applyInternal(Material mat)
        {
            if (checkApply(mat))
            {
                if (main)
                {
                    mat.mainTexture = GameDatabase.Instance.GetTexture(textureName, false);
                }
                else
                {
                    mat.SetTexture(name, GameDatabase.Instance.GetTexture(textureName, normal));
                }
            }
        }
    }

    public static class TextureUtils
    {

        private static Dictionary<string, TextureSet> globalTextureSets = new Dictionary<string, TextureSet>();

        public static void loadTextureSets()
        {
            globalTextureSets.Clear();
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("SSTU_TEXTURESET");
            int len = nodes.Length;
            for (int i = 0; i < len; i++)
            {
                TextureSet n = new TextureSet(nodes[i]);
                globalTextureSets.Add(n.name, n);
            }
        }

        public static void updateModelMaterial(Transform root, string[] excludeMeshes, string[] meshes, string shader, ShaderProperty[] props)
        {
            //black-list, do everything not specified in excludeMeshes array
            if (excludeMeshes != null && excludeMeshes.Length > 0)
            {
                Renderer[] allRends = root.GetComponentsInChildren<Renderer>();
                int len = allRends.Length;
                for (int i = 0; i < len; i++)
                {
                    if (!excludeMeshes.Contains(allRends[i].name))
                    {
                        updateRenderer(allRends[i], shader, props);
                    }
                }
            }
            else if (meshes == null || meshes.Length <= 0)//no validation, do them all
            {
                Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
                int len = rends.Length;
                for (int i = 0; i < len; i++)
                {
                    updateRenderer(rends[i], shader, props);
                }
            }
            else//white-list, only do what is specified by meshes array
            {
                //MonoBehaviour.print("Updating material for meshes...");
                int len = meshes.Length;
                Transform[] trs;
                Transform tr;
                Renderer r;
                for (int i = 0; i < len; i++)
                {
                    //MonoBehaviour.print("----- " + meshes[i]);
                    trs = root.FindChildren(meshes[i]);
                    int len2 = trs.Length;
                    //if (len2 <= 0) { MonoBehaviour.print("Could not find mesh..."); }
                    for (int k = 0; k < len2; k++)
                    {
                        tr = trs[k];
                        if (tr == null)
                        {
                            //MonoBehaviour.print("Could not find mesh...");
                            continue;
                        }
                        r = tr.GetComponent<Renderer>();
                        if (r == null)
                        {
                            //MonoBehaviour.print("Mesh had no renderer...");
                            continue;
                        }
                        //MonoBehaviour.print("Updating renderer: " + r);
                        updateRenderer(r, shader, props);
                    }
                }
            }
        }

        public static void updateRenderer(Renderer rend, string shader, ShaderProperty[] props)
        {
            updateMaterial(rend.material, shader, props);
        }

        public static void updateMaterial(Material mat, string shader, ShaderProperty[] props)
        {
            if (!String.IsNullOrEmpty(shader))
            {
                Shader s = Shader.Find(shader);
                if (s != null && s != mat.shader)
                {
                    mat.shader = s;
                }
            }
            updateMaterialProperties(mat, props);
        }

        public static void updateMaterialProperties(Material m, ShaderProperty[] props)
        {
            if (m == null || props == null || props.Length == 0) { return; }
            int len = props.Length;
            for (int i = 0; i < len; i++)
            {
                props[i].apply(m);
            }
        }

        public static TextureSet getTextureSet(string name)
        {
            return globalTextureSets.ContainsKey(name) ? globalTextureSets[name] : null;
        }

        public static string[] getTextureSetNames(ConfigNode[] nodes)
        {
            List<string> names = new List<string>();
            string name;
            TextureSet set;
            int len = nodes.Length;
            for (int i = 0; i < len; i++)
            {
                name = nodes[i].GetStringValue("name");
                set = getTextureSet(name);
                if (set != null) { names.Add(set.name); }
            }
            return names.ToArray();
        }

        public static string[] getTextureSetTitles(ConfigNode[] nodes)
        {
            List<string> names = new List<string>();
            string name;
            TextureSet set;
            int len = nodes.Length;
            for (int i = 0; i < len; i++)
            {
                name = nodes[i].GetStringValue("name");
                set = getTextureSet(name);
                if (set != null) { names.Add(set.title); }
            }
            return names.ToArray();
        }

    }
}
