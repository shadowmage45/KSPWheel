namespace KSPWheel
{
    public class KSPWheelSettings : GameParameters.CustomParameterNode
    {

        [GameParameters.CustomParameterUI("Manual Suspension Configuration", toolTip = "If enabled wheels use manual setup for suspension tuning and must be configured on a per-wheel basis.")]
        public bool advancedMode = false;

        [GameParameters.CustomParameterUI("Manual Gear Selection", toolTip = "If enabled motors will have multiple gear ratios available (configurable).")]
        public bool manualGearing = false;

        [GameParameters.CustomParameterUI("Wheel Dust Effects", toolTip = "If enabled wheels will kick up dust when traversing terrain.")]
        public bool wheelDustEffects = true;

        [GameParameters.CustomParameterUI("Wear and Damage", toolTip = "Wear and damage model.\nNone = No wheel wear or breakage.\nSimple = Stock equivalent, break on impact/over-stress.\nAdvanced = Time, speed, load, heat, and impact based wheel wear + breakage.")]
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
