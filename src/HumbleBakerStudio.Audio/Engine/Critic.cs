namespace HumbleBakerStudio.Audio;


public static class Critic {
    const int SR = Dsp.SR, N = 1024;
    static void Fft(double[] re, double[] im) {
        int n = re.Length;
        for (int i = 1, j = 0; i < n; i++) {
            int bit = n >> 1; for (; (j & bit) != 0; bit >>= 1) j ^= bit; j ^= bit;
            if (i < j) { double t = re[i]; re[i] = re[j]; re[j] = t; t = im[i]; im[i] = im[j]; im[j] = t; }
        }
        for (int len = 2; len <= n; len <<= 1) {
            double ang = -2 * Math.PI / len, wr = Math.Cos(ang), wi = Math.Sin(ang);
            for (int i = 0; i < n; i += len) {
                double cr = 1, ci = 0;
                for (int k = 0; k < len / 2; k++) {
                    int a = i + k, b = i + k + len / 2;
                    double xr = re[b] * cr - im[b] * ci, xi = re[b] * ci + im[b] * cr;
                    re[b] = re[a] - xr; im[b] = im[a] - xi; re[a] += xr; im[a] += xi;
                    double ncr = cr * wr - ci * wi; ci = cr * wi + ci * wr; cr = ncr;
                }
            }
        }
    }
    static double Db(double x) { return 20 * Math.Log10(Math.Max(x, 1e-9)); }

    public static AudioFeatures Features(float[] L, float[] R) {
        AudioFeatures f = new AudioFeatures(); int n = Math.Min(L.Length, R.Length); f.Seconds = (double)n / SR;
        double pk = 0, ss = 0, dc = 0; int clip = 0;
        for (int i = 0; i < n; i++) { double a = Math.Abs(L[i]), b = Math.Abs(R[i]); pk = Math.Max(pk, Math.Max(a, b)); ss += (double)L[i] * L[i] + (double)R[i] * R[i]; dc += (L[i] + R[i]) * 0.5; if (a >= 0.999 || b >= 0.999) clip++; }
        f.PeakDb = Db(pk); f.RmsDb = 10 * Math.Log10(Math.Max(ss / Math.Max(1, 2.0 * n), 1e-18)); f.ClipFraction = (double)clip / Math.Max(1, n); f.Dc = dc / Math.Max(1, n);
        int cw = SR * 2, cn = Math.Max(1, n / cw); f.Curve = new double[cn];
        for (int c = 0; c < cn; c++) { double s = 0; int cnt = 0; for (int i = c * cw; i < Math.Min(n, (c + 1) * cw); i++) { double m = (L[i] + R[i]) * 0.5; s += m * m; cnt++; } f.Curve[c] = 10 * Math.Log10(Math.Max(s / Math.Max(1, cnt), 1e-12)); }
        int hop = N / 2, frames = Math.Max(0, (n - N) / hop); if (frames < 4) return f;
        double[] re = new double[N], im = new double[N], win = new double[N], prev = new double[N / 2], flux = new double[frames];
        for (int i = 0; i < N; i++) win[i] = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (N - 1));
        double cenSum = 0, cenW = 0, flatSum = 0; int flatN = 0;
        for (int fr = 0; fr < frames; fr++) {
            int o = fr * hop; double e = 0;
            for (int i = 0; i < N; i++) { double v = (L[o + i] + R[o + i]) * 0.5; re[i] = v * win[i]; im[i] = 0; e += v * v; }
            Fft(re, im); double fl = 0, sm = 0, sf = 0, logSum = 0, ar = 0; int bins = 0;
            for (int k = 1; k < N / 2; k++) {
                double mag = Math.Sqrt(re[k] * re[k] + im[k] * im[k]); if (k < 200) fl += Math.Max(0, mag - prev[k]); prev[k] = mag;
                sm += mag; sf += mag * k * SR / (double)N;
                if (k >= 4) { double p = mag * mag + 1e-12; logSum += Math.Log(p); ar += p; bins++; }
            }
            flux[fr] = fl;
            if (e / N > 1e-6 && sm > 1e-9) { double w = e; cenSum += (sf / sm) * w; cenW += w; flatSum += Math.Exp(logSum / bins) / (ar / bins); flatN++; }
        }
        f.CentroidHz = cenW > 0 ? cenSum / cenW : 0; f.Flatness = flatN > 0 ? flatSum / flatN : 0;
        int win1s = (int)(SR / (double)hop), onsets = 0, lastPeak = -100;
        for (int fr = 1; fr < frames - 1; fr++) {
            int a0 = Math.Max(0, fr - win1s / 2), a1 = Math.Min(frames - 1, fr + win1s / 2); double mean = 0, sq = 0; for (int k = a0; k <= a1; k++) { mean += flux[k]; sq += flux[k] * flux[k]; }
            int cnt = a1 - a0 + 1; mean /= cnt; double sd = Math.Sqrt(Math.Max(0, sq / cnt - mean * mean));
            if (flux[fr] > mean + 1.1 * sd && flux[fr] >= flux[fr - 1] && flux[fr] > flux[fr + 1] && fr - lastPeak >= 4) { onsets++; lastPeak = fr; }
        }
        f.OnsetsPerSec = onsets / Math.Max(0.1, f.Seconds);
        return f;
    }

    // Fundamental frequency of x[start, start+len) by normalised autocorrelation; 0 if there is no clear pitch.
    public static double DetectPitch(float[] x, int start, int len, double minHz, double maxHz) {
        if (start < 0 || len < 512 || start + len > x.Length) return 0;
        int minLag = Math.Max(2, (int)(SR / maxHz)), maxLag = Math.Min((int)(SR / minHz), len / 2 - 1); if (maxLag <= minLag + 2) return 0;
        double[] ns = new double[maxLag + 2];
        for (int lag = minLag - 1; lag <= maxLag + 1; lag++) {
            double acf = 0, m = 0; for (int i = 0; i + lag < len; i++) { double a = x[start + i], b = x[start + i + lag]; acf += a * b; m += a * a + b * b; }
            ns[lag] = m > 1e-12 ? 2 * acf / m : 0;
        }
        double mx = 0; for (int lag = minLag; lag <= maxLag; lag++) mx = Math.Max(mx, ns[lag]); if (mx < 0.5) return 0;
        for (int lag = minLag; lag <= maxLag; lag++) {
            if (ns[lag] >= 0.85 * mx && ns[lag] >= ns[lag - 1] && ns[lag] >= ns[lag + 1]) {
                double a = ns[lag - 1], b = ns[lag], c = ns[lag + 1], d = a - 2 * b + c, off = Math.Abs(d) > 1e-12 ? 0.5 * (a - c) / d : 0;
                return SR / (lag + off);
            }
        }
        return 0;
    }
}
