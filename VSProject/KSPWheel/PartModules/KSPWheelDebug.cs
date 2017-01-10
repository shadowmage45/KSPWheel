using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelDebug : KSPWheelSubmodule
    {

        private int id = 0;
        private Rect windowRect = new Rect(100, 100, 640, 480);
        private Vector2 scrollPos;
        private bool guiOpen = false;

        [KSPEvent(guiName = "Open Debug GUI", guiActive = true, guiActiveEditor = true)]
        public void showGUI()
        {
            guiOpen = true;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            id = this.GetInstanceID();
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

            //data column header row
            GUILayout.BeginHorizontal();
            GUILayout.Label("index", GUILayout.Width(30));
            GUILayout.Label("rad", GUILayout.Width(30));//radius
            GUILayout.Label("mass", GUILayout.Width(30));//mass
            GUILayout.Label("len", GUILayout.Width(30));//length of travel
            GUILayout.Label("spr", GUILayout.Width(30));//spring rate
            GUILayout.Label("dmp", GUILayout.Width(30));//damper rate (n * m/s)
            GUILayout.Label("ang", GUILayout.Width(30));//angular velocity
            GUILayout.Label("vel", GUILayout.Width(30));//linear velocity
            GUILayout.Label("trq", GUILayout.Width(30));//motor torque
            GUILayout.Label("brk", GUILayout.Width(30));//brake torque
            GUILayout.Label("comp", GUILayout.Width(30));//compression
            GUILayout.Label("comp%", GUILayout.Width(30));//compression (percent of max)
            GUILayout.Label("fY", GUILayout.Width(30));//springForce
            GUILayout.Label("fZ", GUILayout.Width(30));//longForce
            GUILayout.Label("fX", GUILayout.Width(30));//latForce
            GUILayout.Label("sZ", GUILayout.Width(30));//longSlip
            GUILayout.Label("sX", GUILayout.Width(30));//latSlip
            GUILayout.Label("hit", GUILayout.Width(30));//collider hit
            GUILayout.EndHorizontal();

            //per-wheel instance data view
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            int len = controller.wheelData.Length;
            KSPWheelCollider wheel;
            for (int i = 0; i < len; i++)
            {
                GUILayout.BeginHorizontal();
                wheel = controller.wheelData[i].wheel;

                GUILayout.Label(i.ToString(), GUILayout.Width(30));
                GUILayout.Label(wheel.radius.ToString(), GUILayout.Width(30));//radius
                GUILayout.Label(wheel.mass.ToString(), GUILayout.Width(30));//mass
                GUILayout.Label(wheel.length.ToString(), GUILayout.Width(30));//length of travel
                GUILayout.Label(wheel.spring.ToString(), GUILayout.Width(30));//spring rate
                GUILayout.Label(wheel.damper.ToString(), GUILayout.Width(30));//damper rate (n * m/s)
                GUILayout.Label(wheel.angularVelocity.ToString(), GUILayout.Width(30));//angular velocity
                GUILayout.Label(wheel.linearVelocity.ToString(), GUILayout.Width(30));//linear velocity
                GUILayout.Label(wheel.motorTorque.ToString(), GUILayout.Width(30));//motor torque
                GUILayout.Label(wheel.brakeTorque.ToString(), GUILayout.Width(30));//brake torque
                GUILayout.Label(wheel.compressionDistance.ToString(), GUILayout.Width(30));//compression
                GUILayout.Label((wheel.compressionDistance/wheel.length).ToString(), GUILayout.Width(30));//compression (percent of max)
                GUILayout.Label(wheel.springForce.ToString(), GUILayout.Width(30));//springForce
                GUILayout.Label(wheel.longitudinalForce.ToString(), GUILayout.Width(30));//longForce
                GUILayout.Label(wheel.lateralForce.ToString(), GUILayout.Width(30));//latForce
                GUILayout.Label(wheel.longitudinalSlip.ToString(), GUILayout.Width(30));//longSlip
                GUILayout.Label(wheel.lateralSlip.ToString(), GUILayout.Width(30));//latSlip
                GUILayout.Label(wheel.contactColliderHit==null? "none" : wheel.contactColliderHit.ToString(), GUILayout.Width(30));//collider hit

                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close"))
            {
                guiOpen = false;
            }
            GUILayout.EndVertical();
        }

    }
}
