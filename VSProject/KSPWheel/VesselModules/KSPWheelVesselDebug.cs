using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelVesselDebug : VesselModule
    {
        private int id = 0;
        private Rect windowRect = new Rect(100, 100, 640, 480);
        private Vector2 scrollPos;
        private bool guiInitialized = false;
        private bool debugRendering = false;

        private List<WheelDebugData> wheels = new List<WheelDebugData>();

        public void drawGUI()
        {
            if (!guiInitialized)
            {
                wheels.Clear();
                guiInitialized = true;
                List<KSPWheelBase> baseModules = new List<KSPWheelBase>();
                foreach (Part p in vessel.Parts)
                {
                    baseModules.AddUniqueRange(p.GetComponents<KSPWheelBase>());
                }
                foreach (KSPWheelBase bm in baseModules)
                {
                    foreach (KSPWheelBase.KSPWheelData bd in bm.wheelData)
                    {
                        wheels.Add(new WheelDebugData(bm, bd));
                    }
                }
            }
            if (vessel.isActiveVessel)
            {
                drawDebugGUI();
            }
        }

        private void drawDebugGUI()
        {
            windowRect = GUI.Window(id, windowRect, updateWindow, "Wheel Debug Display");
        }

        private void updateWindow(int id)
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("Toggle Debug Rendering"))
            {
                debugRendering = !debugRendering;
                enableDebugRendering(debugRendering);
            }
            if (GUILayout.Button("Dump Wheel Debug Data"))
            {
                dumpDebugData();
            }

            //per-wheel instance data view
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            float w1 = 30;
            float w2 = 50;
            float w3 = 200;

            //data column header row
            GUILayout.BeginHorizontal();
            GUILayout.Label("idx", GUILayout.Width(w1));//index
            GUILayout.Label("name", GUILayout.Width(w2));//user-set base module label/name
            GUILayout.Label("grp", GUILayout.Width(w1));//group
            GUILayout.Label("rad", GUILayout.Width(w2));//radius
            GUILayout.Label("mass", GUILayout.Width(w2));//mass
            GUILayout.Label("len", GUILayout.Width(w2));//length of travel
            GUILayout.Label("spr", GUILayout.Width(w2));//spring rate
            GUILayout.Label("dmp", GUILayout.Width(w2));//damper rate (n * m/s)
            GUILayout.Label("ang", GUILayout.Width(w2));//angular velocity
            GUILayout.Label("vel", GUILayout.Width(w2));//linear velocity
            GUILayout.Label("trq", GUILayout.Width(w2));//motor torque
            GUILayout.Label("brk", GUILayout.Width(w2));//brake torque
            GUILayout.Label("comp", GUILayout.Width(w2));//compression
            GUILayout.Label("comp%", GUILayout.Width(w2));//compression (percent of max)
            GUILayout.Label("timeB", GUILayout.Width(w2));//time boost factor
            GUILayout.Label("fY", GUILayout.Width(w2));//springForce
            GUILayout.Label("fZ", GUILayout.Width(w2));//longForce
            GUILayout.Label("fX", GUILayout.Width(w2));//latForce
            GUILayout.Label("sZ", GUILayout.Width(w2));//longSlip
            GUILayout.Label("sX", GUILayout.Width(w2));//latSlip
            GUILayout.Label("hit", GUILayout.Width(w3));//collider hit
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            int len = wheels.Count;
            KSPWheelCollider wheel;
            for (int i = 0; i < len; i++)
            {
                GUILayout.BeginHorizontal();
                wheel = wheels[i].wheelData.wheel;
                GUILayout.Label(i.ToString(), GUILayout.Width(w1));//raw wheel index
                GUILayout.Label(wheels[i].baseModule.label, GUILayout.Width(w2));//wheel name/label
                GUILayout.Label(wheels[i].baseModule.wheelGroup.ToString(), GUILayout.Width(w2));//wheel group
                GUILayout.Label(wheel.radius.ToString("0.##"), GUILayout.Width(w2));//radius
                GUILayout.Label(wheel.mass.ToString("0.##"), GUILayout.Width(w2));//mass
                GUILayout.Label(wheel.length.ToString("0.##"), GUILayout.Width(w2));//length of travel
                GUILayout.Label(wheel.spring.ToString("0.##"), GUILayout.Width(w2));//spring rate
                GUILayout.Label(wheel.damper.ToString("0.##"), GUILayout.Width(w2));//damper rate (n * m/s)
                GUILayout.Label(wheel.angularVelocity.ToString("0.##"), GUILayout.Width(w2));//angular velocity
                GUILayout.Label(wheel.linearVelocity.ToString("0.##"), GUILayout.Width(w2));//linear velocity
                GUILayout.Label(wheel.motorTorque.ToString("0.##"), GUILayout.Width(w2));//motor torque
                GUILayout.Label(wheel.brakeTorque.ToString("0.##"), GUILayout.Width(w2));//brake torque
                GUILayout.Label(wheel.compressionDistance.ToString("0.##"), GUILayout.Width(w2));//compression
                GUILayout.Label((wheel.compressionDistance / wheel.length).ToString("0.##"), GUILayout.Width(w2));//compression (percent of max)
                GUILayout.Label(wheels[i].wheelData.timeBoostFactor.ToString("0.##"), GUILayout.Width(w2));//time boost factor
                GUILayout.Label(wheel.springForce.ToString("0.##"), GUILayout.Width(w2));//springForce
                GUILayout.Label(wheel.longitudinalForce.ToString("0.##"), GUILayout.Width(w2));//longForce
                GUILayout.Label(wheel.lateralForce.ToString("0.##"), GUILayout.Width(w2));//latForce
                GUILayout.Label(wheel.longitudinalSlip.ToString("0.##"), GUILayout.Width(w2));//longSlip
                GUILayout.Label(wheel.lateralSlip.ToString("0.##"), GUILayout.Width(w2));//latSlip
                //using a button as for some retarted reason auto-word-wrap is enabled for labels...
                GUILayout.Button(wheel.contactColliderHit == null ? "none" : wheel.contactColliderHit.ToString() + ":" + wheel.contactColliderHit.gameObject.layer);//collider hit
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            //close button at the bottom of the window, below the scroll bar
            if (GUILayout.Button("Close"))
            {
                KSPWheelLauncher.instance.debugGuiDisable();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        public void Update()
        {
            if (debugRendering)
            {
                int len = wheels.Count;
                for (int i = 0; i < len; i++)
                {
                    wheels[i].updateDebugRenderers();
                }
            }
        }

        private void enableDebugRendering(bool enable)
        {
            int len = wheels.Count;
            if (enable)
            {
                for (int i = 0; i < len; i++)
                {
                    wheels[i].enableDebugRenderers();
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    wheels[i].disableDebugRenderers();
                }
            }
        }

        public void dumpDebugData()
        {
            MonoBehaviour.print("---------------------------------------------------------------------------------");
            MonoBehaviour.print("KSPWheel Vessel Wheel Debug Data.");
            int len = wheels.Count;
            for (int i = 0; i < len; i++)
            {
                MonoBehaviour.print("--------------------------------------");
                MonoBehaviour.print(wheels[i].baseModule.getDebugInfo());
                MonoBehaviour.print("--------------------------------------");
            }
            //TODO
            MonoBehaviour.print("---------------------------------------------------------------------------------");
        }
    }

    public class WheelDebugData
    {
        public KSPWheelBase baseModule;
        public KSPWheelBase.KSPWheelData wheelData;

        private GameObject debugLineRenderBase;//empty parent object, holder for the rest, manually positioned into wheel position and orientation
        private GameObject debugLineRendererFwd;
        private GameObject debugLineRendererSide;
        private GameObject debugLineRendererUp;
        private GameObject debugLineRendererWheel;

        private LineRenderer fwd;
        private LineRenderer side;
        private LineRenderer up;
        private LineRenderer wheel;

        public WheelDebugData(KSPWheelBase baseModule, KSPWheelBase.KSPWheelData wheelData)
        {
            this.baseModule = baseModule;
            this.wheelData = wheelData;
        }

        /// <summary>
        /// Set up the line renderers; fwd/side/up axis lines and wheel circle
        /// </summary>
        internal void setupDebugRenderers()
        {
            this.debugLineRenderBase = new GameObject("DebugLineRender");
            debugLineRenderBase.transform.position = wheelData.wheel.transform.position;
            debugLineRenderBase.transform.rotation = wheelData.wheel.transform.rotation;

            debugLineRendererFwd = new GameObject("DebugLineRenderFwd");
            debugLineRendererFwd.transform.parent = debugLineRenderBase.transform;
            debugLineRendererFwd.transform.localPosition = Vector3.zero;
            debugLineRendererFwd.transform.localRotation = Quaternion.identity;
            fwd = debugLineRendererFwd.AddComponent<LineRenderer>();

            debugLineRendererSide = new GameObject("DebugLineRenderSide");
            debugLineRendererSide.transform.parent = debugLineRenderBase.transform;
            debugLineRendererSide.transform.localPosition = Vector3.zero;
            debugLineRendererSide.transform.localRotation = Quaternion.identity;
            side = debugLineRendererSide.AddComponent<LineRenderer>();

            debugLineRendererUp = new GameObject("DebugLineRendererUp");
            debugLineRendererUp.transform.parent = debugLineRenderBase.transform;
            debugLineRendererUp.transform.localPosition = Vector3.zero;
            debugLineRendererUp.transform.localRotation = Quaternion.identity;
            up = debugLineRendererUp.AddComponent<LineRenderer>();

            debugLineRendererWheel = new GameObject("DebugLineRendererWheel");
            debugLineRendererWheel.transform.parent = debugLineRenderBase.transform;
            debugLineRendererWheel.transform.localPosition = Vector3.zero;
            debugLineRendererWheel.transform.localRotation = Quaternion.identity;
            wheel = debugLineRendererWheel.AddComponent<LineRenderer>();

            Material lineRendererMaterial = new Material(Shader.Find("Particles/Additive"));

            fwd.useWorldSpace = false;
            fwd.SetPositions(new Vector3[] { Vector3.zero, Vector3.forward*5f });
            fwd.material = lineRendererMaterial;
            fwd.SetColors(Color.blue, Color.blue);
            fwd.SetWidth(0.1f, 0.1f);

            side.useWorldSpace = false;
            side.SetPositions(new Vector3[] { Vector3.zero, Vector3.right*5f });
            side.material = lineRendererMaterial;
            side.SetColors(Color.red, Color.red);
            side.SetWidth(0.1f, 0.1f);

            up.useWorldSpace = false;
            up.SetPositions(new Vector3[] { Vector3.zero, Vector3.up*5f });
            up.material = lineRendererMaterial;
            up.SetColors(Color.green, Color.green);
            up.SetWidth(0.1f, 0.1f);

            int segments = 24;
            Vector3[] points = new Vector3[segments + 1];
            float radsPerSegment = (360f / (float)segments) * Mathf.Deg2Rad;
            float y, z;
            wheel.useWorldSpace = false;
            wheel.material = lineRendererMaterial;
            wheel.SetColors(Color.magenta, Color.magenta);
            wheel.SetWidth(0.1f, 0.1f);
            wheel.SetVertexCount(25);
            for (int i = 0; i <= segments; i++)//uses <= in order to close the loop
            {
                y = wheelData.wheel.radius * Mathf.Sin(i * radsPerSegment);
                z = wheelData.wheel.radius * Mathf.Cos(i * radsPerSegment);
                points[i] = new Vector3(0, y, z);
            }
            wheel.SetPositions(points);
        }

        /// <summary>
        /// Update the position and orientation of the line renders to mimic the current wheel position (with suspension/hit position data) and orientation (with steering)
        /// </summary>
        internal void updateDebugRenderers()
        {
            debugLineRenderBase.transform.position = wheelData.wheel.transform.position - wheelData.wheel.transform.up * (wheelData.wheel.length - wheelData.wheel.compressionDistance);
            debugLineRenderBase.transform.rotation = wheelData.wheel.transform.rotation;
            debugLineRenderBase.transform.Rotate(0, wheelData.wheel.steeringAngle, 0, Space.Self);
        }

        internal void enableDebugRenderers()
        {
            if (debugLineRenderBase == null)
            {
                setupDebugRenderers();
            }
        }

        internal void disableDebugRenderers()
        {
            if (debugLineRenderBase != null)
            {
                GameObject.Destroy(debugLineRenderBase);
            }
            debugLineRenderBase = null;
        }

    }
    
}
