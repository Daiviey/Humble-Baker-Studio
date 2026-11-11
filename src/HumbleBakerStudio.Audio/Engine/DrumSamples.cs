namespace HumbleBakerStudio.Audio;


// Drum kits rendered as three velocity layers per piece. When a kit folder is present the sampler takes over
// from the procedural Drum() synth, which stays as the fallback for missing kits/pieces.
public static class DrumSamples {
    public static bool Enabled = true;
    public static readonly List<string> Roots = new List<string>();
    static readonly Dictionary<string, float[][]> Cache = new Dictionary<string, float[][]>();
    static readonly Dictionary<string, bool> Missing = new Dictionary<string, bool>();
    static readonly object Gate = new object();

    public static void AddRoot(string dir) {
        if (String.IsNullOrWhiteSpace(dir)) return;
        try { dir = Path.GetFullPath(dir).TrimEnd('\\'); } catch { return; }
        lock (Gate) { if (!Roots.Contains(dir)) Roots.Add(dir); }
    }

    public static void Reset() { lock (Gate) { Cache.Clear(); Missing.Clear(); } }

    public static int LayerFor(int vel) { return vel >= 92 ? 2 : (vel >= 52 ? 1 : 0); }
    static string LayerName(int i) { return i == 0 ? "soft" : (i == 1 ? "mid" : "loud"); }

    public static string PieceFor(int pitch) {
        switch (pitch) {
            case 35: case 36: return "kick";
            case 38: case 40: return "snare";
            case 39: return "clap";
            case 37: return "rim";
            case 42: case 44: return "chat";
            case 46: return "ohat";
            case 49: case 55: case 57: return "crash";
            case 51: case 53: case 59: return "ride";
            case 41: case 43: return "tomlo";
            case 45: case 47: case 48: case 50: return "tommhi";
            case 54: case 56: case 58: return "perc";
            default:
                if (pitch >= 72) return "perc";
                if (pitch >= 60) return "tommhi";
                return "perc";
        }
    }

    static string FindFile(string kit, string file) {
        foreach (string r in Roots) {
            string a = Path.Combine(r, kit, file + ".wav");
            if (File.Exists(a)) return a;
            string b = Path.Combine(r, "DrumKits", kit, file + ".wav");
            if (File.Exists(b)) return b;
        }
        return null;
    }

    // One shared gain across the three layers keeps the soft / mid / loud dynamics intact while still
    // matching the level of the procedural kits.
    public static float[] Get(string kit, int pitch, int vel) {
        if (!Enabled || String.IsNullOrEmpty(kit)) return null;
        string id = kit + "|" + PieceFor(pitch);
        float[][] layers;
        lock (Gate) {
            if (Cache.TryGetValue(id, out layers)) { }
            else if (Missing.ContainsKey(id)) return null;
            else {
                layers = new float[3][];
                bool ok = true;
                for (int i = 0; i < 3 && ok; i++) {
                    string f = FindFile(kit, PieceFor(pitch) + "-" + LayerName(i));
                    if (f == null) { ok = false; break; }
                    int rate; float[] s = WavIn.ReadMono(f, out rate);
                    if (s == null) { ok = false; break; }
                    s = WavIn.Trim(s);
                    if (s == null) { ok = false; break; }
                    s = WavIn.ExtractHit(s, PieceFor(pitch));
                    if (s == null) { ok = false; break; }
                    if (rate != Dsp.SR) s = WavIn.Resample(s, rate, Dsp.SR);
                    layers[i] = s;
                }
                if (!ok) { Missing[id] = true; return null; }
                float peak = WavIn.Peak(layers[1]);
                if (peak < 1e-4f) peak = WavIn.Peak(layers[2]);
                float g = peak > 1e-4f ? Math.Min(6f, 0.85f / peak) : 1f;
                for (int i = 0; i < 3; i++) for (int k = 0; k < layers[i].Length; k++) layers[i][k] *= g;
                if (Cache.Count > 96) Cache.Clear();
                Cache[id] = layers;
            }
        }
        if (layers == null) return null;
        float[] src = layers[LayerFor(vel)];
        // a little pitch spread so repeated toms / hats do not sound machine-stamped
        double ratio = 1;
        int anchor = 0;
        if (PieceFor(pitch) == "tomlo") anchor = 42; else if (PieceFor(pitch) == "tommhi") anchor = 47;
        else if (PieceFor(pitch) == "chat") anchor = 42;
        if (anchor > 0) ratio = Math.Pow(2, (pitch - anchor) / 12.0 * 0.7);
        if (Math.Abs(ratio - 1) < 0.002) return (float[])src.Clone();
        int n = Math.Max(2, (int)(src.Length / ratio));
        float[] o = new float[n]; int w = 0;
        for (int i = 0; i < n; i++) {
            double pos = i * ratio; int a = (int)pos;
            if (a >= src.Length - 1) break;
            double fr = pos - a;
            o[i] = (float)(src[a] * (1 - fr) + src[a + 1] * fr); w++;
        }
        if (w >= n) return o;
        float[] t = new float[Math.Max(2, w)]; Array.Copy(o, t, t.Length); return t;
    }
}
