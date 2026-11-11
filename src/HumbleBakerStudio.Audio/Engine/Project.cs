namespace HumbleBakerStudio.Audio;


public class Project {
    public double Bpm = 140; public int BeatsPerBar = 4; public int BeatUnit = 4; public int PhraseBars = 4; public int Bars = 4; public double Swing = 0;
    public double DelayMix = 0.2, DelayBeats = 0.75, DelayFb = 0.4;
    public int Key = 0; public string Scale = "minor";
    public string ScSessionId = "";
    public List<Track> Tracks = new List<Track>();
    public List<Pattern> Patterns = new List<Pattern>();
    public List<SongItem> Song = new List<SongItem>();
    public List<Marker> Markers = new List<Marker>();
    public MasterSettings Master = new MasterSettings();
    public List<MixRule> Mix = new List<MixRule>();
    public List<Bus> Buses = new List<Bus>();
    public bool Click;   // metronome blip on every beat (accent on the downbeat)
    public PatchGraph Graph = new PatchGraph();
    public double BarBeats { get { return BeatsPerBar * 4.0 / Math.Max(1, BeatUnit); } }
    public double LoopBeats { get { return Bars * BarBeats; } }
}
