namespace HumbleBakerStudio.Audio;


public static class Engine {
    // Notes of a track with swing applied and clipped to the loop; shared by the preview renderer and the SuperCollider export.
    public static List<Ev> Events(Project p, Track t) {
        List<Ev> list = new List<Ev>();
        double loop = p.LoopBeats;
        foreach (Note n in t.Notes) {
            double s = n.Start;
            double q = s / 0.25; long r = (long)Math.Round(q);
            if (Math.Abs(q - r) < 0.02 && (r % 2) != 0) s += p.Swing * 0.125;
            if (s < 0 || s >= loop) continue;
            Ev e = new Ev(); e.Start = s; e.Len = Math.Max(0.02, n.Len); e.Pitch = n.Pitch; e.Vel = n.Vel;
            list.Add(e);
        }
        list.Sort(delegate (Ev a, Ev b) { int c = a.Start.CompareTo(b.Start); return c != 0 ? c : a.Pitch.CompareTo(b.Pitch); });
        return list;
    }
}
