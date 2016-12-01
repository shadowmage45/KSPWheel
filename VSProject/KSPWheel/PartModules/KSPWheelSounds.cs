using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelSounds : PartModule
    {

        [KSPField]
        public string longSlipEffect = String.Empty;

        [KSPField]
        public float longSlipStart = 0.2f;

        [KSPField]
        public float longSlipPeak = 0.6f;

        [KSPField]
        public string latSlipEffect = String.Empty;

    }
}
