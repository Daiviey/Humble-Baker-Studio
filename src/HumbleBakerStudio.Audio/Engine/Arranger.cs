namespace HumbleBakerStudio.Audio;


public static class Arranger {
    // Lays the song out on one long timeline so the normal renderer can play it. Track settings are shared with the editor.
    public static Project Flatten(Project p) {
        Project q = new Project();
        q.Bpm = p.Bpm; q.BeatsPerBar = p.BeatsPerBar; q.BeatUnit = p.BeatUnit; q.PhraseBars = p.PhraseBars; q.Swing = p.Swing; q.DelayMix = p.DelayMix; q.DelayBeats = p.DelayBeats; q.DelayFb = p.DelayFb;
        q.Key = p.Key; q.Scale = p.Scale; q.Master = p.Master; q.Mix = p.Mix; q.Buses = p.Buses; q.Graph = p.Graph;
        foreach (Track t in p.Tracks) {
            Track c = new Track();
            foreach (System.Reflection.FieldInfo fi in typeof(Track).GetFields()) if (fi.Name != "Notes") fi.SetValue(c, fi.GetValue(t));
            c.Mute = false; c.Auto = new List<AutoLane>(); q.Tracks.Add(c);
        }
        double offset = 0;
        foreach (SongItem it in p.Song) {
            if (it.Pattern < 0 || it.Pattern >= p.Patterns.Count) continue;
            Pattern pat = p.Patterns[it.Pattern];
            int reps = Math.Max(1, Math.Min(128, it.Repeats));
            for (int r = 0; r < reps; r++) {
                for (int ti = 0; ti < q.Tracks.Count && ti < pat.Notes.Count; ti++) {
                    if (pat.Mutes[ti]) continue;
                    foreach (Note n in pat.Notes[ti]) q.Tracks[ti].Notes.Add(new Note(n.Pitch, n.Start + offset, n.Len, n.Vel));
                }
                double end = offset + pat.Bars * p.BarBeats;
                foreach (AutoLane al in pat.Auto) {
                    if (al.Track < 0 || al.Track >= q.Tracks.Count || al.Points.Count == 0 || al.Track >= pat.Mutes.Count || pat.Mutes[al.Track]) continue;
                    AutoLane dst = null;
                    foreach (AutoLane x in q.Tracks[al.Track].Auto) if (x.Param == al.Param) { dst = x; break; }
                    if (dst == null) { dst = new AutoLane(al.Track, al.Param); q.Tracks[al.Track].Auto.Add(dst); }
                    foreach (double[] pt in al.Points) dst.Points.Add(new double[] { offset + pt[0], pt[1] });
                    dst.Points.Add(new double[] { end - 0.002, al.Points[al.Points.Count - 1][1] });
                    dst.Points.Add(new double[] { end, AutoLane.Neutral(al.Param) });
                }
                offset = end;
            }
        }
        q.Bars = Math.Max(1, (int)Math.Ceiling(offset / p.BarBeats - 1e-9));
        return q;
    }
    public static double SongBeats(Project p) {
        double b = 0;
        foreach (SongItem it in p.Song) if (it.Pattern >= 0 && it.Pattern < p.Patterns.Count) b += p.Patterns[it.Pattern].Bars * p.BarBeats * Math.Max(1, it.Repeats);
        return b;
    }

    static int Role(Track t) {
        if (t.Synth == "drums") return 0;
        if (t.Synth == "pad" || (t.Patch != null && t.Synth == "patch" && t.Patch.Attack > 0.4)) return 2;
        if (t.Notes.Count == 0) return 3;
        double avg = 0; foreach (Note n in t.Notes) avg += n.Pitch; avg /= t.Notes.Count;
        return avg < 52 ? 1 : 3;
    }
    static List<Note> Vary(List<Note> src, Random rnd, double amount) {
        List<Note> o = new List<Note>();
        foreach (Note n in src) {
            double r = rnd.NextDouble();
            if (r < amount * 0.25) continue;
            if (r < amount && n.Len <= 0.5) {
                int k = rnd.Next(2, 4); double l = n.Len / k;
                for (int i = 0; i < k; i++) o.Add(new Note(n.Pitch, n.Start + i * l, l * 0.9, Math.Max(30, n.Vel - i * 8)));
            } else o.Add(n.Clone());
        }
        return o;
    }
    // Builds intro / build / drop / breakdown / drop B / outro scenes from the current tracks (0 drums, 1 bass, 2 pad, 3 lead).
    public static void AutoSong(Project p, int seed) {
        Random rnd = new Random(seed);
        p.Patterns.Clear(); p.Song.Clear();
        string[] names = new string[] { "Intro", "Build", "Drop", "Breakdown", "Drop B", "Outro" };
        bool[][] use = new bool[][] { new bool[] { false, false, true, true }, new bool[] { true, true, true, false }, new bool[] { true, true, true, true },
            new bool[] { false, false, true, true }, new bool[] { true, true, true, true }, new bool[] { true, false, true, false } };
        double[] vary = new double[] { 0, 0, 0, 0, 0.25, 0 };
        int[] reps = new int[] { 2, 2, 4, 2, 4, 2 };
        for (int s = 0; s < names.Length; s++) {
            Pattern pat = new Pattern(); pat.Name = names[s]; pat.Bars = p.Bars;
            int active = 0;
            foreach (Track t in p.Tracks) {
                bool on = use[s][Role(t)] && !t.Mute && t.Notes.Count > 0;
                List<Note> l = new List<Note>();
                if (on) { active++; if (vary[s] > 0) l = Vary(t.Notes, rnd, Role(t) == 0 ? vary[s] : vary[s] * 0.5); else foreach (Note n in t.Notes) l.Add(n.Clone()); }
                pat.Notes.Add(l); pat.Mutes.Add(!on);
            }
            if (active == 0) { pat.Notes.Clear(); pat.Mutes.Clear(); foreach (Track t in p.Tracks) { List<Note> l = new List<Note>(); foreach (Note n in t.Notes) l.Add(n.Clone()); pat.Notes.Add(l); pat.Mutes.Add(t.Mute); } }
            p.Patterns.Add(pat); p.Song.Add(new SongItem(s, reps[s]));
        }
    }
}
