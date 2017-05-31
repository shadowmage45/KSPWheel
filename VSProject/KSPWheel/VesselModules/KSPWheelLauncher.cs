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

        public static KSPWheelLauncher instance;

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
                //controlsAppButton = ApplicationLauncher.Instance.AddModApplication(controlGuiEnable, controlGuiEnable, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT, tex);
            }
            instance = this;
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
                if (fi != null && fi.Vessel != null)
                {
                    fi.Vessel.GetComponent<KSPWheelVesselDebug>().drawGUI();
                }
            }
            if (controlGuiOpen)
            {
                FlightIntegrator fi = FlightIntegrator.ActiveVesselFI;
                if (fi != null && fi.Vessel != null)
                {
                    fi.Vessel.GetComponent<KSPWheelVesselControl>().drawGUI();
                }
            }
        }

        public void debugGuiEnable()
        {
            debugGuiOpen = false;
            FlightIntegrator fi = FlightIntegrator.ActiveVesselFI;
            if (fi == null || fi.Vessel == null)
            {
                return;
            }
            debugGuiOpen = true;
        }

        public void debugGuiDisable()
        {
            debugGuiOpen = false;
        }

        public void controlGuiEnable()
        {
            controlGuiOpen = false;
            FlightIntegrator fi = FlightIntegrator.ActiveVesselFI;
            if (fi == null || fi.Vessel == null)
            {
                return;
            }
            controlGuiOpen = true;
        }

        public void controlGuiDisable()
        {
            controlGuiOpen = false;
        }

    }
}
