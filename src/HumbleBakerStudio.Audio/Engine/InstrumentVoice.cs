namespace HumbleBakerStudio.Audio;

/// <summary>
/// A small physical/articulation model for sustained and plucked instruments. The built-in
/// "strings", "pluck", "strum" and "hybrid" engines live here so the sampler is not limited to
/// one-shot hits: notes have an attack, a bow/plectrum transient, a body resonance, vibrato,
/// portamento and a real sustain stage that lasts as long as the note does.
/// </summary>
public static class InstrumentVoice
{
    public const int MaxSeconds = 12;

    public static bool IsInstrumentVoice(string? synth) =>
        synth is "strings" or "bowed" or "violin" or "cello" or "pluck" or "strum" or "hybrid";

    public static float[] Render(Track t, int pitch, double dur, int vel, Random rnd, int prevPitch = -1)
    {
        string syn = (t.Synth ?? "").ToLowerInvariant();
        return syn switch
        {
            "pluck" => Pluck(t, pitch, dur, vel, rnd),
            "strum" => Strum(t, pitch, dur, vel, rnd),
            "hybrid" => Hybrid(t, pitch, dur, vel, rnd, prevPitch),
            _ => Bowed(t, pitch, dur, vel, rnd, prevPitch)
        };
    }

    private static double Hz(int midi) => 440.0 * Math.Pow(2, (midi - 69) / 12.0);

    private static int Frames(double dur, double rel)
    {
        int n = (int)((dur + rel + 0.05) * Dsp.SR);
        return Math.Clamp(n, 32, Dsp.SR * MaxSeconds);
    }

    private static double Clamp(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));

    private static double PolyBlep(double phase, double dt)
    {
        if (phase < dt)
        {
            double x = phase / dt;
            return x + x - x * x - 1;
        }
        if (phase > 1 - dt)
        {
            double x = (phase - 1) / dt;
            return x * x + x + x + 1;
        }
        return 0;
    }

    private static double Saw(double phase, double dt)
    {
        double v = 2 * phase - 1;
        return v - PolyBlep(phase, dt);
    }

    private static void BodyFilter(double input, ref double low, ref double band, double cutoff, double q, out double lowOut, out double bandOut)
    {
        double f = 2 * Math.Sin(Math.PI * Clamp(cutoff, 30, Dsp.SR / 6.0) / Dsp.SR);
        low += f * band;
        double high = input - low - q * band;
        band += f * high;
        lowOut = low;
        bandOut = band;
    }

    private static float[] Bowed(Track t, int pitch, double dur, int vel, Random rnd, int prevPitch)
    {
        string syn = (t.Synth ?? "").ToLowerInvariant();
        bool bass = syn is "cello";
        double target = Hz(pitch);
        double start = prevPitch > 0 && t.Glide > 0.001 ? Hz(prevPitch) : target;
        double atk = Math.Max(0.02, t.Atk);
        double rel = Math.Max(0.04, t.Rel);
        int n = Frames(dur, rel);
        float[] o = new float[n];

        int voices = 1 + (int)Math.Round(Clamp(t.Ensemble, 0, 1) * 4);
        double spread = 0.002 + 0.010 * Clamp(t.Ensemble, 0, 1);
        double detune = 0, noiseLp = 0, noiseHp = 0;
        double bodyLow = 0, bodyBand = 0;
        double noiseLp2 = 0, noiseLp3 = 0;
        double vibPhase = rnd.NextDouble() * Math.PI * 2;
        double bright = Clamp(t.Brightness, 0, 1);
        double body = Clamp(t.Body, 0, 1);
        double q = 1.15 - 0.65 * body; // more body = less damping = more resonance
        double bodyCut = bass ? 210 + body * 260 : 300 + body * 340;
        double noiseCut = 900 + bright * 5200;
        double bowPeriod = 1.1 + rnd.NextDouble() * 0.8;
        double[] phases = new double[voices];
        double[] incs = new double[voices];
        for (int v = 0; v < voices; v++)
        {
            phases[v] = rnd.NextDouble();
            double offset = voices == 1 ? 0 : (v / (double)(voices - 1) - 0.5) * 2;
            detune = offset * spread;
            incs[v] = target * (1 + detune) / Dsp.SR;
        }

        double velGain = 0.45 + 0.55 * (vel / 127.0);
        for (int i = 0; i < n; i++)
        {
            double tm = i / (double)Dsp.SR;
            double glide = t.Glide > 0.001 ? Math.Exp(-tm / Math.Max(0.008, t.Glide)) : 0;
            double freq = target * Math.Pow(start / target, glide);

            // Delayed vibrato: players ease into it rather than starting with a wobble.
            double vibDepth = Math.Max(0, t.Vibrato) * Math.Min(1, tm / 0.28);
            double vib = vibDepth * Math.Sin(2 * Math.PI * Math.Max(0.1, t.VibratoRate) * tm + vibPhase);
            double pitchMul = Math.Pow(2, vib / 12.0);

            double x = 0;
            for (int v = 0; v < voices; v++)
            {
                phases[v] += incs[v] * (freq / target) * pitchMul;
                if (phases[v] >= 1) phases[v] -= 1;
                double dt = incs[v] * (freq / target) * pitchMul;
                x += Saw(phases[v], dt);
                // A quiet octave partial gives the section a bit of rosin/edge.
                x += 0.10 * Math.Sin(2 * Math.PI * phases[v] * 2);
            }
            x /= Math.Max(1, voices);

            // Bow contact noise and a slow bow-direction swell.
            double bow = 0.55 + 0.45 * Math.Sin(2 * Math.PI * tm / bowPeriod);
            double white = rnd.NextDouble() * 2 - 1;
            double nc = 1 - Math.Exp(-2 * Math.PI * noiseCut / Dsp.SR);
            noiseLp += nc * (white - noiseLp);
            noiseHp = white - noiseLp;
            x += (noiseLp * 0.12 + noiseHp * 0.035) * bow * (0.4 + 0.6 * velGain);

            // Two body resonances: the low air/wood mode and a brighter bridge mode.
            BodyFilter(x, ref bodyLow, ref bodyBand, bodyCut, q, out double lo1, out double ba1);
            double bodyOut = lo1 * 0.55 + ba1 * 0.22;
            BodyFilter(bodyOut, ref noiseLp2, ref noiseLp3, bass ? 1200 : 2100 + bright * 1400, 1.4, out double lo2, out double ba2);
            x = x * (0.38 + 0.42 * bright) + bodyOut * 0.55 + lo2 * 0.18;

            double env = tm < atk ? tm / atk : 1.0;
            if (tm > atk)
            {
                double decay = Math.Min(1, (tm - atk) / 0.22);
                env *= 1 - 0.16 * decay;
            }
            if (tm > dur) env *= Math.Max(0, 1 - (tm - dur) / rel);
            o[i] = (float)(Math.Tanh(x * 1.25) * env * velGain * 0.55);
            if (tm > dur + rel) break;
        }
        return o;
    }

    private static float[] Pluck(Track t, int pitch, double dur, int vel, Random rnd)
    {
        double rel = Math.Max(0.03, t.Rel);
        int n = Frames(dur, rel);
        float[] o = new float[n];
        double f = Hz(pitch);
        int delay = Math.Max(2, (int)Math.Round(Dsp.SR / f));
        var buf = new double[delay + 2];
        double bright = Clamp(t.Brightness, 0, 1);
        double body = Clamp(t.Body, 0, 1);
        // Pick excitation: a short filtered noise burst plus a little pick click.
        double lp = 0;
        for (int i = 0; i < buf.Length; i++)
        {
            double w = rnd.NextDouble() * 2 - 1;
            double c = 1 - Math.Exp(-2 * Math.PI * (700 + bright * 6000) / Dsp.SR);
            lp += c * (w - lp);
            buf[i] = lp + (i < 3 ? w * 0.6 : 0);
        }
        // Pick-position comb is a cheap but effective way to move the pluck along the string.
        int comb = Math.Max(1, (int)(delay * (0.12 + 0.20 * (1 - body))));
        for (int i = comb; i < buf.Length; i++) buf[i] -= 0.55 * buf[i - comb];

        int idx = 0;
        double damp = 0.9965 - 0.0035 * (1 - body);
        double bodyLow = 0, bodyBand = 0, bodyLow2 = 0, bodyBand2 = 0;
        double velGain = 0.35 + 0.65 * (vel / 127.0);
        double relFade = Math.Max(0.01, rel);
        for (int i = 0; i < n; i++)
        {
            double tm = i / (double)Dsp.SR;
            int nx = (idx + 1) % (buf.Length - 1);
            double y = buf[idx];
            double avg = 0.5 * (buf[idx] + buf[nx]);
            // Fractional body damping and a touch of dispersion.
            buf[idx] = damp * (avg * 0.985 + buf[(idx + 2) % (buf.Length - 1)] * 0.015);
            idx = nx;

            BodyFilter(y, ref bodyLow, ref bodyBand, 180 + body * 420, 1.1, out double lo1, out double ba1);
            BodyFilter(y, ref bodyLow2, ref bodyBand2, 1500 + bright * 3500, 1.8, out double lo2, out double ba2);
            double x = y * (0.65 + 0.25 * bright) + lo1 * 0.45 + ba1 * 0.10 + lo2 * 0.12;

            double env = Math.Min(1, tm / 0.004);
            if (tm > dur) env *= Math.Max(0, 1 - (tm - dur) / relFade);
            o[i] = (float)(Math.Tanh(x * 1.3) * env * velGain * 0.75);
            if (tm > dur + rel) break;
            // Keep the tail alive a little longer than a raw Karplus-Strong string.
            if (Math.Abs(y) < 1e-5 && tm > 0.3) break;
        }
        return o;
    }

    private static float[] Strum(Track t, int pitch, double dur, int vel, Random rnd)
    {
        double rel = Math.Max(0.05, t.Rel);
        int n = Frames(dur, rel);
        int[] intervals = { 0, 7, 12, 16, 19, 24 };
        double spacing = 0.008 + 0.030 * Clamp(t.Ensemble, 0, 1);
        float[] sum = new float[n];
        int strings = 6;
        for (int s = 0; s < strings; s++)
        {
            var part = Pluck(t, pitch + intervals[s], dur, Math.Max(1, vel - s * 2), rnd);
            int offset = (int)(s * spacing * Dsp.SR);
            double gain = 0.65 / (1 + s * 0.10);
            for (int i = 0; i + offset < n && i < part.Length; i++)
                sum[i + offset] += (float)(part[i] * gain);
        }
        return sum;
    }

    private static float[] Hybrid(Track t, int pitch, double dur, int vel, Random rnd, int prevPitch)
    {
        var pluck = Pluck(t, pitch, dur, vel, rnd);
        var bow = Bowed(t, pitch, dur, vel, rnd, prevPitch);
        int n = Math.Max(pluck.Length, bow.Length);
        float[] o = new float[n];
        for (int i = 0; i < n; i++)
        {
            double tm = i / (double)Dsp.SR;
            double p = i < pluck.Length ? pluck[i] : 0;
            double b = i < bow.Length ? bow[i] : 0;
            double bowMix = 0.20 + 0.55 * Math.Min(1, tm / 0.30);
            o[i] = (float)Math.Tanh(p * 0.95 + b * bowMix);
        }
        return o;
    }
}
