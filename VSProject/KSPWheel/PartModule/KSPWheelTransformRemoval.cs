using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelTransformRemoval : KSPWheelSubmodule
    {

        [Persistent]
        public string configNodeData = String.Empty;

        private TransformRemovalData[] data;
                
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (string.IsNullOrEmpty(configNodeData))
            {
                configNodeData = node.ToString();
            }
            loadData(ConfigNode.Parse(configNodeData).nodes[0]);
            process();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (data == null)
            {
                loadData(ConfigNode.Parse(configNodeData).nodes[0]);
                process();
            }
        }

        private void loadData(ConfigNode node)
        {
            ConfigNode[] nodes = node.GetNodes("TRANSFORM");
            int len = nodes.Length;
            data = new TransformRemovalData[len];
            for (int i = 0; i < len; i++)
            {
                data[i] = new TransformRemovalData(nodes[i]);
            }
        }

        private void process()
        {
            int curWhen = 0;
            if (HighLogic.LoadedSceneIsFlight)
            {
                curWhen = 4;
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                curWhen = 2;
            }
            else
            {
                curWhen = 1;
            }
            int len = data.Length;
            for (int i = 0; i < len; i++)
            {
                if ((data[i].whenToRemove & curWhen) != 0)
                {
                    Transform[] trs = part.transform.FindChildren(data[i].name);
                    foreach (Transform tr in trs)
                    {
                        GameObject.DestroyImmediate(tr.gameObject);
                    }
                }
            }
        }

        public class TransformRemovalData
        {
            public readonly string name = string.Empty;
            public readonly int whenToRemove = 0;//1=prefab, 2=editor, 4=flight -- binary mask

            public TransformRemovalData(ConfigNode node)
            {
                name = node.GetStringValue("name");
                whenToRemove = node.GetIntValue("when");
            }
        }
    }
}
