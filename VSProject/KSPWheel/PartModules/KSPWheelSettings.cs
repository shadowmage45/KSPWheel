namespace KSPWheel
{
    public class KSPWheelSettings : GameParameters.CustomParameterNode
    {

        [GameParameters.CustomParameterUI("Advanced Mode?", toolTip = "If true wheels use advanced user GUI and must be configured on a per-vehicle/per-use basis.  Disables all suspension auto-tuning when enabled.")]
        public bool advancedMode = true;

        [GameParameters.CustomFloatParameterUI("Loading Mult", asPercentage = true, minValue = 0, maxValue = 10, toolTip = "Determines how fast wheels react to loading and stresses causing damage or destruction of the part. 0=disable, 1=normal, <1=explode faster, >1=explode slower")]
        public float loadingMultiplier = 1f;

        public override string Section { get { return "KSPWheel"; } }

        public override int SectionOrder { get { return 1; } }

        public override string Title { get { return "KSPWheel Options"; } }

        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }

        public override bool HasPresets { get { return false; } }

    }
}
