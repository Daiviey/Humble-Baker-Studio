namespace HumbleBakerStudio.Audio;


// Designable synth voice. Every property is bounded by [Rng] so AI output and the randomizer stay in a musical, safe range.
public class Patch {
    public Patch() {
        Osc1 = Wave.saw; Level1 = 1; Osc2 = Wave.saw; Level2 = 0.5; Detune2 = 7; Semi2 = 0; Osc3 = Wave.sine; Level3 = 0; Semi3 = -12;
        SubLevel = 0; NoiseLevel = 0; Noise = NoiseColor.white; PulseWidth = 0.5; PulseWidth2 = 0.5; SubShape = Wave.sine; Unison = 1; UnisonSpread = 12; FmAmount = 0; FmRatio = 2;
        Filter = FilterMode.lowpass; Cutoff = 3000; Resonance = 0.3; FilterEnv = 0.5; FilterDecay = 0.25; KeyTrack = 0;
        Attack = 0.005; Decay = 0.3; Sustain = 0.7; Release = 0.2; PitchEnv = 0; PitchDecay = 0.05;
        LfoRate = 4; LfoPitch = 0; LfoFilter = 0; LfoAmp = 0; Fold = 0; Drive = 0; Bits = 16; Gain = 0.8; OutputDrive = 0;
        Harmonics = 16; Tilt = 1; OddEven = 0; Vowel = 0; VowelEnd = 2; TiltEnd = 1.6; Morph = 0;
        Ring = 1.5; Damping = 0.35; Inharmonic = 0.3; Strike = 0.2; SyncRatio = 2;
        FmDecay = 0; FmSustain = 0.25; Fm2Amount = 0; Fm2Ratio = 3; FmFeedback = 0; RingAmount = 0; NoiseRate = 2000;
        FilterDrive = 0; FilterVowel = 0;
        LfoShape = LfoWave.sine; LfoDelay = 0; LfoPwm = 0; LfoFm = 0; LfoMorph = 0;
        Lfo2Rate = 0.3; Lfo2Shape = LfoWave.tri; Lfo2Pitch = 0; Lfo2Filter = 0; Lfo2Fm = 0; Lfo2Morph = 0; Lfo2Pwm = 0; Lfo2Sync = 0; Lfo2Amp = 0;
        ModAttack = 0.01; ModDecay = 0.5; ModPitch = 0; ModFm = 0; ModMorph = 0; ModPwm = 0; ModSync = 0; ModNoise = 0;
        Drift = 0; Variation = 0; VelFilter = 0; VelFm = 0; VelAttack = 0; VelDrive = 0;
    }
    [Category("1 Oscillators"), Description("Oscillator 1 waveform: saw, square, pulse, tri, sine or noise.")] public Wave Osc1 { get; set; }
    [Category("1 Oscillators"), Description("Oscillator 1 level."), Rng(0, 1)] public double Level1 { get; set; }
    [Category("1 Oscillators"), Description("Oscillator 2 waveform.")] public Wave Osc2 { get; set; }
    [Category("1 Oscillators"), Description("Oscillator 2 level."), Rng(0, 1)] public double Level2 { get; set; }
    [Category("1 Oscillators"), Description("Oscillator 2 detune in cents."), Rng(-1200, 1200)] public double Detune2 { get; set; }
    [Category("1 Oscillators"), Description("Oscillator 2 transpose in semitones."), Rng(-36, 36)] public double Semi2 { get; set; }
    [Category("1 Oscillators"), Description("Oscillator 3 waveform.")] public Wave Osc3 { get; set; }
    [Category("1 Oscillators"), Description("Oscillator 3 level."), Rng(0, 1)] public double Level3 { get; set; }
    [Category("1 Oscillators"), Description("Oscillator 3 transpose in semitones."), Rng(-36, 36)] public double Semi3 { get; set; }
    [Category("1 Oscillators"), Description("Sine sub oscillator one octave down."), Rng(0, 1)] public double SubLevel { get; set; }
    [Category("1 Oscillators"), Description("Noise level."), Rng(0, 1)] public double NoiseLevel { get; set; }
    [Category("1 Oscillators"), Description("Noise colour: white, pink or brown.")] public NoiseColor Noise { get; set; }
    [Category("1 Oscillators"), Description("Pulse width for the pulse waveform."), Rng(0.05, 0.95)] public double PulseWidth { get; set; }
    [Category("1 Oscillators"), Description("Pulse width for oscillator 2's pulse waveform."), Rng(0.05, 0.95)] public double PulseWidth2 { get; set; }
    [Category("1 Oscillators"), Description("Sub oscillator waveform: sine, tri, square or saw.")] public Wave SubShape { get; set; }
    [Category("2 Unison and FM"), Description("Number of stacked detuned voices (1 to 7)."), Rng(1, 7)] public double Unison { get; set; }
    [Category("2 Unison and FM"), Description("Unison detune spread in cents."), Rng(0, 100)] public double UnisonSpread { get; set; }
    [Category("2 Unison and FM"), Description("Frequency modulation of oscillator 1 (0 off, 1 bell-like, 5+ metallic)."), Rng(0, 10)] public double FmAmount { get; set; }
    [Category("2 Unison and FM"), Description("FM modulator frequency as a ratio of the note (non-integer = inharmonic)."), Rng(0.25, 16)] public double FmRatio { get; set; }
    [Category("3 Filter"), Description("Filter type: off, lowpass, highpass, bandpass.")] public FilterMode Filter { get; set; }
    [Category("3 Filter"), Description("Base cutoff in Hz."), Rng(30, 16000)] public double Cutoff { get; set; }
    [Category("3 Filter"), Description("Resonance (0.9+ screams, acid style)."), Rng(0, 0.95)] public double Resonance { get; set; }
    [Category("3 Filter"), Description("Filter envelope depth in 5-octave units; negative sweeps down."), Rng(-1, 1)] public double FilterEnv { get; set; }
    [Category("3 Filter"), Description("Filter envelope decay in seconds (long = slow sweep or riser)."), Rng(0.01, 8)] public double FilterDecay { get; set; }
    [Category("3 Filter"), Description("How much the cutoff follows the note pitch."), Rng(0, 1)] public double KeyTrack { get; set; }
    [Category("4 Amp envelope"), Description("Attack seconds."), Rng(0.001, 5)] public double Attack { get; set; }
    [Category("4 Amp envelope"), Description("Decay seconds."), Rng(0.01, 8)] public double Decay { get; set; }
    [Category("4 Amp envelope"), Description("Sustain level."), Rng(0, 1)] public double Sustain { get; set; }
    [Category("4 Amp envelope"), Description("Release seconds."), Rng(0.005, 8)] public double Release { get; set; }
    [Category("5 Pitch and LFO"), Description("Pitch envelope start offset in semitones, decaying to the note (kick drops, laser zaps, risers)."), Rng(-48, 48)] public double PitchEnv { get; set; }
    [Category("5 Pitch and LFO"), Description("Pitch envelope decay seconds."), Rng(0.002, 4)] public double PitchDecay { get; set; }
    [Category("5 Pitch and LFO"), Description("LFO rate in Hz."), Rng(0.05, 40)] public double LfoRate { get; set; }
    [Category("5 Pitch and LFO"), Description("LFO to pitch (vibrato) in cents."), Rng(0, 1200)] public double LfoPitch { get; set; }
    [Category("5 Pitch and LFO"), Description("LFO to cutoff (wobble)."), Rng(0, 1)] public double LfoFilter { get; set; }
    [Category("5 Pitch and LFO"), Description("LFO to volume (tremolo)."), Rng(0, 1)] public double LfoAmp { get; set; }
    [Category("6 Distortion"), Description("Wavefolder amount."), Rng(0, 1)] public double Fold { get; set; }
    [Category("6 Distortion"), Description("Saturation drive."), Rng(0, 1)] public double Drive { get; set; }
    [Category("6 Distortion"), Description("Bit depth (16 = clean, 4 = crunchy)."), Rng(2, 16)] public double Bits { get; set; }
    [Category("6 Distortion"), Description("Output gain."), Rng(0, 1.5, NoRandom = true)] public double Gain { get; set; }
    [Category("6 Distortion"), Description("Post-synth output drive (0 = clean, 1 = saturated)."), Rng(0, 1)] public double OutputDrive { get; set; }
    [Category("7 Wavetable and vowels"), Description("additive/vowel oscillators: number of harmonics in the stack (more = brighter, buzzier)."), Rng(1, 48)] public double Harmonics { get; set; }
    [Category("7 Wavetable and vowels"), Description("additive: spectral tilt, harmonic k has level k^-Tilt (0 = flat and bright, 1 = saw-like, 2 = soft, 3 = nearly sine)."), Rng(0, 3)] public double Tilt { get; set; }
    [Category("7 Wavetable and vowels"), Description("additive: -1 keeps only even harmonics (octave and thin), 0 all, +1 odd harmonics only (hollow, clarinet, square-like)."), Rng(-1, 1)] public double OddEven { get; set; }
    [Category("7 Wavetable and vowels"), Description("vowel oscillator: vowel position 0=a 1=e 2=i 3=o 4=u (voice formants, choirs, talking pads)."), Rng(0, 4)] public double Vowel { get; set; }
    [Category("7 Wavetable and vowels"), Description("vowel position the Morph control moves to."), Rng(0, 4)] public double VowelEnd { get; set; }
    [Category("7 Wavetable and vowels"), Description("additive: tilt the Morph control moves to."), Rng(0, 3)] public double TiltEnd { get; set; }
    [Category("7 Wavetable and vowels"), Description("Wavetable morph 0..1 between the start spectrum (Tilt, Vowel) and the end spectrum (TiltEnd, VowelEnd). Modulate it with LfoMorph, Lfo2Morph and ModMorph for evolving, vocal or sweeping timbres."), Rng(0, 1)] public double Morph { get; set; }
    [Category("7 Wavetable and vowels"), Description("sync oscillator: hard-sync ratio (1 = plain saw; 2-8 gives the sweeping, screaming sync-lead sound when modulated)."), Rng(1, 12)] public double SyncRatio { get; set; }
    [Category("8 Physical models"), Description("pluck/bell oscillators: ring time in seconds (how long the string or metal keeps sounding)."), Rng(0.05, 10)] public double Ring { get; set; }
    [Category("8 Physical models"), Description("pluck/bell: damping (0 = bright and sustained, 1 = muted and dull). Wood and felt are high, glass and steel strings are low."), Rng(0, 1)] public double Damping { get; set; }
    [Category("8 Physical models"), Description("bell: inharmonicity (0 = harmonic string, 0.3 = stiff piano string, 0.5 = marimba or glockenspiel bar, 1 = clangorous church bell)."), Rng(0, 1)] public double Inharmonic { get; set; }
    [Category("8 Physical models"), Description("pluck/bell: strike or pluck position along the string (0.05 = twangy and nasal, 0.5 = round and hollow)."), Rng(0.02, 0.5)] public double Strike { get; set; }
    [Category("9 FM and ring"), Description("FM index decay in seconds (0 = constant). Short = bright attack that mellows: electric piano, bell, pluck, brass blat."), Rng(0, 8)] public double FmDecay { get; set; }
    [Category("9 FM and ring"), Description("Fraction of the FM index that remains after FmDecay."), Rng(0, 1)] public double FmSustain { get; set; }
    [Category("9 FM and ring"), Description("Second modulator that modulates the first modulator (3-operator FM chain; 0 off)."), Rng(0, 10)] public double Fm2Amount { get; set; }
    [Category("9 FM and ring"), Description("Second modulator frequency ratio."), Rng(0.25, 16)] public double Fm2Ratio { get; set; }
    [Category("9 FM and ring"), Description("FM feedback: noisy, sawtooth-like FM tones (0.3+ metallic, 0.8+ harsh)."), Rng(0, 1)] public double FmFeedback { get; set; }
    [Category("9 FM and ring"), Description("Ring modulation between oscillators 1 and 2 (bells, robots, clangs)."), Rng(0, 1)] public double RingAmount { get; set; }
    [Category("1 Oscillators"), Description("bits noise: sample-and-hold rate in Hz; dust noise: crackles per second."), Rng(20, 20000)] public double NoiseRate { get; set; }
    [Category("3 Filter"), Description("Saturation into the filter (0-1), for gritty resonant filters."), Rng(0, 1)] public double FilterDrive { get; set; }
    [Category("3 Filter"), Description("vowel filter: vowel position 0=a 1=e 2=i 3=o 4=u; cutoff modulation shifts the formants up and down (talking wah)."), Rng(0, 4)] public double FilterVowel { get; set; }
    [Category("10 Modulation"), Description("LFO 1 shape: sine, tri, square, saw, sh (random steps) or smooth (random glides).")] public LfoWave LfoShape { get; set; }
    [Category("10 Modulation"), Description("Seconds before LFO 1 starts to fade in (delayed vibrato on sustained notes)."), Rng(0, 5)] public double LfoDelay { get; set; }
    [Category("10 Modulation"), Description("LFO 1 to pulse width (PWM)."), Rng(0, 1)] public double LfoPwm { get; set; }
    [Category("10 Modulation"), Description("LFO 1 to FM index."), Rng(0, 1)] public double LfoFm { get; set; }
    [Category("10 Modulation"), Description("LFO 1 to wavetable morph."), Rng(0, 1)] public double LfoMorph { get; set; }
    [Category("10 Modulation"), Description("LFO 2 rate in Hz (slow = evolving, fast = ring-like)."), Rng(0.02, 40)] public double Lfo2Rate { get; set; }
    [Category("10 Modulation"), Description("LFO 2 shape.")] public LfoWave Lfo2Shape { get; set; }
    [Category("10 Modulation"), Description("LFO 2 to pitch in cents."), Rng(0, 1200)] public double Lfo2Pitch { get; set; }
    [Category("10 Modulation"), Description("LFO 2 to filter cutoff."), Rng(0, 1)] public double Lfo2Filter { get; set; }
    [Category("10 Modulation"), Description("LFO 2 to FM index."), Rng(0, 1)] public double Lfo2Fm { get; set; }
    [Category("10 Modulation"), Description("LFO 2 to wavetable morph."), Rng(0, 1)] public double Lfo2Morph { get; set; }
    [Category("10 Modulation"), Description("LFO 2 to pulse width."), Rng(0, 1)] public double Lfo2Pwm { get; set; }
    [Category("10 Modulation"), Description("LFO 2 to sync ratio."), Rng(0, 1)] public double Lfo2Sync { get; set; }
    [Category("10 Modulation"), Description("LFO 2 to volume."), Rng(0, 1)] public double Lfo2Amp { get; set; }
    [Category("10 Modulation"), Description("Modulation envelope attack seconds."), Rng(0.001, 5)] public double ModAttack { get; set; }
    [Category("10 Modulation"), Description("Modulation envelope decay seconds (a second envelope for movement: use it for FM, morph, sync, noise and pitch)."), Rng(0.01, 8)] public double ModDecay { get; set; }
    [Category("10 Modulation"), Description("Modulation envelope to pitch in semitones (blips, zaps, scoops)."), Rng(-24, 24)] public double ModPitch { get; set; }
    [Category("10 Modulation"), Description("Modulation envelope to FM index."), Rng(0, 1)] public double ModFm { get; set; }
    [Category("10 Modulation"), Description("Modulation envelope to wavetable morph."), Rng(-1, 1)] public double ModMorph { get; set; }
    [Category("10 Modulation"), Description("Modulation envelope to pulse width."), Rng(-1, 1)] public double ModPwm { get; set; }
    [Category("10 Modulation"), Description("Modulation envelope to sync ratio."), Rng(-1, 1)] public double ModSync { get; set; }
    [Category("10 Modulation"), Description("Modulation envelope to noise level (breath, chiff, bow noise at the start of a note)."), Rng(0, 1)] public double ModNoise { get; set; }
    [Category("11 Expression"), Description("Analog drift: slow random wander of pitch and cutoff so held notes feel alive."), Rng(0, 1)] public double Drift { get; set; }
    [Category("11 Expression"), Description("Per-note variation: every repeated note differs slightly (round robin), so riffs sound played instead of machine-gun."), Rng(0, 1)] public double Variation { get; set; }
    [Category("11 Expression"), Description("Velocity to filter cutoff: hard notes are brighter."), Rng(0, 1)] public double VelFilter { get; set; }
    [Category("11 Expression"), Description("Velocity to FM index."), Rng(0, 1)] public double VelFm { get; set; }
    [Category("11 Expression"), Description("Velocity to attack speed: hard notes start faster."), Rng(0, 1)] public double VelAttack { get; set; }
    [Category("11 Expression"), Description("Velocity to drive: hard notes get dirtier."), Rng(0, 1)] public double VelDrive { get; set; }
    public bool NeedsContext() { return Variation > 0 || VelFilter > 0 || VelFm > 0 || VelAttack > 0 || VelDrive > 0; }
    public Patch Clone() { return (Patch)MemberwiseClone(); }
}
