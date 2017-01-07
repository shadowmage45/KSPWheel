namespace KSPWheel
{
    public class KSPWheelSettings : GameParameters.CustomParameterNode
    {

        [GameParameters.CustomParameterUI("Advanced Mode?", toolTip = "If true wheels use advanced user GUI and must be configured on a per-vehicle/per-use basis.  Disables all suspension auto-tuning when enabled.")]
        public bool advancedMode = false;

        [GameParameters.CustomParameterUI("Wear and Damage", toolTip = "None = No wheel wear or breakage.  Simple = Stock equivalent (break on load/impact/stress), Advanced = time/speed/load based wheel wear + breakage.")]
        public KSPWheelWearType wearType = KSPWheelWearType.SIMPLE;

        public override string Section { get { return "KSPWheel"; } }

        public override int SectionOrder { get { return 1; } }

        public override string Title { get { return "KSPWheel Options"; } }

        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }

        public override bool HasPresets { get { return false; } }

    }

    public enum KSPWheelWearType
    {
        NONE,
        SIMPLE,
        ADVANCED
    }
}
