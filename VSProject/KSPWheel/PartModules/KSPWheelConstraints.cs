using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel.PartModules
{
    public class KSPWheelConstraints : PartModule
    {
        [Persistent]
        public string configNodeData = string.Empty;

        private bool initialized = false;
        private ConstraintData[] constraints;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            initializeConstraints();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            initializeConstraints();
        }

        public void reloadConstraints()
        {
            initialized = false;
            constraints = null;
            initializeConstraints();
        }

        public void initializeConstraints()
        {
            if (initialized) { return;}
            initialized = true;
            ConfigNode root = ConfigNode.Parse(configNodeData).nodes[0];
            ConfigNode[] constraintNodes = root.GetNodes("CONSTRAINT");            
            int len = constraintNodes.Length;
            constraints = new ConstraintData[len];
            for (int i = 0; i < len; i++)
            {
                constraints[i] = new ConstraintData(part, constraintNodes[i]);
            }
            //force contraints to update once after initialized
            updateConstraints();
        }

        public void Update()
        {
            updateConstraints();
        }

        private void updateConstraints()
        {
            int len = constraints.Length;
            for (int i = 0; i < len; i++)
            {
                constraints[i].updateConstraint();
            }
        }

    }

    public class ConstraintData
    {
        private Transform mover;
        private Transform target;
        private ConstraintType type = ConstraintType.POSITION;
        private Vector3 mainAxis;
        private Vector3 secAxis;

        public ConstraintData(Part part, ConfigNode node)
        {
            string moverDef = node.GetStringValue("mover");
            string[] moverSplit = moverDef.Split(',');
            string moverName = moverSplit[0];
            int moverIndex = moverSplit.Length >= 2 ? int.Parse(moverSplit[1]) : 0;
            mover = part.transform.FindChildren(moverName)[moverIndex];

            string targetDef = node.GetStringValue("target");
            string[] targetSplit = targetDef.Split(',');
            string targetName = targetSplit[0];
            int targetIndex = targetSplit.Length >= 2 ? int.Parse(targetSplit[1]) : 0;
            target = part.transform.FindChildren(targetName)[targetIndex];

            if (target == null || mover == null)
            {
                MonoBehaviour.print("ERROR SETTING UP CONSTRAINT.  Either mover or target was null for definitions: " + moverDef + " :: " + targetDef+".\n"+
                    "Please check your definitions and correct the error in the model or config.");
            }
            try
            {
                type = (ConstraintType)Enum.Parse(typeof(ConstraintType), node.GetStringValue("type", "POSITION"), true);
            }
            catch (Exception e)
            {
                MonoBehaviour.print("ERROR PARSING CONSTRAINT TYPE: " + node.GetStringValue("type") + " is not a recongnized constraint type.  Setting to default of: POSITION");
                MonoBehaviour.print("Valid types are: POSITION, ROTATION, LOOKFREE, LOOKLOCK");
                MonoBehaviour.print(e);
                type = ConstraintType.POSITION;
            }
        }

        public void updateConstraint()
        {
            switch (type)
            {
                case ConstraintType.POSITION:
                    updatePosition();
                    break;
                case ConstraintType.ROTATION:
                    updateRotation();
                    break;
                case ConstraintType.LOOKFREE:
                    updateLookFree();
                    break;
                case ConstraintType.LOOKLOCK:
                    updateLookLocked();
                    break;
                default:
                    break;
            }
        }

        private void updatePosition()
        {
            mover.position = target.position;
        }

        private void updateRotation()
        {
            mover.localRotation = target.rotation;
        }

        private void updateLookFree()
        {
            //TODO -- adapt to use specified axis
            mover.LookAt(target, mover.up);
        }

        private void updateLookLocked()
        {
            //TODO -- may need to use the position difference and convert via direction to avoid scaling problems
            //TODO -- uhh..yah..something in here needs adjusted for the 'main axis'.
            Vector3 targetPos = target.position;
            Vector3 localDiff = mover.InverseTransformPoint(targetPos);//position of the target, in local space (origin=0,0,0)
            Vector3 localRotation = Vector3.zero;
            float rx = 0f, ry = 0f, rz = 0f;
            if (secAxis.x != 0)
            {
                //use y and z
                rx = Mathf.Atan2(localDiff.y, localDiff.z) * Mathf.Rad2Deg;
            }
            else if (secAxis.y != 0)
            {
                //use x and z
                ry = Mathf.Atan2(localDiff.x, localDiff.z) * Mathf.Rad2Deg;
            }
            else if (secAxis.z != 0)
            {
                //use x and y
                rz = Mathf.Atan2(localDiff.x, localDiff.y) * Mathf.Rad2Deg;
            }
            mover.Rotate(rx, ry, rz, Space.Self);
        }

    }

    public enum ConstraintType
    {
        POSITION,
        ROTATION,
        LOOKFREE,
        LOOKLOCK
    }
}
