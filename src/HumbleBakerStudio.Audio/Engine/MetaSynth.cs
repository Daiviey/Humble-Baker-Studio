namespace HumbleBakerStudio.Audio;


// Morphs between up to four patches. Numbers blend (cutoff-like ranges blend in octaves), choices switch halfway.
public static class MetaSynth {
    public static Patch Morph(Patch a, Patch b, double t) {
        Patch r = new Patch(); t = Math.Max(0, Math.Min(1, t));
        foreach (System.Reflection.PropertyInfo pi in typeof(Patch).GetProperties()) {
            if (!pi.CanWrite) continue;
            object va = pi.GetValue(a, null), vb = pi.GetValue(b, null);
            if (pi.PropertyType.IsEnum) { pi.SetValue(r, t < 0.5 ? va : vb, null); continue; }
            object[] at = pi.GetCustomAttributes(typeof(RngAttribute), true); RngAttribute rg = at.Length > 0 ? (RngAttribute)at[0] : null;
            double x = Convert.ToDouble(va, CultureInfo.InvariantCulture), y = Convert.ToDouble(vb, CultureInfo.InvariantCulture), v;
            if (rg != null && rg.Min > 0 && rg.Max / rg.Min > 20) v = Math.Exp(Math.Log(Math.Max(x, rg.Min)) * (1 - t) + Math.Log(Math.Max(y, rg.Min)) * t);
            else v = x + (y - x) * t;
            if (pi.Name == "Unison") v = Math.Round(v);
            if (rg != null) v = Math.Max(rg.Min, Math.Min(rg.Max, v));
            pi.SetValue(r, v, null);
        }
        return r;
    }
    public static Patch Bilinear(Patch[] s, double x, double y) { return Morph(Morph(s[0], s[1], x), Morph(s[2], s[3], x), y); }

    public static void PathPoint(MetaPath m, double u, int seed, out double x, out double y) {
        u = Math.Max(0, Math.Min(1, u));
        switch (m) {
            case MetaPath.pingpong: x = u < 0.5 ? u * 2 : 2 - u * 2; y = 0; break;
            case MetaPath.circle: x = 0.5 + 0.5 * Math.Cos(2 * Math.PI * u + Math.PI); y = 0.5 + 0.5 * Math.Sin(2 * Math.PI * u + Math.PI); break;
            case MetaPath.wander: {
                Random rnd = new Random(seed); double a1 = rnd.NextDouble() * 6.28, a2 = rnd.NextDouble() * 6.28, a3 = rnd.NextDouble() * 6.28, a4 = rnd.NextDouble() * 6.28;
                x = 0.5 + 0.5 * (0.6 * Math.Sin(2 * Math.PI * u * 1.3 + a1) + 0.4 * Math.Sin(2 * Math.PI * u * 3.1 + a2));
                y = 0.5 + 0.5 * (0.6 * Math.Sin(2 * Math.PI * u * 1.7 + a3) + 0.4 * Math.Sin(2 * Math.PI * u * 2.9 + a4)); break; }
            default: {
                double[][] pts = new double[][] { new double[] { 0, 0 }, new double[] { 1, 0 }, new double[] { 0, 1 }, new double[] { 1, 1 } };
                double f = u * 3; int i = Math.Min(2, (int)f); double t = f - i; x = pts[i][0] + (pts[i + 1][0] - pts[i][0]) * t; y = pts[i][1] + (pts[i + 1][1] - pts[i][1]) * t; break; }
        }
    }
    static int[] ChordTones(MetaChord c) {
        switch (c) {
            case MetaChord.octaves: return new int[] { 0, 12, -12 };
            case MetaChord.fifth: return new int[] { 0, 7 };
            case MetaChord.minor: return new int[] { 0, 3, 7 };
            case MetaChord.major: return new int[] { 0, 4, 7 };
            case MetaChord.sus2: return new int[] { 0, 2, 7 };
            case MetaChord.cluster: return new int[] { 0, 1, 2, 5 };
            default: return new int[] { 0 };
        }
    }
    static Patch Prep(Patch p, MetaSettings m) {
        if (!m.HoldSustain) return p;
        Patch q = p.Clone(); q.Sustain = Math.Max(q.Sustain, 0.85); q.Attack = Math.Min(q.Attack, 0.02); return q;
    }
    public static float[] Evolve(Patch[] slots, MetaSettings m) {
        int steps = Math.Max(4, Math.Min(64, (int)m.Steps)); double hop = Math.Max(1, m.Seconds) / steps; int hopN = (int)(hop * Dsp.SR), winN = hopN * 2;
        float[] o = new float[hopN * (steps + 1)]; int[] tones = ChordTones(m.Chord);
        for (int i = 0; i < steps; i++) {
            double x, y; PathPoint(m.Path, steps == 1 ? 0 : (double)i / (steps - 1), (int)m.Seed, out x, out y);
            Patch p = Prep(Bilinear(slots, x, y), m);
            foreach (int tone in tones) {
                float[] n = PatchSynth.Render(p, Math.Max(0, Math.Min(127, (int)m.Pitch + tone)), hop * 2.2);
                for (int k = 0; k < winN && k < n.Length; k++) {
                    double w = 0.5 - 0.5 * Math.Cos(2 * Math.PI * k / (winN - 1));
                    if (i == 0 && k < winN / 2) w = 1; if (i == steps - 1 && k >= winN / 2) w = 1;
                    int idx = i * hopN + k; if (idx < o.Length) o[idx] += (float)(n[k] * w / tones.Length);
                }
            }
        }
        Dsp.Normalize(o, 0.89f);
        return o;
    }
}
