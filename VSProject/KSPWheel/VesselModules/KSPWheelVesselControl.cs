using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelVesselControl : VesselModule
    {

        private int id = 0;
        private Rect windowRect = new Rect(100, 100, 640, 480);
        private Vector2 scrollPos;
        private bool guiOpen = false;
        private bool guiInitialized = false;        

        public void toggleGUI()
        {
            guiOpen = !guiOpen;
            if (guiOpen && !guiInitialized)
            {
                guiInitialized = true;
                //TODO setup per-vessel data
            }
        }

        public void OnGUI()
        {
            if (guiOpen)
            {
                if (vessel.isActiveVessel)
                {
                    drawControlGUI();
                }
                else
                {
                    guiOpen = false;
                }
            }
        }

        private void drawControlGUI()
        {
            windowRect = GUI.Window(id, windowRect, updateWindow, "Wheel Controls");
        }

        private void updateWindow(int id)
        {
            GUILayout.BeginVertical();

            float w1 = 30;
            float w2 = 50;
            float w3 = 200;
            
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            
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
