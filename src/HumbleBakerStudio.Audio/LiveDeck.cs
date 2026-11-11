namespace HumbleBakerStudio.Audio;

/// <summary>
/// One side of the live performance mixer. A deck holds a fully rendered project loop (or song),
/// plays it at an adjustable rate, and exposes gain, a one-knob filter and a cue flag.
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
    /// <summary>Playback rate; use <see cref="Bpm"/> ratios for sync.</summary>
    public double Rate { get; set; } = 1;

    public int Frames { get { lock (_gate) return _l.Length; } }
    public float[] GetLeft() { lock (_gate) return _l; }
    public float[] GetRight() { lock (_gate) return _r; }
    public double Seconds => Frames / (double)Dsp.SR;
    public double PositionSeconds { get { lock (_gate) return _pos / Dsp.SR; } }
    public bool HasAudio => Frames > 0;

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
        }
    }

    public void SeekSeconds(double seconds)
    {
        lock (_gate) _pos = Math.Clamp(seconds * Dsp.SR, 0, Math.Max(0, _l.Length - 1));
    }

    public void Restart()
    {
        lock (_gate) { _pos = 0; Ended = false; }
    }

    public void Stop()
    {
        lock (_gate) { Playing = false; _pos = 0; Ended = false; }
    }

    /// <summary>Renders up to <paramref name="frames"/> frames into the supplied buffers (added, not cleared).</summary>
    public void Render(float[] dstL, float[] dstR, int offset, int frames)
    {
        lock (_gate)
        {
            if (!Playing || _l.Length < 2 || frames <= 0) return;
            int n = _l.Length;
            if (_r.Length < n) n = _r.Length;

            UpdateFilter();

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

                if (Filter < -0.001 || Filter > 0.001)
                {
                    _lpL += _a * (sL - _lpL);
                    _lpR += _a * (sR - _lpR);
                    if (Filter < 0) { sL = _lpL; sR = _lpR; }
                    else { sL -= _lpL; sR -= _lpR; }
                }

                dstL[offset + i] += (float)(sL * Gain);
                dstR[offset + i] += (float)(sR * Gain);
                _pos += Rate;
            }
        }
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
