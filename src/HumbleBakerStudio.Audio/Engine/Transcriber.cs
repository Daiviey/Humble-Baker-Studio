namespace HumbleBakerStudio.Audio;


// Best-effort audio-to-MIDI transcription: a monophonic line (lead/vocals/bass) and a drum kit from onset + pitch
// analysis. Used to turn a song-model render or its stems into editable notes that keep the song's real performance.
public static class Transcriber {
    const int SR = Dsp.SR, N = 1024, HOP = 512;

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

    // Spectral-flux onset strength per frame; frame fr starts at (fr * HOP) samples into x.
    static double[] Flux(float[] x, int start, int length) {
        int frames = Math.Max(0, (length - N) / HOP);
        double[] flux = new double[frames];
        double[] win = new double[N]; for (int i = 0; i < N; i++) win[i] = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (N - 1));
        double[] re = new double[N], im = new double[N], prev = new double[N / 2];
        for (int fr = 0; fr < frames; fr++) {
            int o = start + fr * HOP; double e = 0;
            for (int i = 0; i < N; i++) { double v = x[o + i] * win[i]; re[i] = v; im[i] = 0; e += v * v; }
            if (e < 1e-8) {
                // Silence: decay the remembered spectrum so the next real onset shows up as a sharp increase.
                flux[fr] = 0; for (int k = 1; k < N / 2; k++) prev[k] *= 0.7;
                continue;
            }
            Fft(re, im); double fl = 0;
            for (int k = 1; k < N / 2; k++) { double m = Math.Sqrt(re[k] * re[k] + im[k] * im[k]); fl += Math.Max(0, m - prev[k]); prev[k] = m; }
            flux[fr] = fl;
        }
        return flux;
    }

    // Frame RMS energy (same hop as Flux), used to reject note-offs where energy is falling.
    static double[] FrameEnergy(float[] x) {
        int frames = Math.Max(0, (x.Length - N) / HOP);
        double[] e = new double[frames];
        for (int fr = 0; fr < frames; fr++) {
            int o = fr * HOP; double s = 0;
            for (int i = 0; i < N; i++) { double v = x[o + i]; s += v * v; }
            e[fr] = Math.Sqrt(s / N);
        }
        return e;
    }

    // Public onset list in seconds, used by the sampler's auto-slice feature.
    public static double[] OnsetSeconds(float[] x, double sens) { return OnsetTimes(x, sens).ToArray(); }

    // Peak-picked onset times in seconds (adaptive threshold over a ~0.25 s window; sens 0..1 lowers the threshold).
    static List<double> OnsetTimes(float[] x, double sens) {
        double[] flux = Flux(x, 0, x.Length);
        double[] energy = FrameEnergy(x);
        List<double> times = new List<double>();
        int frames = flux.Length; if (frames < 4) return times;
        double fmax = 0; for (int fr = 0; fr < frames; fr++) fmax = Math.Max(fmax, flux[fr]);
        if (fmax < 1e-6) return times;
        int win = Math.Max(3, (int)(0.25 * SR / HOP)); int last = -1000;
        for (int fr = 0; fr < frames - 1; fr++) {
            int a0 = Math.Max(0, fr - win / 2), a1 = Math.Min(frames - 1, fr + win / 2);
            double mean = 0, sq = 0; for (int k = a0; k <= a1; k++) { mean += flux[k]; sq += flux[k] * flux[k]; }
            int cnt = a1 - a0 + 1; mean /= cnt; double sd = Math.Sqrt(Math.Max(0, sq / cnt - mean * mean));
            double thr = Math.Max(mean + (1.4 - sens) * sd, fmax * 0.08) + 1e-6;
            double left = fr > 0 ? flux[fr - 1] : 0;
            double prevE = fr > 0 ? energy[fr - 1] : 0;
            if (flux[fr] > thr && flux[fr] >= left && flux[fr] > flux[fr + 1] && energy[fr] > prevE && fr - last >= 3) { times.Add(fr * (double)HOP / SR); last = fr; }
        }
        return times;
    }

    static double Quant16(double beats) { return Math.Round(beats * 4.0) / 4.0; }

    // Sums several sample-aligned mono stems into one buffer for cleaner chord/key detection (fast, in C#).
    public static float[] Sum(System.Collections.Generic.List<float[]> stems) {
        int n = 0; foreach (float[] s in stems) if (s != null) n = Math.Max(n, s.Length);
        float[] o = new float[n];
        foreach (float[] s in stems) { if (s == null) continue; int m = Math.Min(n, s.Length); for (int i = 0; i < m; i++) o[i] += s[i]; }
        return o;
    }

    // One note per onset, pitched by autocorrelation and snapped to the scale; contiguous equal notes merge.
    public static List<Note> Monophonic(float[] x, double bpm, int key, string scale, double minHz, double maxHz) {
        List<Note> res = new List<Note>();
        bool[] mask = Scales.Mask(key, scale);
        double spb = 60.0 / Math.Max(20, bpm);
        List<double> ons = OnsetTimes(x, 0.5);
        int prevMidi = -1;
        for (int i = 0; i < ons.Count; i++) {
            double t = ons[i]; int s0 = (int)(t * SR); if (s0 >= x.Length - 1024) break;
            double f0 = Critic.DetectPitch(x, s0, Math.Min((int)(0.4 * SR), x.Length - s0), minHz, maxHz);
            if (f0 <= 0) continue;
            int midi = (int)Math.Round(69 + 12 * Math.Log(f0 / 440.0, 2));
            // Octave-error correction: autocorrelation can land an octave (or two) off; snap large jumps toward
            // the previous note so the line moves by steps/fifths instead of wild leaps.
            if (prevMidi >= 0) { while (midi - prevMidi > 7) midi -= 12; while (prevMidi - midi > 7) midi += 12; }
            midi = Scales.Snap(midi, mask);
            double startBeats = Quant16(t / spb);
            double endSec = (i + 1 < ons.Count) ? ons[i + 1] : t + 0.6;
            double lenBeats = Quant16((endSec - t) / spb); lenBeats = Math.Max(0.25, Math.Min(8.0, lenBeats));
            // Skip exact duplicates (a single onset picked twice); keep genuine repeated notes separate.
            if (res.Count > 0 && Math.Abs(res[res.Count - 1].Start - startBeats) < 0.02) continue;
            res.Add(new Note(midi, startBeats, lenBeats, 100));
            prevMidi = midi;
        }
        return res;
    }

    // Drum kit from the drums stem: classifies each onset into kick/snare/hat/tom/crash/perc and maps to GM pitches.
    public static List<Note> Drums(float[] x, double bpm) {
        List<Note> res = new List<Note>();
        double spb = 60.0 / Math.Max(20, bpm);
        List<double> ons = OnsetTimes(x, 0.75);
        double[] re = new double[N], im = new double[N], win = new double[N];
        for (int i = 0; i < N; i++) win[i] = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (N - 1));
        foreach (double t in ons) {
            int s0 = (int)(t * SR); if (s0 + N > x.Length) break;
            for (int i = 0; i < N; i++) { re[i] = x[s0 + i] * win[i]; im[i] = 0; }
            Fft(re, im);
            double sub = 0, low = 0, lowMid = 0, mid = 0, high = 0, air = 0, logSum = 0, ar = 0; int bins = 0;
            for (int k = 1; k < N / 2; k++) {
                double m = re[k] * re[k] + im[k] * im[k]; double f = k * (double)SR / N;
                if (f < 80) sub += m; else if (f < 150) low += m; else if (f < 300) lowMid += m; else if (f < 2000) mid += m; else if (f < 8000) high += m; else air += m;
                if (f >= 200 && f <= 8000) { double p = m + 1e-12; logSum += Math.Log(p); ar += p; bins++; }
            }
            double flat = bins > 0 ? Math.Exp(logSum / bins) / (ar / bins) : 1;
            double total = sub + low + lowMid + mid + high + air + 1e-9;
            double rSubLow = (sub + low) / total, rLowMid = (lowMid + mid) / total, rLowMidOnly = lowMid / total, rAir = air / total, rHighAir = (high + air) / total;
            int pitch;
            if (rSubLow > 0.38) pitch = 36;                                 // kick: sub + bass dominant
            else if (rAir > 0.45 && flat > 0.35) pitch = 42;                // closed hat: airy noise
            else if (flat > 0.3 && rLowMid > 0.3) pitch = 38;               // snare / clap: noise with a low-mid body
            else if (rHighAir > 0.6 && flat < 0.25) pitch = 49;             // crash / ride: bright, tonal
            else if (rLowMidOnly > 0.25 && flat < 0.35) pitch = 45;         // tom: low-mid tonal
            else pitch = 54;                                                // shaker / perc
            double startBeats = Quant16(t / spb);
            if (res.Count > 0) { Note p = res[res.Count - 1]; if (p.Pitch == pitch && startBeats - (p.Start + p.Len) < 0.02) continue; }
            res.Add(new Note(pitch, startBeats, 0.25, (pitch == 36 || pitch == 38) ? 112 : 92));
        }
        return res;
    }

    // ---- timbre -> patch, time signature and song-structure inference ------------------------------------------------

    static double Clamp(double v, double lo, double hi) { return Math.Max(lo, Math.Min(hi, v)); }

    // RMS envelope (hop HOP), plus the global peak, the rise time to that peak and how much energy the tail holds.
    static void Envelope(float[] x, out double peak, out double attackMs, out double sustain) {
        int n = (x.Length - N) / HOP; if (n < 2) { peak = attackMs = sustain = 0; return; }
        double[] r = new double[n];
        for (int fr = 0; fr < n; fr++) { int o = fr * HOP; double s = 0; for (int i = 0; i < N; i++) { double v = x[o + i]; s += v * v; } r[fr] = Math.Sqrt(s / N); }
        peak = 0; int pi = 0; for (int i = 0; i < n; i++) if (r[i] > peak) { peak = r[i]; pi = i; }
        if (peak < 1e-6) { attackMs = 0; sustain = 0; return; }
        double rise = 0.1 * peak, rise9 = 0.9 * peak; int i0 = -1, i9 = -1;
        for (int i = 0; i <= pi; i++) { if (i0 < 0 && r[i] >= rise) i0 = i; if (r[i] >= rise9) { i9 = i; break; } }
        attackMs = (i0 >= 0 && i9 > i0) ? (i9 - i0) * HOP * 1000.0 / SR : 0;
        double tail = 0; int tc = 0; for (int i = n - Math.Max(1, n / 4); i < n; i++) { tail += r[i]; tc++; }
        sustain = tail / Math.Max(1, tc) / peak;
    }

    // Spectral centroid, noisiness (flatness) and low-frequency weight, averaged over decimated frames.
    static void Spectral(float[] x, out double centroid, out double flatness, out double lowRatio) {
        double cs = 0, fs = 0, ls = 0, w = 0;
        double[] re = new double[N], im = new double[N], win = new double[N];
        for (int i = 0; i < N; i++) win[i] = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (N - 1));
        int step = HOP * 8;
        for (int o = 0; o + N <= x.Length; o += step) {
            double e = 0; for (int i = 0; i < N; i++) { double v = x[o + i] * win[i]; re[i] = v; im[i] = 0; e += v * v; }
            if (e < 1e-6) continue;
            Fft(re, im);
            double sm = 0, sf = 0, logSum = 0, ar = 0, lo = 0; int bins = 0;
            for (int k = 1; k < N / 2; k++) {
                double f = k * (double)SR / N, m = re[k] * re[k] + im[k] * im[k];
                sm += m; sf += m * f; if (f < 200) lo += m;
                if (k >= 4) { double p = m + 1e-12; logSum += Math.Log(p); ar += p; bins++; }
            }
            if (sm > 1e-9) { cs += sf / sm; ls += lo / sm; }
            if (bins > 0) fs += Math.Exp(logSum / bins) / (ar / bins);
            w += 1;
        }
        centroid = w > 0 ? cs / w : 0; flatness = w > 0 ? fs / w : 0; lowRatio = w > 0 ? ls / w : 0;
    }

    // Builds a synth patch that matches the stem's timbre: brightness, attack, sustain, noisiness and low-end weight.
    // Kept conservative so the result stays musical (gentle noise, modest resonance, no harsh static).
    public static Patch PatchForStem(float[] x) {
        Patch p = new Patch();
        double peak, attackMs, sustain; Envelope(x, out peak, out attackMs, out sustain);
        double centroid, flatness, lowRatio; Spectral(x, out centroid, out flatness, out lowRatio);
        double bright = Clamp(centroid / 3500.0, 0, 1);
        bool isBass = lowRatio > 0.45;
        bool isNoisy = flatness > 0.6;
        if (isNoisy) { p.Osc1 = Wave.noise; p.Level1 = 0.8; p.Noise = NoiseColor.pink; p.NoiseLevel = 0.45; p.Osc2 = Wave.sine; p.Level2 = 0.35; }
        else {
            if (bright > 0.65) { p.Osc1 = Wave.saw; p.Level1 = 1; }
            else if (bright > 0.4) { p.Osc1 = Wave.pulse; p.Level1 = 1; p.PulseWidth = 0.45; }
            else { p.Osc1 = Wave.tri; p.Level1 = 1; }
            p.Osc2 = p.Osc1; p.Level2 = 0.4; p.Detune2 = 7;
            if (isBass) { p.SubLevel = 0.45; p.Semi2 = -12; }
            p.NoiseLevel = Clamp((flatness - 0.35) * 0.5, 0, 0.25);
        }
        p.Filter = FilterMode.lowpass;
        p.Cutoff = Clamp(500 + centroid * 1.1, 250, 11000);
        p.Resonance = Clamp(bright * 0.25, 0, 0.4);
        p.KeyTrack = 0.25;
        p.Attack = Clamp(attackMs / 1000.0, 0.001, 2.5);
        p.Decay = isBass ? 0.3 : 0.35;
        p.Sustain = Clamp(sustain, 0.25, 1.0);
        p.Release = 0.25;
        if (attackMs > 120) { p.Unison = 3; p.UnisonSpread = 14; p.Release = 1.6; p.Cutoff = Clamp(p.Cutoff * 0.75, 200, 8000); }
        else if (attackMs < 15 && sustain < 0.4) { p.Sustain = 0; p.Decay = 0.4; p.Release = 0.18; }
        p.Gain = 0.7;
        return p;
    }

    // Estimates beats-per-bar (3, 4 or 6) from the low-frequency (kick/bass) accent pattern; 4 when unclear.
    public static int DetectTimeSig(float[] x, double bpm) {
        double spb = 60.0 / Math.Max(30, Math.Min(300, bpm));
        int nbeats = Math.Min(64, (int)(x.Length / (spb * SR))); if (nbeats < 8) return 4;
        double[] lo = new double[nbeats];
        for (int b = 0; b < nbeats; b++) { int s0 = (int)(b * spb * SR), len = (int)(spb * SR); double e = 0; for (int i = s0; i < Math.Min(x.Length, s0 + len); i++) { double v = x[i]; e += v * v; } lo[b] = e; }
        double sum = 0; for (int i = 0; i < nbeats; i++) sum += lo[i]; if (sum < 1e-9) return 4;
        double mean = sum / nbeats; for (int i = 0; i < nbeats; i++) lo[i] /= mean;
        int best = 4; double bestScore = -1;
        foreach (int L in new int[] { 3, 4, 6 }) {
            double score = -1;
            for (int off = 0; off < L; off++) { double db = 0, rest = 0; int dcn = 0, rn = 0; for (int i = 0; i < nbeats; i++) { if ((i - off) % L == 0) { db += lo[i]; dcn++; } else { rest += lo[i]; rn++; } } if (dcn < 2 || rn < 2) continue; double s = db / dcn - rest / rn; if (s > score) score = s; }
            if (score > bestScore) { bestScore = score; best = L; }
        }
        if (best == 4) return 4;
        double s4 = -1; for (int off = 0; off < 4; off++) { double db = 0, rest = 0; int dcn = 0, rn = 0; for (int i = 0; i < nbeats; i++) { if ((i - off) % 4 == 0) { db += lo[i]; dcn++; } else { rest += lo[i]; rn++; } } double s = dcn > 1 && rn > 1 ? db / dcn - rest / rn : -1; if (s > s4) s4 = s; }
        return bestScore > s4 + 0.15 ? best : 4;
    }

    // Splits the song into named sections (intro/build/drop/breakdown/outro...) by smoothed bar energy.
    public static List<Section> Sections(float[] x, double bpm, int beatsPerBar, int beatUnit) {
        List<Section> res = new List<Section>();
        double barSec = 60.0 / Math.Max(30, Math.Min(300, bpm)) * beatsPerBar * 4.0 / Math.Max(1, beatUnit);
        int nbars = (int)(x.Length / (barSec * SR)); if (nbars < 4) return res;
        double[] e = new double[nbars];
        for (int b = 0; b < nbars; b++) { int s0 = (int)(b * barSec * SR), len = (int)(barSec * SR); double s = 0; for (int i = s0; i < Math.Min(x.Length, s0 + len); i++) { double v = x[i]; s += v * v; } e[b] = s / len; }
        double[] sm = new double[nbars];
        for (int i = 0; i < nbars; i++) { double s = 0; int c = 0; for (int k = i - 1; k <= i + 1; k++) if (k >= 0 && k < nbars) { s += e[k]; c++; } sm[i] = s / c; }
        double mx = 0; for (int i = 0; i < nbars; i++) mx = Math.Max(mx, sm[i]); if (mx < 1e-9) return res;
        for (int i = 0; i < nbars; i++) sm[i] /= mx;
        int[] buck = new int[nbars];
        for (int i = 0; i < nbars; i++) buck[i] = sm[i] < 0.34 ? 0 : (sm[i] > 0.66 ? 2 : 1);
        List<int> starts = new List<int>(); List<int> lens = new List<int>();
        for (int i = 0; i < nbars; ) { int j = i; while (j < nbars && buck[j] == buck[i]) j++; starts.Add(i); lens.Add(j - i); i = j; }
        // merge segments shorter than 2 bars into the neighbour whose energy is closest
        bool changed = true;
        while (changed && starts.Count > 1) {
            changed = false;
            for (int s = 0; s < starts.Count; s++) {
                if (lens[s] >= 2) continue;
                double here = sm[starts[s]];
                double prev = s > 0 ? sm[starts[s] - 1] : -1;
                double next = s + 1 < starts.Count ? sm[starts[s] + lens[s]] : -1;
                if (s > 0 && (next < 0 || Math.Abs(here - prev) <= Math.Abs(here - next))) { lens[s - 1] += lens[s]; }
                else if (s + 1 < starts.Count) { lens[s + 1] += lens[s]; }
                else { lens[s] = 2; }
                if (lens[s] != 2) { starts.RemoveAt(s); lens.RemoveAt(s); }
                changed = true; break;
            }
        }
        int peakBar = 0; for (int i = 0; i < nbars; i++) if (sm[i] > sm[peakBar]) peakBar = i;
        double barBeats = beatsPerBar * 4.0 / Math.Max(1, beatUnit);
        for (int s = 0; s < starts.Count; s++) {
            int a = starts[s], len = lens[s]; double avg = 0; for (int i = a; i < a + len; i++) avg += sm[i]; avg /= len;
            int en = (int)Clamp(1 + avg * 9, 1, 10);
            bool isFirst = s == 0, isLast = s == starts.Count - 1, isPeak = (a <= peakBar && peakBar < a + len);
            string name = isFirst && avg < 0.4 ? "Intro" : isLast && avg < 0.5 ? "Outro" : isPeak ? (avg > 0.6 ? "Drop" : "Chorus") : avg < 0.4 ? "Breakdown" : "Verse";
            res.Add(new Section { Name = name, Beat = a * barBeats, Bars = len, Energy = en });
        }
        return res;
    }
}
