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

        [GameParameters.CustomParameterUI("Wheel Dust Camera", toolTip = "If enabled the dust system will use real-time camera rendering data to determine dust colors.  If disabled dust colors will fall-back to the pre-defined lookup map.")]
        public bool wheelDustCamera = true;

        [GameParameters.CustomFloatParameterUI("Wheel Dust Power", minValue = 0, maxValue = 4, stepCount = 15, displayFormat = "F2", toolTip = "Increases or decreases dust emission rate. 1=standard, 0=off")]
        public float wheelDustPower = 1f;

        [GameParameters.CustomParameterUI("Wear and Damage", toolTip = "Wear and damage model.\nNone = No wheel wear or breakage.\nSimple = Stock equivalent, break on impact/over-stress.\nAdvanced = Time, speed, load, heat, and impact based wheel wear + breakage.")]
        public KSPWheelWearType wearType = KSPWheelWearType.SIMPLE;

        [GameParameters.CustomParameterUI("Enable Debugging", toolTip = "If enabled debug tools will be available in the app-launcher bar..")]
        public bool debugMode = false;

        public override string Section { get { return "KSPWheel"; } }

        public override int SectionOrder { get { return 1; } }

        public override string Title { get { return "Basic Options"; } }

        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }

        public override bool HasPresets { get { return false; } }

    }

    public class KSPWheelScaleSettings : GameParameters.CustomParameterNode
    {

        [GameParameters.CustomFloatParameterUI("Part Mass Scale Power", minValue = 1, maxValue = 4, stepCount = 11, displayFormat = "F2", toolTip = "Sets the exponent to which part mass is scaled when scaling up or down")]
        public float partMassScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Part Cost Scale Power", minValue = 1, maxValue = 4, stepCount = 11, displayFormat = "F2", toolTip = "Sets the exponent to which part cost is scaled when scaling up or down")]
        public float partCostScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Wheel Mass Scale Power", minValue = 1, maxValue = 4, stepCount = 11, displayFormat = "F2", toolTip = "Sets the exponent to which wheel mass is scaled when scaling up or down")]
        public float wheelMassScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Wheel Max Speed Scale Power", minValue = 1, maxValue = 4, stepCount = 11, displayFormat = "F2", toolTip = "Sets the exponent to which wheel max safe speed is scaled when scaling up or down")]
        public float wheelMaxSpeedScalingPower = 1f;

        [GameParameters.CustomFloatParameterUI("Wheel Max Load Scale Power", minValue = 1, maxValue = 4, stepCount = 11, displayFormat = "F2", toolTip = "Sets the exponent to which wheel min/max load are scaled when scaling up or down")]
        public float wheelMaxLoadScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Wheel Rolling Resistance Scale Power", minValue = 1, maxValue = 4, stepCount = 11, displayFormat = "F2", toolTip = "Sets the exponent to which rolling resistance is scaled when scaling up or down")]
        public float rollingResistanceScalingPower = 1f;

        [GameParameters.CustomFloatParameterUI("Motor Torque Scale Power", minValue = 1, maxValue = 4, stepCount = 11, displayFormat = "F2", toolTip = "Sets the exponent to which motor torque is scaled when scaling up or down")]
        public float motorTorqueScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Motor Power Scale Power", minValue = 1, maxValue = 4, stepCount = 11, displayFormat = "F2", toolTip = "Sets the exponent to which motor power draw is scaled when scaling up or down")]
        public float motorPowerScalingPower = 3f;

        [GameParameters.CustomFloatParameterUI("Motor RPM Scale Power", minValue = 0, maxValue = 4, stepCount = 15, displayFormat = "F2", toolTip = "Sets the exponent to which motor max rpm is scaled when scaling up or down")]
        public float motorMaxRPMScalingPower = 0f;

        public override string Section { get { return "KSPWheel"; } }

        public override int SectionOrder { get { return 3; } }

        public override string Title { get { return "Scaling Options"; } }

        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }

        public override bool HasPresets { get { return false; } }

    }

    public class KSPWheelWearSettings : GameParameters.CustomParameterNode
    {

        [GameParameters.CustomFloatParameterUI("Impact Tolerance Multiplier", minValue = 0, maxValue = 4, stepCount = 40, displayFormat = "F2", toolTip = "Global multiplier to the config specified impact tolerance of wheels, applied to the calculated impact velocity.  Lower values result in higher impact tolerance, setting to zero disables impact damage.")]
        public float impactToleranceMultiplier = 1f;

        [GameParameters.CustomFloatParameterUI("Wheel Stress Damage Rate", minValue = 0, maxValue = 4, stepCount = 40, displayFormat = "F2", toolTip = "Determines how quickly wheels break from being overloaded or absorbing impact forces.  Lower values result in increased load and impact stress tolerance, setting to zero disables stress based damage.")]
        public float stressDamageMultiplier = 1f;

        [GameParameters.CustomFloatParameterUI("Wheel Speed Damage Rate", minValue = 0, maxValue = 4, stepCount = 40, displayFormat = "F2", toolTip = "Determines how quickly wheels break from being driven past their maximum safe speed.  Lower values result in increased over-speed tolerance, setting to zero disables speed based damage.\nIn advanced wear mode this setting influences the overall rate of wheel wear accumulation that is contributed to speed.")]
        public float speedDamageMultiplier = 1f;

        [GameParameters.CustomFloatParameterUI("Wheel Slip Damage Rate", minValue = 0, maxValue = 4, stepCount = 40, displayFormat = "F2", toolTip = "ADVANCED WEAR MODE ONLY\nDetermines how quickly wheels accumulate wear from wheel slip.  Lower values result in increased slip tolerance, setting to zero disables slip based damage.")]
        public float slipDamageMultiplier = 1f;

        [GameParameters.CustomFloatParameterUI("Motor Use Wear Rate", minValue = 0, maxValue = 4, stepCount = 40, displayFormat = "F2", toolTip = "ADVANCED WEAR MODE ONLY\nDetermines how quickly motors accumulate wear from standard use.  Lower values result in increased motor lifespan, setting to zero disables use based damage.")]
        public float motorDamageMultiplier = 1f;

        [GameParameters.CustomFloatParameterUI("Motor Heat Wear Rate", minValue = 0, maxValue = 4, stepCount = 40, displayFormat = "F2", toolTip = "ADVANCED WEAR MODE ONLY\nDetermines how quickly motors accumulate wear from being used while overheated.  Lower values result in increased motor heat tolerance, setting to zero disables heat based damage.")]
        public float motorHeatMultiplier = 1f;

        public override string Section { get { return "KSPWheel"; } }

        public override int SectionOrder { get { return 2; } }

        public override string Title { get { return "Damage Options"; } }

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
