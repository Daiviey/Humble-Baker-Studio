# Effect Reference

Humble Baker Studio ships **113 effects**. Every effect is a Patch-graph node: right-click the Patch graph and choose **Add / Insert FX module**. There is no rack or quick-add path.

## How effects run

- Each node owns its own effect settings; the node label shows the effect type.
- Nodes can be chained, split, merged, grouped and routed through buses like a modular rack.
- A **shared FX node** (bus/group/track FX) processes everything routed into it.
- Select a node to edit its parameters in the Knobs tab (or the node inspector).
- Mix controls are wet/dry blends: `0` normally means the effect is off, `1` is fully wet unless the description says otherwise.
- **Sidechain**: for shared bus/group FX, set **Sidechain Mix > 0**; the bottom-most connected input (largest Y) becomes the trigger.

## Quick recipes

| Goal | Chain |
|---|---|
| Warm master bus | `Pultec EQ -> VCA compressor -> Tape saturator -> Limiter` |
| Club sidechain pump | Bus FX with `Sidechain Mix > 0`, add `Opto compressor`; route kick to the lower input |
| Wide pad | `Dimension -> Chorus -> Plate -> Tilt EQ` |
| Aggressive acid | `Ladder filter -> Overdrive -> Metal drive -> Clipper` |
| Dub echo | `Dub delay -> Echo-verb -> Non-linear reverb` |
| Broken IDM beat | `Beat repeat -> Glitch gate -> Chopper -> Grain` |
| Vocal chop | `Formant -> Talk box -> Vocoder -> De-esser` |
| Ambient texture | `Spectral freeze -> Bloom reverb -> Cathedral reverb -> Shimmer` |

## Core effect set (v1.0.0-v1.0.7)

### Filter (`filter`)

| Control | Range | Description |
|---|---|---|
| `Filter` | - | Filter type: off, lowpass, highpass, bandpass. |
| `Cutoff` | 30 - 16000 | Cutoff in Hz. |
| `Resonance` | 0 - 0.95 | Resonance. |
| `FilterLfoRate` | 0 - 30 | Filter LFO rate in Hz (0 = off). Wobble and auto-wah. |
| `FilterLfoDepth` | 0 - 1 | Filter LFO depth in octaves-ish. |

### Drive (`drive`)

| Control | Range | Description |
|---|---|---|
| `Drive` | 0 - 1 | Saturation. |

### Crush (`crush`)

| Control | Range | Description |
|---|---|---|
| `Bits` | 2 - 16 | Bit depth (16 = clean). |
| `Downsample` | 1 - 32 | Sample-rate reduction factor (1 = off, 8 = lo-fi aliasing). |

### Fold (`fold`)

| Control | Range | Description |
|---|---|---|
| `Fold` | 0 - 1 | Wavefolder. |

### Ring mod (`ring`)

| Control | Range | Description |
|---|---|---|
| `RingMix` | 0 - 1 | Ring modulator mix. |
| `RingFreq` | 0 - 4000 | Ring modulator frequency in Hz. |

### Stutter (`stutter`)

| Control | Range | Description |
|---|---|---|
| `StutterBeats` | 0 - 4 | Stutter block length in beats (0 = off). 0.25 = sixteenth-note stutters. |
| `StutterDiv` | 2 - 16 | Each stuttered block repeats its first 1/N. |
| `StutterChance` | 0 - 1 | Chance that a block stutters. |

### Chorus (`chorus`)

| Control | Range | Description |
|---|---|---|
| `Chorus` | 0 - 1 | Chorus / flanger mix. |
| `ChorusRate` | 0.05 - 8 | Chorus rate in Hz. |
| `ChorusDepth` | 0 - 1 | Chorus depth. |

### Delay (`delay`)

| Control | Range | Description |
|---|---|---|
| `DelayMix` | 0 - 1 | Ping-pong delay mix. |
| `DelayBeats` | 0.0625 - 8 | Delay time in beats (0.75 = dotted eighth). |
| `DelayFeedback` | 0 - 0.95 | Delay feedback. |

### Reverb (`reverb`)

| Control | Range | Description |
|---|---|---|
| `ReverbMix` | 0 - 1 | Reverb mix. |
| `ReverbSize` | 0 - 1 | Reverb size / decay. |

### Phaser (`phaser`)

| Control | Range | Description |
|---|---|---|
| `PhaserMix` | 0 - 1 | Phaser mix. |
| `PhaserRate` | 0.01 - 8 | Phaser LFO rate in Hz. |
| `PhaserDepth` | 0 - 1 | Phaser LFO depth. |
| `PhaserStages` | 1 - 12 | Phaser stages (notch count). |

### Flanger (`flanger`)

| Control | Range | Description |
|---|---|---|
| `FlangerMix` | 0 - 1 | Flanger mix. |
| `FlangerRate` | 0.02 - 10 | Flanger rate in Hz. |
| `FlangerDepth` | 0 - 1 | Flanger depth. |
| `FlangerFb` | 0 - 0.95 | Flanger feedback (jet whoosh). |

### Tape stop (`tapestop`)

| Control | Range | Description |
|---|---|---|
| `TapeStop` | 0 - 1 | Tape stop effect (1 = full stop-and-restart with pitch slam). |
| `TapeStopRate` | 0.05 - 3 | Tape stop time in seconds (how long the stop takes). |

### Tape delay (`tapedelay`)

| Control | Range | Description |
|---|---|---|
| `TapeDelayMix` | 0 - 1 | Tape delay mix. |
| `TapeDelayTime` | 10 - 2000 | Tape delay time in milliseconds. |
| `TapeDelayFb` | 0 - 0.95 | Tape delay feedback. |
| `TapeDelayWow` | 0 - 1 | Tape delay wow/flutter depth (wobble). |

### Plate (`plate`)

| Control | Range | Description |
|---|---|---|
| `PlateMix` | 0 - 1 | Plate reverb mix. |
| `PlateSize` | 0 - 1 | Plate reverb size (small room to big hall). |
| `PlateDamping` | 0 - 1 | Plate reverb damping (dark to bright). |

### Vinyl (`vinyl`)

| Control | Range | Description |
|---|---|---|
| `VinylCrackle` | 0 - 1 | Vinyl crackle amount. |
| `TapeHiss` | 0 - 1 | Tape hiss amount. |
| `TapeWow` | 0 - 1 | Tape wow/flutter pitch wobble. |

### Compressor (`comp`)

| Control | Range | Description |
|---|---|---|
| `CompThresh` | -40 - 0 | Compressor threshold in dB (higher = gentler). |
| `CompRatio` | 1 - 12 | Compressor ratio (1 = off). |
| `CompAttack` | 0.1 - 100 | Compressor attack in ms. |
| `CompRelease` | 10 - 1000 | Compressor release in ms. |
| `CompMakeup` | -12 - 24 | Compressor makeup gain in dB (after compression, before the limiter). |
| `SidechainSource` | - | Sidechain source track name (empty = self). Duck this track's gain when the source plays. |
| `SidechainMix` | 0 - 1 | Sidechain duck amount (1 = fully silent on source hits). |
| `SidechainAttack` | 0.5 - 100 | Sidechain attack in ms (how fast the duck starts). |
| `SidechainRelease` | 10 - 1000 | Sidechain release in ms (how long recovery takes). |

### EQ + width (`eq`)

| Control | Range | Description |
|---|---|---|
| `LowShelf` | -12 - 12 | Low shelf gain in dB (bass boost/cut). |
| `HighShelf` | -12 - 12 | High shelf gain in dB (air boost/cut). |
| `MidPeak` | -12 - 12 | Mid peak gain in dB (presence). |
| `Width` | 0 - 1.6 | Stereo width (1 = unchanged, 0 = mono, >1 = wide). |
| `PitchShift` | -12 - 12 | Pitch shift in semitones (formant preserved-ish via resample). |

### Width (`width`)

| Control | Range | Description |
|---|---|---|
| `Width` | 0 - 1.6 | Stereo width (1 = unchanged, 0 = mono, >1 = wide). |
| `PitchShift` | -12 - 12 | Pitch shift in semitones (formant preserved-ish via resample). |

### Saturator (`saturator`)

| Control | Range | Description |
|---|---|---|
| `Saturator` | 0 - 1 | Saturator amount (0 = off). |
| `SatBias` | -0.5 - 0.5 | Saturator DC bias (asymmetry). |
| `SatTone` | 0 - 1 | Saturator tone: dark to bright. |
| `SatMix` | 0 - 1 | Saturator dry/wet mix. |

### Exciter (`exciter`)

| Control | Range | Description |
|---|---|---|
| `Exciter` | 0 - 1 | High-frequency exciter amount (0 = off). |
| `ExciterFreq` | 1000 - 12000 | Exciter crossover frequency in Hz. |
| `ExciterMix` | 0 - 1 | Exciter dry/wet mix. |

### Rectifier (`rectify`)

| Control | Range | Description |
|---|---|---|
| `RectifyMix` | 0 - 1 | Rectifier mix (0 = off). |
| `RectifyHalf` | on/off | Half-wave rectification (otherwise full-wave). |
| `RectifyDrive` | 0 - 1 | Rectifier pre-gain. |

### Gate (`gate`)

| Control | Range | Description |
|---|---|---|
| `GateAmount` | 0 - 1 | Noise gate amount (0 = off). Attenuates the signal below the threshold. |
| `GateThreshold` | -60 - 0 | Gate threshold in dBFS. |
| `GateAttack` | 0.1 - 50 | Gate attack in ms. |
| `GateRelease` | 10 - 500 | Gate release in ms. |

### Transient (`transient`)

| Control | Range | Description |
|---|---|---|
| `TransientAttack` | -1 - 1 | Transient attack boost/cut (positive = punchier). |
| `TransientSustain` | -1 - 1 | Transient sustain boost/cut (negative = tighter). |
| `TransientSensitivity` | 0 - 1 | Transient detector sensitivity. |

### Tremolo (`tremolo`)

| Control | Range | Description |
|---|---|---|
| `TremoloMix` | 0 - 1 | Tremolo mix (0 = off). |
| `TremoloRate` | 0.1 - 20 | Tremolo rate in Hz. |
| `TremoloDepth` | 0 - 1 | Tremolo depth. |

### Auto-pan (`autopan`)

| Control | Range | Description |
|---|---|---|
| `AutoPanMix` | 0 - 1 | Auto-pan mix (0 = off). |
| `AutoPanRate` | 0.05 - 5 | Auto-pan rate in Hz. |
| `AutoPanDepth` | 0 - 1 | Auto-pan depth. |

### Vibrato (`vibrato`)

| Control | Range | Description |
|---|---|---|
| `VibratoMix` | 0 - 1 | Vibrato mix (0 = off). |
| `VibratoRate` | 0.1 - 12 | Vibrato rate in Hz. |
| `VibratoDepth` | 0 - 1 | Vibrato depth (pitch bend). |

### Auto-wah (`autowah`)

| Control | Range | Description |
|---|---|---|
| `AutoWahMix` | 0 - 1 | Auto-wah mix (0 = off). |
| `AutoWahRate` | 0.1 - 8 | Auto-wah LFO rate in Hz. |
| `AutoWahDepth` | 0 - 1 | Auto-wah filter depth. |
| `AutoWahResonance` | 0 - 0.95 | Auto-wah resonance. |

### Comb (`comb`)

| Control | Range | Description |
|---|---|---|
| `CombMix` | 0 - 1 | Comb filter mix (0 = off). |
| `CombFreq` | 20 - 2000 | Comb filter frequency in Hz. |
| `CombFeedback` | 0 - 0.95 | Comb filter feedback. |

### Spring reverb (`spring`)

| Control | Range | Description |
|---|---|---|
| `SpringMix` | 0 - 1 | Spring reverb mix (0 = off). |
| `SpringDecay` | 0 - 1 | Spring reverb decay. |
| `SpringTone` | 0 - 1 | Spring reverb tone. |

### Shimmer (`shimmer`)

| Control | Range | Description |
|---|---|---|
| `ShimmerMix` | 0 - 1 | Shimmer reverb mix (0 = off). |
| `ShimmerPitch` | -12 - 12 | Shimmer pitch shift in semitones. |
| `ShimmerDecay` | 0 - 1 | Shimmer decay. |

### Haas (`haas`)

| Control | Range | Description |
|---|---|---|
| `HaasMix` | 0 - 1 | Haas widener mix (0 = off). |
| `HaasDelay` | 1 - 40 | Haas delay in ms. |

### Limiter (`limiter`)

| Control | Range | Description |
|---|---|---|
| `LimiterOn` | on/off | Brickwall limiter on/off. |
| `LimiterThreshold` | -24 - 0 | Limiter threshold in dBFS. |
| `LimiterRelease` | 10 - 500 | Limiter release in ms. |
| `LimiterCeiling` | -6 - 0 | Limiter output ceiling in dBFS. |

## Sound-design pack (v1.0.8)

### Ensemble (`ensemble`)

| Control | Range | Description |
|---|---|---|
| `EnsembleMix` | 0 - 1 | Ensemble chorus mix (0 = off). |
| `EnsembleRate` | 0.05 - 8 | Ensemble rate in Hz. |
| `EnsembleDepth` | 0 - 1 | Ensemble depth. |

### Rotary (`rotary`)

| Control | Range | Description |
|---|---|---|
| `RotaryMix` | 0 - 1 | Rotary speaker mix (0 = off). |
| `RotaryRate` | 0.1 - 10 | Rotary rate in Hz. |
| `RotaryDepth` | 0 - 1 | Rotary depth. |

### Octaver (`octaver`)

| Control | Range | Description |
|---|---|---|
| `OctaverMix` | 0 - 1 | Sub-octave mix (0 = off). |

### Harmonizer (`harmonizer`)

| Control | Range | Description |
|---|---|---|
| `HarmonizerMix` | 0 - 1 | Harmonizer mix (0 = off). |
| `HarmonizerSemi` | -12 - 12 | Harmonizer interval in semitones. |

### Resonator (`resonator`)

| Control | Range | Description |
|---|---|---|
| `ResonatorMix` | 0 - 1 | Resonator mix (0 = off). |
| `ResonatorFreq` | 40 - 2000 | Resonator base frequency in Hz. |
| `ResonatorFeedback` | 0 - 0.95 | Resonator feedback. |

### Tilt EQ (`tilteq`)

| Control | Range | Description |
|---|---|---|
| `TiltGain` | -12 - 12 | Tilt EQ in dB: positive brightens, negative darkens. |

### Mid/Side EQ (`midside`)

| Control | Range | Description |
|---|---|---|
| `MidGain` | -12 - 12 | Mid gain in dB. |
| `SideGain` | -12 - 12 | Side gain in dB. |

### De-esser (`deesser`)

| Control | Range | Description |
|---|---|---|
| `DeEsserAmount` | 0 - 1 | De-esser amount (0 = off). |
| `DeEsserFreq` | 2000 - 12000 | De-esser crossover in Hz. |

### Multiband comp (`mbcomp`)

| Control | Range | Description |
|---|---|---|
| `MBOn` | on/off | Multiband compressor on/off. |
| `MBLowThresh` | -40 - 0 | Low band threshold in dB. |
| `MBMidThresh` | -40 - 0 | Mid band threshold in dB. |
| `MBHighThresh` | -40 - 0 | High band threshold in dB. |
| `MBRatio` | 1 - 12 | Multiband ratio. |
| `MBMakeup` | -12 - 24 | Multiband makeup gain in dB. |

### Clipper (`clipper`)

| Control | Range | Description |
|---|---|---|
| `ClipperAmount` | 0 - 1 | Clipper amount (0 = off). |
| `ClipperDrive` | 0 - 1 | Clipper pre-gain. |

### Auto-filter (`autofilter`)

| Control | Range | Description |
|---|---|---|
| `AutoFilterMix` | 0 - 1 | Auto-filter mix (0 = off). |
| `AutoFilterSensitivity` | 0 - 1 | Auto-filter envelope sensitivity. |
| `AutoFilterResonance` | 0 - 0.95 | Auto-filter resonance. |

### Gated reverb (`gatedverb`)

| Control | Range | Description |
|---|---|---|
| `GateVerbMix` | 0 - 1 | Gated reverb mix (0 = off). |
| `GateVerbDecay` | 0 - 1 | Gated reverb decay. |
| `GateVerbHold` | 0.02 - 2 | Gated reverb hold in seconds. |

### Reverse reverb (`reverseverb`)

| Control | Range | Description |
|---|---|---|
| `ReverseVerbMix` | 0 - 1 | Reverse reverb mix (0 = off). |
| `ReverseVerbDecay` | 0 - 1 | Reverse reverb decay. |

### Multi-tap (`multitap`)

| Control | Range | Description |
|---|---|---|
| `MultiTapMix` | 0 - 1 | Multi-tap delay mix (0 = off). |
| `MultiTapTime` | 20 - 1000 | Multi-tap time in ms. |
| `MultiTapFb` | 0 - 0.95 | Multi-tap feedback. |

### Doubler (`doubler`)

| Control | Range | Description |
|---|---|---|
| `DoublerMix` | 0 - 1 | Doubler mix (0 = off). |
| `DoublerDelay` | 1 - 40 | Doubler delay in ms. |

### Formant (`formant`)

| Control | Range | Description |
|---|---|---|
| `FormantMix` | 0 - 1 | Formant filter mix (0 = off). |
| `FormantVowel` | 0 - 4 | Formant vowel 0=a 1=e 2=i 3=o 4=u. |
| `FormantShift` | 0.5 - 2 | Formant shift (0.5 = down, 1 = natural, 2 = up). |

## Performance pack (v1.0.10)

### Ladder filter (`ladder`)

| Control | Range | Description |
|---|---|---|
| `LadderMix` | 0 - 1 | Ladder filter mix (0 = off). |
| `LadderCutoff` | 40 - 16000 | Ladder cutoff in Hz. |
| `LadderReso` | 0 - 0.95 | Ladder resonance. |
| `LadderDrive` | 0 - 1 | Ladder input drive. |

### Tube preamp (`tube`)

| Control | Range | Description |
|---|---|---|
| `TubeDrive` | 0 - 1 | Tube preamp drive. |
| `TubeBias` | -1 - 1 | Tube bias (asymmetry). |
| `TubeMix` | 0 - 1 | Tube preamp mix (0 = off). |

### Tape saturator (`tapesat`)

| Control | Range | Description |
|---|---|---|
| `TapeSatDrive` | 0 - 1 | Tape saturation drive. |
| `TapeSatTone` | 0 - 1 | Tape saturation tone (0 dark, 1 bright). |
| `TapeSatMix` | 0 - 1 | Tape saturation mix (0 = off). |

### Diode drive (`diode`)

| Control | Range | Description |
|---|---|---|
| `DiodeDrive` | 0 - 1 | Diode distortion drive. |
| `DiodeTone` | 0 - 1 | Diode distortion tone. |
| `DiodeMix` | 0 - 1 | Diode distortion mix (0 = off). |

### Fuzz (`fuzz`)

| Control | Range | Description |
|---|---|---|
| `FuzzAmount` | 0 - 1 | Fuzz amount (0 = off). |
| `FuzzGate` | 0 - 1 | Fuzz noise gate. |
| `FuzzMix` | 0 - 1 | Fuzz mix. |

### Chip tune (`chiptune`)

| Control | Range | Description |
|---|---|---|
| `ChipMix` | 0 - 1 | Chip-tune mix (0 = off). |
| `ChipBits` | 2 - 12 | Chip-tune bit depth. |
| `ChipRate` | 1 - 44 | Chip-tune sample rate in kHz. |

### Sub bass (`subbass`)

| Control | Range | Description |
|---|---|---|
| `SubBassMix` | 0 - 1 | Sub-bass mix (0 = off). |
| `SubBassFreq` | 20 - 120 | Sub-bass frequency in Hz. |
| `SubBassDrive` | 0 - 1 | Sub-bass drive. |

### Bass enhancer (`bassenh`)

| Control | Range | Description |
|---|---|---|
| `BassEnhMix` | 0 - 1 | Bass enhancer mix (0 = off). |
| `BassEnhFreq` | 40 - 300 | Bass enhancer crossover in Hz. |
| `BassEnhAmount` | 0 - 1 | Bass enhancer amount. |

### Frequency shifter (`freqshift`)

| Control | Range | Description |
|---|---|---|
| `FreqShiftMix` | 0 - 1 | Frequency shifter mix (0 = off). |
| `FreqShiftHz` | -1000 - 1000 | Frequency shift in Hz (positive = up). |

### Air band (`airband`)

| Control | Range | Description |
|---|---|---|
| `AirMix` | 0 - 1 | Air band mix (0 = off). |
| `AirFreq` | 2000 - 16000 | Air band frequency in Hz. |
| `AirGain` | 0 - 12 | Air band gain in dB. |

### Stereo image (`stereoimage`)

| Control | Range | Description |
|---|---|---|
| `StereoWidth` | 0 - 2 | Stereo width (1 = normal). |
| `BassMonoFreq` | 0 - 500 | Bass mono crossover in Hz (0 = off). |

### Ping-pong delay (`pingpong`)

| Control | Range | Description |
|---|---|---|
| `PingPongMix` | 0 - 1 | Ping-pong delay mix (0 = off). |
| `PingPongTime` | 20 - 1200 | Ping-pong time in ms. |
| `PingPongFb` | 0 - 0.95 | Ping-pong feedback. |

### Dub delay (`dubdelay`)

| Control | Range | Description |
|---|---|---|
| `DubMix` | 0 - 1 | Dub delay mix (0 = off). |
| `DubTime` | 40 - 1200 | Dub delay time in ms. |
| `DubFb` | 0 - 0.95 | Dub delay feedback. |
| `DubTone` | 0 - 1 | Dub delay tone (0 dark, 1 bright). |

### Cathedral reverb (`cathedral`)

| Control | Range | Description |
|---|---|---|
| `CathedralMix` | 0 - 1 | Cathedral reverb mix (0 = off). |
| `CathedralSize` | 0 - 1 | Cathedral reverb size. |
| `CathedralDamp` | 0 - 1 | Cathedral reverb damping. |

### Room reverb (`roomverb`)

| Control | Range | Description |
|---|---|---|
| `RoomMix` | 0 - 1 | Room reverb mix (0 = off). |
| `RoomSize` | 0 - 1 | Room reverb size. |
| `RoomDamp` | 0 - 1 | Room reverb damping. |

### Ambience (`ambience`)

| Control | Range | Description |
|---|---|---|
| `AmbienceMix` | 0 - 1 | Ambience mix (0 = off). |
| `AmbienceSize` | 0 - 1 | Ambience size. |
| `AmbienceDamp` | 0 - 1 | Ambience damping. |

### Wah pedal (`wahpedal`)

| Control | Range | Description |
|---|---|---|
| `WahPedalMix` | 0 - 1 | Wah pedal mix (0 = off). |
| `WahPedalRate` | 0.05 - 10 | Wah pedal LFO rate in Hz. |
| `WahPedalDepth` | 0 - 1 | Wah pedal depth. |
| `WahPedalQ` | 0 - 0.95 | Wah pedal resonance. |

### Talk box (`talkbox`)

| Control | Range | Description |
|---|---|---|
| `TalkMix` | 0 - 1 | Talk box mix (0 = off). |
| `TalkRate` | 0.05 - 12 | Talk box vowel rate in Hz. |
| `TalkVowel` | 0 - 4 | Talk box vowel offset 0-4. |

### Ducker (`ducker`)

| Control | Range | Description |
|---|---|---|
| `DuckerMix` | 0 - 1 | Ducker mix (0 = off). |
| `DuckerThreshold` | -60 - 0 | Ducker threshold in dB. |
| `DuckerDepth` | 0 - 1 | Ducker depth. |
| `DuckerRelease` | 10 - 1000 | Ducker release in ms. |

### Level rider (`rider`)

| Control | Range | Description |
|---|---|---|
| `RiderAmount` | 0 - 1 | Level rider amount (0 = off). |
| `RiderTarget` | -40 - 0 | Level rider target in dB. |
| `RiderSpeed` | 0.01 - 1 | Level rider speed. |

### Harmonic tremolo (`harmtrem`)

| Control | Range | Description |
|---|---|---|
| `HarmTremMix` | 0 - 1 | Harmonic tremolo mix (0 = off). |
| `HarmTremRate` | 0.1 - 12 | Harmonic tremolo rate in Hz. |
| `HarmTremDepth` | 0 - 1 | Harmonic tremolo depth. |

### Beat repeat (`beatrepeat`)

| Control | Range | Description |
|---|---|---|
| `BeatRepeatMix` | 0 - 1 | Beat repeat mix (0 = off). |
| `BeatRepeatDiv` | 2 - 16 | Beat repeat divisions per half second. |
| `BeatRepeatChance` | 0 - 1 | Beat repeat chance. |

### Granular cloud (`grain`)

| Control | Range | Description |
|---|---|---|
| `GrainMix` | 0 - 1 | Granular cloud mix (0 = off). |
| `GrainSize` | 10 - 300 | Grain size in ms. |
| `GrainDensity` | 1 - 80 | Grains per second. |

### Glitch gate (`glitchgate`)

| Control | Range | Description |
|---|---|---|
| `GlitchMix` | 0 - 1 | Glitch gate mix (0 = off). |
| `GlitchDensity` | 0 - 1 | Glitch gate density. |
| `GlitchSeed` | 0 - 99 | Glitch gate seed. |

### Sweep filter (`sweep`)

| Control | Range | Description |
|---|---|---|
| `SweepMix` | 0 - 1 | Sweep filter mix (0 = off). |
| `SweepRate` | 0.05 - 10 | Sweep filter LFO rate in Hz. |
| `SweepDepth` | 0 - 1 | Sweep filter depth. |
| `SweepQ` | 0 - 0.95 | Sweep filter resonance. |
| `SweepCenter` | 80 - 8000 | Sweep filter center in Hz. |

## Dynamics and space pack (v1.0.11)

### Opto compressor (`optocomp`)

| Control | Range | Description |
|---|---|---|
| `OptoMix` | 0 - 1 | Optical compressor mix (0 = off). |
| `OptoThresh` | -60 - 0 | Optical compressor threshold in dB. |
| `OptoRatio` | 1 - 20 | Optical compressor ratio. |
| `OptoRelease` | 20 - 1000 | Optical compressor release in ms. |

### FET compressor (`fetcomp`)

| Control | Range | Description |
|---|---|---|
| `FetMix` | 0 - 1 | FET compressor mix (0 = off). |
| `FetThresh` | -60 - 0 | FET compressor threshold in dB. |
| `FetRatio` | 1 - 20 | FET compressor ratio. |
| `FetAttack` | 0.1 - 50 | FET compressor attack in ms. |

### VCA compressor (`vcacomp`)

| Control | Range | Description |
|---|---|---|
| `VcaMix` | 0 - 1 | VCA bus compressor mix (0 = off). |
| `VcaThresh` | -60 - 0 | VCA compressor threshold in dB. |
| `VcaRatio` | 1 - 20 | VCA compressor ratio. |
| `VcaRelease` | 20 - 1000 | VCA compressor release in ms. |

### Expander (`expander`)

| Control | Range | Description |
|---|---|---|
| `ExpMix` | 0 - 1 | Downward expander mix (0 = off). |
| `ExpThresh` | -70 - -10 | Expander threshold in dB. |
| `ExpRatio` | 1 - 8 | Expander ratio. |
| `ExpRange` | 3 - 48 | Expander maximum range in dB. |

### Mid/Side comp (`mscomp`)

| Control | Range | Description |
|---|---|---|
| `MSCompMix` | 0 - 1 | Mid/side compressor mix (0 = off). |
| `MSCompThresh` | -60 - 0 | Mid/side compressor threshold in dB. |
| `MSCompRatio` | 1 - 20 | Mid/side compressor ratio. |
| `MSCompRelease` | 20 - 1000 | Mid/side compressor release in ms. |

### Dynamic EQ (`dyneq`)

| Control | Range | Description |
|---|---|---|
| `DynEqMix` | 0 - 1 | Dynamic EQ mix (0 = off). |
| `DynEqFreq` | 80 - 12000 | Dynamic EQ frequency in Hz. |
| `DynEqThresh` | -60 - 0 | Dynamic EQ threshold in dB. |
| `DynEqAmount` | 0 - 1 | Dynamic EQ gain reduction amount. |

### Pultec EQ (`pulteceq`)

| Control | Range | Description |
|---|---|---|
| `PultecLow` | 0 - 12 | Pultec low boost in dB. |
| `PultecHigh` | 0 - 12 | Pultec high boost in dB. |
| `PultecAtten` | 0 - 12 | Pultec high attenuation in dB (boost and cut together for the classic curve). |

### Overdrive (`overdrive`)

| Control | Range | Description |
|---|---|---|
| `OverdriveDrive` | 0 - 1 | Overdrive drive. |
| `OverdriveTone` | 0 - 1 | Overdrive tone (0 dark, 1 bright). |
| `OverdriveMix` | 0 - 1 | Overdrive mix (0 = off). |

### Metal drive (`metaldrive`)

| Control | Range | Description |
|---|---|---|
| `MetalDrive` | 0 - 1 | Metal distortion drive. |
| `MetalMid` | 0 - 1 | Metal distortion mid scoop (0 = flat, 1 = scooped). |
| `MetalMix` | 0 - 1 | Metal distortion mix (0 = off). |

### Speaker cab (`cabsim`)

| Control | Range | Description |
|---|---|---|
| `CabDrive` | 0 - 1 | Speaker simulator drive. |
| `CabTone` | 0 - 1 | Speaker simulator tone. |
| `CabMix` | 0 - 1 | Speaker simulator mix (0 = off). |

### Multiband drive (`mbdist`)

| Control | Range | Description |
|---|---|---|
| `MBDistMix` | 0 - 1 | Multiband distortion mix (0 = off). |
| `MBDistLow` | 0 - 1 | Multiband distortion low drive. |
| `MBDistMid` | 0 - 1 | Multiband distortion mid drive. |
| `MBDistHigh` | 0 - 1 | Multiband distortion high drive. |

### Tape flutter (`tapeflutter`)

| Control | Range | Description |
|---|---|---|
| `FlutterMix` | 0 - 1 | Tape flutter mix (0 = off). |
| `FlutterRate` | 0.05 - 8 | Tape flutter rate in Hz. |
| `FlutterDepth` | 0 - 1 | Tape flutter depth. |

### Dimension (`dimension`)

| Control | Range | Description |
|---|---|---|
| `DimMix` | 0 - 1 | Dimension stereo chorus mix (0 = off). |
| `DimRate` | 0.05 - 5 | Dimension rate in Hz. |
| `DimDepth` | 0 - 1 | Dimension depth. |

### Envelope phaser (`envphaser`)

| Control | Range | Description |
|---|---|---|
| `EnvPhaserMix` | 0 - 1 | Envelope phaser mix (0 = off). |
| `EnvPhaserThresh` | -60 - 0 | Envelope phaser threshold in dB. |
| `EnvPhaserDepth` | 0 - 1 | Envelope phaser depth. |
| `EnvPhaserRate` | 0 - 8 | Envelope phaser LFO rate in Hz. |

### Envelope flanger (`envflanger`)

| Control | Range | Description |
|---|---|---|
| `EnvFlangeMix` | 0 - 1 | Envelope flanger mix (0 = off). |
| `EnvFlangeThresh` | -60 - 0 | Envelope flanger threshold in dB. |
| `EnvFlangeDepth` | 0 - 1 | Envelope flanger depth. |
| `EnvFlangeFb` | 0 - 0.95 | Envelope flanger feedback. |

### Chopper (`chopper`)

| Control | Range | Description |
|---|---|---|
| `ChopMix` | 0 - 1 | Chopper mix (0 = off). |
| `ChopRate` | 0.5 - 30 | Chopper rate in Hz. |
| `ChopDepth` | 0 - 1 | Chopper depth. |
| `ChopShape` | 0 - 1 | Chopper shape (0 sine, 1 square). |

### Barber-pole (`barberpole`)

| Control | Range | Description |
|---|---|---|
| `BarberMix` | 0 - 1 | Barber-pole phaser mix (0 = off). |
| `BarberRate` | 0.05 - 5 | Barber-pole rate in Hz. |
| `BarberDepth` | 0 - 1 | Barber-pole depth. |
| `BarberStages` | 2 - 8 | Barber-pole allpass stages. |

### Half speed (`halfspeed`)

| Control | Range | Description |
|---|---|---|
| `HalfSpeedMix` | 0 - 1 | Half-speed mix (0 = off). |
| `HalfSpeedAmount` | 0 - 1 | Half-speed amount (1 = half speed). |

### Pitch echo (`pitchecho`)

| Control | Range | Description |
|---|---|---|
| `PitchEchoMix` | 0 - 1 | Pitch echo mix (0 = off). |
| `PitchEchoTime` | 40 - 1200 | Pitch echo time in ms. |
| `PitchEchoFb` | 0 - 0.9 | Pitch echo feedback. |
| `PitchEchoSemi` | -12 - 12 | Pitch echo interval in semitones. |

### Spectral freeze (`spectralfreeze`)

| Control | Range | Description |
|---|---|---|
| `FreezeMix` | 0 - 1 | Spectral freeze mix (0 = off). |
| `FreezeThreshold` | -60 - 0 | Freeze trigger threshold in dB. |
| `FreezeDecay` | 0 - 1 | Freeze decay. |

### Reverse delay (`revdelay`)

| Control | Range | Description |
|---|---|---|
| `RevDelayMix` | 0 - 1 | Reverse delay mix (0 = off). |
| `RevDelayTime` | 60 - 1200 | Reverse delay block time in ms. |
| `RevDelayFb` | 0 - 0.9 | Reverse delay feedback. |

### Echo-verb (`echoverb`)

| Control | Range | Description |
|---|---|---|
| `EchoVerbMix` | 0 - 1 | Echo-verb mix (0 = off). |
| `EchoVerbTime` | 40 - 900 | Echo-verb delay time in ms. |
| `EchoVerbFb` | 0 - 0.9 | Echo-verb feedback. |
| `EchoVerbSize` | 0 - 1 | Echo-verb reverb size. |

### Bloom reverb (`bloomverb`)

| Control | Range | Description |
|---|---|---|
| `BloomMix` | 0 - 1 | Bloom reverb mix (0 = off). |
| `BloomSize` | 0 - 1 | Bloom reverb size. |
| `BloomThresh` | -60 - 0 | Bloom trigger threshold in dB. |
| `BloomAttack` | 0.01 - 1 | Bloom attack in seconds. |

### Non-linear verb (`nonlinverb`)

| Control | Range | Description |
|---|---|---|
| `NonLinVerbMix` | 0 - 1 | Non-linear reverb mix (0 = off). |
| `NonLinVerbSize` | 0 - 1 | Non-linear reverb size. |
| `NonLinVerbDrive` | 0 - 1 | Non-linear reverb drive. |
| `NonLinVerbGate` | 0 - 1 | Non-linear reverb gate. |

### Vocoder (`vocoder`)

| Control | Range | Description |
|---|---|---|
| `VocoderMix` | 0 - 1 | Vocoder mix (0 = off). |
| `VocoderBands` | 4 - 12 | Vocoder band count. |
| `VocoderRelease` | 10 - 500 | Vocoder envelope release in ms. |
| `VocoderCarrier` | 0 - 1 | Vocoder carrier (0 = noise, 1 = saw). |

## Spectral, convolution and performance pack (v1.0.19)

### Spectral gate (`specgate`)

| Control | Range | Description |
|---|---|---|
| `SpecGateMix` | 0 - 1 | Spectral gate mix (0 = off). |
| `SpecGateThresh` | 0.001 - 0.5 | Spectral gate threshold. |
| `SpecGateBands` | 2 - 8 | Spectral gate band count. |

### Spectral blur (`specblur`)

| Control | Range | Description |
|---|---|---|
| `SpecBlurMix` | 0 - 1 | Spectral blur mix (0 = off). |
| `SpecBlurAmount` | 0 - 1 | Spectral blur amount. |
| `SpecBlurSpread` | 0 - 1 | Spectral blur stereo spread. |

### Spectral delay (`specdelay`)

| Control | Range | Description |
|---|---|---|
| `SpecDelayMix` | 0 - 1 | Spectral delay mix (0 = off). |
| `SpecDelayTime` | 20 - 900 | Spectral delay time in ms. |
| `SpecDelayFb` | 0 - 0.9 | Spectral delay feedback. |
| `SpecDelaySpread` | 0 - 1 | Spectral delay band spread. |

### Convolution reverb (`convreverb`)

| Control | Range | Description |
|---|---|---|
| `ConvMix` | 0 - 1 | Convolution reverb mix (0 = off). |
| `ConvSize` | 0 - 1 | Convolution reverb size. |
| `ConvDamp` | 0 - 1 | Convolution reverb damping. |
| `ConvPre` | 0 - 120 | Convolution pre-delay in ms. |

### Tank reverb (`tankreverb`)

| Control | Range | Description |
|---|---|---|
| `TankMix` | 0 - 1 | Tank reverb mix (0 = off). |
| `TankDecay` | 0 - 1 | Tank reverb decay. |
| `TankTone` | 0 - 1 | Tank reverb tone. |
| `TankDrip` | 0 - 1 | Tank reverb drip. |

### Auto-tune (`autotune`)

| Control | Range | Description |
|---|---|---|
| `TuneMix` | 0 - 1 | Auto-tune mix (0 = off). |
| `TuneSpeed` | 0 - 1 | Auto-tune correction speed. |
| `TuneScale` | 0 - 2 | Auto-tune scale: 0 chromatic, 1 major, 2 minor. |
| `TuneMinHz` | 40 - 300 | Auto-tune minimum frequency in Hz. |

### Octave up (`octaveup`)

| Control | Range | Description |
|---|---|---|
| `OctUpMix` | 0 - 1 | Octave-up mix (0 = off). |
| `OctUpTone` | 0 - 1 | Octave-up tone. |

### Chord generator (`chordgen`)

| Control | Range | Description |
|---|---|---|
| `ChordMix` | 0 - 1 | Chord generator mix (0 = off). |
| `ChordType` | 0 - 3 | Chord type: 0 major, 1 minor, 2 fifth, 3 sus4. |
| `ChordSpread` | 0 - 1 | Chord voice spread. |

### Upward compressor (`upcomp`)

| Control | Range | Description |
|---|---|---|
| `UpCompMix` | 0 - 1 | Upward compressor mix (0 = off). |
| `UpCompThresh` | -60 - 0 | Upward compressor threshold in dB. |
| `UpCompRatio` | 1 - 8 | Upward compressor ratio. |
| `UpCompRange` | 0 - 36 | Maximum upward gain in dB. |

### Transient gate (`trangate`)

| Control | Range | Description |
|---|---|---|
| `TranGateMix` | 0 - 1 | Transient gate mix (0 = off). |
| `TranGateThresh` | -60 - 0 | Transient gate threshold in dB. |
| `TranGateHold` | 1 - 500 | Transient gate hold in ms. |
| `TranGateRelease` | 10 - 1000 | Transient gate release in ms. |

### Stereo shuffler (`shuffle`)

| Control | Range | Description |
|---|---|---|
| `ShuffleMix` | 0 - 1 | Stereo shuffler mix (0 = off). |
| `ShuffleLow` | 0 - 2 | Low-frequency side width. |
| `ShuffleHigh` | 0 - 2 | High-frequency side width. |
| `ShuffleFreq` | 40 - 800 | Stereo shuffler crossover in Hz. |

### Mono maker (`monomaker`)

| Control | Range | Description |
|---|---|---|
| `MonoMix` | 0 - 1 | Mono maker mix (0 = off). |
| `MonoFreq` | 20 - 500 | Mono maker crossover in Hz. |
| `MonoWidth` | 0 - 2 | Mono maker side width above crossover. |

### Phase aligner (`phasealign`)

| Control | Range | Description |
|---|---|---|
| `PhaseMix` | 0 - 1 | Phase aligner mix (0 = off). |
| `PhaseFreq` | 80 - 8000 | Phase aligner frequency in Hz. |
| `PhaseAmount` | 0 - 1 | Phase aligner amount. |

### Tape loop (`tapeloop`)

| Control | Range | Description |
|---|---|---|
| `LoopMix` | 0 - 1 | Tape loop mix (0 = off). |
| `LoopLength` | 50 - 2000 | Tape loop length in ms. |
| `LoopSpeed` | 0.25 - 2 | Tape loop speed. |
| `LoopFb` | 0 - 0.95 | Tape loop feedback. |

### Stutter gate (`stuttergate`)

| Control | Range | Description |
|---|---|---|
| `StutterGateMix` | 0 - 1 | Stutter gate mix (0 = off). |
| `StutterGateRate` | 0.5 - 30 | Stutter gate rate in Hz. |
| `StutterGateDiv` | 2 - 16 | Stutter gate divisions. |
| `StutterGateChance` | 0 - 1 | Stutter gate chance. |

