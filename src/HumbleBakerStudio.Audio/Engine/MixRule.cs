namespace HumbleBakerStudio.Audio;


// One mixing move applied to one or more tracks before they are summed. Bounded like every other parameter class.
public class MixRule {
    public MixRule() { Enabled = true; Kind = MixKind.sidechain; Target = "*"; Source = ""; Depth = 0.7; AttackMs = 3; ReleaseMs = 160; FreqHz = 120; Db = 0; Q = 0.8; Width = 1; Pan = 0; }
    [Category("1 What"), Description("Turn this rule on or off.")] public bool Enabled { get; set; }
    [Category("1 What"), Description("sidechain (duck targets from the source track), highpass, lowpass, peak (EQ bell), lowshelf, highshelf, gain, pan, width, saturate, monobass (collapse low frequencies to mono).")] public MixKind Kind { get; set; }
    [Category("1 What"), Description("Which tracks it affects: * for all, or track names / synth names separated by commas (for example: bass, pad).")] public string Target { get; set; }
    [Category("1 What"), Description("Sidechain only: the track that triggers the ducking, usually the kick (matched by name). Works even if that track is muted.")] public string Source { get; set; }
    [Category("2 Sidechain and drive"), Description("Sidechain: how far the targets duck (1 = fully silent on each hit). Saturate: drive amount."), Rng(0, 1)] public double Depth { get; set; }
    [Category("2 Sidechain and drive"), Description("Sidechain: how fast it ducks, in milliseconds."), Rng(0.5, 100)] public double AttackMs { get; set; }
    [Category("2 Sidechain and drive"), Description("Sidechain: how long it takes to recover, in milliseconds. Short = tight pump, long = swell."), Rng(20, 1000)] public double ReleaseMs { get; set; }
    [Category("3 Filter and level"), Description("Frequency in Hz for filters, EQ bell, shelves and monobass."), Rng(20, 20000)] public double FreqHz { get; set; }
    [Category("3 Filter and level"), Description("Gain in dB for peak, shelves and gain."), Rng(-24, 12)] public double Db { get; set; }
    [Category("3 Filter and level"), Description("Filter resonance / bell width."), Rng(0.2, 8)] public double Q { get; set; }
    [Category("3 Filter and level"), Description("Stereo width for kind width (1 = unchanged, 0 = mono, 2 = very wide)."), Rng(0, 2)] public double Width { get; set; }
    [Category("3 Filter and level"), Description("Pan position for kind pan, -1 left to 1 right."), Rng(-1, 1)] public double Pan { get; set; }
}
