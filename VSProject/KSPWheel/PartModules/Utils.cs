using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KSPWheel
{
    /// <summary>
    /// Generic utility class for extension and util methods that do not clearly belong in any other class.
    /// Mostly this entails some helper methods for getting typed values from ConfigNode, and some Transform extensions to recursively locate transforms
    /// </summary>
    public static class Utils
    {
        public static Transform FindRecursive(this Transform transform, String name)
        {
            if (transform.name == name) { return transform; }
            Transform tr = transform.Find(name);
            if (tr != null) { return tr; }
            foreach (Transform child in transform)
            {
                tr = child.FindRecursive(name);
                if (tr != null) { return tr; }
            }
            return null;
        }

        public static Transform[] FindChildren(this Transform transform, String name)
        {
            List<Transform> trs = new List<Transform>();
            transform.FindRecursiveMulti(name, trs);
            return trs.ToArray();
        }

        public static void FindRecursiveMulti(this Transform transform, String name, List<Transform> addTo)
        {
            if (transform.name == name) { addTo.Add(transform); }
            foreach (Transform child in transform)
            {
                FindRecursiveMulti(child, name, addTo);
            }
        }

        public static void LookAtLocked(this Transform transform, Vector3 worldPos, Vector3 lookAxis, Vector3 rotationAxis)
        {
            Vector3 localDiff = transform.InverseTransformPoint(worldPos);//position of the target, in local space (origin=0,0,0)
            Vector3 localRotation = Vector3.zero;
            float rx = 0f, ry = 0f, rz = 0f;
            if (rotationAxis.x != 0)
            {
                //use y and z
                rx = -Mathf.Atan2(localDiff.y, localDiff.z) * Mathf.Rad2Deg;
                if (lookAxis.y > 0) { rx += 90f; }
                else if (lookAxis.y < 0) { rx -= 90f; }
                else if (lookAxis.z < 0) { rx += 180f; }
            }
            else if (rotationAxis.y != 0)
            {
                //use x and z
                ry = Mathf.Atan2(localDiff.x, localDiff.z) * Mathf.Rad2Deg;
                if (lookAxis.z < 0) { ry += 180f; }
                else if (lookAxis.x > 0) { ry -= 90f; }
                else if (lookAxis.x < 0) { ry += 90f; }
            }
            else if (rotationAxis.z != 0)
            {
                //use x and y
                rz = -Mathf.Atan2(localDiff.x, localDiff.y) * Mathf.Rad2Deg;
                if (lookAxis.x > 0) { rz += 90f; }
                else if (lookAxis.x < 0) { rz -= 90f; }
                else if (lookAxis.y < 0) { rz += 180f; }
            }
            transform.Rotate(rx, ry, rz, Space.Self);
        }

        public static void RotateFromDefault(this Transform transform, Quaternion def, float x, float y, float z, Space space = Space.Self)
        {
            transform.localRotation = def;
            transform.Rotate(x, y, z, space);
        }
        
        public static String[] GetStringValues(this ConfigNode node, String name)
        {
            String[] values = node.GetValues(name);
            return values == null ? new String[0] : values;
        }

        public static string GetStringValue(this ConfigNode node, String name, String defaultValue)
        {
            String value = node.GetValue(name);
            return value == null ? defaultValue : value;
        }

        public static string GetStringValue(this ConfigNode node, String name)
        {
            return GetStringValue(node, name, "");
        }

        public static bool[] GetBoolValues(this ConfigNode node, String name)
        {
            String[] values = node.GetValues(name);
            int len = values.Length;
            bool[] vals = new bool[len];
            for (int i = 0; i < len; i++)
            {
                vals[i] = bool.Parse(values[i]);
            }
            return vals;
        }

        public static bool GetBoolValue(this ConfigNode node, String name, bool defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return bool.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static bool GetBoolValue(this ConfigNode node, String name)
        {
            return GetBoolValue(node, name, false);
        }

        public static float[] GetFloatValues(this ConfigNode node, String name, float[] defaults)
        {
            String baseVal = node.GetStringValue(name);
            if (!String.IsNullOrEmpty(baseVal))
            {
                String[] split = baseVal.Split(new char[] { ',' });
                float[] vals = new float[split.Length];
                for (int i = 0; i < split.Length; i++) { vals[i] = float.Parse(split[i]); }
                return vals;
            }
            return defaults;
        }

        public static float[] GetFloatValues(this ConfigNode node, String name)
        {
            return GetFloatValues(node, name, new float[] { });
        }

        public static float[] GetFloatValuesCSV(this ConfigNode node, String name)
        {
            return GetFloatValuesCSV(node, name, new float[] { });
        }

        public static float[] GetFloatValuesCSV(this ConfigNode node, String name, float[] defaults)
        {
            float[] values = defaults;
            if (node.HasValue(name))
            {
                string strVal = node.GetStringValue(name);
                string[] splits = strVal.Split(',');
                values = new float[splits.Length];
                for (int i = 0; i < splits.Length; i++)
                {
                    values[i] = float.Parse(splits[i]);
                }
            }
            return values;
        }

        public static float GetFloatValue(this ConfigNode node, String name, float defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return float.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static float GetFloatValue(this ConfigNode node, String name)
        {
            return GetFloatValue(node, name, 0);
        }

        public static double GetDoubleValue(this ConfigNode node, String name, double defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return double.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static double GetDoubleValue(this ConfigNode node, String name)
        {
            return GetDoubleValue(node, name, 0);
        }

        public static int GetIntValue(this ConfigNode node, String name, int defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return int.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static int GetIntValue(this ConfigNode node, String name)
        {
            return GetIntValue(node, name, 0);
        }

        public static Vector3 GetVector3(this ConfigNode node, String name, Vector3 defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null)
            {
                return defaultValue;
            }
            String[] vals = value.Split(',');
            if (vals.Length < 3)
            {
                MonoBehaviour.print("ERROR parsing values for Vector3 from input: " + value + ". found less than 3 values, cannot create Vector3");
                return defaultValue;
            }
            return new Vector3(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]));
        }

        public static Vector3 GetVector3(this ConfigNode node, String name)
        {
            String value = node.GetValue(name);
            if (value == null)
            {
                MonoBehaviour.print("No value for name: " + name + " found in config node: " + node);
                return Vector3.zero;
            }
            String[] vals = value.Split(',');
            if (vals.Length < 3)
            {
                MonoBehaviour.print("ERROR parsing values for Vector3 from input: " + value + ". found less than 3 values, cannot create Vector3");
                return Vector3.zero;
            }
            return new Vector3(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]));
        }

        public static FloatCurve GetFloatCurve(this ConfigNode node, String name, FloatCurve defaultValue = null)
        {
            FloatCurve curve = new FloatCurve();
            if (node.HasNode(name))
            {
                ConfigNode curveNode = node.GetNode(name);
                String[] values = curveNode.GetValues("key");
                int len = values.Length;
                String[] splitValue;
                float a, b, c, d;
                for (int i = 0; i < len; i++)
                {
                    splitValue = Regex.Replace(values[i], @"\s+", " ").Split(' ');
                    if (splitValue.Length > 2)
                    {
                        a = float.Parse(splitValue[0]);
                        b = float.Parse(splitValue[1]);
                        c = float.Parse(splitValue[2]);
                        d = float.Parse(splitValue[3]);
                        curve.Add(a, b, c, d);
                    }
                    else
                    {
                        a = float.Parse(splitValue[0]);
                        b = float.Parse(splitValue[1]);
                        curve.Add(a, b);
                    }
                }
            }
            else if (defaultValue != null)
            {
                foreach (Keyframe f in defaultValue.Curve.keys)
                {
                    curve.Add(f.time, f.value, f.inTangent, f.outTangent);
                }
            }
            else
            {
                curve.Add(0, 0);
                curve.Add(1, 1);
            }
            return curve;
        }

        public static void printHierarchy(GameObject go)
        {
            printHierarchy(go.transform, "");
        }

        public static void printHierarchy(Transform tr, string prefix)
        {
            prefix = prefix + "    ";
            MonoBehaviour.print(prefix+"GO: "+tr.gameObject + " -- scale: "+tr.localScale+" : "+tr.lossyScale);
            foreach (UnityEngine.Component c in tr.gameObject.GetComponents<UnityEngine.Component>())
            {
                MonoBehaviour.print(prefix + " CP: " + c);
            }
            foreach (Transform trc in tr)
            {
                printHierarchy(trc, prefix);
            }
        }

        public static void setPartColliderField(Part part)
        {
            Collider[] cols = part.GetComponentsInChildren<Collider>();
            if (cols != null && cols.Length > 0) { part.collider = cols[0]; }
        }

        public static bool rayPlaneIntersect(Vector3 rayStart, Vector3 rayDirection, Vector3 point, Vector3 normal, out Vector3 hit)
        {
            float lndot = Vector3.Dot(rayDirection, normal);
            if (lndot == 0)//parallel
            {
                //if(point - start) dot normal == 0 line is on plane
                if (Vector3.Dot(point - rayStart, normal) == 0)
                {
                    hit = point;
                    return true;
                }
                hit = Vector3.zero;
                return false;
            }
            float dist = Vector3.Dot((point - rayStart), normal) / lndot;
            hit = rayStart + dist * rayDirection;
            return true;
        }

        public static void updateAttachNodes(Part part, float prevScale, float newScale, bool userInput)
        {
            if (part.srfAttachNode != null)
            {
                updateAttachNode(part, part.srfAttachNode, prevScale, newScale, userInput);
            }
            int len = part.attachNodes.Count;
            for (int i = 0; i < len; i++)
            {
                updateAttachNode(part, part.attachNodes[i], prevScale, newScale, userInput);
            }
        }

        public static void updateAttachNode(Part part, AttachNode node, float prevScale, float newScale, bool userInput)
        {
            Vector3 basePosition = node.position / prevScale;
            Vector3 newPosition = basePosition * newScale;
            Vector3 diff = newPosition - node.position;
            node.position = node.originalPosition = newPosition;
            if (userInput && node.attachedPart != null)
            {
                Vector3 globalDiff = part.transform.TransformPoint(diff);
                globalDiff -= part.transform.position;
                if (node.attachedPart.parent == part)//is a child of this part, move it the entire offset distance
                {
                    node.attachedPart.attPos0 += diff;
                    node.attachedPart.transform.position += globalDiff;
                }
                else//is a parent of this part, do not move it, instead move this part the full amount
                {
                    part.attPos0 -= diff;
                    part.transform.position -= globalDiff;
                    //and then, if this is not the root part, offset the root part in the negative of the difference to maintain relative part position
                    Part p = part.localRoot;
                    if (p != null && p != part)
                    {
                        p.transform.position += globalDiff;
                    }
                }
            }
        }

        public static void symmetryUpdate<T>(this T t, Action<T> act) where T : PartModule
        {
            act(t);
            int len = t.part.symmetryCounterparts.Count;
            T[] ts;
            int len2;
            for (int i = 0; i < len; i++)
            {
                ts = t.part.symmetryCounterparts[i].GetComponents<T>();
                len2 = ts.Length;
                for (int k = 0; k < len2; k++)
                {
                    act(ts[k]);
                }
            }
        }

        public static void wheelGroupUpdate<T>(this T t, int wheelGroup, Action<T> act) where T : KSPWheelSubmodule
        {
            if (wheelGroup <= 0)
            {
                act.Invoke(t);
                return;
            }
            try
            {//need to call the passed in delegate on each module of the given type where the controller module for that part shares the wheel group that is passed in
                List<T> subModules = new List<T>();
                if (HighLogic.LoadedSceneIsFlight)
                {
                    int len = t.part.vessel.Parts.Count;
                    for (int i = 0; i < len; i++)
                    {
                        KSPWheelBase baseModule = t.part.vessel.Parts[i].GetComponent<KSPWheelBase>();
                        if (baseModule == null) { continue; }
                        if (int.Parse(baseModule.wheelGroup) == wheelGroup)
                        {
                            subModules.AddRange(baseModule.part.GetComponents<T>());
                        }
                    }
                }
                int len2 = subModules.Count;
                for (int i = 0; i < len2; i++)
                {
                    act.Invoke(subModules[i]);
                }
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e);
            }            
        }

        public static void wheelGroupUpdateBase<T>(this T t, int wheelGroup, Action<T> act) where T : KSPWheelBase
        {
            if (wheelGroup <= 0)
            {
                act.Invoke(t);
                return;
            }
            try
            {
                //need to call the passed in delegate on each module of the given type where the controller module for that part shares the wheel group that is passed in
                List<T> subModules = new List<T>();
                if (HighLogic.LoadedSceneIsFlight)
                {
                    int len = t.part.vessel.Parts.Count;
                    for (int i = 0; i < len; i++)
                    {
                        KSPWheelBase baseModule = t.part.vessel.Parts[i].GetComponent<KSPWheelBase>();
                        if (baseModule == null) { continue; }
                        if (int.Parse(baseModule.wheelGroup) == wheelGroup)
                        {
                            subModules.AddRange(baseModule.part.GetComponents<T>());
                        }
                    }
                }
                else if (HighLogic.LoadedSceneIsEditor)
                {
                    //TODO loop through ship construct parts list...
                }
                int len2 = subModules.Count;
                for (int i = 0; i < len2; i++)
                {
                    act.Invoke(subModules[i]);
                    MonoUtilities.RefreshContextWindows(subModules[i].part);
                }
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e);
            }
        }

        public static void updateUIFloatEditControl(this PartModule module, string fieldName, float newValue)
        {
            UI_FloatEdit widget = null;
            if (HighLogic.LoadedSceneIsEditor)
            {
                widget = (UI_FloatEdit)module.Fields[fieldName].uiControlEditor;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                widget = (UI_FloatEdit)module.Fields[fieldName].uiControlFlight;
            }
            else
            {
                return;
            }
            if (widget == null)
            {
                return;
            }
            BaseField field = module.Fields[fieldName];
            field.SetValue(newValue, field.host);
            if (widget.partActionItem != null)//force widget re-setup for changed values; this will update the GUI value and slider positions/internal cached data
            {
                UIPartActionFloatEdit ctr = (UIPartActionFloatEdit)widget.partActionItem;
                var t = widget.onFieldChanged;//temporarily remove the callback; we don't need an event fired when -we- are the ones editing the value...            
                widget.onFieldChanged = null;
                ctr.incSmall.onToggle.RemoveAllListeners();
                ctr.incLarge.onToggle.RemoveAllListeners();
                ctr.decSmall.onToggle.RemoveAllListeners();
                ctr.decLarge.onToggle.RemoveAllListeners();
                ctr.slider.onValueChanged.RemoveAllListeners();
                ctr.Setup(ctr.Window, module.part, module, HighLogic.LoadedSceneIsEditor ? UI_Scene.Editor : UI_Scene.Flight, widget, module.Fields[fieldName]);
                widget.onFieldChanged = t;//re-seat callback
            }
        }

        public static void updateUIFloatRangeControl(this PartModule module, string fieldName, float newValue, float min, float max, float inc)
        {
            UI_FloatRange widget = null;
            if (HighLogic.LoadedSceneIsEditor)
            {
                widget = (UI_FloatRange)module.Fields[fieldName].uiControlEditor;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                widget = (UI_FloatRange)module.Fields[fieldName].uiControlFlight;
            }
            else
            {
                return;
            }
            if (widget == null)
            {
                return;
            }
            BaseField field = module.Fields[fieldName];
            field.SetValue(newValue, field.host);
            widget.minValue = min;
            widget.maxValue = max;
            widget.stepIncrement = inc;
        }

        public static Rigidbody locateRigidbodyUpwards(Part part)
        {
            Rigidbody rb = null;
            while (rb == null)
            {
                if (part.parent == null) { break; }
                part = part.parent;
                rb = part.GetComponent<Rigidbody>();
            }
            return rb;
        }

        public class PIDController
        {

            private float kp;
            private float ki;
            private float kd;

            private float error;
            private float prevError;
            private float errorSum;

            private float input;
            private float dest;
            private float output;

            public PIDController(float input, float dest, float kp, float ki, float kd)
            {
                this.input = input;
                this.dest = dest;
                this.kp = kp;
                this.ki = ki;
                this.kd = kd;
                prevError = error = dest - input;
                errorSum = 0;
            }

            public void setParams(float dest, float kp, float ki, float kd)
            {
                this.dest = dest;
                this.kp = kp;
                this.ki = ki;
                this.kd = kd;
                error = prevError = 0f;
                errorSum = 0;
            }

            public float update(float current, float dt)
            {
                input = current;
                error = dest - input;
                errorSum = errorSum + error * dt;
                float dError = (error - prevError) / dt;
                output = kp * error + ki * errorSum + kd * dError;
                return output;
            }

        }

    }
}
