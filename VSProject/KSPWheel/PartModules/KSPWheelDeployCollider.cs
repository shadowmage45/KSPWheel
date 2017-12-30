using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelDeployCollider : KSPWheelSubmodule
    {

        [KSPField]
        public string colliderNames = string.Empty;

        [KSPField]
        public float colliderRadius = 0.1f;

        [KSPField]
        public Vector3 colliderOffset = Vector3.zero;

        private string[] parsedNames = null;

        private Transform[] transforms;

        private SphereCollider[] colliders;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            locateTransforms();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            locateTransforms();
        }

        private void locateTransforms()
        {
            parsedNames = colliderNames.Split(',');
            int len = parsedNames.Length;
            List<Transform> transformList = new List<Transform>();
            for (int i = 0; i < len; i++)
            {
                parsedNames[i] = parsedNames[i].Trim();
                Transform[] trs = part.transform.FindRecursive("model").FindChildren(parsedNames[i]);
                transformList.AddUniqueRange(trs);
            }
            transforms = transformList.ToArray();
        }

        private void enableColliders()
        {
            if (!HighLogic.LoadedSceneIsFlight) { return; }
            int len = transforms.Length;
            colliders = new SphereCollider[len];
            Vector3 offset;
            for (int i = 0; i < len; i++)
            {
                colliders[i] = transforms[i].gameObject.AddComponent<SphereCollider>();
                colliders[i].radius = colliderRadius;
                offset = part.transform.TransformDirection(colliderOffset);//from part-local to world
                offset = transforms[i].InverseTransformDirection(offset);//from world to transform local
                colliders[i].center = offset;                
                CollisionManager.IgnoreCollidersOnVessel(vessel, colliders[i]);
            }
        }

        private void disableColliders()
        {
            if (colliders == null) { return; }
            int len = transforms.Length;
            for (int i = 0; i < len; i++)
            {
                colliders[i].enabled = false;
                GameObject.Destroy(colliders[i]);
                colliders[i] = null;
            }
            colliders = null;
        }

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            if (controller.wheelState == KSPWheelState.DEPLOYING || controller.wheelState == KSPWheelState.RETRACTING)
            {
                enableColliders();
            }
            else
            {
                disableColliders();
            }
        }

        internal override void onStateChanged(KSPWheelState oldState, KSPWheelState newState)
        {
            base.onStateChanged(oldState, newState);
            if (newState == KSPWheelState.DEPLOYING || newState == KSPWheelState.RETRACTING)
            {
                enableColliders();
            }
            else
            {
                disableColliders();
            }
        }
    }
}
