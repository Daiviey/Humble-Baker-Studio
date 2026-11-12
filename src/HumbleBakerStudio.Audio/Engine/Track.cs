namespace HumbleBakerStudio.Audio;


public class Track {
    public string Name = "Track"; public string Synth = "acid";
    public double Vol = 0.8, Pan = 0, Cutoff = 2400, Res = 0.3, EnvAmt = 0.6, Atk = 0.005, Rel = 0.15, Crush = 16;
    public bool Mute;
    public bool Solo;   // when any track is soloed, only soloed tracks (and sidechain sources) are heard
    public int Bus = 0; // 0 = direct to master, 1..4 = Bus A..D
    public Patch Patch; public Fx Fx = new Fx();
    public string Uid = Guid.NewGuid().ToString("N");
    public float[] Sample; public int SampleRoot = 60; public bool SampleGate; public int SampleMode = 0;
    public string DrumKit = "Standard"; public string InstrumentName = "";
    // Optional VST3 slot: an instrument replaces the built-in synth, an effect processes the track after its FX chain.
    public string PluginFormat = "";     // vst3
    public string PluginPath = "";       // bundle or module path
    public string PluginClass = "";      // VST3 class id
    public string PluginName = "";       // display name
    public bool PluginInstrument;        // true = VSTi, false = audio effect
    public string PluginState = "";      // base64 component state
    public bool PluginBypass;
    public bool HasPlugin => !string.IsNullOrEmpty(PluginPath) && !PluginBypass;
    public List<Note> Notes = new List<Note>();
    public List<AutoLane> Auto = new List<AutoLane>();
}
