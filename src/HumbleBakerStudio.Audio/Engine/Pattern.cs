namespace HumbleBakerStudio.Audio;


// A scene: the notes and mute state of every track for one section of a song.
public class Pattern {
    public string Name = "Pattern"; public int Bars = 4;
    public List<List<Note>> Notes = new List<List<Note>>(); public List<bool> Mutes = new List<bool>();
    public List<AutoLane> Auto = new List<AutoLane>();
    public int NoteCount { get { int c = 0; foreach (List<Note> l in Notes) c += l.Count; return c; } }
    public static Pattern Capture(Project p, string name) {
        Pattern q = new Pattern(); q.Name = name; q.Bars = p.Bars;
        foreach (Track t in p.Tracks) { List<Note> l = new List<Note>(); foreach (Note n in t.Notes) l.Add(n.Clone()); q.Notes.Add(l); q.Mutes.Add(t.Mute); }
        return q;
    }
    public void Apply(Project p) {
        p.Bars = Bars;
        for (int i = 0; i < p.Tracks.Count; i++) {
            p.Tracks[i].Notes.Clear();
            if (i < Notes.Count) { foreach (Note n in Notes[i]) p.Tracks[i].Notes.Add(n.Clone()); p.Tracks[i].Mute = Mutes[i]; }
        }
    }
    public void RemoveTrack(int i) {
        if (i >= 0 && i < Notes.Count) { Notes.RemoveAt(i); Mutes.RemoveAt(i); }
        Auto.RemoveAll(delegate (AutoLane a) { return a.Track == i; });
        foreach (AutoLane a in Auto) if (a.Track > i) a.Track--;
    }
}
