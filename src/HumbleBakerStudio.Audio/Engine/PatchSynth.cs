namespace HumbleBakerStudio.Audio;


public static class PatchSynth {
    const int SR = Dsp.SR;
    const int TS = 2048;
    static readonly double[][] Formants = { new double[] { 730, 1090, 2440 }, new double[] { 530, 1840, 2480 }, new double[] { 270, 2290, 3010 }, new double[] { 570, 840, 2410 }, new double[] { 300, 870, 2240 } };
    static double FormantAt(double v, int j) {
        v = Math.Max(0, Math.Min(4, v)); int a = (int)Math.Floor(v); if (a >= 4) a = 3;
        double fr = v - a; return Formants[a][j] * (1 - fr) + Formants[a + 1][j] * fr;
    }

    // Deterministic pseudo-random value in -1..1 for step i (used by the sample-and-hold and smooth-random LFOs).
    static double Rnd(int i, int seed) {
        unchecked {
            uint h = (uint)(i * 374761393 + seed * 668265263); h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
            return (h & 0xFFFFFF) / 8388608.0 - 1.0;
        }
    }
    static double Lfo(LfoWave w, double rate, double t, int seed) {
        double x = rate * t, fl = Math.Floor(x), ph = x - fl;
        switch (w) {
            case LfoWave.tri: return ph < 0.25 ? 4 * ph : (ph < 0.75 ? 2 - 4 * ph : 4 * ph - 4);
            case LfoWave.square: return ph < 0.5 ? 1 : -1;
            case LfoWave.saw: return 2 * ph - 1;
            case LfoWave.sh: return Rnd((int)fl, seed);
            case LfoWave.smooth: { double s = ph * ph * (3 - 2 * ph); return Rnd((int)fl, seed) * (1 - s) + Rnd((int)fl + 1, seed) * s; }
            default: return Math.Sin(2 * Math.PI * ph);
        }
    }

    // One cycle of a harmonic stack: an additive tilt/odd-even spectrum, or the formant peaks of a vowel.
    static float[] Table(Wave w, Patch p, double f0, double tilt, double vowel) {
        float[] t = new float[TS];
        int maxH = (int)Math.Max(1, Math.Min(Math.Round(p.Harmonics), Math.Floor(SR * 0.42 / (Math.Max(20, f0) * 1.5))));
        double[] amp = new double[maxH + 1];
        for (int k = 1; k <= maxH; k++) {
            double a;
            if (w == Wave.vowel) {
                double f = k * f0; a = Math.Pow(k, -0.6) * 0.08;
                for (int j = 0; j < 3; j++) { double F = FormantAt(vowel, j), bw = 70 + 0.07 * F, g = j == 0 ? 1.0 : (j == 1 ? 0.55 : 0.3), d = (f - F) / bw; a += g * Math.Exp(-0.5 * d * d); }
            } else {
                a = Math.Pow(k, -tilt);
                if (k % 2 == 0) a *= 1 - Math.Max(0, p.OddEven); else if (k > 1) a *= 1 + Math.Min(0, p.OddEven);
            }
            amp[k] = a;
        }
        double peak = 0;
        for (int i = 0; i < TS; i++) {
            double s = 0, ph = 2 * Math.PI * i / TS;
            for (int k = 1; k <= maxH; k++) s += amp[k] * Math.Sin(k * ph);
            t[i] = (float)s; if (Math.Abs(s) > peak) peak = Math.Abs(s);
        }
        if (peak > 1e-9) for (int i = 0; i < TS; i++) t[i] = (float)(t[i] / peak);
        return t;
    }
    static double Tab(float[] t, double ph) {
        double x = (ph - Math.Floor(ph)) * TS; int i = (int)x; double f = x - i;
        return t[i & (TS - 1)] * (1 - f) + t[(i + 1) & (TS - 1)] * f;
    }

    // Karplus-Strong plucked string: a noise burst circulating in a damped delay line tuned to the note.
    static float[] Pluck(Patch p, double f0, int n, Random rnd) {
        double N = SR / Math.Max(20, f0), d = p.Damping, nc = Math.Max(3.0, N - 0.5 * d), g = Math.Pow(10, -3.0 * nc / (SR * Math.Max(0.05, p.Ring)));
        double[] y = new double[n]; int ex = (int)Math.Min(n, Math.Ceiling(nc)), pos = (int)Math.Max(1, Math.Round(p.Strike * N));
        double[] e = new double[ex]; for (int i = 0; i < ex; i++) e[i] = rnd.NextDouble() * 2 - 1;
        for (int i = ex - 1; i >= pos; i--) e[i] -= e[i - pos] * 0.9;
        double lp = 0, a = Math.Max(0.05, 1 - 0.92 * d);
        for (int i = 0; i < ex; i++) { lp += a * (e[i] - lp); y[i] = lp; }
        for (int i = ex; i < n; i++) {
            double r = i - nc; int j = (int)Math.Floor(r); double fr = r - j;
            double d0 = y[j] * (1 - fr) + y[j + 1 < i ? j + 1 : j] * fr, d1 = (j > 0) ? y[j - 1] * (1 - fr) + y[j] * fr : d0;
            y[i] = g * ((1 - d) * d0 + d * 0.5 * (d0 + d1));
        }
        float[] o = new float[n]; double pk = 1e-9; for (int i = 0; i < n; i++) pk = Math.Max(pk, Math.Abs(y[i]));
        for (int i = 0; i < n; i++) o[i] = (float)(y[i] / pk * 0.9);
        return o;
    }

    // Modal resonator: a bank of decaying partials. Inharmonic 0 = string harmonics, 0.5 = tuned bar (marimba, glockenspiel), 1 = church bell.
    static float[] Modal(Patch p, double f0, int n) {
        double[] bar = { 1, 2.756, 5.404, 8.933, 13.344, 18.64, 24.81, 31.87, 39.8, 48.6, 58.3, 68.9 };
        double[] bell = { 1, 2, 2.4, 3, 4.2, 5.4, 6.8, 8.2, 9.6, 11, 12.5, 14 };
        double io = p.Inharmonic; int K = 12; double[] fr = new double[K], am = new double[K], dc = new double[K];
        for (int k = 0; k < K; k++) {
            double hm = k + 1, r = io <= 0.5 ? hm + (bar[k] - hm) * (io / 0.5) : bar[k] + (bell[k] - bar[k]) * ((io - 0.5) / 0.5);
            fr[k] = f0 * r;
            double pluckPos = Math.Abs(Math.Sin(Math.PI * hm * p.Strike)) * (1 - io) + io;
            am[k] = Math.Pow(hm, -0.75) * (0.25 + 0.75 * pluckPos);
            double T = Math.Max(0.03, p.Ring) / (1 + k * (0.15 + 1.6 * p.Damping)); dc[k] = 6.9 / T;
        }
        double[] y = new double[n]; double pk = 1e-9;
        for (int k = 0; k < K; k++) {
            if (fr[k] > SR * 0.45) continue;
            double w = 2 * Math.PI * fr[k] / SR;
            for (int i = 0; i < n; i++) { double t = (double)i / SR; double ev = Math.Exp(-dc[k] * t); if (ev < 1e-4) break; y[i] += am[k] * ev * Math.Sin(w * i); }
        }
        for (int i = 0; i < n; i++) pk = Math.Max(pk, Math.Abs(y[i]));
        float[] o = new float[n]; for (int i = 0; i < n; i++) o[i] = (float)(y[i] / pk * 0.9);
        return o;
    }

    static double Slot(Wave w, double ph, double pw, Random rnd, float[] tA, float[] tB, double m, double syncR, float[] buf, int i) {
        ph -= Math.Floor(ph);
        switch (w) {
            case Wave.square: return ph < 0.5 ? 1 : -1;
            case Wave.pulse: return ph < pw ? 1 : -1;
            case Wave.sine: return Math.Sin(2 * Math.PI * ph);
            case Wave.tri: return 4 * Math.Abs(ph - 0.5) - 1;
            case Wave.noise: return rnd.NextDouble() * 2 - 1;
            case Wave.additive: case Wave.vowel: { double a = Tab(tA, ph); return (tB == null || m <= 0) ? a : a * (1 - m) + Tab(tB, ph) * m; }
            case Wave.sync: { double x = ph * syncR; return 2 * (x - Math.Floor(x)) - 1; }
            case Wave.pluck: case Wave.bell: return (buf != null && i < buf.Length) ? buf[i] : 0;
            default: return 2 * ph - 1;
        }
    }

    public static float[] Render(Patch p, int pitch, double dur) { return Render(p, pitch, dur, 100, 0); }

    // vel (1-127) and variant (round-robin index) only matter for patches with expression settings (Vel*, Variation).
    public static float[] Render(Patch p, int pitch, double dur, int vel, int variant) {
        double vd = Math.Max(-1.5, Math.Min(1.0, (vel - 90) / 37.0));
        double rel = Math.Max(0.005, p.Release), atk = Math.Max(0.001, Math.Min(5, p.Attack * (1 - 0.7 * p.VelAttack * vd))), dec = Math.Max(0.01, p.Decay), sus = p.Sustain;
        Random vrnd = new Random(pitch * 31 + variant * 7919 + 5);
        double vr = p.Variation;
        if (vr > 0) { double sc = 1 + (vrnd.NextDouble() - 0.5) * 0.16 * vr; atk *= sc; dec *= 2 - sc; rel *= sc; }
        int n = (int)((dur + rel) * SR) + 1; if (n > SR * 12) n = SR * 12; if (n < 2) n = 2;
        float[] o = new float[n];
        Random rnd = new Random(pitch * 7919 + 17 + variant * 104729);
        double f0 = 440 * Math.Pow(2, (pitch - 69) / 12.0);
        double noteCents = vr > 0 ? (vrnd.NextDouble() - 0.5) * 2 * vr * 10 : 0, noteOct = vr > 0 ? (vrnd.NextDouble() - 0.5) * 0.6 * vr : 0;
        int uni = Math.Max(1, Math.Min(7, (int)Math.Round(p.Unison)));
        double[] p1 = new double[uni], p2 = new double[uni], p3 = new double[uni], pm = new double[uni], pm3 = new double[uni], fb = new double[uni], det = new double[uni];
        for (int u = 0; u < uni; u++) {
            p1[u] = rnd.NextDouble(); p2[u] = rnd.NextDouble(); p3[u] = rnd.NextDouble(); pm[u] = 0;
            det[u] = Math.Pow(2, (uni == 1 ? 0 : ((double)u / (uni - 1) - 0.5) * 2 * p.UnisonSpread) / 1200.0);
        }
        double psub = 0, lo = 0, bd = 0, lo2 = 0, bd2 = 0, b0 = 0, b1 = 0, b2 = 0, brown = 0, prevW = 0, hold = 0; int holdCnt = 0;
        double[] vlo = new double[3], vbd = new double[3];
        double q = Math.Max(0.05, 1 - Math.Min(0.95, p.Resonance));
        double bitsSteps = Math.Pow(2, Math.Max(2, Math.Min(16, p.Bits)) - 1);
        double pw0 = Math.Max(0.05, Math.Min(0.95, p.PulseWidth));
        double pw20 = Math.Max(0.05, Math.Min(0.95, p.PulseWidth2));
        double norm = 1.0 / Math.Sqrt(uni);
        double s2 = Math.Pow(2, p.Semi2 / 12.0) * Math.Pow(2, p.Detune2 / 1200.0), s3 = Math.Pow(2, p.Semi3 / 12.0);
        double ampAtDur = Adsr(Math.Min(dur, 1e9), dur, atk, dec, sus);
        double fNote = f0 * Math.Pow(2, noteCents / 1200.0);
        // per-oscillator tables and physical-model buffers
        Wave[] ow = { p.Osc1, p.Osc2, p.Osc3 }; double[] ol = { p.Level1, p.Level2, p.Level3 }, of = { 1, s2, s3 };
        float[][] tA = new float[3][], tB = new float[3][], pb = new float[3][];
        bool morphOn = p.Morph > 0 || p.LfoMorph > 0 || p.Lfo2Morph > 0 || p.ModMorph != 0;
        for (int s = 0; s < 3; s++) {
            if (ol[s] <= 0) continue;
            double fs = fNote * of[s];
            if (ow[s] == Wave.additive || ow[s] == Wave.vowel) {
                tA[s] = Table(ow[s], p, fs, p.Tilt, p.Vowel);
                if (morphOn) tB[s] = Table(ow[s], p, fs, p.TiltEnd, p.VowelEnd);
            } else if (ow[s] == Wave.pluck) pb[s] = Pluck(p, fs, n, rnd);
            else if (ow[s] == Wave.bell) pb[s] = Modal(p, fs, n);
        }
        // slow random drift (pitch and cutoff), 50 control points per second
        double[] drift = null;
        if (p.Drift > 0) { drift = new double[n / (SR / 50) + 3]; double dv = 0; for (int k = 0; k < drift.Length; k++) { dv = dv * 0.96 + (vrnd.NextDouble() - 0.5) * 0.4; drift[k] = dv; } }
        double[] comb = p.Filter == FilterMode.comb ? new double[SR / 20 + 8] : null; int cw = 0;
        int lseed1 = pitch * 13 + variant + 1, lseed2 = pitch * 17 + variant + 2;
        bool anyFm = p.FmAmount > 0 || p.LfoFm > 0 || p.Lfo2Fm > 0 || p.ModFm > 0;
        for (int i = 0; i < n; i++) {
            double t = (double)i / SR;
            double lfoG = p.LfoDelay > 0 ? Math.Max(0, Math.Min(1, (t - p.LfoDelay) / 0.5)) : 1;
            double lfo = Lfo(p.LfoShape, p.LfoRate, t, lseed1) * lfoG;
            double lfo2 = (p.Lfo2Pitch > 0 || p.Lfo2Filter > 0 || p.Lfo2Fm > 0 || p.Lfo2Morph > 0 || p.Lfo2Pwm > 0 || p.Lfo2Sync > 0 || p.Lfo2Amp > 0) ? Lfo(p.Lfo2Shape, p.Lfo2Rate, t, lseed2) : 0;
            double me = t < p.ModAttack ? t / p.ModAttack : Math.Exp(-(t - p.ModAttack) / Math.Max(0.01, p.ModDecay));
            double dr = 0; if (drift != null) { double dx = t * 50; int di = (int)dx; double df = dx - di; dr = drift[di] * (1 - df) + drift[di + 1] * df; }
            double fm = Math.Pow(2, (p.PitchEnv * Math.Exp(-t / Math.Max(0.002, p.PitchDecay)) + p.ModPitch * me) / 12.0) * Math.Pow(2, (lfo * p.LfoPitch + lfo2 * p.Lfo2Pitch + dr * p.Drift * 18) / 1200.0);
            double fmE = p.FmDecay > 0 ? p.FmSustain + (1 - p.FmSustain) * Math.Exp(-t / p.FmDecay) : 1;
            double fmIdx = anyFm ? Math.Max(0, p.FmAmount * fmE * (1 + p.VelFm * vd * 0.8) + 4 * p.LfoFm * lfo + 4 * p.Lfo2Fm * lfo2 + 6 * p.ModFm * me) : 0;
            double pw = Math.Max(0.05, Math.Min(0.95, pw0 + 0.4 * (p.LfoPwm * lfo + p.Lfo2Pwm * lfo2 + p.ModPwm * me)));
            double pw2 = Math.Max(0.05, Math.Min(0.95, pw20 + 0.4 * (p.LfoPwm * lfo + p.Lfo2Pwm * lfo2 + p.ModPwm * me)));
            double mo = Math.Max(0, Math.Min(1, p.Morph + 0.5 * p.LfoMorph * lfo + 0.5 * p.Lfo2Morph * lfo2 + p.ModMorph * me));
            double syncR = Math.Max(1, p.SyncRatio + 6 * (p.Lfo2Sync * lfo2 + p.ModSync * me));
            double x = 0;
            for (int u = 0; u < uni; u++) {
                double fu = fNote * fm * det[u];
                double f1 = fu;
                if (fmIdx > 0) {
                    double m3 = 0;
                    if (p.Fm2Amount > 0) { m3 = Math.Sin(2 * Math.PI * pm3[u]) * p.Fm2Amount * fmE; pm3[u] += fu * p.Fm2Ratio / SR; if (pm3[u] >= 1) pm3[u] -= 1; }
                    double sm = Math.Sin(2 * Math.PI * pm[u] + m3 + (p.FmFeedback > 0 ? p.FmFeedback * 2.5 * fb[u] : 0)); fb[u] = sm;
                    f1 = fu * (1 + fmIdx * sm); pm[u] += fu * p.FmRatio / SR; if (pm[u] >= 1) pm[u] -= 1;
                }
                double v = 0, a1 = 0, a2 = 0;
                bool phys1 = ow[0] == Wave.pluck || ow[0] == Wave.bell, phys2 = ow[1] == Wave.pluck || ow[1] == Wave.bell, phys3 = ow[2] == Wave.pluck || ow[2] == Wave.bell;
                if (ol[0] > 0 && (u == 0 || !phys1)) { a1 = Slot(ow[0], p1[u], pw, rnd, tA[0], tB[0], mo, syncR, pb[0], i); v += ol[0] * a1; }
                if (ol[1] > 0 && (u == 0 || !phys2)) { a2 = Slot(ow[1], p2[u], pw2, rnd, tA[1], tB[1], mo, syncR, pb[1], i); v += ol[1] * a2; }
                if (ol[2] > 0 && (u == 0 || !phys3)) v += ol[2] * Slot(ow[2], p3[u], pw, rnd, tA[2], tB[2], mo, syncR, pb[2], i);
                if (p.RingAmount > 0 && ol[0] > 0 && ol[1] > 0) v = v * (1 - 0.7 * p.RingAmount) + p.RingAmount * 1.6 * a1 * a2 * (ol[0] + ol[1]) * 0.5;
                x += v;
                p1[u] += f1 / SR; p1[u] -= Math.Floor(p1[u]);
                p2[u] += fu * s2 / SR; p2[u] -= Math.Floor(p2[u]);
                p3[u] += fu * s3 / SR; p3[u] -= Math.Floor(p3[u]);
            }
            x *= norm * 0.5;
            if (p.SubLevel > 0) {
                double sp = psub * 2 * Math.PI;
                double sv = p.SubShape == Wave.square ? Math.Sign(Math.Sin(sp)) : (p.SubShape == Wave.tri ? (2 / Math.PI) * Math.Asin(Math.Sin(sp)) : (p.SubShape == Wave.saw ? 2 * (psub - Math.Floor(psub + 0.5)) : Math.Sin(sp)));
                x += p.SubLevel * 0.6 * sv; psub += fNote * fm * 0.5 / SR; psub -= Math.Floor(psub);
            }
            double nl = p.NoiseLevel + p.ModNoise * me;
            if (nl > 0) {
                double w = rnd.NextDouble() * 2 - 1, nz;
                if (p.Noise == NoiseColor.pink) { b0 = 0.99765 * b0 + w * 0.0990460; b1 = 0.96300 * b1 + w * 0.2965164; b2 = 0.57000 * b2 + w * 1.0526913; nz = (b0 + b1 + b2 + w * 0.1848) * 0.25; }
                else if (p.Noise == NoiseColor.brown) { brown = (brown + 0.02 * w) / 1.02; nz = brown * 3.5; }
                else if (p.Noise == NoiseColor.blue) { nz = (w - prevW) * 0.7; prevW = w; }
                else if (p.Noise == NoiseColor.dust) { nz = rnd.NextDouble() < p.NoiseRate / SR ? (rnd.NextDouble() < 0.5 ? 1.8 : -1.8) * rnd.NextDouble() : 0; }
                else if (p.Noise == NoiseColor.bits) { if (holdCnt <= 0) { hold = w; holdCnt = Math.Max(1, (int)(SR / Math.Max(20, p.NoiseRate))); } holdCnt--; nz = hold; }
                else nz = w;
                x += nl * 0.5 * nz;
            }
            if (p.Filter != FilterMode.off) {
                double fe = t < 0.001 ? t / 0.001 : Math.Exp(-(t - 0.001) / Math.Max(0.01, p.FilterDecay));
                double oct = p.FilterEnv * 5 * fe + p.LfoFilter * 2 * lfo + p.Lfo2Filter * 2 * lfo2 + p.KeyTrack * (pitch - 60) / 12.0 + p.VelFilter * 2.5 * vd + dr * p.Drift * 0.8 + noteOct;
                double fc = p.Cutoff * Math.Pow(2, oct);
                fc = Math.Max(30, Math.Min(fc, SR / 6.0));
                if (p.FilterDrive > 0) x = Math.Tanh(x * (1 + 6 * p.FilterDrive));
                double ff = 2 * Math.Sin(Math.PI * fc / SR);
                switch (p.Filter) {
                    case FilterMode.comb: {
                        double D = Math.Max(2, Math.Min(comb.Length - 2, SR / fc)); double fbk = Math.Min(0.97, p.Resonance);
                        int rd = cw - (int)Math.Round(D); if (rd < 0) rd += comb.Length;
                        double yv = x + fbk * comb[rd]; comb[cw] = yv; cw++; if (cw >= comb.Length) cw = 0;
                        x = yv * (1 - 0.5 * fbk); break;
                    }
                    case FilterMode.vowel: {
                        double sh = fc / Math.Max(30, p.Cutoff), sum = 0, xin = x;
                        for (int j = 0; j < 3; j++) {
                            double fj = Math.Max(60, Math.Min(SR / 6.0, FormantAt(p.FilterVowel, j) * sh)), fj2 = 2 * Math.Sin(Math.PI * fj / SR);
                            vlo[j] += fj2 * vbd[j]; double hj = xin - vlo[j] - 0.12 * vbd[j]; vbd[j] += fj2 * hj;
                            sum += vbd[j] * (j == 0 ? 1.0 : (j == 1 ? 0.7 : 0.45));
                        }
                        x = sum * 0.5; break;
                    }
                    case FilterMode.lowpass24: {
                        lo += ff * bd; double hi = x - lo - q * bd; bd += ff * hi;
                        double q2 = Math.Max(0.5, q); lo2 += ff * bd2; double hi2 = lo - lo2 - q2 * bd2; bd2 += ff * hi2; x = lo2; break;
                    }
                    default: {
                        lo += ff * bd; double hi = x - lo - q * bd; bd += ff * hi;
                        x = p.Filter == FilterMode.lowpass ? lo : (p.Filter == FilterMode.highpass ? hi : (p.Filter == FilterMode.notch ? lo + hi : bd)); break;
                    }
                }
            }
            double drv = p.Drive + 0.4 * p.VelDrive * vd; if (drv < 0) drv = 0;
            if (drv > 0) x = Math.Tanh(x * (1 + drv * 12)) * 0.6;
            if (p.Fold > 0) x = x * (1 - p.Fold) + p.Fold * Math.Sin(x * (1 + p.Fold * 8) * 1.5707963);
            if (p.Bits < 16) x = Math.Round(x * bitsSteps) / bitsSteps;
            double env;
            if (t <= dur) env = Adsr(t, dur, atk, dec, sus);
            else env = ampAtDur * Math.Max(0, 1 - (t - dur) / rel);
            env *= Math.Max(0, 1 - p.LfoAmp * (0.5 + 0.5 * lfo) - p.Lfo2Amp * (0.5 + 0.5 * lfo2));
            double outv = x * env * p.Gain * 1.6;
            if (p.OutputDrive > 0) outv = Math.Tanh(outv * (1 + 8 * p.OutputDrive));
            if (double.IsNaN(outv) || double.IsInfinity(outv)) { outv = 0; lo = bd = lo2 = bd2 = 0; }
            o[i] = (float)outv;
        }
        return o;
    }
    static double Adsr(double t, double dur, double atk, double dec, double sus) {
        if (t < atk) return t / atk;
        if (t < atk + dec) return 1 - (1 - sus) * (t - atk) / dec;
        return sus;
    }
}
