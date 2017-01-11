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
            updateDebugRendering();
            Fields[nameof(showDebugRendering)].uiControlFlight.onFieldChanged = onDebugRenderingUpdated;
        }

        private void updateDebugRendering()
        {

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

            //upper main data display row
            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();


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
                GUILayout.Label(wheel.contactColliderHit==null? "none" : wheel.contactColliderHit.ToString());//collider hit

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

    }
}
