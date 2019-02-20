using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{

    public class KSPWheelTreadDirection : KSPWheelSubmodule
    {

        [KSPField]
        public string wheelMeshName = "WheelTread";

        [KSPField]
        public int standardDirection = 1;

        [KSPField(isPersistant = true, guiActiveEditor = true),
         UI_Toggle(enabledText = "Inverted", disabledText = "Standard", suppressEditorShipModified =true, scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.None)]
        public bool inverted;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(inverted)].uiControlEditor.onFieldChanged = textureInvertToggled;
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            Material mat = part.transform.FindRecursive("model").FindRecursive(wheelMeshName)?.GetComponent<Renderer>()?.material;
            updateTexture(mat);
        }

        private void updateTexture(Material mat)
        {
            Vector2 scaling = mat.mainTextureScale;
            scaling.x = standardDirection * (inverted ? -1 : 1);
            //TU shaders use maintex UV for all texture coords
            mat.SetTextureScale("_MainTex", scaling);
            //special feature of TU shaders to flip the X coordinate of the normal map, fixes lighting issues with negative UV scaling
            mat.SetFloat("_NormalFlipX", scaling.x);
        }

        private void textureInvertToggled(BaseField a, object b)
        {
            Material mat = part.transform.FindRecursive("model").FindRecursive(wheelMeshName)?.GetComponent<Renderer>()?.material;
            updateTexture(mat);
        }

    }

}
