namespace HumbleBakerStudio.Audio;


public static class Spectral {
    const int SR = Dsp.SR;
    public static double RowToHz(double row, double minHz, double maxHz, int rows) { return minHz * Math.Pow(maxHz / minHz, row / (rows - 1.0)); }
    public static double HzToRow(double hz, double minHz, double maxHz, int rows) { return Math.Log(hz / minHz) / Math.Log(maxHz / minHz) * (rows - 1.0); }

    static readonly int[][] Scales = new int[][] {
        null, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }, new int[] { 0, 2, 4, 5, 7, 9, 11 }, new int[] { 0, 2, 3, 5, 7, 8, 10 },
        new int[] { 0, 3, 5, 7, 10 }, new int[] { 0, 2, 3, 7, 8 }, new int[] { 0, 2, 4, 6, 8, 10 }, new int[] { 0, 2, 3, 5, 6, 8, 9, 11 } };
    public static double SnapHz(double hz, SpecSnap snap, int root) {
        if (snap == SpecSnap.off) return hz;
        int[] sc = Scales[(int)snap]; double midi = 69 + 12 * Math.Log(hz / 440.0, 2); int best = (int)Math.Round(midi); double bd = 1e9;
        for (int m = (int)Math.Round(midi) - 6; m <= (int)Math.Round(midi) + 6; m++) {
            int pc = (((m - root) % 12) + 12) % 12; bool ok = false; foreach (int s in sc) if (s == pc) ok = true;
            if (ok && Math.Abs(m - midi) < bd) { bd = Math.Abs(m - midi); best = m; }
        }
        return 440.0 * Math.Pow(2, (best - 69) / 12.0);
    }

    // Additive resynthesis: each grid row is a sine partial whose loudness follows the picture over time.
    public static float[] Render(SpectralGrid g, SpectralSettings s) {
        int N = (int)(Math.Max(0.5, Math.Min(30, s.Seconds)) * SR), rows = g.Rows, cols = g.Cols;
        float[] o = new float[N]; Random rnd = new Random(7);
        double minHz = Math.Min(s.MinHz, s.MaxHz * 0.5), maxHz = s.MaxHz, colScale = (cols - 1) / (double)Math.Max(1, N - 1), contrast = Math.Max(0.1, s.Contrast);
        float[] env = new float[cols];
        for (int r = 0; r < rows; r++) {
            int src = s.Flip ? rows - 1 - r : r; bool any = false;
            for (int c = 0; c < cols; c++) { float v = g.A[c * rows + src]; env[c] = (float)Math.Pow(Math.Max(0, v), contrast); if (env[c] > 0.003f) any = true; }
            if (!any) continue;
            double hz = SnapHz(RowToHz(r, minHz, maxHz, rows), s.Snap, (int)s.Root);
            if (hz > SR * 0.45) continue;
            double w = 2 * Math.PI * hz / SR, ph = rnd.NextDouble() * 6.2832, jt = 0, jn = 0, jit = s.Noisiness * 0.08;
            for (int n = 0; n < N; n++) {
                double x = (s.Reverse ? (N - 1 - n) : n) * colScale; int c0 = (int)x; int c1 = Math.Min(cols - 1, c0 + 1); double fr = x - c0;
                double a = env[c0] + (env[c1] - env[c0]) * fr;
                if (jit > 0) { int k = n & 63; if (k == 0) { jt = jn; jn = rnd.NextDouble() * 2 - 1; } ph += w * (1 + jit * (jt + (jn - jt) * (k / 64.0))); } else ph += w;
                if (a < 1e-4) continue;
                double v = Math.Sin(ph);
                if (s.Brightness > 0) v += s.Brightness * (0.5 * Math.Sin(2 * ph) + 0.25 * Math.Sin(3 * ph));
                o[n] += (float)(a * v);
            }
        }
        int fade = Math.Min(N / 2, (int)(s.FadeMs * SR / 1000.0));
        for (int i = 0; i < fade; i++) { float k = (float)i / fade; o[i] *= k; o[N - 1 - i] *= k; }
        Dsp.Normalize(o, (float)s.Gain);
        return o;
    }

    static void Fft(double[] re, double[] im) {
        int n = re.Length;
        for (int i = 1, j = 0; i < n; i++) { int bit = n >> 1; for (; (j & bit) != 0; bit >>= 1) j ^= bit; j ^= bit; if (i < j) { double t = re[i]; re[i] = re[j]; re[j] = t; t = im[i]; im[i] = im[j]; im[j] = t; } }
        for (int len = 2; len <= n; len <<= 1) {
            double ang = -2 * Math.PI / len, wr = Math.Cos(ang), wi = Math.Sin(ang);
            for (int i = 0; i < n; i += len) {
                double cr = 1, ci = 0;
                for (int k = 0; k < len / 2; k++) {
                    int a = i + k, b = i + k + len / 2; double xr = re[b] * cr - im[b] * ci, xi = re[b] * ci + im[b] * cr;
                    re[b] = re[a] - xr; im[b] = im[a] - xi; re[a] += xr; im[a] += xi;
                    double ncr = cr * wr - ci * wi; ci = cr * wi + ci * wr; cr = ncr;
                }
            }
        }
    }

    // Spectrogram of a sample into the grid, so you can reshape a sound by painting on it and resynthesise.
    public static void FromSample(SpectralGrid g, float[] s, double minHz, double maxHz) {
        const int FN = 4096; double[] re = new double[FN], im = new double[FN], win = new double[FN];
        for (int i = 0; i < FN; i++) win[i] = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (FN - 1));
        double binHz = (double)SR / FN, gmax = 1e-9;
        for (int c = 0; c < g.Cols; c++) {
            int center = (int)((c + 0.5) / g.Cols * s.Length);
            for (int i = 0; i < FN; i++) { int p = center - FN / 2 + i; re[i] = (p >= 0 && p < s.Length ? s[p] : 0) * win[i]; im[i] = 0; }
            Fft(re, im);
            for (int r = 0; r < g.Rows; r++) {
                double f0 = RowToHz(r - 0.5, minHz, maxHz, g.Rows), f1 = RowToHz(r + 0.5, minHz, maxHz, g.Rows);
                int b0 = Math.Max(1, (int)Math.Floor(f0 / binHz)), b1 = Math.Min(FN / 2 - 1, Math.Max(b0, (int)Math.Ceiling(f1 / binHz))); double m = 0;
                for (int b = b0; b <= b1; b++) m = Math.Max(m, Math.Sqrt(re[b] * re[b] + im[b] * im[b]));
                double db = 20 * Math.Log10(m / (FN / 4.0) + 1e-9); double v = Math.Max(0, Math.Min(1, (db + 75) / 75.0));
                g.A[c * g.Rows + r] = (float)v; if (v > gmax) gmax = v;
            }
        }
        g.Normalize();
    }
}
