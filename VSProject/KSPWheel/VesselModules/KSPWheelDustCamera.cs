using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelDustCamera : VesselModule
    {
        private static float frameWidth = 6f;
        private static float frameHeight = 6f;
        private static float frameTime = 3f;
        private static int cameraMask = 32784;

        public Color cameraColor = new Color(1f, 1f, 1f, 1f);

        //active color is a linear interp between these two based on 'frameTime' above
        private Color destColor = new Color(1f, 1f, 1f, 1f);
        private Color prevColor = new Color(1f, 1f, 1f, 1f);

        private GameObject cameraObject;
        private Camera dustCamera;
        private Texture2D cameraTexture;
        private RenderTexture cameraRenderTexture;
        private bool cameraActive = false;
        private float currentTime = 0f;//init to zero to trigger camera update on first frame
        private bool initialShot = false;//used to detect first frame for initial color lerping setups; sets prev color to current color and uses current color exclusively

        public void OnDestroy()
        {
            GameObject.Destroy(dustCamera);
            GameObject.Destroy(cameraObject);
            GameObject.Destroy(cameraTexture);
            GameObject.Destroy(cameraRenderTexture);
        }

        public void Update()
        {
            if (cameraObject!=null && cameraActive)
            {
                currentTime -= Time.deltaTime;
                if (currentTime <= 0)
                {
                    currentTime = frameTime;
                    updateCameraColor();
                }
                if (initialShot)
                {
                    float p = 1 - (currentTime / frameTime);
                    cameraColor.r = Mathf.Lerp(prevColor.r, destColor.r, p);
                    cameraColor.g = Mathf.Lerp(prevColor.g, destColor.g, p);
                    cameraColor.b = Mathf.Lerp(prevColor.b, destColor.b, p);
                    cameraColor.a = 0.014f;
                }
                else
                {
                    initialShot = true;
                    cameraColor = prevColor = destColor;
                }
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            setupCamera();
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

        private void setupCamera()
        {
            cameraObject = new GameObject("KSPWheelDustCamera");
            cameraObject.transform.parent = vessel.transform;
            cameraObject.transform.position = vessel.transform.position;//pos will be updated when taking cam shots
            cameraObject.transform.rotation = vessel.transform.rotation;//rot will be updated when taking cam shots
            dustCamera = cameraObject.AddComponent<Camera>();
            dustCamera.targetTexture = cameraRenderTexture;
            dustCamera.cullingMask = cameraMask;
            dustCamera.enabled = false;
            cameraRenderTexture = new RenderTexture(Convert.ToInt32(frameWidth), Convert.ToInt32(frameHeight), 24);
            cameraTexture = new Texture2D(Convert.ToInt32(frameWidth), Convert.ToInt32(frameHeight), TextureFormat.RGB24, false);
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

        private void updateCameraColor()
        {
            prevColor.r = destColor.r;
            prevColor.g = destColor.g;
            prevColor.b = destColor.b;
            cameraObject.transform.position = vessel.transform.position;
            cameraObject.transform.LookAt(vessel.mainBody.transform.position);
            cameraObject.transform.Translate(0, 0, -10f);//translate negative z, as it is pointed at the ground this will leave it 10m above the ground at the vessels position, with the ground fully in the camera view box

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
                r += cols[0].r;
                g += cols[0].g;
                b += cols[0].b;
            }
            destColor.r = r / len;
            destColor.g = g / len;
            destColor.b = b / len;
            destColor.a = 0.014f;
        }

    }
}
