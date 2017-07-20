using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelDebug : KSPWheelSubmodule
    {

        [KSPField(guiName = "Debug Rendering", guiActive = true, guiActiveEditor = false),
         UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", suppressEditorShipModified = true)]
        public bool showDebugRendering = false;

        private int id = 0;
        private Rect windowRect = new Rect(100, 100, 640, 480);
        private Vector2 scrollPos;
        private bool guiOpen = false;

        private float longFriction = 1f;
        private float latFriction = 1f;
        private KSPWheelSweepType sweepType = KSPWheelSweepType.RAY;

        //TODO -- add debug game objects to wheels
        private GameObject[] debugHitObjects;

        [KSPEvent(guiName = "Open Debug GUI", guiActive = true, guiActiveEditor = false)]
        public void showGUI()
        {
            guiOpen = true;
        }

        private void onDebugRenderingUpdated(BaseField field, System.Object obj)
        {
            updateDebugRendering();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            id = this.GetInstanceID();
            Fields[nameof(showDebugRendering)].uiControlFlight.onFieldChanged = onDebugRenderingUpdated;
        }

        internal override void postWheelPhysicsUpdate()
        {
            base.postWheelPhysicsUpdate();
            if (debugHitObjects != null)
            {
                int len = debugHitObjects.Length;
                KSPWheelCollider wheel;
                for (int i = 0; i < len; i++)
                {
                    wheel = controller.wheelData[i].wheel;
                    debugHitObjects[i].transform.position = wheel.transform.position - (wheel.transform.up * wheel.length) + (wheel.transform.up * wheel.compressionDistance) - (wheel.transform.up * wheel.radius);
                }
            }
        }

        private void updateDebugRendering()
        {
            if (debugHitObjects == null)
            {
                int len = controller.wheelData.Length;
                debugHitObjects = new GameObject[len];
                GameObject debugHitObject;
                for (int i = 0; i < len; i++)
                {
                    debugHitObject = debugHitObjects[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Collider c = debugHitObject.GetComponent<Collider>();
                    GameObject.Destroy(c);
                    debugHitObject.transform.NestToParent(part.transform.FindRecursive("model"));
                    debugHitObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                }
            }
            else
            {
                int len = debugHitObjects.Length;
                for (int i = 0; i < len; i++)
                {
                    if (debugHitObjects[i] != null) { GameObject.Destroy(debugHitObjects[i]); }
                }
                debugHitObjects = null;
            }
        }

        public void OnGUI()
        {
            if (guiOpen)
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

            GUILayout.BeginHorizontal();
            GUILayout.Label("Long Friction", GUILayout.Width(100));
            float val = longFriction;
            longFriction = GUILayout.HorizontalSlider(longFriction, 0.0f, 4.0f, GUILayout.Width(200));
            if (val != longFriction)
            {
                int wlen = controller.wheelData.Length;
                for (int i = 0; i < wlen; i++)
                {
                    controller.wheelData[i].wheel.forwardFrictionCoefficient = longFriction;
                }
            }
            GUILayout.Label(longFriction.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Lat Friction", GUILayout.Width(100));
            val = latFriction;
            latFriction = GUILayout.HorizontalSlider(latFriction, 0.0f, 4.0f, GUILayout.Width(200));
            if (val != latFriction)
            {
                int wlen = controller.wheelData.Length;
                for (int i = 0; i < wlen; i++)
                {
                    controller.wheelData[i].wheel.sideFrictionCoefficient = latFriction;
                }
            }
            GUILayout.Label(latFriction.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Sweep Type", GUILayout.Width(100));
            if (GUILayout.Button("Prev"))
            {
                if (sweepType == KSPWheelSweepType.CAPSULE) { sweepType = KSPWheelSweepType.SPHERE; }
                else if (sweepType == KSPWheelSweepType.RAY) { sweepType = KSPWheelSweepType.CAPSULE; }
                else { sweepType = KSPWheelSweepType.RAY; }
                int wlen = controller.wheelData.Length;
                for (int i = 0; i < wlen; i++)
                {
                    controller.wheelData[i].wheel.sweepType = sweepType;
                }
            }
            if (GUILayout.Button("Next"))
            {
                if (sweepType == KSPWheelSweepType.CAPSULE) { sweepType = KSPWheelSweepType.RAY; }
                else if (sweepType == KSPWheelSweepType.RAY) { sweepType = KSPWheelSweepType.SPHERE; }
                else { sweepType = KSPWheelSweepType.CAPSULE; }
                int wlen = controller.wheelData.Length;
                for (int i = 0; i < wlen; i++)
                {
                    controller.wheelData[i].wheel.sweepType = sweepType;
                }
            }
            GUILayout.Label(sweepType.ToString());
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Toggle Debug Rendering"))
            {
                updateDebugRendering();
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
            GUILayout.Label("idx", GUILayout.Width(w1));
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
            GUILayout.Label("fY", GUILayout.Width(w2));//springForce
            GUILayout.Label("fZ", GUILayout.Width(w2));//longForce
            GUILayout.Label("fX", GUILayout.Width(w2));//latForce
            GUILayout.Label("sZ", GUILayout.Width(w2));//longSlip
            GUILayout.Label("sX", GUILayout.Width(w2));//latSlip
            GUILayout.Label("hit", GUILayout.Width(w3));//collider hit
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            int len = controller.wheelData.Length;
            KSPWheelCollider wheel;
            for (int i = 0; i < len; i++)
            {
                GUILayout.BeginHorizontal();
                wheel = controller.wheelData[i].wheel;

                GUILayout.Label(i.ToString(), GUILayout.Width(w1));
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
                GUILayout.Label((wheel.compressionDistance/wheel.length).ToString("0.##"), GUILayout.Width(w2));//compression (percent of max)
                GUILayout.Label(wheel.springForce.ToString("0.##"), GUILayout.Width(w2));//springForce
                GUILayout.Label(wheel.longitudinalForce.ToString("0.##"), GUILayout.Width(w2));//longForce
                GUILayout.Label(wheel.lateralForce.ToString("0.##"), GUILayout.Width(w2));//latForce
                GUILayout.Label(wheel.longitudinalSlip.ToString("0.##"), GUILayout.Width(w2));//longSlip
                GUILayout.Label(wheel.lateralSlip.ToString("0.##"), GUILayout.Width(w2));//latSlip
                //using a button as for some retarted reason auto-word-wrap is enabled for labels...
                GUILayout.Button(wheel.contactColliderHit==null? "none" : wheel.contactColliderHit.ToString() + ":" + wheel.contactColliderHit.gameObject.layer);//collider hit

                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            //close button at the bottom of the window, below the scroll bar
            if (GUILayout.Button("Close"))
            {
                guiOpen = false;
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        public void dumpDebugData()
        {
            //TODO
        }

    }
}
