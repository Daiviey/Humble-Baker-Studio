namespace HumbleBakerStudio.Audio;


public static class MixEngine {
    static bool Any(string spec) { return string.IsNullOrEmpty(spec) || spec.Trim() == "*" || spec.Trim().ToLowerInvariant() == "all"; }
    public static bool Matches(Track t, string spec) {
        if (Any(spec)) return true;
        string nm = (t.Name ?? "").ToLowerInvariant(), sy = (t.Synth ?? "").ToLowerInvariant();
        foreach (string part in spec.Split(new char[] { ',', ';', '+', '&' })) {
            string s = part.Trim().ToLowerInvariant(); if (s.Length == 0) continue;
            if (s == "*" || s == "all" || nm.Contains(s) || sy == s) return true;
        }
        return false;
    }
    static Track FindSource(List<Track> ts, string spec) {
        if (string.IsNullOrEmpty(spec) || Any(spec)) return null;
        foreach (Track t in ts) if (Matches(t, spec)) return t;
        return null;
    }
    public static bool IsSource(Project p, Track t) {
        if (p.Mix == null) return false;
        foreach (MixRule r in p.Mix) if (r != null && r.Enabled && r.Kind == MixKind.sidechain && FindSource(p.Tracks, r.Source) == t) return true;
        return false;
    }

    static float[] Envelope(float[] sl, float[] sr, double atkMs, double relMs, bool loop) {
        int n = sl.Length; float[] env = new float[n];
        double a = Math.Exp(-1.0 / Math.Max(1, atkMs * Dsp.SR / 1000.0)), r = Math.Exp(-1.0 / Math.Max(1, relMs * Dsp.SR / 1000.0)), e = 0;
        for (int pass = 0; pass < (loop ? 2 : 1); pass++)
            for (int i = 0; i < n; i++) { double x = (Math.Abs(sl[i]) + Math.Abs(sr[i])) * 0.5, c = x > e ? a : r; e = c * e + (1 - c) * x; env[i] = (float)e; }
        return env;
    }

    public static void Apply(Project p, List<Track> ts, List<float[]> TL, List<float[]> TR, List<bool> ghost, bool loop, double spb) {
        if (p.Mix == null) return;
        foreach (MixRule r in p.Mix) {
            if (r == null || !r.Enabled) continue;
            int si = -1;
            if (r.Kind == MixKind.sidechain) {
                Track src = FindSource(p.Tracks, r.Source); if (src == null) continue;
                si = ts.IndexOf(src); if (si < 0) continue;
            }
            float[] env = null; double pk = 0;
            if (si >= 0) { env = Envelope(TL[si], TR[si], r.AttackMs, r.ReleaseMs, loop); foreach (float v in env) pk = Math.Max(pk, v); if (pk < 1e-6) continue; }
            for (int k = 0; k < ts.Count; k++) {
                if (ghost[k] || k == si || !Matches(ts[k], r.Target)) continue;
                float[] L = TL[k], R = TR[k]; int n = L.Length;
                switch (r.Kind) {
                    case MixKind.sidechain:
                        for (int i = 0; i < n; i++) { float g = (float)(1 - r.Depth * Math.Min(1.0, env[i] / (pk * 0.6))); L[i] *= g; R[i] *= g; }
                        break;
                    case MixKind.gain: { float g = (float)Math.Pow(10, r.Db / 20); for (int i = 0; i < n; i++) { L[i] *= g; R[i] *= g; } break; }
                    case MixKind.pan: {
                        double pl = Math.Cos((r.Pan + 1) * Math.PI / 4) / Math.Cos(Math.PI / 4), pr = Math.Sin((r.Pan + 1) * Math.PI / 4) / Math.Sin(Math.PI / 4);
                        for (int i = 0; i < n; i++) { double m = (L[i] + R[i]) * 0.5; L[i] = (float)(m * pl); R[i] = (float)(m * pr); }
                        break; }
                    case MixKind.width:
                        for (int i = 0; i < n; i++) { double m = (L[i] + R[i]) * 0.5, s = (L[i] - R[i]) * 0.5 * r.Width; L[i] = (float)(m + s); R[i] = (float)(m - s); }
                        break;
                    case MixKind.saturate: {
                        double dr = 1 + r.Depth * 8, norm = 1 + (dr - 1) * 0.5;
                        for (int i = 0; i < n; i++) { L[i] = (float)(Math.Tanh(L[i] * dr) / norm); R[i] = (float)(Math.Tanh(R[i] * dr) / norm); }
                        break; }
                    case MixKind.monobass: {
                        Biquad lp = Biquad.LowPass(Dsp.SR, r.FreqHz, 0.707);
                        for (int i = 0; i < n; i++) { double m = (L[i] + R[i]) * 0.5, s = (L[i] - R[i]) * 0.5; s -= lp.Tick(s); L[i] = (float)(m + s); R[i] = (float)(m - s); }
                        break; }
                    default: {
                        Biquad bl, br;
                        double f = Math.Min(r.FreqHz, Dsp.SR * 0.45);
                        if (r.Kind == MixKind.highpass) { bl = Biquad.HighPass(Dsp.SR, f, r.Q); br = Biquad.HighPass(Dsp.SR, f, r.Q); }
                        else if (r.Kind == MixKind.lowpass) { bl = Biquad.LowPass(Dsp.SR, f, r.Q); br = Biquad.LowPass(Dsp.SR, f, r.Q); }
                        else if (r.Kind == MixKind.lowshelf) { bl = Biquad.LowShelf(Dsp.SR, f, r.Db, 0.707); br = Biquad.LowShelf(Dsp.SR, f, r.Db, 0.707); }
                        else if (r.Kind == MixKind.highshelf) { bl = Biquad.HighShelf(Dsp.SR, f, r.Db, 0.707); br = Biquad.HighShelf(Dsp.SR, f, r.Db, 0.707); }
                        else { bl = Biquad.Peak(Dsp.SR, f, r.Db, r.Q); br = Biquad.Peak(Dsp.SR, f, r.Db, r.Q); }
                        for (int i = 0; i < n; i++) { L[i] = (float)bl.Tick(L[i]); R[i] = (float)br.Tick(R[i]); }
                        break; }
                }
            }
        }
    }
}
