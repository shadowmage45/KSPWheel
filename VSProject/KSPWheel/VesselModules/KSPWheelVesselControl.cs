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
        private List<KSPWheelBase> baseModules = new List<KSPWheelBase>();

        private static float w1 = 30;
        private static float w2 = 50;
        private static float w3 = 250;

        public void toggleGUI()
        {
            guiOpen = !guiOpen;
            if (guiOpen && !guiInitialized)
            {
                guiInitialized = true;
                baseModules.Clear();
                foreach (Part p in vessel.Parts)
                {
                    baseModules.AddUniqueRange(p.GetComponentsInChildren<KSPWheelBase>());
                }
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
            
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Part", GUILayout.Width(w2));
            GUILayout.Label("Module", GUILayout.Width(w2));
            GUILayout.Label("Controls", GUILayout.Width(w3));
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            int len = baseModules.Count;
            int len2;
            KSPWheelSubmodule sub;
            Type type;
            float val;
            for (int i = 0; i < len; i++)
            {
                len2 = baseModules[i].subModules.Count;

                //per-base-module controls
                //wheel name, spring, damper, friction adjustment
                GUILayout.BeginHorizontal();
                baseModules[i].label = GUILayout.TextField(baseModules[i].label, GUILayout.Width(w2));//user-definable per-base-module label; merely to tell the parts/base-modules apart...
                if (GUILayout.Button("Highlight", GUILayout.Width(w2)))
                {
                    //TODO toggle highlighting of part
                }
                //TODO add group adjustment buttons
                GUILayout.Label("Group: " + baseModules[i].wheelGroup);
                GUILayout.Label("Spring", GUILayout.Width(w1));
                val = GUILayout.HorizontalSlider(baseModules[i].springRating, 0, 1, GUILayout.Width(w2));
                if (val != baseModules[i].springRating)
                {
                    baseModules[i].springRating = val;
                    baseModules[i].onLoadUpdated(null, null);
                }
                GUILayout.Label("Damp Ratio", GUILayout.Width(w2));
                val = GUILayout.HorizontalSlider(baseModules[i].dampRatio, 0.35f, 1.0f, GUILayout.Width(w2));
                if (val != baseModules[i].dampRatio)
                {
                    baseModules[i].dampRatio = val;
                    baseModules[i].onLoadUpdated(null, null);
                }
                GUILayout.EndHorizontal();

                //for each wheel control module
                //check module type, call draw routine for that module
                for (int k = 0; k < len2; k++)
                {
                    GUILayout.BeginHorizontal();
                    sub = baseModules[i].subModules[k];
                    type = sub.GetType();
                    GUILayout.Label(type.ToString(), GUILayout.Width(w2));
                    if (type == typeof(KSPWheelSteering))
                    {
                        drawSteeringControls((KSPWheelSteering)sub);
                    }
                    else if (type == typeof(KSPWheelTracks))
                    {
                        drawTrackControls((KSPWheelTracks)sub);
                    }
                    else if (type == typeof(KSPWheelMotor))
                    {
                        drawMotorControls((KSPWheelMotor)sub);
                    }
                    else if (type == typeof(KSPWheelBrakes))
                    {
                        drawBrakeControls((KSPWheelBrakes)sub);
                    }
                    else if (type == typeof(KSPWheelRepulsor))
                    {
                        drawRepulsorControls((KSPWheelRepulsor)sub);
                    }
                    GUILayout.EndHorizontal();
                }
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

        private void drawSteeringControls(KSPWheelSteering steering)
        {
            if (GUILayout.Button("Invert: " + steering.invertSteering))
            {
                steering.invertSteering = !steering.invertSteering;
                steering.onSteeringInverted(null, null);
            }
            if (GUILayout.Button("Lock: " + steering.steeringLocked))
            {
                steering.steeringLocked = !steering.steeringLocked;
                steering.onSteeringLocked(null, null);
            }
            float val = 0f;
            GUILayout.Label("Low Speed Limit", GUILayout.Width(w2));
            val = GUILayout.HorizontalSlider(steering.steeringLimit, 0, 1, GUILayout.Width(w2));
            if (val != steering.steeringLimit)
            {
                steering.steeringLimit = val;
                steering.onSteeringLimitUpdated(null, null);
            }
            GUILayout.Label("High Speed Limit", GUILayout.Width(w2));
            val = GUILayout.HorizontalSlider(steering.steeringLimitHigh, 0, 1, GUILayout.Width(w2));
            if (val != steering.steeringLimitHigh)
            {
                steering.steeringLimitHigh = val;
                steering.onSteeringLimitUpdated(null, null);
            }
            GUILayout.Label("Response Speed", GUILayout.Width(w2));
            val = GUILayout.HorizontalSlider(steering.steeringResponse, 0, 1, GUILayout.Width(w2));
            if (val != steering.steeringResponse)
            {
                steering.steeringResponse = val;
                steering.onSteeringLimitUpdated(null, null);
            }
            GUILayout.Label("Bias", GUILayout.Width(w2));
            val = GUILayout.HorizontalSlider(steering.steeringBias, 0, 1, GUILayout.Width(w2));
            if (val != steering.steeringBias)
            {
                steering.steeringBias = val;
                steering.onSteeringBiasUpdated(null, null);
            }
        }

        private void drawTrackControls(KSPWheelTracks tracks)
        {
            drawMotorControls(tracks);
        }

        private void drawMotorControls(KSPWheelMotor motor)
        {
            float val = 0f;
            if (GUILayout.Button("Invert Motor" + motor.invertMotor, GUILayout.Width(w2)))
            {
                motor.invertMotor = !motor.invertMotor;
                motor.onMotorInvert(null, null);
            }
            if (GUILayout.Button("Lock Motor" + motor.motorLocked, GUILayout.Width(w2)))
            {
                motor.motorLocked = !motor.motorLocked;
                motor.onMotorLock(null, null);
            }
            GUILayout.Label("Motor Limit", GUILayout.Width(w2));
            val = GUILayout.HorizontalSlider(motor.motorOutput, 0, 100, GUILayout.Width(w2));
            if (val != motor.motorOutput)
            {
                motor.motorOutput = val;
                motor.onMotorLimitUpdated(null, null);
            }
            if (GUILayout.Button("<", GUILayout.Width(w1)))
            {
                motor.gearRatio = Mathf.Clamp(motor.gearRatio - 1f, 1f, 20f);
                motor.onGearUpdated(null, null);
            }
            GUILayout.Label("Gear: " + motor.gearRatio, GUILayout.Width(w2));
            if (GUILayout.Button(">", GUILayout.Width(w1)))
            {
                motor.gearRatio = Mathf.Clamp(motor.gearRatio + 1f, 1f, 20f);
                motor.onGearUpdated(null, null);
            }
            if (motor.tankSteering)
            {
                if (GUILayout.Button("Lock Steering: " + motor.steeringLocked, GUILayout.Width(w2)))
                {
                    motor.steeringLocked = !motor.steeringLocked;
                    motor.onSteeringLock(null, null);
                }
                if (GUILayout.Button("Invert Steering: " + motor.invertSteering, GUILayout.Width(w2)))
                {
                    motor.invertSteering = !motor.invertSteering;
                    motor.onSteeringInvert(null, null);
                }
                if (GUILayout.Button("Half Track Mode: " + motor.halfTrackSteering, GUILayout.Width(w2)))
                {
                    motor.halfTrackSteering = !motor.halfTrackSteering;
                    motor.onHalftrackToggle(null, null);
                }
            }
        }

        private void drawBrakeControls(KSPWheelBrakes brakes)
        {
            //TODO
        }

        private void drawRepulsorControls(KSPWheelRepulsor repulsor)
        {
            //TODO
        }
    }
}
