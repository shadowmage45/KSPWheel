using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;


namespace KSPWheel
{
    public class KSPWheelFrictionCurve
    {
        private AnimationCurve curveData;
        private float minT;
        private float maxT;
        private float minV;
        private float maxV;

        public KSPWheelFrictionCurve()
        {
            curveData = new AnimationCurve();
        }

        public void addPoint(float t, float v)
        {
            addPoint(t, v, 0, 0);
        }

        public void addPoint(float t, float v, float i, float o)
        {
            Keyframe key = new Keyframe(t, v, i, o);
            curveData.AddKey(key);
            if (t > maxT) { maxT = t; }
            if (t < minT) { minT = t; }
            if (v > maxV) { maxV = v; }
            if (v < minV) { minV = v; }
        }

        /// <summary>
        /// Input = slip percent or cosin<para/>
        /// Value must be between 0...1 (inclusive)
        /// </summary>
        /// <param name="slipPercent"></param>
        /// <returns>Normalized force multiplier (normally 0...1, but can be more!)</returns>
        public float evaluate(float slipPercent)
        {
            slipPercent = Mathf.Abs(slipPercent);
            if (slipPercent > 1) { slipPercent = 1; }
            return curveData.Evaluate(slipPercent);
        }

        public void exportCurve(string fileName, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            int len = width;
            float input, output;
            
            float max = 2f;
            float outYF;
            int outY;
            //TODO fill texture with black...
            for (int i = 0; i < len; i++)
            {
                input = (i + 1) / (float)width;//0-1 percentage value for time input
                output = curveData.Evaluate(input);
                outYF = output / max;
                outY = (int)(outYF * height);
                texture.SetPixel(i, outY, Color.green);
                //MonoBehaviour.print("input: " + input + " :: " + output);
            }
            byte[] fileBytes = texture.EncodeToPNG();
            File.WriteAllBytes(fileName, fileBytes);
            //MonoBehaviour.print("Wrote file to: " + fileName);
        }
    }
}

