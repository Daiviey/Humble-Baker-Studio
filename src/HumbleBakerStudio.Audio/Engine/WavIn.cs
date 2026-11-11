namespace HumbleBakerStudio.Audio;


// ---------------------------------------------------------------------------------------------------------------
// AI one-shot libraries (rendered offline with Stable Audio). Reads 16/24/32-bit PCM or 32-bit float WAV
// into mono float, trims the dead air and hands the samples to the drum engine and the instrument importer.
// ---------------------------------------------------------------------------------------------------------------
public static class WavIn {
    // Mono, 44.1 kHz (Dsp.ReadWav already mixes and resamples); returns null instead of throwing.
    public static float[] ReadMono(string path, out int rate) {
        rate = Dsp.SR;
        try {
            if (!File.Exists(path)) return null;
            float[] s = Dsp.ReadWav(path);
            if (s == null || s.Length < 16) return null;
            return s;
        } catch { return null; }
    }

    public static float Peak(float[] s) { float p = 0; if (s == null) return 0; for (int i = 0; i < s.Length; i++) { float a = Math.Abs(s[i]); if (a > p) p = a; } return p; }

    // Drops the leading/trailing silence a generated one-shot usually has, with short fades so nothing clicks.
    public static float[] Trim(float[] s) {
        if (s == null || s.Length < 64) return s;
        int first = 0, last = s.Length - 1;
        while (first < s.Length && Math.Abs(s[first]) < 0.002f) first++;
        while (last > first && Math.Abs(s[last]) < 0.002f) last--;
        if (last - first < 32) return null;
        first = Math.Max(0, first - (int)(Dsp.SR * 0.003));
        last = Math.Min(s.Length - 1, last + (int)(Dsp.SR * 0.02));
        float[] o = new float[last - first + 1];
        Array.Copy(s, first, o, 0, o.Length);
        int fin = Math.Min(o.Length / 4, (int)(Dsp.SR * 0.002));
        for (int i = 0; i < fin; i++) o[i] *= (float)i / fin;
        int fout = Math.Min(o.Length / 2, (int)(Dsp.SR * 0.02));
        for (int i = 0; i < fout; i++) o[o.Length - 1 - i] *= (float)i / fout;
        return o;
    }

    public static float[] Resample(float[] s, int from, int to) {
        if (s == null || s.Length < 2 || from <= 0 || from == to) return s;
        int n = Math.Max(2, (int)((long)s.Length * to / from));
        float[] o = new float[n];
        double step = (double)from / to; int w = 0;
        for (int i = 0; i < n; i++) {
            double pos = i * step; int a = (int)pos;
            if (a >= s.Length - 1) break;
            double fr = pos - a;
            o[i] = (float)(s[a] * (1 - fr) + s[a + 1] * fr); w++;
        }
        if (w >= n) return o;
        float[] t = new float[Math.Max(2, w)]; Array.Copy(o, t, t.Length); return t;
    }

    // Library hits are rendered one file at a time, so their level wanders several dB (a chain rattle peaks at
    // 0.42 where a kick peaks at 1.0). Peak-normalize on load, with a capped boost, so switching instruments
    // does not jump the mix and quiet one-shots stay usable.
    public static float[] NormalizePeak(float[] s, float target) {
        if (s == null || s.Length < 16) return s;
        double pk = 0;
        for (int i = 0; i < s.Length; i++) { double a = Math.Abs(s[i]); if (a > pk) pk = a; }
        if (pk < 0.01 || pk >= target) return s;
        double g = Math.Min(target / pk, 4.0);
        float[] o = new float[s.Length];
        for (int i = 0; i < s.Length; i++) {
            double v = s[i] * g;
            o[i] = (float)(v > 1.0 ? 1.0 : (v < -1.0 ? -1.0 : v));
        }
        return o;
    }

    // Stable Audio likes to continue a beat, so a "one shot" often arrives with a second hit further in.
    // Keep the first transient, stop before the next one and taper the end, so the sampler gets a real one-shot.
    public static double MaxSeconds(string piece) {
        switch (piece) {
            case "kick": return 0.90;
            case "snare": return 0.55;
            case "clap": return 0.45;
            case "rim": return 0.28;
            case "chat": return 0.20;
            case "ohat": return 0.80;
            case "crash": return 2.20;
            case "ride": return 1.50;
            case "tomlo": return 0.72;
            case "tommhi": return 0.66;
            default: return 0.30;
        }
    }

    public static float[] ExtractHit(float[] s, string piece) { return ExtractHit(s, MaxSeconds(piece)); }

    // Keeps the whole trimmed one-shot (natural tail included) and simply caps the length with a short fade.
    // Used for musical hits, where a transient detector would mistake ensemble beating for a second hit.
    public static float[] TrimTail(float[] s, double maxSeconds) {
        if (s == null || s.Length < 16) return null;
        int n = Math.Min(s.Length, (int)(maxSeconds * Dsp.SR));
        if (n < 16) return null;
        float[] o = new float[n];
        Array.Copy(s, o, n);
        int fade = Math.Min(n, Math.Max(64, (int)(Dsp.SR * 0.04)));
        for (int i = 0; i < fade; i++) {
            double g = 0.5 - 0.5 * Math.Cos(Math.PI * (double)i / fade);
            o[n - 1 - i] *= (float)(1 - g);
        }
        return o;
    }

    // Same isolation pass with an explicit ceiling, used for the drum kits and the foley one-shots.
    // Works on a 5 ms RMS envelope: find the loudest hit, back up to the start of its attack, and only cut
    // before a later hit when there is a real gap (40 ms under 12 % of the peak) followed by a fresh attack.
    // The old sample-peak detector mistook beating and reverb dips for second hits and cut musical tails short.
    public static float[] ExtractHit(float[] s, double maxSeconds) {
        if (s == null || s.Length < 64) return s;
        int win = Math.Max(16, (int)(Dsp.SR * 0.005));
        int frames = s.Length / win;
        if (frames < 4) return null;
        float[] env = new float[frames];
        float peakEnv = 0;
        for (int f = 0; f < frames; f++) {
            float sum = 0; int b = f * win;
            for (int k = 0; k < win; k++) { float v = s[b + k]; sum += v * v; }
            env[f] = (float)Math.Sqrt(sum / win);
            if (env[f] > peakEnv) peakEnv = env[f];
        }
        if (peakEnv < 1e-5f) return null;
        int scanF = Math.Min(frames, (int)(1.2 * Dsp.SR) / win);
        int bestF = 0; float best = 0;
        for (int f = 0; f < scanF; f++) if (env[f] > best) { best = env[f]; bestF = f; }
        int startF = bestF;
        while (startF > 0 && env[startF - 1] > best * 0.12f) startF--;
        int cutF = frames;
        int quiet = 0, quietStart = -1;
        for (int f = bestF + 1; f < frames; f++) {
            if (env[f] < best * 0.12f) {
                if (quiet == 0) quietStart = f;
                quiet++;
            } else {
                if (quiet >= 8 && env[f] > best * 0.5f) { cutF = quietStart; break; }
                quiet = 0; quietStart = -1;
            }
        }
        int startI = startF * win;
        int end = Math.Min(s.Length, startI + (int)(maxSeconds * Dsp.SR));
        int cut = Math.Min(end, cutF * win);
        if (cut - startI < (int)(0.02 * Dsp.SR)) cut = end;
        int n = cut - startI;
        if (n < 16) return null;
        float[] o = new float[n];
        Array.Copy(s, startI, o, 0, n);
        int fade = Math.Max(64, Math.Min(n / 2, (int)(Dsp.SR * 0.03)));
        for (int i = 0; i < fade; i++) {
            double g = 0.5 - 0.5 * Math.Cos(Math.PI * (double)i / fade);
            o[n - 1 - i] *= (float)(1 - g);
        }
        return o;
    }

    // First-hit isolation for foley: recorded object hits often contain two to five knocks or rattles.
    // Cut at the first point where the envelope drops under a quarter of the peak and stays down for
    // three frames, so the sampler gets one clean knock instead of a little rhythm.
    public static float[] ExtractFirstHit(float[] s, double maxSeconds) {
        if (s == null || s.Length < 64) return s;
        int win = Math.Max(16, (int)(Dsp.SR * 0.020));
        int frames = s.Length / win;
        if (frames < 6) return null;
        float[] env = new float[frames];
        for (int f = 0; f < frames; f++) {
            float sum = 0; int b = f * win;
            for (int k = 0; k < win; k++) { float v = s[b + k]; sum += v * v; }
            env[f] = (float)Math.Sqrt(sum / win);
        }
        float best = 0; int bestF = 0;
        int scanF = Math.Min(frames, (int)(1.0 * Dsp.SR) / win);
        for (int f = 0; f < scanF; f++) if (env[f] > best) { best = env[f]; bestF = f; }
        if (best < 1e-5f) return null;
        int startF = bestF;
        while (startF > 0 && env[startF - 1] > best * 0.1f) startF--;
        // A dip only counts as the end of the hit when another loud attack follows it. A single hit
        // simply decays through the same level, so its natural tail survives untouched.
        int lookAhead = Math.Max(4, (int)(0.30 * Dsp.SR) / win);
        int minDip = bestF + Math.Max(6, (int)(0.08 * Dsp.SR) / win);
        int cutF = frames;
        for (int f = minDip; f < frames - 3; f++) {
            if (env[f] > best * 0.30f) continue;
            if (env[f + 1] > best * 0.45f || env[f + 2] > best * 0.45f) continue;
            int lim = Math.Min(frames - 1, f + lookAhead);
            bool reattack = false;
            for (int g = f + 3; g <= lim; g++) { if (env[g] > best * 0.45f) { reattack = true; break; } }
            if (reattack) { cutF = f; break; }
        }
        int startI = startF * win;
        int end = Math.Min(s.Length, startI + (int)(maxSeconds * Dsp.SR));
        int cut = Math.Min(end, cutF * win);
        if (cut - startI < (int)(0.03 * Dsp.SR)) cut = end;
        int n = cut - startI;
        if (n < 16) return null;
        float[] o = new float[n];
        Array.Copy(s, startI, o, 0, n);
        int fade = Math.Max(64, Math.Min(n / 2, (int)(Dsp.SR * 0.025)));
        for (int i = 0; i < fade; i++) {
            double g = 0.5 - 0.5 * Math.Cos(Math.PI * (double)i / fade);
            o[n - 1 - i] *= (float)(1 - g);
        }
        return o;
    }
}
