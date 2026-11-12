namespace HumbleBakerStudio.Audio;

/// <summary>
/// Two-deck live mixer: equal-power crossfader, per-deck gain/filter, and a separate cue
/// (headphone) bus. The audio thread calls <see cref="Fill"/>; the UI only writes plain fields.
/// </summary>
public sealed class LiveMixer
{
    public LiveDeck A { get; } = new();
    public LiveDeck B { get; } = new();

    /// <summary>0 = deck A, 1 = deck B.</summary>
    public double Crossfade { get; set; } = 0.5;
    public double MasterGain { get; set; } = 0.85;
    public double CueLevel { get; set; } = 0.8;
    /// <summary>Headphone cue/mix blend: 0 = cued decks only, 1 = the live master mix.</summary>
    public double CueMonitorMix { get; set; }
    public bool CueToMaster { get; set; }

    public double LastMasterPeak { get; private set; }
    public double LastCuePeak { get; private set; }

    private float[] _aL = Array.Empty<float>(), _aR = Array.Empty<float>();
    private float[] _bL = Array.Empty<float>(), _bR = Array.Empty<float>();

    public void Fill(float[] masterL, float[] masterR, float[] cueL, float[] cueR, int frames)
    {
        Array.Clear(masterL, 0, frames);
        Array.Clear(masterR, 0, frames);
        Array.Clear(cueL, 0, frames);
        Array.Clear(cueR, 0, frames);
        if (frames <= 0) return;

        if (_aL.Length < frames)
        {
            _aL = new float[frames]; _aR = new float[frames];
            _bL = new float[frames]; _bR = new float[frames];
        }
        Array.Clear(_aL, 0, frames); Array.Clear(_aR, 0, frames);
        Array.Clear(_bL, 0, frames); Array.Clear(_bR, 0, frames);

        A.Render(_aL, _aR, 0, frames);
        B.Render(_bL, _bR, 0, frames);

        double x = Math.Clamp(Crossfade, 0, 1);
        double ga = Math.Cos(x * Math.PI / 2);
        double gb = Math.Sin(x * Math.PI / 2);
        double master = Math.Clamp(MasterGain, 0, 2);
        double cueLevel = Math.Clamp(CueLevel, 0, 2);
        double monitorMix = Math.Clamp(CueMonitorMix, 0, 1);
        bool cueA = A.Cue, cueB = B.Cue;

        double peakM = 0, peakC = 0;
        for (int i = 0; i < frames; i++)
        {
            double ml = _aL[i] * ga + _bL[i] * gb;
            double mr = _aR[i] * ga + _bR[i] * gb;
            double cueDeckL = (cueA ? _aL[i] : 0) + (cueB ? _bL[i] : 0);
            double cueDeckR = (cueA ? _aR[i] : 0) + (cueB ? _bR[i] : 0);

            double oL = Math.Clamp(ml * master, -1, 1);
            double oR = Math.Clamp(mr * master, -1, 1);

            // Headphones: blend the cued decks with the live master so the next track can be
            // auditioned against what the room currently hears.
            double headL = (cueDeckL * (1 - monitorMix) + oL * monitorMix) * cueLevel;
            double headR = (cueDeckR * (1 - monitorMix) + oR * monitorMix) * cueLevel;

            // Optional single-output monitoring of the cue source.
            if (CueToMaster)
            {
                oL = Math.Clamp(oL + cueDeckL * cueLevel, -1, 1);
                oR = Math.Clamp(oR + cueDeckR * cueLevel, -1, 1);
            }

            masterL[i] = (float)oL;
            masterR[i] = (float)oR;
            cueL[i] = (float)Math.Clamp(headL, -1, 1);
            cueR[i] = (float)Math.Clamp(headR, -1, 1);

            peakM = Math.Max(peakM, Math.Max(Math.Abs(oL), Math.Abs(oR)));
            peakC = Math.Max(peakC, Math.Max(Math.Abs(headL), Math.Abs(headR)));
        }

        LastMasterPeak = peakM;
        LastCuePeak = peakC;
    }

    /// <summary>Matches a deck's tempo to the other deck (rate = other BPM / own BPM).</summary>
    public void Sync(LiveDeck deck)
    {
        var other = ReferenceEquals(deck, A) ? B : A;
        if (deck.Bpm <= 0 || other.Bpm <= 0) return;
        deck.Rate = Math.Clamp(other.Bpm / deck.Bpm, 0.5, 2.0);
    }

    public void ResetSync(LiveDeck deck) => deck.Rate = 1;
}
