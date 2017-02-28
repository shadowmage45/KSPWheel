using UnityEngine;
using KSP.UI.Screens;

namespace KSPWheel
{
    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class KSPWheelLauncher : MonoBehaviour
    {
        private ApplicationLauncherButton debugAppButton;
        private ApplicationLauncherButton controlsAppButton;

        private bool debugGuiOpen = false;
        private bool controlGuiOpen = false;

        public void Awake()
        {
            Texture2D tex;
            if (HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().debugMode && HighLogic.LoadedSceneIsFlight && debugAppButton==null)
            {
                tex = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/RDIcon_fuelSystems-highPerformance", false);
                debugAppButton = ApplicationLauncher.Instance.AddModApplication(debugGuiEnable, debugGuiDisable, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT, tex);
            }
            if ((HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) && controlsAppButton == null)
            {
                //tex = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/R&D_node_icon_advancedmotors", false);
                //controlsAppButton = ApplicationLauncher.Instance.AddModApplication(controlGuiEnable, controlGuiEnable, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, tex);
            }
        }

        public void OnDestroy()
        {
            if (debugAppButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(debugAppButton);
            }
            if (controlsAppButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(debugAppButton);
            }
            debugAppButton = null;
            controlsAppButton = null;
        }

        public void OnGUI()
        {
            if (debugGuiOpen)
            {
                FlightIntegrator fi = FlightIntegrator.ActiveVesselFI;
                if (fi == null || fi.Vessel == null)
                {
                    fi.Vessel.GetComponent<KSPWheelVesselDebug>().drawGUI();
                }
            }
            if (controlGuiOpen)
            {
                FlightIntegrator fi = FlightIntegrator.ActiveVesselFI;
                if (fi == null || fi.Vessel == null)
                {
                    fi.Vessel.GetComponent<KSPWheelVesselControl>().drawGUI();
                }
            }
        }

        private void debugGuiEnable()
        {
            FlightIntegrator fi = FlightIntegrator.ActiveVesselFI;
            if (fi == null || fi.Vessel == null)
            {
                return;
            }
            toggleDebugGui(fi.Vessel, true);
        }

        private void debugGuiDisable()
        {
            FlightIntegrator fi = FlightIntegrator.ActiveVesselFI;
            if (fi == null || fi.Vessel == null)
            {
                return;
            }
            toggleDebugGui(fi.Vessel, false);
        }

        private void toggleDebugGui(Vessel vessel, bool active)
        {
            KSPWheelVesselDebug debug = vessel.GetComponent<KSPWheelVesselDebug>();
            if (debug != null) { debug.toggleGUI(active); }
        }

        private void controlGuiEnable()
        {
            FlightIntegrator fi = FlightIntegrator.ActiveVesselFI;
            if (fi == null || fi.Vessel == null)
            {
                return;
            }
            Vessel vessel = fi.Vessel;
            KSPWheelVesselControl control = vessel.GetComponent<KSPWheelVesselControl>();//TODO -- does this work?
            if (control != null) { control.toggleGUI(); }
        }

    }
}
