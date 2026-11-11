namespace HumbleBakerStudio.Audio;


// Per-track effect chain. Order: stutter, filter, fold, drive, crush, ring mod, chorus, delay, reverb, gain.
public class Fx {
    public Fx() {
        Filter = FilterMode.off; Cutoff = 4000; Resonance = 0.3; FilterLfoRate = 0; FilterLfoDepth = 0.5; Drive = 0; Fold = 0; Bits = 16; Downsample = 1;
        RingFreq = 200; RingMix = 0; StutterBeats = 0; StutterDiv = 4; StutterChance = 0.3; Chorus = 0; ChorusRate = 0.6; ChorusDepth = 0.5;
        DelayMix = 0; DelayBeats = 0.75; DelayFeedback = 0.4; ReverbMix = 0; ReverbSize = 0.6; Gain = 1;
        PhaserMix = 0; PhaserRate = 0.5; PhaserDepth = 0.6; PhaserStages = 4; FlangerMix = 0; FlangerRate = 0.8; FlangerDepth = 0.6; FlangerFb = 0.4;
        TapeStop = 0; TapeStopRate = 0.5; TapeDelayMix = 0; TapeDelayTime = 280; TapeDelayFb = 0.45; TapeDelayWow = 0.3;
        PlateMix = 0; PlateSize = 0.6; PlateDamping = 0.5; VinylCrackle = 0; TapeHiss = 0; TapeWow = 0;
        CompThresh = -18; CompRatio = 1; CompAttack = 10; CompRelease = 120; LowShelf = 0; HighShelf = 0; MidPeak = 0; Width = 1; PitchShift = 0;
        SidechainSource = ""; SidechainMix = 0; SidechainAttack = 3; SidechainRelease = 160; CompMakeup = 0;
        GateAmount = 0; GateThreshold = -45; GateAttack = 2; GateRelease = 90;
        TransientAttack = 0; TransientSustain = 0; TransientSensitivity = 0.5;
        Saturator = 0; SatBias = 0; SatTone = 0.5; SatMix = 0.5;
        Exciter = 0; ExciterFreq = 4000; ExciterMix = 0.5;
        RectifyMix = 0; RectifyHalf = false; RectifyDrive = 0;
        TremoloMix = 0; TremoloRate = 5; TremoloDepth = 0.5;
        AutoPanMix = 0; AutoPanRate = 0.5; AutoPanDepth = 0.7;
        VibratoMix = 0; VibratoRate = 5; VibratoDepth = 0.3;
        AutoWahMix = 0; AutoWahRate = 1.5; AutoWahDepth = 0.6; AutoWahResonance = 0.6;
        CombMix = 0; CombFreq = 220; CombFeedback = 0.5;
        SpringMix = 0; SpringDecay = 0.6; SpringTone = 0.5;
        ShimmerMix = 0; ShimmerPitch = 12; ShimmerDecay = 0.7;
        HaasMix = 0; HaasDelay = 14;
        LimiterOn = false; LimiterThreshold = -6; LimiterRelease = 100; LimiterCeiling = -1;
        EnsembleMix = 0; EnsembleRate = 0.6; EnsembleDepth = 0.5;
        RotaryMix = 0; RotaryRate = 0.8; RotaryDepth = 0.7;
        OctaverMix = 0;
        HarmonizerMix = 0; HarmonizerSemi = 7;
        ResonatorMix = 0; ResonatorFreq = 220; ResonatorFeedback = 0.6;
        TiltGain = 0; MidGain = 0; SideGain = 0;
        DeEsserAmount = 0; DeEsserFreq = 5000;
        ClipperAmount = 0; ClipperDrive = 0.5;
        AutoFilterMix = 0; AutoFilterSensitivity = 0.6; AutoFilterResonance = 0.5;
        GateVerbMix = 0; GateVerbDecay = 0.6; GateVerbHold = 0.25;
        ReverseVerbMix = 0; ReverseVerbDecay = 0.6;
        MultiTapMix = 0; MultiTapTime = 180; MultiTapFb = 0.4;
        DoublerMix = 0; DoublerDelay = 18;
        FormantMix = 0; FormantVowel = 0; FormantShift = 1;
        MBOn = false; MBLowThresh = -18; MBMidThresh = -18; MBHighThresh = -18; MBRatio = 3; MBMakeup = 0;
        LadderMix = 0; LadderCutoff = 1200; LadderReso = 0.4; LadderDrive = 0.3;
        SubBassMix = 0; SubBassFreq = 60; SubBassDrive = 0.3;
        BassEnhMix = 0; BassEnhFreq = 120; BassEnhAmount = 0.4;
        AirMix = 0; AirFreq = 8000; AirGain = 3;
        TubeDrive = 0.5; TubeBias = 0.2; TubeMix = 0;
        TapeSatDrive = 0.4; TapeSatTone = 0.5; TapeSatMix = 0;
        DiodeDrive = 0.5; DiodeTone = 0.5; DiodeMix = 0;
        FuzzAmount = 0; FuzzGate = 0.2; FuzzMix = 0.5;
        ChipMix = 0; ChipBits = 6; ChipRate = 4;
        FreqShiftHz = 100; FreqShiftMix = 0;
        BeatRepeatMix = 0; BeatRepeatDiv = 4; BeatRepeatChance = 0.3;
        GrainMix = 0; GrainSize = 80; GrainDensity = 20;
        PingPongMix = 0; PingPongTime = 350; PingPongFb = 0.45;
        DubMix = 0; DubTime = 380; DubFb = 0.55; DubTone = 0.5;
        CathedralMix = 0; CathedralSize = 0.8; CathedralDamp = 0.5;
        RoomMix = 0; RoomSize = 0.5; RoomDamp = 0.5;
        AmbienceMix = 0; AmbienceSize = 0.6; AmbienceDamp = 0.5;
        WahPedalMix = 0; WahPedalRate = 2; WahPedalDepth = 0.7; WahPedalQ = 0.6;
        TalkMix = 0; TalkRate = 3; TalkVowel = 0;
        DuckerMix = 0; DuckerThreshold = -30; DuckerDepth = 0.6; DuckerRelease = 150;
        RiderAmount = 0; RiderTarget = -18; RiderSpeed = 0.3;
        StereoWidth = 1; BassMonoFreq = 0;
        HarmTremMix = 0; HarmTremRate = 4; HarmTremDepth = 0.5;
        GlitchMix = 0; GlitchDensity = 0.3; GlitchSeed = 1;
        SweepMix = 0; SweepRate = 0.5; SweepDepth = 0.6; SweepQ = 0.5; SweepCenter = 1200;
        OptoMix = 0; OptoThresh = -18; OptoRatio = 3; OptoRelease = 250;
        FetMix = 0; FetThresh = -20; FetRatio = 4; FetAttack = 2;
        VcaMix = 0; VcaThresh = -16; VcaRatio = 2; VcaRelease = 200;
        ExpMix = 0; ExpThresh = -40; ExpRatio = 2; ExpRange = 24;
        DynEqMix = 0; DynEqFreq = 2500; DynEqThresh = -24; DynEqAmount = 0.5;
        PultecLow = 0; PultecHigh = 0; PultecAtten = 0;
        OverdriveDrive = 0.5; OverdriveTone = 0.5; OverdriveMix = 0;
        MetalDrive = 0.6; MetalMid = 0.3; MetalMix = 0;
        CabDrive = 0.4; CabTone = 0.4; CabMix = 0;
        FlutterMix = 0; FlutterRate = 0.7; FlutterDepth = 0.5;
        VocoderMix = 0; VocoderBands = 8; VocoderRelease = 80; VocoderCarrier = 0;
        DimMix = 0; DimRate = 0.4; DimDepth = 0.6;
        EnvPhaserMix = 0; EnvPhaserThresh = -30; EnvPhaserDepth = 0.7; EnvPhaserRate = 0.5;
        EnvFlangeMix = 0; EnvFlangeThresh = -30; EnvFlangeDepth = 0.7; EnvFlangeFb = 0.4;
        ChopMix = 0; ChopRate = 8; ChopDepth = 1; ChopShape = 0.5;
        HalfSpeedMix = 0; HalfSpeedAmount = 1;
        RevDelayMix = 0; RevDelayTime = 300; RevDelayFb = 0;
        EchoVerbMix = 0; EchoVerbTime = 320; EchoVerbFb = 0.4; EchoVerbSize = 0.6;
        BloomMix = 0; BloomSize = 0.8; BloomThresh = -40; BloomAttack = 0.15;
        FreezeMix = 0; FreezeThreshold = -50; FreezeDecay = 0.8;
        PitchEchoMix = 0; PitchEchoTime = 350; PitchEchoFb = 0.4; PitchEchoSemi = 12;
        BarberMix = 0; BarberRate = 0.3; BarberDepth = 0.8; BarberStages = 4;
        NonLinVerbMix = 0; NonLinVerbSize = 0.7; NonLinVerbDrive = 0.6; NonLinVerbGate = 0.3;
        MSCompMix = 0; MSCompThresh = -20; MSCompRatio = 3; MSCompRelease = 120;
        MBDistMix = 0; MBDistLow = 0.3; MBDistMid = 0.6; MBDistHigh = 0.4;
        SpecGateMix = 0; SpecGateThresh = 0.02; SpecGateBands = 6;
        SpecBlurMix = 0; SpecBlurAmount = 0.5; SpecBlurSpread = 0.6;
        SpecDelayMix = 0; SpecDelayTime = 260; SpecDelayFb = 0.4; SpecDelaySpread = 0.6;
        ConvMix = 0; ConvSize = 0.6; ConvDamp = 0.45; ConvPre = 20;
        TankMix = 0; TankDecay = 0.6; TankTone = 0.5; TankDrip = 0.4;
        TuneMix = 0; TuneSpeed = 0.5; TuneScale = 0; TuneMinHz = 70;
        OctUpMix = 0; OctUpTone = 0.5;
        ChordMix = 0; ChordType = 0; ChordSpread = 0.3;
        UpCompMix = 0; UpCompThresh = -30; UpCompRatio = 2; UpCompRange = 18;
        TranGateMix = 0; TranGateThresh = -28; TranGateHold = 80; TranGateRelease = 120;
        ShuffleMix = 0; ShuffleLow = 0.5; ShuffleHigh = 1.5; ShuffleFreq = 180;
        MonoMix = 0; MonoFreq = 120; MonoWidth = 0.5;
        PhaseMix = 0; PhaseFreq = 800; PhaseAmount = 0.5;
        LoopMix = 0; LoopLength = 500; LoopSpeed = 1; LoopFb = 0.2;
        StutterGateMix = 0; StutterGateRate = 8; StutterGateDiv = 4; StutterGateChance = 0.35;
    }
    [Category("1 Filter"), Description("Filter type: off, lowpass, highpass, bandpass.")] public FilterMode Filter { get; set; }
    [Category("1 Filter"), Description("Cutoff in Hz."), Rng(30, 16000)] public double Cutoff { get; set; }
    [Category("1 Filter"), Description("Resonance."), Rng(0, 0.95)] public double Resonance { get; set; }
    [Category("1 Filter"), Description("Filter LFO rate in Hz (0 = off). Wobble and auto-wah."), Rng(0, 30)] public double FilterLfoRate { get; set; }
    [Category("1 Filter"), Description("Filter LFO depth in octaves-ish."), Rng(0, 1)] public double FilterLfoDepth { get; set; }
    [Category("2 Distortion"), Description("Saturation."), Rng(0, 1)] public double Drive { get; set; }
    [Category("2 Distortion"), Description("Wavefolder."), Rng(0, 1)] public double Fold { get; set; }
    [Category("2 Distortion"), Description("Bit depth (16 = clean)."), Rng(2, 16)] public double Bits { get; set; }
    [Category("2 Distortion"), Description("Sample-rate reduction factor (1 = off, 8 = lo-fi aliasing)."), Rng(1, 32)] public double Downsample { get; set; }
    [Category("2 Distortion"), Description("Ring modulator frequency in Hz."), Rng(0, 4000)] public double RingFreq { get; set; }
    [Category("2 Distortion"), Description("Ring modulator mix."), Rng(0, 1)] public double RingMix { get; set; }
    [Category("3 Glitch"), Description("Stutter block length in beats (0 = off). 0.25 = sixteenth-note stutters."), Rng(0, 4)] public double StutterBeats { get; set; }
    [Category("3 Glitch"), Description("Each stuttered block repeats its first 1/N."), Rng(2, 16)] public double StutterDiv { get; set; }
    [Category("3 Glitch"), Description("Chance that a block stutters."), Rng(0, 1)] public double StutterChance { get; set; }
    [Category("4 Space"), Description("Chorus / flanger mix."), Rng(0, 1)] public double Chorus { get; set; }
    [Category("4 Space"), Description("Chorus rate in Hz."), Rng(0.05, 8)] public double ChorusRate { get; set; }
    [Category("4 Space"), Description("Chorus depth."), Rng(0, 1)] public double ChorusDepth { get; set; }
    [Category("4 Space"), Description("Ping-pong delay mix."), Rng(0, 1)] public double DelayMix { get; set; }
    [Category("4 Space"), Description("Delay time in beats (0.75 = dotted eighth)."), Rng(0.0625, 8)] public double DelayBeats { get; set; }
    [Category("4 Space"), Description("Delay feedback."), Rng(0, 0.95)] public double DelayFeedback { get; set; }
    [Category("4 Space"), Description("Reverb mix."), Rng(0, 1)] public double ReverbMix { get; set; }
    [Category("4 Space"), Description("Reverb size / decay."), Rng(0, 1)] public double ReverbSize { get; set; }
    [Category("4 Space"), Description("Plate reverb mix."), Rng(0, 1)] public double PlateMix { get; set; }
    [Category("4 Space"), Description("Plate reverb size (small room to big hall)."), Rng(0, 1)] public double PlateSize { get; set; }
    [Category("4 Space"), Description("Plate reverb damping (dark to bright)."), Rng(0, 1)] public double PlateDamping { get; set; }
    [Category("6 Modulation"), Description("Phaser mix."), Rng(0, 1)] public double PhaserMix { get; set; }
    [Category("6 Modulation"), Description("Phaser LFO rate in Hz."), Rng(0.01, 8)] public double PhaserRate { get; set; }
    [Category("6 Modulation"), Description("Phaser LFO depth."), Rng(0, 1)] public double PhaserDepth { get; set; }
    [Category("6 Modulation"), Description("Phaser stages (notch count)."), Rng(1, 12)] public double PhaserStages { get; set; }
    [Category("6 Modulation"), Description("Flanger mix."), Rng(0, 1)] public double FlangerMix { get; set; }
    [Category("6 Modulation"), Description("Flanger rate in Hz."), Rng(0.02, 10)] public double FlangerRate { get; set; }
    [Category("6 Modulation"), Description("Flanger depth."), Rng(0, 1)] public double FlangerDepth { get; set; }
    [Category("6 Modulation"), Description("Flanger feedback (jet whoosh)."), Rng(0, 0.95)] public double FlangerFb { get; set; }
    [Category("7 Tape / Vinyl"), Description("Tape stop effect (1 = full stop-and-restart with pitch slam)."), Rng(0, 1)] public double TapeStop { get; set; }
    [Category("7 Tape / Vinyl"), Description("Tape stop time in seconds (how long the stop takes)."), Rng(0.05, 3)] public double TapeStopRate { get; set; }
    [Category("7 Tape / Vinyl"), Description("Tape delay mix."), Rng(0, 1)] public double TapeDelayMix { get; set; }
    [Category("7 Tape / Vinyl"), Description("Tape delay time in milliseconds."), Rng(10, 2000)] public double TapeDelayTime { get; set; }
    [Category("7 Tape / Vinyl"), Description("Tape delay feedback."), Rng(0, 0.95)] public double TapeDelayFb { get; set; }
    [Category("7 Tape / Vinyl"), Description("Tape delay wow/flutter depth (wobble)."), Rng(0, 1)] public double TapeDelayWow { get; set; }
    [Category("7 Tape / Vinyl"), Description("Vinyl crackle amount."), Rng(0, 1)] public double VinylCrackle { get; set; }
    [Category("7 Tape / Vinyl"), Description("Tape hiss amount."), Rng(0, 1)] public double TapeHiss { get; set; }
    [Category("7 Tape / Vinyl"), Description("Tape wow/flutter pitch wobble."), Rng(0, 1)] public double TapeWow { get; set; }
    [Category("8 Mastering"), Description("Compressor threshold in dB (higher = gentler)."), Rng(-40, 0)] public double CompThresh { get; set; }
    [Category("8 Mastering"), Description("Compressor ratio (1 = off)."), Rng(1, 12)] public double CompRatio { get; set; }
    [Category("8 Mastering"), Description("Compressor attack in ms."), Rng(0.1, 100)] public double CompAttack { get; set; }
    [Category("8 Mastering"), Description("Compressor release in ms."), Rng(10, 1000)] public double CompRelease { get; set; }
    [Category("8 Mastering"), Description("Low shelf gain in dB (bass boost/cut)."), Rng(-12, 12)] public double LowShelf { get; set; }
    [Category("8 Mastering"), Description("High shelf gain in dB (air boost/cut)."), Rng(-12, 12)] public double HighShelf { get; set; }
    [Category("8 Mastering"), Description("Mid peak gain in dB (presence)."), Rng(-12, 12)] public double MidPeak { get; set; }
    [Category("8 Mastering"), Description("Stereo width (1 = unchanged, 0 = mono, >1 = wide)."), Rng(0, 1.6)] public double Width { get; set; }
    [Category("8 Mastering"), Description("Pitch shift in semitones (formant preserved-ish via resample)."), Rng(-12, 12)] public double PitchShift { get; set; }
    [Category("8 Mastering"), Description("Sidechain source track name (empty = self). Duck this track's gain when the source plays.")] public string SidechainSource { get; set; }
    [Category("8 Mastering"), Description("Sidechain duck amount (1 = fully silent on source hits)."), Rng(0, 1)] public double SidechainMix { get; set; }
    [Category("8 Mastering"), Description("Sidechain attack in ms (how fast the duck starts)."), Rng(0.5, 100)] public double SidechainAttack { get; set; }
    [Category("8 Mastering"), Description("Sidechain release in ms (how long recovery takes)."), Rng(10, 1000)] public double SidechainRelease { get; set; }
    [Category("8 Mastering"), Description("Compressor makeup gain in dB (after compression, before the limiter)."), Rng(-12, 24)] public double CompMakeup { get; set; }
    [Category("9 Dynamics"), Description("Noise gate amount (0 = off). Attenuates the signal below the threshold."), Rng(0, 1)] public double GateAmount { get; set; }
    [Category("9 Dynamics"), Description("Gate threshold in dBFS."), Rng(-60, 0)] public double GateThreshold { get; set; }
    [Category("9 Dynamics"), Description("Gate attack in ms."), Rng(0.1, 50)] public double GateAttack { get; set; }
    [Category("9 Dynamics"), Description("Gate release in ms."), Rng(10, 500)] public double GateRelease { get; set; }
    [Category("9 Dynamics"), Description("Transient attack boost/cut (positive = punchier)."), Rng(-1, 1)] public double TransientAttack { get; set; }
    [Category("9 Dynamics"), Description("Transient sustain boost/cut (negative = tighter)."), Rng(-1, 1)] public double TransientSustain { get; set; }
    [Category("9 Dynamics"), Description("Transient detector sensitivity."), Rng(0, 1)] public double TransientSensitivity { get; set; }
    [Category("9 Dynamics"), Description("Brickwall limiter on/off.")] public bool LimiterOn { get; set; }
    [Category("9 Dynamics"), Description("Limiter threshold in dBFS."), Rng(-24, 0)] public double LimiterThreshold { get; set; }
    [Category("9 Dynamics"), Description("Limiter release in ms."), Rng(10, 500)] public double LimiterRelease { get; set; }
    [Category("9 Dynamics"), Description("Limiter output ceiling in dBFS."), Rng(-6, 0)] public double LimiterCeiling { get; set; }
    [Category("10 Saturation"), Description("Saturator amount (0 = off)."), Rng(0, 1)] public double Saturator { get; set; }
    [Category("10 Saturation"), Description("Saturator DC bias (asymmetry)."), Rng(-0.5, 0.5)] public double SatBias { get; set; }
    [Category("10 Saturation"), Description("Saturator tone: dark to bright."), Rng(0, 1)] public double SatTone { get; set; }
    [Category("10 Saturation"), Description("Saturator dry/wet mix."), Rng(0, 1)] public double SatMix { get; set; }
    [Category("10 Saturation"), Description("High-frequency exciter amount (0 = off)."), Rng(0, 1)] public double Exciter { get; set; }
    [Category("10 Saturation"), Description("Exciter crossover frequency in Hz."), Rng(1000, 12000)] public double ExciterFreq { get; set; }
    [Category("10 Saturation"), Description("Exciter dry/wet mix."), Rng(0, 1)] public double ExciterMix { get; set; }
    [Category("10 Saturation"), Description("Rectifier mix (0 = off)."), Rng(0, 1)] public double RectifyMix { get; set; }
    [Category("10 Saturation"), Description("Half-wave rectification (otherwise full-wave).")] public bool RectifyHalf { get; set; }
    [Category("10 Saturation"), Description("Rectifier pre-gain."), Rng(0, 1)] public double RectifyDrive { get; set; }
    [Category("11 Modulation"), Description("Tremolo mix (0 = off)."), Rng(0, 1)] public double TremoloMix { get; set; }
    [Category("11 Modulation"), Description("Tremolo rate in Hz."), Rng(0.1, 20)] public double TremoloRate { get; set; }
    [Category("11 Modulation"), Description("Tremolo depth."), Rng(0, 1)] public double TremoloDepth { get; set; }
    [Category("11 Modulation"), Description("Auto-pan mix (0 = off)."), Rng(0, 1)] public double AutoPanMix { get; set; }
    [Category("11 Modulation"), Description("Auto-pan rate in Hz."), Rng(0.05, 5)] public double AutoPanRate { get; set; }
    [Category("11 Modulation"), Description("Auto-pan depth."), Rng(0, 1)] public double AutoPanDepth { get; set; }
    [Category("11 Modulation"), Description("Vibrato mix (0 = off)."), Rng(0, 1)] public double VibratoMix { get; set; }
    [Category("11 Modulation"), Description("Vibrato rate in Hz."), Rng(0.1, 12)] public double VibratoRate { get; set; }
    [Category("11 Modulation"), Description("Vibrato depth (pitch bend)."), Rng(0, 1)] public double VibratoDepth { get; set; }
    [Category("11 Modulation"), Description("Auto-wah mix (0 = off)."), Rng(0, 1)] public double AutoWahMix { get; set; }
    [Category("11 Modulation"), Description("Auto-wah LFO rate in Hz."), Rng(0.1, 8)] public double AutoWahRate { get; set; }
    [Category("11 Modulation"), Description("Auto-wah filter depth."), Rng(0, 1)] public double AutoWahDepth { get; set; }
    [Category("11 Modulation"), Description("Auto-wah resonance."), Rng(0, 0.95)] public double AutoWahResonance { get; set; }
    [Category("12 Space / Utility"), Description("Comb filter mix (0 = off)."), Rng(0, 1)] public double CombMix { get; set; }
    [Category("12 Space / Utility"), Description("Comb filter frequency in Hz."), Rng(20, 2000)] public double CombFreq { get; set; }
    [Category("12 Space / Utility"), Description("Comb filter feedback."), Rng(0, 0.95)] public double CombFeedback { get; set; }
    [Category("12 Space / Utility"), Description("Spring reverb mix (0 = off)."), Rng(0, 1)] public double SpringMix { get; set; }
    [Category("12 Space / Utility"), Description("Spring reverb decay."), Rng(0, 1)] public double SpringDecay { get; set; }
    [Category("12 Space / Utility"), Description("Spring reverb tone."), Rng(0, 1)] public double SpringTone { get; set; }
    [Category("12 Space / Utility"), Description("Shimmer reverb mix (0 = off)."), Rng(0, 1)] public double ShimmerMix { get; set; }
    [Category("12 Space / Utility"), Description("Shimmer pitch shift in semitones."), Rng(-12, 12)] public double ShimmerPitch { get; set; }
    [Category("12 Space / Utility"), Description("Shimmer decay."), Rng(0, 1)] public double ShimmerDecay { get; set; }
    [Category("12 Space / Utility"), Description("Haas widener mix (0 = off)."), Rng(0, 1)] public double HaasMix { get; set; }
    [Category("12 Space / Utility"), Description("Haas delay in ms."), Rng(1, 40)] public double HaasDelay { get; set; }
    [Category("13 Ensemble / Rotary"), Description("Ensemble chorus mix (0 = off)."), Rng(0, 1)] public double EnsembleMix { get; set; }
    [Category("13 Ensemble / Rotary"), Description("Ensemble rate in Hz."), Rng(0.05, 8)] public double EnsembleRate { get; set; }
    [Category("13 Ensemble / Rotary"), Description("Ensemble depth."), Rng(0, 1)] public double EnsembleDepth { get; set; }
    [Category("13 Ensemble / Rotary"), Description("Rotary speaker mix (0 = off)."), Rng(0, 1)] public double RotaryMix { get; set; }
    [Category("13 Ensemble / Rotary"), Description("Rotary rate in Hz."), Rng(0.1, 10)] public double RotaryRate { get; set; }
    [Category("13 Ensemble / Rotary"), Description("Rotary depth."), Rng(0, 1)] public double RotaryDepth { get; set; }
    [Category("14 Pitch"), Description("Sub-octave mix (0 = off)."), Rng(0, 1)] public double OctaverMix { get; set; }
    [Category("14 Pitch"), Description("Harmonizer mix (0 = off)."), Rng(0, 1)] public double HarmonizerMix { get; set; }
    [Category("14 Pitch"), Description("Harmonizer interval in semitones."), Rng(-12, 12)] public double HarmonizerSemi { get; set; }
    [Category("14 Pitch"), Description("Resonator mix (0 = off)."), Rng(0, 1)] public double ResonatorMix { get; set; }
    [Category("14 Pitch"), Description("Resonator base frequency in Hz."), Rng(40, 2000)] public double ResonatorFreq { get; set; }
    [Category("14 Pitch"), Description("Resonator feedback."), Rng(0, 0.95)] public double ResonatorFeedback { get; set; }
    [Category("15 EQ / Dynamics"), Description("Tilt EQ in dB: positive brightens, negative darkens."), Rng(-12, 12)] public double TiltGain { get; set; }
    [Category("15 EQ / Dynamics"), Description("Mid gain in dB."), Rng(-12, 12)] public double MidGain { get; set; }
    [Category("15 EQ / Dynamics"), Description("Side gain in dB."), Rng(-12, 12)] public double SideGain { get; set; }
    [Category("15 EQ / Dynamics"), Description("De-esser amount (0 = off)."), Rng(0, 1)] public double DeEsserAmount { get; set; }
    [Category("15 EQ / Dynamics"), Description("De-esser crossover in Hz."), Rng(2000, 12000)] public double DeEsserFreq { get; set; }
    [Category("15 EQ / Dynamics"), Description("Multiband compressor on/off.")] public bool MBOn { get; set; }
    [Category("15 EQ / Dynamics"), Description("Low band threshold in dB."), Rng(-40, 0)] public double MBLowThresh { get; set; }
    [Category("15 EQ / Dynamics"), Description("Mid band threshold in dB."), Rng(-40, 0)] public double MBMidThresh { get; set; }
    [Category("15 EQ / Dynamics"), Description("High band threshold in dB."), Rng(-40, 0)] public double MBHighThresh { get; set; }
    [Category("15 EQ / Dynamics"), Description("Multiband ratio."), Rng(1, 12)] public double MBRatio { get; set; }
    [Category("15 EQ / Dynamics"), Description("Multiband makeup gain in dB."), Rng(-12, 24)] public double MBMakeup { get; set; }
    [Category("16 Drive / Filter"), Description("Clipper amount (0 = off)."), Rng(0, 1)] public double ClipperAmount { get; set; }
    [Category("16 Drive / Filter"), Description("Clipper pre-gain."), Rng(0, 1)] public double ClipperDrive { get; set; }
    [Category("16 Drive / Filter"), Description("Auto-filter mix (0 = off)."), Rng(0, 1)] public double AutoFilterMix { get; set; }
    [Category("16 Drive / Filter"), Description("Auto-filter envelope sensitivity."), Rng(0, 1)] public double AutoFilterSensitivity { get; set; }
    [Category("16 Drive / Filter"), Description("Auto-filter resonance."), Rng(0, 0.95)] public double AutoFilterResonance { get; set; }
    [Category("17 Reverb / Delay"), Description("Gated reverb mix (0 = off)."), Rng(0, 1)] public double GateVerbMix { get; set; }
    [Category("17 Reverb / Delay"), Description("Gated reverb decay."), Rng(0, 1)] public double GateVerbDecay { get; set; }
    [Category("17 Reverb / Delay"), Description("Gated reverb hold in seconds."), Rng(0.02, 2)] public double GateVerbHold { get; set; }
    [Category("17 Reverb / Delay"), Description("Reverse reverb mix (0 = off)."), Rng(0, 1)] public double ReverseVerbMix { get; set; }
    [Category("17 Reverb / Delay"), Description("Reverse reverb decay."), Rng(0, 1)] public double ReverseVerbDecay { get; set; }
    [Category("17 Reverb / Delay"), Description("Multi-tap delay mix (0 = off)."), Rng(0, 1)] public double MultiTapMix { get; set; }
    [Category("17 Reverb / Delay"), Description("Multi-tap time in ms."), Rng(20, 1000)] public double MultiTapTime { get; set; }
    [Category("17 Reverb / Delay"), Description("Multi-tap feedback."), Rng(0, 0.95)] public double MultiTapFb { get; set; }
    [Category("17 Reverb / Delay"), Description("Doubler mix (0 = off)."), Rng(0, 1)] public double DoublerMix { get; set; }
    [Category("17 Reverb / Delay"), Description("Doubler delay in ms."), Rng(1, 40)] public double DoublerDelay { get; set; }
    [Category("14 Pitch"), Description("Formant filter mix (0 = off)."), Rng(0, 1)] public double FormantMix { get; set; }
    [Category("14 Pitch"), Description("Formant vowel 0=a 1=e 2=i 3=o 4=u."), Rng(0, 4)] public double FormantVowel { get; set; }
    [Category("14 Pitch"), Description("Formant shift (0.5 = down, 1 = natural, 2 = up)."), Rng(0.5, 2)] public double FormantShift { get; set; }
    [Category("18 Drive / Saturation"), Description("Ladder filter mix (0 = off)."), Rng(0, 1)] public double LadderMix { get; set; }
    [Category("18 Drive / Saturation"), Description("Ladder cutoff in Hz."), Rng(40, 16000)] public double LadderCutoff { get; set; }
    [Category("18 Drive / Saturation"), Description("Ladder resonance."), Rng(0, 0.95)] public double LadderReso { get; set; }
    [Category("18 Drive / Saturation"), Description("Ladder input drive."), Rng(0, 1)] public double LadderDrive { get; set; }
    [Category("18 Drive / Saturation"), Description("Tube preamp drive."), Rng(0, 1)] public double TubeDrive { get; set; }
    [Category("18 Drive / Saturation"), Description("Tube bias (asymmetry)."), Rng(-1, 1)] public double TubeBias { get; set; }
    [Category("18 Drive / Saturation"), Description("Tube preamp mix (0 = off)."), Rng(0, 1)] public double TubeMix { get; set; }
    [Category("18 Drive / Saturation"), Description("Tape saturation drive."), Rng(0, 1)] public double TapeSatDrive { get; set; }
    [Category("18 Drive / Saturation"), Description("Tape saturation tone (0 dark, 1 bright)."), Rng(0, 1)] public double TapeSatTone { get; set; }
    [Category("18 Drive / Saturation"), Description("Tape saturation mix (0 = off)."), Rng(0, 1)] public double TapeSatMix { get; set; }
    [Category("18 Drive / Saturation"), Description("Diode distortion drive."), Rng(0, 1)] public double DiodeDrive { get; set; }
    [Category("18 Drive / Saturation"), Description("Diode distortion tone."), Rng(0, 1)] public double DiodeTone { get; set; }
    [Category("18 Drive / Saturation"), Description("Diode distortion mix (0 = off)."), Rng(0, 1)] public double DiodeMix { get; set; }
    [Category("18 Drive / Saturation"), Description("Fuzz amount (0 = off)."), Rng(0, 1)] public double FuzzAmount { get; set; }
    [Category("18 Drive / Saturation"), Description("Fuzz noise gate."), Rng(0, 1)] public double FuzzGate { get; set; }
    [Category("18 Drive / Saturation"), Description("Fuzz mix."), Rng(0, 1)] public double FuzzMix { get; set; }
    [Category("18 Drive / Saturation"), Description("Chip-tune mix (0 = off)."), Rng(0, 1)] public double ChipMix { get; set; }
    [Category("18 Drive / Saturation"), Description("Chip-tune bit depth."), Rng(2, 12)] public double ChipBits { get; set; }
    [Category("18 Drive / Saturation"), Description("Chip-tune sample rate in kHz."), Rng(1, 44)] public double ChipRate { get; set; }
    [Category("19 Pitch / Bass"), Description("Sub-bass mix (0 = off)."), Rng(0, 1)] public double SubBassMix { get; set; }
    [Category("19 Pitch / Bass"), Description("Sub-bass frequency in Hz."), Rng(20, 120)] public double SubBassFreq { get; set; }
    [Category("19 Pitch / Bass"), Description("Sub-bass drive."), Rng(0, 1)] public double SubBassDrive { get; set; }
    [Category("19 Pitch / Bass"), Description("Bass enhancer mix (0 = off)."), Rng(0, 1)] public double BassEnhMix { get; set; }
    [Category("19 Pitch / Bass"), Description("Bass enhancer crossover in Hz."), Rng(40, 300)] public double BassEnhFreq { get; set; }
    [Category("19 Pitch / Bass"), Description("Bass enhancer amount."), Rng(0, 1)] public double BassEnhAmount { get; set; }
    [Category("19 Pitch / Bass"), Description("Frequency shifter mix (0 = off)."), Rng(0, 1)] public double FreqShiftMix { get; set; }
    [Category("19 Pitch / Bass"), Description("Frequency shift in Hz (positive = up)."), Rng(-1000, 1000)] public double FreqShiftHz { get; set; }
    [Category("20 Air / EQ"), Description("Air band mix (0 = off)."), Rng(0, 1)] public double AirMix { get; set; }
    [Category("20 Air / EQ"), Description("Air band frequency in Hz."), Rng(2000, 16000)] public double AirFreq { get; set; }
    [Category("20 Air / EQ"), Description("Air band gain in dB."), Rng(0, 12)] public double AirGain { get; set; }
    [Category("20 Air / EQ"), Description("Stereo width (1 = normal)."), Rng(0, 2)] public double StereoWidth { get; set; }
    [Category("20 Air / EQ"), Description("Bass mono crossover in Hz (0 = off)."), Rng(0, 500)] public double BassMonoFreq { get; set; }
    [Category("21 Space"), Description("Ping-pong delay mix (0 = off)."), Rng(0, 1)] public double PingPongMix { get; set; }
    [Category("21 Space"), Description("Ping-pong time in ms."), Rng(20, 1200)] public double PingPongTime { get; set; }
    [Category("21 Space"), Description("Ping-pong feedback."), Rng(0, 0.95)] public double PingPongFb { get; set; }
    [Category("21 Space"), Description("Dub delay mix (0 = off)."), Rng(0, 1)] public double DubMix { get; set; }
    [Category("21 Space"), Description("Dub delay time in ms."), Rng(40, 1200)] public double DubTime { get; set; }
    [Category("21 Space"), Description("Dub delay feedback."), Rng(0, 0.95)] public double DubFb { get; set; }
    [Category("21 Space"), Description("Dub delay tone (0 dark, 1 bright)."), Rng(0, 1)] public double DubTone { get; set; }
    [Category("21 Space"), Description("Cathedral reverb mix (0 = off)."), Rng(0, 1)] public double CathedralMix { get; set; }
    [Category("21 Space"), Description("Cathedral reverb size."), Rng(0, 1)] public double CathedralSize { get; set; }
    [Category("21 Space"), Description("Cathedral reverb damping."), Rng(0, 1)] public double CathedralDamp { get; set; }
    [Category("21 Space"), Description("Room reverb mix (0 = off)."), Rng(0, 1)] public double RoomMix { get; set; }
    [Category("21 Space"), Description("Room reverb size."), Rng(0, 1)] public double RoomSize { get; set; }
    [Category("21 Space"), Description("Room reverb damping."), Rng(0, 1)] public double RoomDamp { get; set; }
    [Category("21 Space"), Description("Ambience mix (0 = off)."), Rng(0, 1)] public double AmbienceMix { get; set; }
    [Category("21 Space"), Description("Ambience size."), Rng(0, 1)] public double AmbienceSize { get; set; }
    [Category("21 Space"), Description("Ambience damping."), Rng(0, 1)] public double AmbienceDamp { get; set; }
    [Category("22 Modulation"), Description("Wah pedal mix (0 = off)."), Rng(0, 1)] public double WahPedalMix { get; set; }
    [Category("22 Modulation"), Description("Wah pedal LFO rate in Hz."), Rng(0.05, 10)] public double WahPedalRate { get; set; }
    [Category("22 Modulation"), Description("Wah pedal depth."), Rng(0, 1)] public double WahPedalDepth { get; set; }
    [Category("22 Modulation"), Description("Wah pedal resonance."), Rng(0, 0.95)] public double WahPedalQ { get; set; }
    [Category("22 Modulation"), Description("Talk box mix (0 = off)."), Rng(0, 1)] public double TalkMix { get; set; }
    [Category("22 Modulation"), Description("Talk box vowel rate in Hz."), Rng(0.05, 12)] public double TalkRate { get; set; }
    [Category("22 Modulation"), Description("Talk box vowel offset 0-4."), Rng(0, 4)] public double TalkVowel { get; set; }
    [Category("22 Modulation"), Description("Harmonic tremolo mix (0 = off)."), Rng(0, 1)] public double HarmTremMix { get; set; }
    [Category("22 Modulation"), Description("Harmonic tremolo rate in Hz."), Rng(0.1, 12)] public double HarmTremRate { get; set; }
    [Category("22 Modulation"), Description("Harmonic tremolo depth."), Rng(0, 1)] public double HarmTremDepth { get; set; }
    [Category("22 Modulation"), Description("Sweep filter mix (0 = off)."), Rng(0, 1)] public double SweepMix { get; set; }
    [Category("22 Modulation"), Description("Sweep filter LFO rate in Hz."), Rng(0.05, 10)] public double SweepRate { get; set; }
    [Category("22 Modulation"), Description("Sweep filter depth."), Rng(0, 1)] public double SweepDepth { get; set; }
    [Category("22 Modulation"), Description("Sweep filter resonance."), Rng(0, 0.95)] public double SweepQ { get; set; }
    [Category("22 Modulation"), Description("Sweep filter center in Hz."), Rng(80, 8000)] public double SweepCenter { get; set; }
    [Category("25 Dynamics"), Description("Optical compressor mix (0 = off)."), Rng(0, 1)] public double OptoMix { get; set; }
    [Category("25 Dynamics"), Description("Optical compressor threshold in dB."), Rng(-60, 0)] public double OptoThresh { get; set; }
    [Category("25 Dynamics"), Description("Optical compressor ratio."), Rng(1, 20)] public double OptoRatio { get; set; }
    [Category("25 Dynamics"), Description("Optical compressor release in ms."), Rng(20, 1000)] public double OptoRelease { get; set; }
    [Category("25 Dynamics"), Description("FET compressor mix (0 = off)."), Rng(0, 1)] public double FetMix { get; set; }
    [Category("25 Dynamics"), Description("FET compressor threshold in dB."), Rng(-60, 0)] public double FetThresh { get; set; }
    [Category("25 Dynamics"), Description("FET compressor ratio."), Rng(1, 20)] public double FetRatio { get; set; }
    [Category("25 Dynamics"), Description("FET compressor attack in ms."), Rng(0.1, 50)] public double FetAttack { get; set; }
    [Category("25 Dynamics"), Description("VCA bus compressor mix (0 = off)."), Rng(0, 1)] public double VcaMix { get; set; }
    [Category("25 Dynamics"), Description("VCA compressor threshold in dB."), Rng(-60, 0)] public double VcaThresh { get; set; }
    [Category("25 Dynamics"), Description("VCA compressor ratio."), Rng(1, 20)] public double VcaRatio { get; set; }
    [Category("25 Dynamics"), Description("VCA compressor release in ms."), Rng(20, 1000)] public double VcaRelease { get; set; }
    [Category("25 Dynamics"), Description("Downward expander mix (0 = off)."), Rng(0, 1)] public double ExpMix { get; set; }
    [Category("25 Dynamics"), Description("Expander threshold in dB."), Rng(-70, -10)] public double ExpThresh { get; set; }
    [Category("25 Dynamics"), Description("Expander ratio."), Rng(1, 8)] public double ExpRatio { get; set; }
    [Category("25 Dynamics"), Description("Expander maximum range in dB."), Rng(3, 48)] public double ExpRange { get; set; }
    [Category("25 Dynamics"), Description("Mid/side compressor mix (0 = off)."), Rng(0, 1)] public double MSCompMix { get; set; }
    [Category("25 Dynamics"), Description("Mid/side compressor threshold in dB."), Rng(-60, 0)] public double MSCompThresh { get; set; }
    [Category("25 Dynamics"), Description("Mid/side compressor ratio."), Rng(1, 20)] public double MSCompRatio { get; set; }
    [Category("25 Dynamics"), Description("Mid/side compressor release in ms."), Rng(20, 1000)] public double MSCompRelease { get; set; }
    [Category("26 EQ / Filter"), Description("Dynamic EQ mix (0 = off)."), Rng(0, 1)] public double DynEqMix { get; set; }
    [Category("26 EQ / Filter"), Description("Dynamic EQ frequency in Hz."), Rng(80, 12000)] public double DynEqFreq { get; set; }
    [Category("26 EQ / Filter"), Description("Dynamic EQ threshold in dB."), Rng(-60, 0)] public double DynEqThresh { get; set; }
    [Category("26 EQ / Filter"), Description("Dynamic EQ gain reduction amount."), Rng(0, 1)] public double DynEqAmount { get; set; }
    [Category("26 EQ / Filter"), Description("Pultec low boost in dB."), Rng(0, 12)] public double PultecLow { get; set; }
    [Category("26 EQ / Filter"), Description("Pultec high boost in dB."), Rng(0, 12)] public double PultecHigh { get; set; }
    [Category("26 EQ / Filter"), Description("Pultec high attenuation in dB (boost and cut together for the classic curve)."), Rng(0, 12)] public double PultecAtten { get; set; }
    [Category("27 Drive / Amp"), Description("Overdrive mix (0 = off)."), Rng(0, 1)] public double OverdriveMix { get; set; }
    [Category("27 Drive / Amp"), Description("Overdrive drive."), Rng(0, 1)] public double OverdriveDrive { get; set; }
    [Category("27 Drive / Amp"), Description("Overdrive tone (0 dark, 1 bright)."), Rng(0, 1)] public double OverdriveTone { get; set; }
    [Category("27 Drive / Amp"), Description("Metal distortion mix (0 = off)."), Rng(0, 1)] public double MetalMix { get; set; }
    [Category("27 Drive / Amp"), Description("Metal distortion drive."), Rng(0, 1)] public double MetalDrive { get; set; }
    [Category("27 Drive / Amp"), Description("Metal distortion mid scoop (0 = flat, 1 = scooped)."), Rng(0, 1)] public double MetalMid { get; set; }
    [Category("27 Drive / Amp"), Description("Speaker simulator mix (0 = off)."), Rng(0, 1)] public double CabMix { get; set; }
    [Category("27 Drive / Amp"), Description("Speaker simulator drive."), Rng(0, 1)] public double CabDrive { get; set; }
    [Category("27 Drive / Amp"), Description("Speaker simulator tone."), Rng(0, 1)] public double CabTone { get; set; }
    [Category("27 Drive / Amp"), Description("Multiband distortion mix (0 = off)."), Rng(0, 1)] public double MBDistMix { get; set; }
    [Category("27 Drive / Amp"), Description("Multiband distortion low drive."), Rng(0, 1)] public double MBDistLow { get; set; }
    [Category("27 Drive / Amp"), Description("Multiband distortion mid drive."), Rng(0, 1)] public double MBDistMid { get; set; }
    [Category("27 Drive / Amp"), Description("Multiband distortion high drive."), Rng(0, 1)] public double MBDistHigh { get; set; }
    [Category("32 Spectral"), Description("Spectral gate mix (0 = off)."), Rng(0, 1)] public double SpecGateMix { get; set; }
    [Category("32 Spectral"), Description("Spectral gate threshold."), Rng(0.001, 0.5)] public double SpecGateThresh { get; set; }
    [Category("32 Spectral"), Description("Spectral gate band count."), Rng(2, 8)] public double SpecGateBands { get; set; }
    [Category("32 Spectral"), Description("Spectral blur mix (0 = off)."), Rng(0, 1)] public double SpecBlurMix { get; set; }
    [Category("32 Spectral"), Description("Spectral blur amount."), Rng(0, 1)] public double SpecBlurAmount { get; set; }
    [Category("32 Spectral"), Description("Spectral blur stereo spread."), Rng(0, 1)] public double SpecBlurSpread { get; set; }
    [Category("32 Spectral"), Description("Spectral delay mix (0 = off)."), Rng(0, 1)] public double SpecDelayMix { get; set; }
    [Category("32 Spectral"), Description("Spectral delay time in ms."), Rng(20, 900)] public double SpecDelayTime { get; set; }
    [Category("32 Spectral"), Description("Spectral delay feedback."), Rng(0, 0.9)] public double SpecDelayFb { get; set; }
    [Category("32 Spectral"), Description("Spectral delay band spread."), Rng(0, 1)] public double SpecDelaySpread { get; set; }
    [Category("33 Convolution / Spring"), Description("Convolution reverb mix (0 = off)."), Rng(0, 1)] public double ConvMix { get; set; }
    [Category("33 Convolution / Spring"), Description("Convolution reverb size."), Rng(0, 1)] public double ConvSize { get; set; }
    [Category("33 Convolution / Spring"), Description("Convolution reverb damping."), Rng(0, 1)] public double ConvDamp { get; set; }
    [Category("33 Convolution / Spring"), Description("Convolution pre-delay in ms."), Rng(0, 120)] public double ConvPre { get; set; }
    [Category("33 Convolution / Spring"), Description("Tank reverb mix (0 = off)."), Rng(0, 1)] public double TankMix { get; set; }
    [Category("33 Convolution / Spring"), Description("Tank reverb decay."), Rng(0, 1)] public double TankDecay { get; set; }
    [Category("33 Convolution / Spring"), Description("Tank reverb tone."), Rng(0, 1)] public double TankTone { get; set; }
    [Category("33 Convolution / Spring"), Description("Tank reverb drip."), Rng(0, 1)] public double TankDrip { get; set; }
    [Category("34 Pitch / Harmony"), Description("Auto-tune mix (0 = off)."), Rng(0, 1)] public double TuneMix { get; set; }
    [Category("34 Pitch / Harmony"), Description("Auto-tune correction speed."), Rng(0, 1)] public double TuneSpeed { get; set; }
    [Category("34 Pitch / Harmony"), Description("Auto-tune scale: 0 chromatic, 1 major, 2 minor."), Rng(0, 2)] public double TuneScale { get; set; }
    [Category("34 Pitch / Harmony"), Description("Auto-tune minimum frequency in Hz."), Rng(40, 300)] public double TuneMinHz { get; set; }
    [Category("34 Pitch / Harmony"), Description("Octave-up mix (0 = off)."), Rng(0, 1)] public double OctUpMix { get; set; }
    [Category("34 Pitch / Harmony"), Description("Octave-up tone."), Rng(0, 1)] public double OctUpTone { get; set; }
    [Category("34 Pitch / Harmony"), Description("Chord generator mix (0 = off)."), Rng(0, 1)] public double ChordMix { get; set; }
    [Category("34 Pitch / Harmony"), Description("Chord type: 0 major, 1 minor, 2 fifth, 3 sus4."), Rng(0, 3)] public double ChordType { get; set; }
    [Category("34 Pitch / Harmony"), Description("Chord voice spread."), Rng(0, 1)] public double ChordSpread { get; set; }
    [Category("35 Dynamics"), Description("Upward compressor mix (0 = off)."), Rng(0, 1)] public double UpCompMix { get; set; }
    [Category("35 Dynamics"), Description("Upward compressor threshold in dB."), Rng(-60, 0)] public double UpCompThresh { get; set; }
    [Category("35 Dynamics"), Description("Upward compressor ratio."), Rng(1, 8)] public double UpCompRatio { get; set; }
    [Category("35 Dynamics"), Description("Maximum upward gain in dB."), Rng(0, 36)] public double UpCompRange { get; set; }
    [Category("35 Dynamics"), Description("Transient gate mix (0 = off)."), Rng(0, 1)] public double TranGateMix { get; set; }
    [Category("35 Dynamics"), Description("Transient gate threshold in dB."), Rng(-60, 0)] public double TranGateThresh { get; set; }
    [Category("35 Dynamics"), Description("Transient gate hold in ms."), Rng(1, 500)] public double TranGateHold { get; set; }
    [Category("35 Dynamics"), Description("Transient gate release in ms."), Rng(10, 1000)] public double TranGateRelease { get; set; }
    [Category("36 Stereo / Phase"), Description("Stereo shuffler mix (0 = off)."), Rng(0, 1)] public double ShuffleMix { get; set; }
    [Category("36 Stereo / Phase"), Description("Low-frequency side width."), Rng(0, 2)] public double ShuffleLow { get; set; }
    [Category("36 Stereo / Phase"), Description("High-frequency side width."), Rng(0, 2)] public double ShuffleHigh { get; set; }
    [Category("36 Stereo / Phase"), Description("Stereo shuffler crossover in Hz."), Rng(40, 800)] public double ShuffleFreq { get; set; }
    [Category("36 Stereo / Phase"), Description("Mono maker mix (0 = off)."), Rng(0, 1)] public double MonoMix { get; set; }
    [Category("36 Stereo / Phase"), Description("Mono maker crossover in Hz."), Rng(20, 500)] public double MonoFreq { get; set; }
    [Category("36 Stereo / Phase"), Description("Mono maker side width above crossover."), Rng(0, 2)] public double MonoWidth { get; set; }
    [Category("36 Stereo / Phase"), Description("Phase aligner mix (0 = off)."), Rng(0, 1)] public double PhaseMix { get; set; }
    [Category("36 Stereo / Phase"), Description("Phase aligner frequency in Hz."), Rng(80, 8000)] public double PhaseFreq { get; set; }
    [Category("36 Stereo / Phase"), Description("Phase aligner amount."), Rng(0, 1)] public double PhaseAmount { get; set; }
    [Category("37 Loops / Glitch"), Description("Tape loop mix (0 = off)."), Rng(0, 1)] public double LoopMix { get; set; }
    [Category("37 Loops / Glitch"), Description("Tape loop length in ms."), Rng(50, 2000)] public double LoopLength { get; set; }
    [Category("37 Loops / Glitch"), Description("Tape loop speed."), Rng(0.25, 2)] public double LoopSpeed { get; set; }
    [Category("37 Loops / Glitch"), Description("Tape loop feedback."), Rng(0, 0.95)] public double LoopFb { get; set; }
    [Category("37 Loops / Glitch"), Description("Stutter gate mix (0 = off)."), Rng(0, 1)] public double StutterGateMix { get; set; }
    [Category("37 Loops / Glitch"), Description("Stutter gate rate in Hz."), Rng(0.5, 30)] public double StutterGateRate { get; set; }
    [Category("37 Loops / Glitch"), Description("Stutter gate divisions."), Rng(2, 16)] public double StutterGateDiv { get; set; }
    [Category("37 Loops / Glitch"), Description("Stutter gate chance."), Rng(0, 1)] public double StutterGateChance { get; set; }
    [Category("28 Modulation"), Description("Tape flutter mix (0 = off)."), Rng(0, 1)] public double FlutterMix { get; set; }
    [Category("28 Modulation"), Description("Tape flutter rate in Hz."), Rng(0.05, 8)] public double FlutterRate { get; set; }
    [Category("28 Modulation"), Description("Tape flutter depth."), Rng(0, 1)] public double FlutterDepth { get; set; }
    [Category("28 Modulation"), Description("Dimension stereo chorus mix (0 = off)."), Rng(0, 1)] public double DimMix { get; set; }
    [Category("28 Modulation"), Description("Dimension rate in Hz."), Rng(0.05, 5)] public double DimRate { get; set; }
    [Category("28 Modulation"), Description("Dimension depth."), Rng(0, 1)] public double DimDepth { get; set; }
    [Category("28 Modulation"), Description("Envelope phaser mix (0 = off)."), Rng(0, 1)] public double EnvPhaserMix { get; set; }
    [Category("28 Modulation"), Description("Envelope phaser threshold in dB."), Rng(-60, 0)] public double EnvPhaserThresh { get; set; }
    [Category("28 Modulation"), Description("Envelope phaser depth."), Rng(0, 1)] public double EnvPhaserDepth { get; set; }
    [Category("28 Modulation"), Description("Envelope phaser LFO rate in Hz."), Rng(0, 8)] public double EnvPhaserRate { get; set; }
    [Category("28 Modulation"), Description("Envelope flanger mix (0 = off)."), Rng(0, 1)] public double EnvFlangeMix { get; set; }
    [Category("28 Modulation"), Description("Envelope flanger threshold in dB."), Rng(-60, 0)] public double EnvFlangeThresh { get; set; }
    [Category("28 Modulation"), Description("Envelope flanger depth."), Rng(0, 1)] public double EnvFlangeDepth { get; set; }
    [Category("28 Modulation"), Description("Envelope flanger feedback."), Rng(0, 0.95)] public double EnvFlangeFb { get; set; }
    [Category("28 Modulation"), Description("Chopper mix (0 = off)."), Rng(0, 1)] public double ChopMix { get; set; }
    [Category("28 Modulation"), Description("Chopper rate in Hz."), Rng(0.5, 30)] public double ChopRate { get; set; }
    [Category("28 Modulation"), Description("Chopper depth."), Rng(0, 1)] public double ChopDepth { get; set; }
    [Category("28 Modulation"), Description("Chopper shape (0 sine, 1 square)."), Rng(0, 1)] public double ChopShape { get; set; }
    [Category("28 Modulation"), Description("Barber-pole phaser mix (0 = off)."), Rng(0, 1)] public double BarberMix { get; set; }
    [Category("28 Modulation"), Description("Barber-pole rate in Hz."), Rng(0.05, 5)] public double BarberRate { get; set; }
    [Category("28 Modulation"), Description("Barber-pole depth."), Rng(0, 1)] public double BarberDepth { get; set; }
    [Category("28 Modulation"), Description("Barber-pole allpass stages."), Rng(2, 8)] public double BarberStages { get; set; }
    [Category("29 Pitch / Time"), Description("Half-speed mix (0 = off)."), Rng(0, 1)] public double HalfSpeedMix { get; set; }
    [Category("29 Pitch / Time"), Description("Half-speed amount (1 = half speed)."), Rng(0, 1)] public double HalfSpeedAmount { get; set; }
    [Category("29 Pitch / Time"), Description("Pitch echo mix (0 = off)."), Rng(0, 1)] public double PitchEchoMix { get; set; }
    [Category("29 Pitch / Time"), Description("Pitch echo time in ms."), Rng(40, 1200)] public double PitchEchoTime { get; set; }
    [Category("29 Pitch / Time"), Description("Pitch echo feedback."), Rng(0, 0.9)] public double PitchEchoFb { get; set; }
    [Category("29 Pitch / Time"), Description("Pitch echo interval in semitones."), Rng(-12, 12)] public double PitchEchoSemi { get; set; }
    [Category("29 Pitch / Time"), Description("Spectral freeze mix (0 = off)."), Rng(0, 1)] public double FreezeMix { get; set; }
    [Category("29 Pitch / Time"), Description("Freeze trigger threshold in dB."), Rng(-60, 0)] public double FreezeThreshold { get; set; }
    [Category("29 Pitch / Time"), Description("Freeze decay."), Rng(0, 1)] public double FreezeDecay { get; set; }
    [Category("30 Space"), Description("Reverse delay mix (0 = off)."), Rng(0, 1)] public double RevDelayMix { get; set; }
    [Category("30 Space"), Description("Reverse delay block time in ms."), Rng(60, 1200)] public double RevDelayTime { get; set; }
    [Category("30 Space"), Description("Reverse delay feedback."), Rng(0, 0.9)] public double RevDelayFb { get; set; }
    [Category("30 Space"), Description("Echo-verb mix (0 = off)."), Rng(0, 1)] public double EchoVerbMix { get; set; }
    [Category("30 Space"), Description("Echo-verb delay time in ms."), Rng(40, 900)] public double EchoVerbTime { get; set; }
    [Category("30 Space"), Description("Echo-verb feedback."), Rng(0, 0.9)] public double EchoVerbFb { get; set; }
    [Category("30 Space"), Description("Echo-verb reverb size."), Rng(0, 1)] public double EchoVerbSize { get; set; }
    [Category("30 Space"), Description("Bloom reverb mix (0 = off)."), Rng(0, 1)] public double BloomMix { get; set; }
    [Category("30 Space"), Description("Bloom reverb size."), Rng(0, 1)] public double BloomSize { get; set; }
    [Category("30 Space"), Description("Bloom trigger threshold in dB."), Rng(-60, 0)] public double BloomThresh { get; set; }
    [Category("30 Space"), Description("Bloom attack in seconds."), Rng(0.01, 1)] public double BloomAttack { get; set; }
    [Category("30 Space"), Description("Non-linear reverb mix (0 = off)."), Rng(0, 1)] public double NonLinVerbMix { get; set; }
    [Category("30 Space"), Description("Non-linear reverb size."), Rng(0, 1)] public double NonLinVerbSize { get; set; }
    [Category("30 Space"), Description("Non-linear reverb drive."), Rng(0, 1)] public double NonLinVerbDrive { get; set; }
    [Category("30 Space"), Description("Non-linear reverb gate."), Rng(0, 1)] public double NonLinVerbGate { get; set; }
    [Category("31 Vocoder"), Description("Vocoder mix (0 = off)."), Rng(0, 1)] public double VocoderMix { get; set; }
    [Category("31 Vocoder"), Description("Vocoder band count."), Rng(4, 12)] public double VocoderBands { get; set; }
    [Category("31 Vocoder"), Description("Vocoder envelope release in ms."), Rng(10, 500)] public double VocoderRelease { get; set; }
    [Category("31 Vocoder"), Description("Vocoder carrier (0 = noise, 1 = saw)."), Rng(0, 1)] public double VocoderCarrier { get; set; }
    [Category("23 Dynamics"), Description("Ducker mix (0 = off)."), Rng(0, 1)] public double DuckerMix { get; set; }
    [Category("23 Dynamics"), Description("Ducker threshold in dB."), Rng(-60, 0)] public double DuckerThreshold { get; set; }
    [Category("23 Dynamics"), Description("Ducker depth."), Rng(0, 1)] public double DuckerDepth { get; set; }
    [Category("23 Dynamics"), Description("Ducker release in ms."), Rng(10, 1000)] public double DuckerRelease { get; set; }
    [Category("23 Dynamics"), Description("Level rider amount (0 = off)."), Rng(0, 1)] public double RiderAmount { get; set; }
    [Category("23 Dynamics"), Description("Level rider target in dB."), Rng(-40, 0)] public double RiderTarget { get; set; }
    [Category("23 Dynamics"), Description("Level rider speed."), Rng(0.01, 1)] public double RiderSpeed { get; set; }
    [Category("24 Creative"), Description("Beat repeat mix (0 = off)."), Rng(0, 1)] public double BeatRepeatMix { get; set; }
    [Category("24 Creative"), Description("Beat repeat divisions per half second."), Rng(2, 16)] public double BeatRepeatDiv { get; set; }
    [Category("24 Creative"), Description("Beat repeat chance."), Rng(0, 1)] public double BeatRepeatChance { get; set; }
    [Category("24 Creative"), Description("Granular cloud mix (0 = off)."), Rng(0, 1)] public double GrainMix { get; set; }
    [Category("24 Creative"), Description("Grain size in ms."), Rng(10, 300)] public double GrainSize { get; set; }
    [Category("24 Creative"), Description("Grains per second."), Rng(1, 80)] public double GrainDensity { get; set; }
    [Category("24 Creative"), Description("Glitch gate mix (0 = off)."), Rng(0, 1)] public double GlitchMix { get; set; }
    [Category("24 Creative"), Description("Glitch gate density."), Rng(0, 1)] public double GlitchDensity { get; set; }
    [Category("24 Creative"), Description("Glitch gate seed."), Rng(0, 99)] public double GlitchSeed { get; set; }
    [Category("5 Output"), Description("Output gain."), Rng(0, 2, NoRandom = true)] public double Gain { get; set; }
    public bool Active {
        get {
            return Filter != FilterMode.off || Drive > 0 || Fold > 0 || Bits < 16 || Downsample > 1 || RingMix > 0 || (StutterBeats > 0 && StutterChance > 0)
                || Chorus > 0 || DelayMix > 0 || ReverbMix > 0 || Math.Abs(Gain - 1) > 0.001
                || PhaserMix > 0 || FlangerMix > 0 || TapeStop > 0 || TapeDelayMix > 0 || PlateMix > 0
                || VinylCrackle > 0 || TapeHiss > 0 || TapeWow > 0 || CompRatio > 1 || LowShelf != 0 || HighShelf != 0 || MidPeak != 0 || Math.Abs(Width - 1) > 0.001 || PitchShift != 0
                || GateAmount > 0 || Math.Abs(TransientAttack) > 0.001 || Math.Abs(TransientSustain) > 0.001 || LimiterOn
                || Saturator > 0 || Exciter > 0 || RectifyMix > 0
                || TremoloMix > 0 || AutoPanMix > 0 || VibratoMix > 0 || AutoWahMix > 0
                || CombMix > 0 || SpringMix > 0 || ShimmerMix > 0 || HaasMix > 0
                || EnsembleMix > 0 || RotaryMix > 0 || OctaverMix > 0 || HarmonizerMix > 0 || ResonatorMix > 0
                || TiltGain != 0 || MidGain != 0 || SideGain != 0 || DeEsserAmount > 0 || MBOn
                || ClipperAmount > 0 || AutoFilterMix > 0 || GateVerbMix > 0 || ReverseVerbMix > 0
                || MultiTapMix > 0 || DoublerMix > 0 || FormantMix > 0
                || LadderMix > 0 || SubBassMix > 0 || BassEnhMix > 0 || AirMix > 0 || TubeMix > 0
                || TapeSatMix > 0 || DiodeMix > 0 || FuzzAmount > 0 || ChipMix > 0 || FreqShiftMix > 0
                || BeatRepeatMix > 0 || GrainMix > 0 || PingPongMix > 0 || DubMix > 0
                || CathedralMix > 0 || RoomMix > 0 || AmbienceMix > 0 || WahPedalMix > 0 || TalkMix > 0
                || DuckerMix > 0 || RiderAmount > 0 || Math.Abs(StereoWidth - 1) > 0.001 || BassMonoFreq > 0
                || HarmTremMix > 0 || GlitchMix > 0 || SweepMix > 0
                || OptoMix > 0 || FetMix > 0 || VcaMix > 0 || ExpMix > 0 || MSCompMix > 0
                || DynEqMix > 0 || PultecLow > 0 || PultecHigh > 0 || PultecAtten > 0
                || OverdriveMix > 0 || MetalMix > 0 || CabMix > 0 || MBDistMix > 0
                || FlutterMix > 0 || DimMix > 0 || EnvPhaserMix > 0 || EnvFlangeMix > 0
                || ChopMix > 0 || BarberMix > 0 || HalfSpeedMix > 0 || PitchEchoMix > 0 || FreezeMix > 0
                || RevDelayMix > 0 || EchoVerbMix > 0 || BloomMix > 0 || NonLinVerbMix > 0 || VocoderMix > 0
                || SpecGateMix > 0 || SpecBlurMix > 0 || SpecDelayMix > 0 || ConvMix > 0 || TankMix > 0
                || TuneMix > 0 || OctUpMix > 0 || ChordMix > 0 || UpCompMix > 0 || TranGateMix > 0
                || ShuffleMix > 0 || MonoMix > 0 || PhaseMix > 0 || LoopMix > 0 || StutterGateMix > 0;
        }
    }
}
