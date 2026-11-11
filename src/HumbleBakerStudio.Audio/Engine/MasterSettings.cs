namespace HumbleBakerStudio.Audio;


public class MasterSettings {
    public MasterSettings() {
        Enabled = false; InputGain = 0; LowShelfDb = 0; LowShelfHz = 100; MidDb = 0; MidHz = 400; MidQ = 1; HighShelfDb = 0; HighShelfHz = 8000;
        Warmth = 0; CompThreshold = -16; CompRatio = 1; CompAttack = 25; CompRelease = 180; CompKnee = 6; Width = 1; BassMonoHz = 0; OutputGain = 0;
        Limiter = true; Ceiling = -1; LimiterRelease = 80; TargetLufs = -14;
    }
    [Category("0 Master"), Description("Apply the mastering chain to previews and renders.")] public bool Enabled { get; set; }
    [Category("0 Master"), Description("Target integrated loudness in LUFS used by Auto master (streaming -14, club -9)."), Rng(-30, -5)] public double TargetLufs { get; set; }
    [Category("1 Gain"), Description("Gain into the chain in dB (drives the compressor and limiter)."), Rng(-12, 30)] public double InputGain { get; set; }
    [Category("2 EQ"), Description("Low shelf gain in dB."), Rng(-9, 9)] public double LowShelfDb { get; set; }
    [Category("2 EQ"), Description("Low shelf frequency in Hz."), Rng(30, 400)] public double LowShelfHz { get; set; }
    [Category("2 EQ"), Description("Mid bell gain in dB (cut around 300 Hz to clear mud)."), Rng(-9, 9)] public double MidDb { get; set; }
    [Category("2 EQ"), Description("Mid bell frequency in Hz."), Rng(100, 6000)] public double MidHz { get; set; }
    [Category("2 EQ"), Description("Mid bell Q."), Rng(0.3, 4)] public double MidQ { get; set; }
    [Category("2 EQ"), Description("High shelf gain in dB (air)."), Rng(-9, 9)] public double HighShelfDb { get; set; }
    [Category("2 EQ"), Description("High shelf frequency in Hz."), Rng(2000, 16000)] public double HighShelfHz { get; set; }
    [Category("3 Colour"), Description("Soft saturation amount (analogue warmth)."), Rng(0, 1)] public double Warmth { get; set; }
    [Category("4 Compressor"), Description("Threshold in dB."), Rng(-40, 0)] public double CompThreshold { get; set; }
    [Category("4 Compressor"), Description("Ratio (1 = off)."), Rng(1, 10)] public double CompRatio { get; set; }
    [Category("4 Compressor"), Description("Attack in ms."), Rng(1, 100)] public double CompAttack { get; set; }
    [Category("4 Compressor"), Description("Release in ms."), Rng(20, 1000)] public double CompRelease { get; set; }
    [Category("4 Compressor"), Description("Soft knee width in dB."), Rng(0, 18)] public double CompKnee { get; set; }
    [Category("5 Stereo"), Description("Stereo width (1 = unchanged, 0 = mono)."), Rng(0, 1.6)] public double Width { get; set; }
    [Category("5 Stereo"), Description("Make everything below this frequency mono (0 = off)."), Rng(0, 300)] public double BassMonoHz { get; set; }
    [Category("6 Output"), Description("Gain after the compressor in dB."), Rng(-12, 12)] public double OutputGain { get; set; }
    [Category("6 Output"), Description("Brickwall look-ahead limiter on or off.")] public bool Limiter { get; set; }
    [Category("6 Output"), Description("True-peak-safe ceiling in dBFS."), Rng(-6, 0)] public double Ceiling { get; set; }
    [Category("6 Output"), Description("Limiter release in ms."), Rng(10, 500)] public double LimiterRelease { get; set; }
    public MasterSettings Clone() { return (MasterSettings)MemberwiseClone(); }
}

class Biquad {
    double b0, b1, b2, a1, a2, z1, z2;
    void Norm(double nb0, double nb1, double nb2, double a0, double na1, double na2) { b0 = nb0 / a0; b1 = nb1 / a0; b2 = nb2 / a0; a1 = na1 / a0; a2 = na2 / a0; }
    public double Tick(double x) { double y = b0 * x + z1; z1 = b1 * x - a1 * y + z2; z2 = b2 * x - a2 * y; return y; }
    public static Biquad LowShelf(double fs, double f, double g, double q) {
        double A = Math.Pow(10, g / 40), w = 2 * Math.PI * f / fs, c = Math.Cos(w), al = Math.Sin(w) / (2 * q), sa = 2 * Math.Sqrt(A) * al; Biquad b = new Biquad();
        b.Norm(A * ((A + 1) - (A - 1) * c + sa), 2 * A * ((A - 1) - (A + 1) * c), A * ((A + 1) - (A - 1) * c - sa), (A + 1) + (A - 1) * c + sa, -2 * ((A - 1) + (A + 1) * c), (A + 1) + (A - 1) * c - sa); return b;
    }
    public static Biquad HighShelf(double fs, double f, double g, double q) {
        double A = Math.Pow(10, g / 40), w = 2 * Math.PI * f / fs, c = Math.Cos(w), al = Math.Sin(w) / (2 * q), sa = 2 * Math.Sqrt(A) * al; Biquad b = new Biquad();
        b.Norm(A * ((A + 1) + (A - 1) * c + sa), -2 * A * ((A - 1) + (A + 1) * c), A * ((A + 1) + (A - 1) * c - sa), (A + 1) - (A - 1) * c + sa, 2 * ((A - 1) - (A + 1) * c), (A + 1) - (A - 1) * c - sa); return b;
    }
    public static Biquad Peak(double fs, double f, double g, double q) {
        double A = Math.Pow(10, g / 40), w = 2 * Math.PI * f / fs, c = Math.Cos(w), al = Math.Sin(w) / (2 * q); Biquad b = new Biquad();
        b.Norm(1 + al * A, -2 * c, 1 - al * A, 1 + al / A, -2 * c, 1 - al / A); return b;
    }
    public static Biquad LowPass(double fs, double f, double q) {
        double w = 2 * Math.PI * f / fs, c = Math.Cos(w), al = Math.Sin(w) / (2 * q); Biquad b = new Biquad();
        b.Norm((1 - c) / 2, 1 - c, (1 - c) / 2, 1 + al, -2 * c, 1 - al); return b;
    }
    public static Biquad HighPass(double fs, double f, double q) {
        double w = 2 * Math.PI * f / fs, c = Math.Cos(w), al = Math.Sin(w) / (2 * q); Biquad b = new Biquad();
        b.Norm((1 + c) / 2, -(1 + c), (1 + c) / 2, 1 + al, -2 * c, 1 - al); return b;
    }
}
