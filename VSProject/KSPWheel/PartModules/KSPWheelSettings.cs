namespace KSPWheel
{
    public class KSPWheelSettings : GameParameters.CustomParameterNode
    {

        [GameParameters.CustomParameterUI("Manual Suspension Configuration", toolTip = "If enabled wheels use manual setup for suspension tuning and must be configured on a per-wheel basis.")]
        public bool advancedMode = false;

        [GameParameters.CustomParameterUI("Manual Gear Selection", toolTip = "If enabled motors will have multiple gear ratios available (configurable).")]
        public bool manualGearing = true;

        [GameParameters.CustomParameterUI("Wheel Dust Effects", toolTip = "If enabled wheels will kick up dust when traversing terrain.")]
        public bool wheelDustEffects = true;

        [GameParameters.CustomParameterUI("Wear and Damage", toolTip = "Wear and damage model.\nNone = No wheel wear or breakage.\nSimple = Stock equivalent, break on impact/over-stress.\nAdvanced = Time, speed, load, heat, and impact based wheel wear + breakage.")]
        public KSPWheelWearType wearType = KSPWheelWearType.SIMPLE;

        public override string Section { get { return "KSPWheel"; } }

        public override int SectionOrder { get { return 1; } }

        public override string Title { get { return "Basic Options"; } }

        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }

        public override bool HasPresets { get { return false; } }

    }

    public class KSPWheelScaleSettings : GameParameters.CustomParameterNode
    {

        [GameParameters.CustomFloatParameterUI("Part Mass Scale Power", minValue = 1, maxValue = 4, stepCount = 12, toolTip = "Sets the exponent to which part mass is scaled when scaling up or down")]
        public float partMassScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Part Cost Scale Power", minValue = 1, maxValue = 4, stepCount = 12, toolTip = "Sets the exponent to which part cost is scaled when scaling up or down")]
        public float partCostScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Wheel Mass Scale Power", minValue = 1, maxValue = 4, stepCount = 12, toolTip = "Sets the exponent to which wheel mass is scaled when scaling up or down")]
        public float wheelMassScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Wheel Max Speed Scale Power", minValue = 1, maxValue = 4, stepCount = 12, toolTip = "Sets the exponent to which wheel max safe speed is scaled when scaling up or down")]
        public float wheelMaxSpeedScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Wheel Max Load Scale Power", minValue = 1, maxValue = 4, stepCount = 12, toolTip = "Sets the exponent to which wheel min/max load are scaled when scaling up or down")]
        public float wheelMaxLoadScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Wheel Rolling Resistance Scale Power", minValue = 1, maxValue = 4, stepCount = 12, toolTip = "Sets the exponent to which rolling resistance is scaled when scaling up or down")]
        public float rollingResistanceScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Motor Torque Scale Power", minValue = 1, maxValue = 4, stepCount = 12, toolTip = "Sets the exponent to which motor torque is scaled when scaling up or down")]
        public float motorTorqueScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Motor Power Scale Power", minValue = 1, maxValue = 4, stepCount = 12, toolTip = "Sets the exponent to which motor power draw is scaled when scaling up or down")]
        public float motorPowerScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Motor RPM Scale Power", minValue = 1, maxValue = 4, stepCount = 12, toolTip = "Sets the exponent to which motor max rpm is scaled when scaling up or down")]
        public float motorMaxRPMScalingPower = 3f;

        public override string Section { get { return "KSPWheel"; } }

        public override int SectionOrder { get { return 2; } }

        public override string Title { get { return "Scaling Options"; } }

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
