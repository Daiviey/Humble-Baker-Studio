namespace HumbleBakerStudio.Audio;


public static class Mastering {
    const int SR = Dsp.SR;
    static double Db(double x) { return 20 * Math.Log10(Math.Max(x, 1e-9)); }

    public static Analysis Analyze(float[] L, float[] R) {
        Analysis a = new Analysis(); int n = L.Length; a.Seconds = (double)n / SR;
        double pk = 0, ss = 0, sl = 0, sr = 0, slr = 0;
        for (int i = 0; i < n; i++) {
            double l = L[i], r = R[i]; pk = Math.Max(pk, Math.Max(Math.Abs(l), Math.Abs(r)));
            ss += l * l + r * r; sl += l * l; sr += r * r; slr += l * r;
        }
        a.PeakDb = Db(pk); a.RmsDb = 10 * Math.Log10(Math.Max(ss / Math.Max(1, 2.0 * n), 1e-18)); a.Crest = a.PeakDb - a.RmsDb;
        a.Correlation = (sl > 1e-12 && sr > 1e-12) ? slr / Math.Sqrt(sl * sr) : 1;
        a.Lufs = Lufs(L, R);
        double[] edges = new double[] { 20, 60, 250, 500, 2000, 6000, 20000 };
        float[] mono = new float[n]; for (int i = 0; i < n; i++) mono[i] = (L[i] + R[i]) * 0.5f;
        for (int b = 0; b < 6; b++) {
            Biquad hp = edges[b] > 20 ? Biquad.HighPass(SR, edges[b], 0.7071) : null, lp = edges[b + 1] < 20000 ? Biquad.LowPass(SR, edges[b + 1], 0.7071) : null;
            double e = 0;
            for (int i = 0; i < n; i++) { double x = mono[i]; if (hp != null) x = hp.Tick(x); if (lp != null) x = lp.Tick(x); e += x * x; }
            a.Bands[b] = 10 * Math.Log10(Math.Max(e / Math.Max(1, n), 1e-18));
        }
        return a;
    }

    // ITU BS.1770 style integrated loudness (K-weighting, 400 ms blocks, absolute and relative gates).
    public static double Lufs(float[] L, float[] R) {
        int n = L.Length; if (n == 0) return -70;
        Biquad[] kl = new Biquad[] { Biquad.HighShelf(SR, 1681.97, 3.9998, 0.7071752), Biquad.HighPass(SR, 38.1355, 0.5003) };
        Biquad[] kr = new Biquad[] { Biquad.HighShelf(SR, 1681.97, 3.9998, 0.7071752), Biquad.HighPass(SR, 38.1355, 0.5003) };
        double[] cum = new double[n + 1];
        for (int i = 0; i < n; i++) {
            double l = kl[1].Tick(kl[0].Tick(L[i])), r = kr[1].Tick(kr[0].Tick(R[i]));
            cum[i + 1] = cum[i] + l * l + r * r;
        }
        int blk = (int)(0.4 * SR), hop = (int)(0.1 * SR);
        if (n < blk) return -0.691 + 10 * Math.Log10(Math.Max(cum[n] / n, 1e-12));
        List<double> z = new List<double>();
        for (int s = 0; s + blk <= n; s += hop) z.Add((cum[s + blk] - cum[s]) / blk);
        double sum = 0; int cnt = 0;
        foreach (double v in z) if (-0.691 + 10 * Math.Log10(Math.Max(v, 1e-12)) > -70) { sum += v; cnt++; }
        if (cnt == 0) return -70;
        double thr = -0.691 + 10 * Math.Log10(sum / cnt) - 10; sum = 0; cnt = 0;
        foreach (double v in z) if (-0.691 + 10 * Math.Log10(Math.Max(v, 1e-12)) > thr) { sum += v; cnt++; }
        return cnt == 0 ? -70 : -0.691 + 10 * Math.Log10(sum / cnt);
    }

    public static void Process(float[] L, float[] R, MasterSettings m) {
        int n = L.Length;
        float g = (float)Math.Pow(10, m.InputGain / 20);
        for (int i = 0; i < n; i++) { L[i] *= g; R[i] *= g; }
        for (int ch = 0; ch < 2; ch++) {
            float[] x = ch == 0 ? L : R; List<Biquad> f = new List<Biquad>();
            if (Math.Abs(m.LowShelfDb) > 0.05) f.Add(Biquad.LowShelf(SR, m.LowShelfHz, m.LowShelfDb, 0.7071));
            if (Math.Abs(m.MidDb) > 0.05) f.Add(Biquad.Peak(SR, m.MidHz, m.MidDb, m.MidQ));
            if (Math.Abs(m.HighShelfDb) > 0.05) f.Add(Biquad.HighShelf(SR, Math.Min(m.HighShelfHz, SR * 0.45), m.HighShelfDb, 0.7071));
            if (f.Count == 0) continue;
            for (int i = 0; i < n; i++) { double v = x[i]; for (int k = 0; k < f.Count; k++) v = f[k].Tick(v); x[i] = (float)v; }
        }
        if (m.Warmth > 0.001) {
            double w = m.Warmth, k = 1 + 3 * w;
            for (int i = 0; i < n; i++) { L[i] = (float)(L[i] * (1 - w) + w * Math.Tanh(L[i] * k) / k); R[i] = (float)(R[i] * (1 - w) + w * Math.Tanh(R[i] * k) / k); }
        }
        bool mono = m.BassMonoHz > 20, wide = Math.Abs(m.Width - 1) > 0.01;
        if (mono || wide) {
            Biquad h1 = mono ? Biquad.HighPass(SR, m.BassMonoHz, 0.7071) : null, h2 = mono ? Biquad.HighPass(SR, m.BassMonoHz, 0.7071) : null;
            for (int i = 0; i < n; i++) {
                double mid = (L[i] + R[i]) * 0.5, side = (L[i] - R[i]) * 0.5;
                if (mono) side = h2.Tick(h1.Tick(side));
                side *= m.Width; L[i] = (float)(mid + side); R[i] = (float)(mid - side);
            }
        }
        if (m.CompRatio > 1.01) Compress(L, R, m);
        float og = (float)Math.Pow(10, m.OutputGain / 20);
        if (Math.Abs(og - 1) > 0.001) for (int i = 0; i < n; i++) { L[i] *= og; R[i] *= og; }
        double ceil = Math.Pow(10, m.Ceiling / 20);
        if (m.Limiter) Limit(L, R, ceil, m.LimiterRelease);
        for (int i = 0; i < n; i++) {
            if (L[i] > ceil) L[i] = (float)ceil; else if (L[i] < -ceil) L[i] = (float)-ceil;
            if (R[i] > ceil) R[i] = (float)ceil; else if (R[i] < -ceil) R[i] = (float)-ceil;
        }
    }

    static void Compress(float[] L, float[] R, MasterSettings m) {
        double thr = m.CompThreshold, ratio = m.CompRatio, knee = Math.Max(0.01, m.CompKnee);
        double atk = Math.Exp(-1.0 / (m.CompAttack * 0.001 * SR)), rel = Math.Exp(-1.0 / (m.CompRelease * 0.001 * SR)), env = 0, slope = 1 - 1 / ratio;
        for (int i = 0; i < L.Length; i++) {
            double over = Db(Math.Max(Math.Abs(L[i]), Math.Abs(R[i]))) - thr, gr;
            if (2 * over < -knee) gr = 0; else if (2 * Math.Abs(over) <= knee) { double x = over + knee / 2; gr = slope * x * x / (2 * knee); } else gr = slope * over;
            env = gr > env ? atk * env + (1 - atk) * gr : rel * env + (1 - rel) * gr;
            float gain = (float)Math.Pow(10, -env / 20); L[i] *= gain; R[i] *= gain;
        }
    }

    // Look-ahead brickwall: the gain curve is a moving minimum of the required gain, box-smoothed, so it can never exceed the ceiling.
    static void Limit(float[] L, float[] R, double ceil, double relMs) {
        int n = L.Length, la = Math.Max(2, (int)(0.005 * SR)); if (n == 0) return;
        float[] g = new float[n];
        for (int i = 0; i < n; i++) { double pk = Math.Max(Math.Abs(L[i]), Math.Abs(R[i])); g[i] = pk > ceil ? (float)(ceil / pk) : 1f; }
        float[] gm = new float[n]; int[] dq = new int[n]; int head = 0, tail = 0;
        for (int j = 0; j < n; j++) {
            while (tail > head && g[dq[tail - 1]] >= g[j]) tail--;
            dq[tail++] = j;
            while (dq[head] <= j - la) head++;
            int idx = j - la + 1; if (idx >= 0) gm[idx] = g[dq[head]];
        }
        float run = 1f;
        for (int idx = n - 1; idx >= Math.Max(0, n - la + 1); idx--) { run = Math.Min(run, g[idx]); gm[idx] = run; }
        double[] cum = new double[n + 1];
        for (int i = 0; i < n; i++) cum[i + 1] = cum[i] + gm[i];
        double rc = 1 - Math.Exp(-1.0 / (Math.Max(5, relMs) * 0.001 * SR)), envg = 1;
        for (int i = 0; i < n; i++) {
            int s = i - la + 1; double sm;
            if (s >= 0) sm = (cum[i + 1] - cum[s]) / la; else sm = (cum[i + 1] + gm[0] * (-s)) / la;
            envg += (1 - envg) * rc; if (envg > sm) envg = sm;
            L[i] = (float)(L[i] * envg); R[i] = (float)(R[i] * envg);
        }
    }

    // Rule-based assistant: measures the mix, sets EQ / compression / stereo, then trims the input gain until the target loudness is reached.
    public static MasterSettings Auto(float[] L, float[] R, double targetLufs) {
        Analysis a = Analyze(L, R); MasterSettings m = new MasterSettings(); m.Enabled = true; m.TargetLufs = targetLufs; m.Ceiling = -1; m.LimiterRelease = 80;
        double bassDev = a.Bands[1] - a.Bands[3] - 4, airDev = a.Bands[5] - a.Bands[3] + 9, mudDev = a.Bands[2] - a.Bands[3] - 1;
        if (Math.Abs(bassDev) > 2) { m.LowShelfDb = Math.Max(-3, Math.Min(3, -bassDev * 0.5)); m.LowShelfHz = 100; }
        if (Math.Abs(airDev) > 2) { m.HighShelfDb = Math.Max(-3, Math.Min(3, -airDev * 0.4)); m.HighShelfHz = 9000; }
        if (mudDev > 3) { m.MidDb = -Math.Min(3, mudDev * 0.5); m.MidHz = 330; m.MidQ = 1; }
        double ratio = a.Crest > 18 ? 3 : a.Crest > 14 ? 2.2 : a.Crest > 10 ? 1.6 : 1.0;
        if (targetLufs >= -10 && ratio > 1) ratio += 0.5;
        m.CompRatio = ratio; m.CompThreshold = Math.Max(-40, Math.Min(0, a.RmsDb + 3)); m.CompAttack = 30; m.CompRelease = 150;
        m.BassMonoHz = 120; m.Warmth = targetLufs >= -12 ? 0.15 : 0.08;
        if (a.Correlation < 0.1) m.Width = 0.9; else if (a.Correlation > 0.95) m.Width = 1.15;
        m.InputGain = Math.Max(-12, Math.Min(30, targetLufs - a.Lufs));
        FitGain(L, R, m, targetLufs);
        return m;
    }

    // Adjusts InputGain so the processed result lands on the target loudness (the limiter keeps peaks under the ceiling).
    public static double FitGain(float[] L, float[] R, MasterSettings m, double targetLufs) {
        double got = 0;
        for (int it = 0; it < 5; it++) {
            float[] cl = (float[])L.Clone(), cr = (float[])R.Clone(); Process(cl, cr, m);
            got = Lufs(cl, cr);
            if (Math.Abs(targetLufs - got) < 0.3) break;
            m.InputGain = Math.Max(-12, Math.Min(30, m.InputGain + (targetLufs - got)));
        }
        return got;
    }
}
