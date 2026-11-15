namespace HumbleBakerStudio.Audio;

/// <summary>
/// One side of the live performance mixer. A deck holds a fully rendered project loop (or song),
/// plays it at an adjustable speed, and can change key independently of that speed. Gain, a
/// one-knob filter and a cue flag are also exposed.
/// Thread-safe: the UI writes state while the audio thread renders.
/// </summary>
public sealed class LiveDeck
{
    private readonly object _gate = new();
    private float[] _l = Array.Empty<float>();
    private float[] _r = Array.Empty<float>();
    private double _pos;
    private double _lpL, _lpR;
    private double _lastFilter = double.NaN;
    private double _a = 1;

    // Key-lock is a two-grain granular time-stretcher. The grain origins advance at the
    // requested speed while each grain is read at the requested pitch ratio, so tempo and
    // pitch can move independently (Traktor-style key lock).
    private const int GrainSize = 2048;
    private const int GrainFade = 128;
    private double _grainOriginA, _grainOriginB;
    private double _grainAgeA, _grainAgeB;

    public string Name { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public double Bpm { get; set; } = 120;
    public string KeyText { get; set; } = "";

    public bool Playing { get; set; }
    public bool Loop { get; set; } = true;
    public bool Cue { get; set; }
    public bool Ended { get; private set; }

    /// <summary>Channel gain, 0..1.5.</summary>
    public double Gain { get; set; } = 1;
    /// <summary>Filter: -1 = low-pass down to 200 Hz, 0 = bypass, +1 = high-pass up to 8 kHz.</summary>
    public double Filter { get; set; }

    /// <summary>Playback speed (tempo) multiplier. 1 = original tempo.</summary>
    public double Speed { get; set; } = 1;
    /// <summary>Backwards-compatible alias for <see cref="Speed"/>; Sync() uses it.</summary>
    public double Rate { get => Speed; set => Speed = value; }
    /// <summary>Key/pitch offset in semitones, independent of speed when <see cref="KeyLock"/> is on.</summary>
    public double Pitch { get; set; }
    /// <summary>When on, changing the speed keeps the deck's key unchanged.</summary>
    public bool KeyLock { get; set; }

    public int Frames { get { lock (_gate) return _l.Length; } }
    public float[] GetLeft() { lock (_gate) return _l; }
    public float[] GetRight() { lock (_gate) return _r; }
    public double Seconds => Frames / (double)Dsp.SR;
    public double PositionSeconds { get { lock (_gate) return _pos / Dsp.SR; } }
    public bool HasAudio => Frames > 0;

    /// <summary>Pitch multiplier currently audible, for display.</summary>
    public double AudiblePitchRatio => Math.Pow(2, Math.Clamp(Pitch, -24.0, 24.0) / 12.0) * (KeyLock ? 1 : Math.Clamp(Speed, 0.25, 4.0));

    public void SetBuffer(string name, string path, float[] left, float[] right, double bpm, string keyText, bool songMode)
    {
        lock (_gate)
        {
            Name = name;
            SourcePath = path;
            _l = left ?? Array.Empty<float>();
            _r = right ?? Array.Empty<float>();
            Bpm = bpm <= 0 ? 120 : bpm;
            KeyText = keyText;
            Loop = !songMode;
            _pos = 0;
            Ended = false;
            _lpL = _lpR = 0;
            _lastFilter = double.NaN;
            ResetKeyLockState();
            Playing = false;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _l = Array.Empty<float>();
            _r = Array.Empty<float>();
            _pos = 0;
            Playing = false;
            Ended = false;
            Name = "";
            SourcePath = "";
            ResetKeyLockState();
        }
    }

    public void SeekSeconds(double seconds)
    {
        lock (_gate)
        {
            _pos = Math.Clamp(seconds * Dsp.SR, 0, Math.Max(0, _l.Length - 1));
            ResetKeyLockState();
        }
    }

    public void Restart()
    {
        lock (_gate) { _pos = 0; Ended = false; ResetKeyLockState(); }
    }

    public void Stop()
    {
        lock (_gate) { Playing = false; _pos = 0; Ended = false; ResetKeyLockState(); }
    }

    /// <summary>Renders up to <paramref name="frames"/> frames into the supplied buffers (added, not cleared).</summary>
    public void Render(float[] dstL, float[] dstR, int offset, int frames)
    {
        lock (_gate)
        {
            if (!Playing || _l.Length < 2 || frames <= 0) return;
            int n = _l.Length;
            if (_r.Length < n) n = _r.Length;

            double speed = Math.Clamp(Speed, 0.25, 4.0);
            double pitchRatio = Math.Pow(2, Math.Clamp(Pitch, -24.0, 24.0) / 12.0);
            UpdateFilter();

            if (KeyLock)
                RenderKeyLock(dstL, dstR, offset, frames, n, speed, pitchRatio);
            else
                RenderVarispeed(dstL, dstR, offset, frames, n, speed * pitchRatio);
        }
    }

    private void RenderVarispeed(float[] dstL, float[] dstR, int offset, int frames, int n, double step)
    {
        for (int i = 0; i < frames; i++)
        {
            if (_pos >= n)
            {
                if (Loop) _pos %= n;
                else { Ended = true; Playing = false; break; }
            }

            int i0 = (int)_pos;
            int i1 = i0 + 1;
            if (i1 >= n) i1 = Loop ? 0 : n - 1;
            double frac = _pos - i0;
            double sL = _l[i0] * (1 - frac) + _l[i1] * frac;
            double sR = _r[i0] * (1 - frac) + _r[i1] * frac;

            ApplyFilter(ref sL, ref sR);
            dstL[offset + i] += (float)(sL * Gain);
            dstR[offset + i] += (float)(sR * Gain);
            _pos += step;
        }
    }

    private void RenderKeyLock(float[] dstL, float[] dstR, int offset, int frames, int n, double speed, double pitchRatio)
    {
        if (_grainAgeA <= 0 && _grainAgeB <= 0)
        {
            _grainOriginA = _pos;
            _grainOriginB = _pos - (GrainSize / 2.0) * pitchRatio;
            _grainAgeA = 0;
            _grainAgeB = GrainSize / 2.0;
        }

        for (int i = 0; i < frames; i++)
        {
            if (_pos >= n)
            {
                if (Loop)
                {
                    _pos %= n;
                    _grainOriginA %= n;
                    _grainOriginB %= n;
                }
                else { Ended = true; Playing = false; break; }
            }

            ReadGrain(_l, _grainOriginA, _grainAgeA, pitchRatio, n, out double aL);
            ReadGrain(_r, _grainOriginA, _grainAgeA, pitchRatio, n, out double aR);
            ReadGrain(_l, _grainOriginB, _grainAgeB, pitchRatio, n, out double bL);
            ReadGrain(_r, _grainOriginB, _grainAgeB, pitchRatio, n, out double bR);
            double wA = 0.5 - 0.5 * Math.Cos(2 * Math.PI * _grainAgeA / GrainSize);
            double wB = 0.5 - 0.5 * Math.Cos(2 * Math.PI * _grainAgeB / GrainSize);
            double sum = wA + wB;
            double sL, sR;
            if (sum > 1e-6)
            {
                sL = (aL * wA + bL * wB) / sum;
                sR = (aR * wA + bR * wB) / sum;
            }
            else
            {
                sL = aL;
                sR = aR;
            }

            ApplyFilter(ref sL, ref sR);
            dstL[offset + i] += (float)(sL * Gain);
            dstR[offset + i] += (float)(sR * Gain);

            _pos += speed;
            _grainAgeA += 1;
            _grainAgeB += 1;
            if (_grainAgeA >= GrainSize)
            {
                _grainAgeA -= GrainSize;
                _grainOriginA = _pos;
            }
            if (_grainAgeB >= GrainSize)
            {
                _grainAgeB -= GrainSize;
                _grainOriginB = _pos;
            }
        }
    }

    private void ReadGrain(float[] data, double origin, double age, double pitchRatio, int n, out double value)
    {
        double p = origin + age * pitchRatio;
        if (Loop)
        {
            p %= n;
            if (p < 0) p += n;
        }
        else if (p >= n)
        {
            value = 0;
            return;
        }
        int a = (int)p;
        int b = a + 1;
        if (b >= n) b = Loop ? 0 : n - 1;
        double f = p - a;
        value = data[a] * (1 - f) + data[b] * f;
    }

    private void ApplyFilter(ref double sL, ref double sR)
    {
        if (Filter < -0.001 || Filter > 0.001)
        {
            _lpL += _a * (sL - _lpL);
            _lpR += _a * (sR - _lpR);
            if (Filter < 0) { sL = _lpL; sR = _lpR; }
            else { sL -= _lpL; sR -= _lpR; }
        }
    }

    private void ResetKeyLockState()
    {
        _grainOriginA = _grainOriginB = _pos;
        _grainAgeA = _grainAgeB = 0;
    }

    private void UpdateFilter()
    {
        double f = Math.Clamp(Filter, -1, 1);
        if (Math.Abs(f - _lastFilter) < 0.0001) return;
        _lastFilter = f;
        double cutoff = f < 0
            ? 20000.0 * Math.Pow(200.0 / 20000.0, -f)  // 20 kHz -> 200 Hz
            : f > 0
                ? 20.0 * Math.Pow(8000.0 / 20.0, f)     // 20 Hz -> 8 kHz
                : 20000.0;
        _a = 1 - Math.Exp(-2 * Math.PI * cutoff / Dsp.SR);
    }
}
