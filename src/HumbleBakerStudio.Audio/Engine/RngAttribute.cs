namespace HumbleBakerStudio.Audio;


[AttributeUsage(AttributeTargets.Property)]
public class RngAttribute : Attribute {
    public double Min, Max; public bool NoRandom;
    public RngAttribute(double min, double max) { Min = min; Max = max; }
}
