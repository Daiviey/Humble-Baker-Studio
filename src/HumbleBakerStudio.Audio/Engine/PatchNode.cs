namespace HumbleBakerStudio.Audio;


// -----------------------------------------------------------------------------------
// Eurorack-style patch graph: modules (tracks, FX units, bus/mixer amps, the master out)
// are wired jack-to-jack. Unpatched tracks and orphaned modules still reach the speakers,
// so a fresh graph sounds exactly like an unpatched mixer.
// -----------------------------------------------------------------------------------
public class PatchNode {
    public string Id = Guid.NewGuid().ToString("N");
    public string Kind = "fx";       // track | fx | bus | out
    public string Type = "filter";   // fx: module type, bus: bus label base, track: track, out: out
    public string Label = "";        // fallback label (fx/bus/out); track nodes resolve live by TrackUid
    public string Module = "";       // fx: selected effect module (filter, drive, delay, ... )
    public double X = 60, Y = 60;
    public string TrackUid;          // Kind == track: uid of the linked Track
    [Category("1 Module"), Description("Module output gain (bus modules only). 0 = silent, 1 = unity, 2 = hot."), Rng(0, 2)] public double Gain { get; set; }
    [Category("1 Module"), Description("Pan of this bus module (bus modules only). -1 left, 0 centre, +1 right."), Rng(-1, 1)] public double Pan { get; set; }
    [Category("1 Module"), Description("Bypass this module: the signal passes through untouched.")] public bool Bypass { get; set; }
    [Browsable(false)] public Fx Fx = new Fx();   // Kind == fx: the module's own effect chain
    [Browsable(false)] public List<AutoLane> Auto = new List<AutoLane>();
}
