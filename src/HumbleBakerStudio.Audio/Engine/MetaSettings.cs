namespace HumbleBakerStudio.Audio;


public class MetaSettings {
    public MetaSettings() { Seconds = 8; Steps = 24; Pitch = 48; Path = MetaPath.tour; Chord = MetaChord.single; HoldSustain = true; Seed = 1; X = 0.5; Y = 0.5; }
    [Category("1 Morph"), Description("Morph position left (A, C) to right (B, D)."), Rng(0, 1, NoRandom = true)] public double X { get; set; }
    [Category("1 Morph"), Description("Morph position top (A, B) to bottom (C, D)."), Rng(0, 1, NoRandom = true)] public double Y { get; set; }
    [Category("2 Evolving sound"), Description("Length of the evolving sound in seconds."), Rng(1, 30)] public double Seconds { get; set; }
    [Category("2 Evolving sound"), Description("Number of morph steps (more = smoother, slower to render)."), Rng(4, 64)] public double Steps { get; set; }
    [Category("2 Evolving sound"), Description("MIDI pitch of the sound."), Rng(24, 96)] public double Pitch { get; set; }
    [Category("2 Evolving sound"), Description("Journey through the four patches: tour (A to B to C to D), pingpong (A to B and back), circle, wander (smooth random).")] public MetaPath Path { get; set; }
    [Category("2 Evolving sound"), Description("Stack extra notes: single, octaves, fifth, minor, major, sus2, cluster.")] public MetaChord Chord { get; set; }
    [Category("2 Evolving sound"), Description("Keep each step sustaining so the morph sounds continuous.")] public bool HoldSustain { get; set; }
    [Category("2 Evolving sound"), Description("Random seed for wander."), Rng(0, 9999, NoRandom = true)] public double Seed { get; set; }
}
