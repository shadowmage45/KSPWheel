using System.IO;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelFrictionCurve
    {
        private AnimationCurve curveData;
        private float extSlip;
        private float extVal;
        private float asSlip;
        private float asVal;
        private float tailVal;

        private Keyframe[] keyframes;

        /// <summary>
        /// No-param constructor that initializes with default curve values
        /// </summary>
        public KSPWheelFrictionCurve() : this(0.06f, 1.2f, 0.08f, 1.0f, 0.6f)
        {
            // NOOP
            // chained to the parameter constructor below with default values
        }

        /// <summary>
        /// Parameter constructor to initialize to a given curve setup
        /// </summary>
        /// <param name="extSlip"></param>
        /// <param name="extVal"></param>
        /// <param name="asSlip"></param>
        /// <param name="asVal"></param>
        /// <param name="tailVal"></param>
        public KSPWheelFrictionCurve(float extSlip, float extVal, float asSlip, float asVal, float tailVal)
        {
            keyframes = new Keyframe[4];
            curveData = new AnimationCurve();
            this.extSlip = extSlip;
            this.extVal = extVal;
            this.asSlip = asSlip;
            this.asVal = asVal;
            this.tailVal = tailVal;
            setupCurve();
            //exportCurve("temp" + extSlip + ".png", 1024, 1024);
        }

        public float extremumSlip
        {
            get { return extSlip; }
            set { extSlip = value; setupCurve(); }
        }

        public float extremumValue
        {
            get { return extVal; }
            set { extVal = value; setupCurve(); }
        }

        public float asymptoteSlip
        {
            get { return asSlip; }
            set { asSlip = value; setupCurve(); }
        }

        public float asymptoteValue
        {
            get { return asVal; }
            set { asVal = value;  setupCurve(); }
        }

        public float tailValue
        {
            get { return tailVal; }
            set { tailVal = value;  setupCurve(); }
        }

        public float max
        {
            get { return Mathf.Max(asVal, extVal); }
        }

        /// <summary>
        /// Input = slip percent or cosin<para/>
        /// Value must be between 0...1 (inclusive)
        /// </summary>
        /// <param name="slipRatio"></param>
        /// <returns>Normalized force multiplier (normally 0...1, but can be more!)</returns>
        public float evaluate(float slipRatio)
        {
            return curveData.Evaluate(clampRatio(slipRatio));
        }

        /// <summary>
        /// Utility method to export this curve as a .png image file with the input width/height, stored to the input file name/location<para/>
        /// X range is clamped to 0-1 (slip ratio)
        /// Y range is clamped to 0-2 (output coefficient)
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void exportCurve(string fileName, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            int len = width;
            float input, output;

            float max = 2f;
            float outYF;
            int outY;
            //fill with black
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    texture.SetPixel(x, y, Color.black);
                }
            }
            //plot the friction curve, extremum mapped to min = 0, max = 2
            for (int i = 0; i < len; i++)
            {
                input = (i + 1) / (float)width;//0-1 percentage value for time input
                output = curveData.Evaluate(input);
                outYF = output / max;
                outY = (int)(outYF * height);
                texture.SetPixel(i, outY, Color.green);
            }
            byte[] fileBytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(fileName, fileBytes);
        }

        /// <summary>
        /// Setup the cached keyframes with the current wheel slip parameters
        /// </summary>
        private void setupCurve()
        {
            //entry frame
            keyframes[0].time = 0;
            keyframes[0].value = 0;
            //extremum frame
            keyframes[1].time = extSlip;
            keyframes[1].value = extVal;
            //asymptote frame
            keyframes[2].time = asSlip;
            keyframes[2].value = asVal;
            //tail frame
            keyframes[3].time = 1;
            keyframes[3].value = tailVal;

            //clear current data from the curve
            int len = curveData.length;
            for (int i = len - 1; i >= 0; i--) { curveData.RemoveKey(i); }

            //re-insert keyframes
            curveData.AddKey(keyframes[0]);
            curveData.AddKey(keyframes[1]);
            curveData.AddKey(keyframes[2]);
            curveData.AddKey(keyframes[3]);
        }

        /// <summary>
        /// Clamps an input slip ratio to the valid range of 0-1
        /// </summary>
        /// <param name="slipRatio"></param>
        /// <returns></returns>
        private float clampRatio(float slipRatio)
        {
            slipRatio = Mathf.Abs(slipRatio);
            slipRatio = Mathf.Min(1, slipRatio);
            slipRatio = Mathf.Max(0, slipRatio);
            return slipRatio;
        }
    }
}

