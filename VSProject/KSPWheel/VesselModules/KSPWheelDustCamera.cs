using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelDustCamera : VesselModule
    {

        private static float frameTime = 1f;
        private float currentTime = 0f;//init to zero to trigger camera update on first frame
        private bool cameraActive = false;
        private bool initialShot = false;//used to detect first frame for initial color lerping setups; sets prev color to current color and uses current color exclusively
        public Color cameraColor = new Color(1f, 1f, 1f, 1f);
        private Color destColor = new Color(1f, 1f, 1f, 1f);
        private Color prevColor = new Color(1f, 1f, 1f, 1f);

        public void Update()
        {
            if (cameraActive && KSPWheelDustCameraRenderer.Instance!=null && vessel!=null && vessel.loaded)
            {
                currentTime -= Time.deltaTime;
                if (currentTime <= 0)
                {
                    prevColor = destColor;
                    currentTime = frameTime;
                    destColor = KSPWheelDustCameraRenderer.Instance.getCameraColor(vessel);
                }
                if (!initialShot)//this is the first frame update for this vessel, use the color output directly
                {
                    initialShot = true;
                    cameraColor = prevColor = destColor;
                }
                else//lerp between previous and current destination color
                {
                    float p = 1 - (currentTime / frameTime);
                    cameraColor.r = Mathf.Lerp(prevColor.r, destColor.r, p);
                    cameraColor.g = Mathf.Lerp(prevColor.g, destColor.g, p);
                    cameraColor.b = Mathf.Lerp(prevColor.b, destColor.b, p);
                    cameraColor.a = 0.014f;
                }
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            updateActiveStatus();
        }

        public override void OnGoOffRails()
        {
            base.OnGoOffRails();
            updateActiveStatus();
        }

        public override void OnLoadVessel()
        {
            base.OnLoadVessel();
            updateActiveStatus();
        }

        /// <summary>
        /// Exmines current game settings and vessel parts to see if camera should be active for this vessel.
        /// </summary>
        private void updateActiveStatus()
        {
            cameraActive = false;
            if (!HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().wheelDustCamera)
            {
                return;
            }
            int len = vessel.Parts.Count;
            KSPWheelDustEffects fx;
            for (int i = 0; i < len; i++)
            {
                fx = vessel.Parts[i].GetComponent<KSPWheelDustEffects>();
                if (fx != null)
                {
                    cameraActive = true;
                    break;
                }
            }
        }

    }

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class KSPWheelDustCameraRenderer : MonoBehaviour
    {
        private static float frameWidth = 6f;
        private static float frameHeight = 6f;
        private static int cameraMask = 32784;

        private static KSPWheelDustCameraRenderer instance;

        private GameObject cameraObject;
        private Camera dustCamera;
        private Texture2D cameraTexture;
        private RenderTexture cameraRenderTexture;

        private bool cameraInitialized = false;

        public static KSPWheelDustCameraRenderer Instance
        {
            get { return instance; }
        }

        public void Awake ()
        {
            DontDestroyOnLoad(this);
            instance = this;
        }

        public void Start()
        {
            DontDestroyOnLoad(this);
            instance = this;
        }

        public void OnDestroy()
        {
            GameObject.Destroy(dustCamera);
            GameObject.Destroy(cameraObject);
            GameObject.Destroy(cameraTexture);
            GameObject.Destroy(cameraRenderTexture);
            instance = null;
        }

        private void initializeCamera()
        {
            cameraInitialized = true;
            cameraObject = new GameObject("KSPWheelDustCamera");
            cameraObject.transform.parent = this.gameObject.transform;
            dustCamera = cameraObject.AddComponent<Camera>();
            dustCamera.targetTexture = cameraRenderTexture;
            dustCamera.cullingMask = cameraMask;
            dustCamera.enabled = false;
            cameraRenderTexture = new RenderTexture(Convert.ToInt32(frameWidth), Convert.ToInt32(frameHeight), 24);
            cameraTexture = new Texture2D(Convert.ToInt32(frameWidth), Convert.ToInt32(frameHeight), TextureFormat.RGB24, false);
        }

        private void setupCameraForVessel(Vessel vessel)
        {
            if (!cameraInitialized)
            {
                initializeCamera();
            }
            cameraObject.transform.position = vessel.transform.position;
            cameraObject.transform.LookAt(vessel.mainBody.transform.position);
            cameraObject.transform.Translate(0, 0, -10f);//translate negative z, as it is pointed at the ground this will leave it 10m above the ground at the vessels position, with the ground fully in the camera view box
        }

        public Color getCameraColor(Vessel vessel)
        {
            setupCameraForVessel(vessel);

            dustCamera.targetTexture = cameraRenderTexture;
            dustCamera.enabled = true;
            dustCamera.Render();

            RenderTexture.active = cameraRenderTexture;
            cameraTexture.ReadPixels(new Rect(0, 0, frameWidth, frameHeight), 0, 0);

            dustCamera.targetTexture = null;
            dustCamera.enabled = false;
            RenderTexture.active = null;

            Color[] cols = cameraTexture.GetPixels();
            float r = 0, g = 0, b = 0;
            int len = cols.Length;
            for (int i = 0; i < len; i++)
            {
                r += cols[i].r;
                g += cols[i].g;
                b += cols[i].b;
            }
            Color outColor = new Color();
            outColor.r = r / len;
            outColor.g = g / len;
            outColor.b = b / len;
            outColor.a = 0.014f;
            return outColor;
        }

    }
}
