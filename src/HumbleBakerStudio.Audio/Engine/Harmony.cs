namespace HumbleBakerStudio.Audio;


// Chroma analysis of audio: the key and one chord per bar. Used to turn a song-model render into editable chords.
public static class Harmony {
    const int SR = 44100, N = 8192;
    static readonly string[] Names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    static readonly string[] Suffix = { "", "m", "7", "maj7", "m7", "dim", "sus4" };
    static readonly int[][] Tpl = { new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7, 10 }, new int[] { 0, 4, 7, 11 }, new int[] { 0, 3, 7, 10 }, new int[] { 0, 3, 6 }, new int[] { 0, 5, 7 } };
    static readonly double[] Pen = { 0, 0, 0.05, 0.06, 0.04, 0.08, 0.08 };
    static readonly double[] KsMajor = { 6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88 };
    static readonly double[] KsMinor = { 6.33, 2.68, 3.52, 5.38, 2.60, 3.53, 2.54, 4.75, 3.98, 2.69, 3.34, 3.17 };

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

    // Adds the pitch-class energy (and the low-register energy, for the bass) of x[start, start+len) into chroma and bass.
    static void Accumulate(float[] x, int start, int len, double[] chroma, double[] bass) {
        double[] re = new double[N], im = new double[N], win = new double[N];
        for (int i = 0; i < N; i++) win[i] = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (N - 1));
        int end = Math.Min(x.Length, start + len);
        for (int p = Math.Max(0, start); p < end; p += N) {
            double e = 0;
            for (int i = 0; i < N; i++) { double v = (p + i < end) ? x[p + i] : 0; re[i] = v * win[i]; im[i] = 0; e += v * v; }
            if (e < 1e-6) continue;
            Fft(re, im);
            for (int k = 1; k < N / 2; k++) {
                double f = k * (double)SR / N; if (f < 65 || f > 2100) continue;
                double mag = Math.Sqrt(re[k] * re[k] + im[k] * im[k]);
                int pc = (((int)Math.Round(69 + 12 * Math.Log(f / 440.0, 2)) % 12) + 12) % 12;
                chroma[pc] += mag; if (f < 260) bass[pc] += mag;
            }
        }
    }
    static void Norm(double[] a) { double s = 0; for (int i = 0; i < a.Length; i++) s += a[i]; if (s > 1e-9) for (int i = 0; i < a.Length; i++) a[i] /= s; }

    public static string DetectKey(float[] x, out double confidence) {
        double[] c = new double[12], b = new double[12]; Accumulate(x, 0, x.Length, c, b); Norm(c);
        double best = -2; int bk = 0; bool bm = false;
        for (int minor = 0; minor < 2; minor++) for (int k = 0; k < 12; k++) {
            double[] prof = minor == 1 ? KsMinor : KsMajor; double mc = 1.0 / 12, mp = 0; for (int i = 0; i < 12; i++) mp += prof[i] / 12;
            double num = 0, dc = 0, dp = 0;
            for (int i = 0; i < 12; i++) { double a = c[(i + k) % 12] - mc, p = prof[i] - mp; num += a * p; dc += a * a; dp += p * p; }
            double r = num / Math.Sqrt(dc * dp + 1e-12);
            if (r > best) { best = r; bk = k; bm = minor == 1; }
        }
        confidence = best;
        return Names[bk] + (bm ? " minor" : " major");
    }

    // One chord name per bar (triads, sevenths, dim, sus4) from beat-aligned chroma; the downbeat is assumed at offsetSec.
    public static string[] ChordsPerBar(float[] x, double bpm, int beatsPerBar, int beatUnit, int bars, double offsetSec) {
        double barSec = 60.0 / bpm * beatsPerBar * 4.0 / Math.Max(1, beatUnit);
        string[] res = new string[bars]; string prev = "";
        for (int b = 0; b < bars; b++) {
            int start = (int)((offsetSec + b * barSec) * SR), len = (int)(barSec * SR);
            if (start >= x.Length) { res[b] = prev; continue; }
            double[] c = new double[12], bs = new double[12]; Accumulate(x, start, len, c, bs); Norm(c); Norm(bs);
            double cn = 0; for (int i = 0; i < 12; i++) cn += c[i] * c[i];
            if (cn < 1e-9) { res[b] = prev; continue; }
            double best = -9; string name = prev;
            for (int r = 0; r < 12; r++) for (int t = 0; t < Tpl.Length; t++) {
                double dot = 0, tn = 0;
                foreach (int iv in Tpl[t]) { double w = iv == 0 ? 1.2 : 1.0; dot += c[(r + iv) % 12] * w; tn += w * w; }
                double s = dot / Math.Sqrt(cn * tn) + 0.25 * bs[r] - Pen[t];
                if (s > best) { best = s; name = Names[r] + Suffix[t]; }
            }
            res[b] = name; prev = name;
        }
        return res;
    }
}
