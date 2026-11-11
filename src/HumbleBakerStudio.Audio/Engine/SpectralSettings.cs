namespace HumbleBakerStudio.Audio;


// Settings for turning a spectrogram picture into sound. Every property is bounded so the grid, the AI and the randomizer agree.
public class SpectralSettings {
    public SpectralSettings() { Seconds = 6; MinHz = 60; MaxHz = 9000; Snap = SpecSnap.off; Root = 0; Brightness = 0; Noisiness = 0; Contrast = 1; FadeMs = 15; Gain = 0.9; }
    [Category("1 Time and range"), Description("Length of the sound in seconds (the picture is stretched over it)."), Rng(0.5, 30)] public double Seconds { get; set; }
    [Category("1 Time and range"), Description("Frequency of the bottom row in Hz."), Rng(20, 2000)] public double MinHz { get; set; }
    [Category("1 Time and range"), Description("Frequency of the top row in Hz."), Rng(1000, 20000)] public double MaxHz { get; set; }
    [Category("1 Time and range"), Description("Play the picture backwards.")] public bool Reverse { get; set; }
    [Category("1 Time and range"), Description("Flip the picture upside down (low and high swap).")] public bool Flip { get; set; }
    [Category("2 Pitch"), Description("Snap every row to a musical scale so images sound tonal: off, chromatic, major, minor, pentatonic, hirajoshi, wholetone, diminished.")] public SpecSnap Snap { get; set; }
    [Category("2 Pitch"), Description("Scale root as a pitch class (0 = C, 9 = A)."), Rng(0, 11)] public double Root { get; set; }
    [Category("3 Timbre"), Description("Adds 2nd and 3rd harmonics to every partial (0 pure sines, 1 buzzy)."), Rng(0, 1)] public double Brightness { get; set; }
    [Category("3 Timbre"), Description("Random pitch jitter on each partial: 0 clean sines, 1 airy noisy bands."), Rng(0, 1)] public double Noisiness { get; set; }
    [Category("3 Timbre"), Description("Gamma on brightness: below 1 lifts faint areas, above 1 keeps only the brightest."), Rng(0.3, 3)] public double Contrast { get; set; }
    [Category("4 Output"), Description("Fade in and out in milliseconds."), Rng(0, 200)] public double FadeMs { get; set; }
    [Category("4 Output"), Description("Peak level of the result."), Rng(0.1, 1, NoRandom = true)] public double Gain { get; set; }
}
