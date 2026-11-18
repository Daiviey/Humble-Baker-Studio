namespace HumbleBakerStudio.Audio;


// The list of automatable parameters shown by the Compose lane editor and the spectral automation
// painter. Param keys match AutoLane / AutoRange; Min/Max come from AutoRange so both features agree.
public readonly record struct AutoParamDef(string Module, string Name, string Param);

public static class AutoCatalog {
    public static IReadOnlyList<AutoParamDef> All { get; } = new List<AutoParamDef> {
        new("Track Mixer", "Volume", "vol"), new("Track Mixer", "Pan", "pan"), new("Track Mixer", "Low-pass", "lp"), new("Track Mixer", "High-pass", "hp"),
        new("Synth Filter", "Cutoff", "p:Cutoff"), new("Synth Filter", "Resonance", "p:Resonance"), new("Synth Filter", "Filter Env", "p:FilterEnv"),
        new("Synth Drive", "Drive", "p:Drive"), new("Synth Drive", "Bits", "p:Bits"), new("Synth Drive", "Fold", "p:Fold"),
        new("Synth FM", "FM Amount", "p:FmAmount"), new("Synth FM", "Unison", "p:Unison"), new("Synth FM", "Noise Level", "p:NoiseLevel"),
        new("Synth LFO", "LFO Rate", "p:LfoRate"), new("Synth LFO", "LFO Filter", "p:LfoFilter"), new("Synth LFO", "LFO Pitch", "p:LfoPitch"),
        new("Synth Wavetable", "Morph", "p:Morph"), new("Synth Wavetable", "Sync Ratio", "p:SyncRatio"), new("Synth Wavetable", "Pulse Width", "p:PulseWidth"),
        new("Track FX Filter", "Cutoff", "f:Cutoff"), new("Track FX Filter", "Resonance", "f:Resonance"), new("Track FX Filter", "Filter LFO Depth", "f:FilterLfoDepth"),
        new("Track FX Drive", "Drive", "f:Drive"), new("Track FX Drive", "Bits", "f:Bits"), new("Track FX Drive", "Fold", "f:Fold"),
        new("Track FX Ring", "Ring Mix", "f:RingMix"), new("Track FX Ring", "Ring Freq", "f:RingFreq"),
        new("Track FX Chorus", "Chorus", "f:Chorus"), new("Track FX Chorus", "Chorus Rate", "f:ChorusRate"), new("Track FX Chorus", "Chorus Depth", "f:ChorusDepth"),
        new("Track FX Delay", "Delay Mix", "f:DelayMix"), new("Track FX Delay", "Delay Beats", "f:DelayBeats"), new("Track FX Delay", "Delay Feedback", "f:DelayFeedback"),
        new("Track FX Reverb", "Reverb Mix", "f:ReverbMix"), new("Track FX Reverb", "Reverb Size", "f:ReverbSize"),
        new("Track FX Phaser", "Phaser Mix", "f:PhaserMix"), new("Track FX Phaser", "Phaser Rate", "f:PhaserRate"), new("Track FX Phaser", "Phaser Depth", "f:PhaserDepth"),
        new("Track FX Flanger", "Flanger Mix", "f:FlangerMix"), new("Track FX Flanger", "Flanger Rate", "f:FlangerRate"), new("Track FX Flanger", "Flanger Depth", "f:FlangerDepth"), new("Track FX Flanger", "Flanger Feedback", "f:FlangerFb"),
        new("Track FX Tape", "Tape Stop", "f:TapeStop"), new("Track FX Tape", "Tape Stop Time", "f:TapeStopRate"), new("Track FX Tape", "Tape Delay Mix", "f:TapeDelayMix"), new("Track FX Tape", "Tape Delay Ms", "f:TapeDelayTime"), new("Track FX Tape", "Tape Delay Feedback", "f:TapeDelayFb"), new("Track FX Tape", "Tape Delay Wow", "f:TapeDelayWow"),
        new("Track FX Plate/Vinyl", "Plate Mix", "f:PlateMix"), new("Track FX Plate/Vinyl", "Plate Size", "f:PlateSize"), new("Track FX Plate/Vinyl", "Vinyl Crackle", "f:VinylCrackle"), new("Track FX Plate/Vinyl", "Tape Hiss", "f:TapeHiss"), new("Track FX Plate/Vinyl", "Tape Wow", "f:TapeWow"),
        new("Track FX Compressor", "Threshold", "f:CompThresh"), new("Track FX Compressor", "Ratio", "f:CompRatio"), new("Track FX Compressor", "Attack", "f:CompAttack"), new("Track FX Compressor", "Release", "f:CompRelease"), new("Track FX Compressor", "Makeup Gain", "f:CompMakeup"), new("Track FX Compressor", "Sidechain Mix", "f:SidechainMix"), new("Track FX Compressor", "Sidechain Attack", "f:SidechainAttack"), new("Track FX Compressor", "Sidechain Release", "f:SidechainRelease"),
        new("Track FX EQ/Width", "Low Shelf", "f:LowShelf"), new("Track FX EQ/Width", "High Shelf", "f:HighShelf"), new("Track FX EQ/Width", "Mid Peak", "f:MidPeak"), new("Track FX EQ/Width", "Width", "f:Width"), new("Track FX EQ/Width", "Pitch Shift", "f:PitchShift"),
    };
}
