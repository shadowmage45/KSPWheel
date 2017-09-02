using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    /// <summary>
    /// Special non-submodule class that should be added to a part config - BEFORE the KSPWheelBase module.
    /// This ensures that its OnStart code is run before any other modules start their initialization process,
    /// which allows for the transforms they initially grab to be setup properly for the chosen model.
    /// </summary>
    public class KSPWheelSidedModel : PartModule
    {
        /// <summary>
        /// If this is found to be a clone part, the following model will replace the existing model on the prefab part.
        /// </summary>
        [KSPField]
        public string baseModel = string.Empty;

        /// <summary>
        /// If this is found to be a clone part, the following model will replace the existing model on the prefab part.
        /// </summary>
        [KSPField]
        public string symmetryModel = string.Empty;

        [KSPField(isPersistant = true)]
        private bool isClone = false;

        [SerializeField]
        private bool setupClone = false;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            KSPWheelSidedModel cloneModule = findCloneModule();
            if (cloneModule != null)//has symmetry parts
            {
                bool c = !cloneModule.isClone;
                if (c != isClone)//model is being flipped from what it currently is
                {
                    setupClone = false;//set flag to update model
                }
                isClone = c;
            }
            else if(!isClone)//has no symmetry parts, and doesn't need any model swapping
            {
                setupClone = true;
            }
            if (!setupClone)
            {
                setupClone = true;
                setupModel();
            }
        }

        private void setupModel()
        {
            Transform modelRoot = part.transform.FindRecursive("model");
            int len = modelRoot.childCount;
            for (int i = len-1; i >= 0; i--)
            {
                //TODO any problems caused by destroyimmediate?  I remember having issues with it in SSTU in specific cases
                GameObject.DestroyImmediate(modelRoot.GetChild(i).gameObject);
            }

            string modelName = isClone ? symmetryModel : baseModel;
            GameObject modelClone = GameDatabase.Instance.GetModel(modelName);//this is already a clone of the model from the database, no need to re-clone it
            if (modelClone == null)
            {
                MonoBehaviour.print("ERROR: Could not clone model for URL: " + modelName+" No model found.  Check the model name and path and correct the config file.");
            }
            modelClone.SetActive(true);
            modelClone.transform.parent = modelRoot;
            modelClone.transform.localPosition = Vector3.zero;
            modelClone.transform.localRotation = Quaternion.identity;
            part.gameObject.SendMessage("onPartGeometryChanged");//triggers texture-set module to reinit texture sets
        }

        private KSPWheelSidedModel findCloneModule()
        {
            if (part.symmetryCounterparts != null && part.symmetryCounterparts.Count > 0)
            {
                return part.symmetryCounterparts[0].GetComponent<KSPWheelSidedModel>();
            }
            return null;
        }

    }
}
