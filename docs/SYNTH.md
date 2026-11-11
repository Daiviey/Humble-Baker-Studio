# Synth Patch Reference

A **patch** is the full synth voice used by a track (`Synth = patch`). Every parameter below is editable in the Sound Lab grid or the Knobs tab (`Track synth patch`).

Patches are saved as instruments and can be morphed on the Meta synth tab, randomised, mutated, or designed from a text prompt.

## Oscillators

| Parameter | Type | Range | Description |
|---|---|---|---|
| `Osc1` | Wave | - | Oscillator 1 waveform: saw, square, pulse, tri, sine or noise. |
| `Level1` | double | 0 - 1 | Oscillator 1 level. |
| `Osc2` | Wave | - | Oscillator 2 waveform. |
| `Level2` | double | 0 - 1 | Oscillator 2 level. |
| `Detune2` | double | -1200 - 1200 | Oscillator 2 detune in cents. |
| `Semi2` | double | -36 - 36 | Oscillator 2 transpose in semitones. |
| `Osc3` | Wave | - | Oscillator 3 waveform. |
| `Level3` | double | 0 - 1 | Oscillator 3 level. |
| `Semi3` | double | -36 - 36 | Oscillator 3 transpose in semitones. |
| `SubLevel` | double | 0 - 1 | Sine sub oscillator one octave down. |
| `NoiseLevel` | double | 0 - 1 | Noise level. |
| `Noise` | NoiseColor | - | Noise colour: white, pink or brown. |
| `PulseWidth` | double | 0.05 - 0.95 | Pulse width for the pulse waveform. |
| `PulseWidth2` | double | 0.05 - 0.95 | Pulse width for oscillator 2's pulse waveform. |
| `SubShape` | Wave | - | Sub oscillator waveform: sine, tri, square or saw. |
| `NoiseRate` | double | 20 - 20000 | bits noise: sample-and-hold rate in Hz; dust noise: crackles per second. |

## Unison and FM

| Parameter | Type | Range | Description |
|---|---|---|---|
| `Unison` | double | 1 - 7 | Number of stacked detuned voices (1 to 7). |
| `UnisonSpread` | double | 0 - 100 | Unison detune spread in cents. |
| `FmAmount` | double | 0 - 10 | Frequency modulation of oscillator 1 (0 off, 1 bell-like, 5+ metallic). |
| `FmRatio` | double | 0.25 - 16 | FM modulator frequency as a ratio of the note (non-integer = inharmonic). |

## Filter

| Parameter | Type | Range | Description |
|---|---|---|---|
| `Filter` | FilterMode | - | Filter type: off, lowpass, highpass, bandpass. |
| `Cutoff` | double | 30 - 16000 | Base cutoff in Hz. |
| `Resonance` | double | 0 - 0.95 | Resonance (0.9+ screams, acid style). |
| `FilterEnv` | double | -1 - 1 | Filter envelope depth in 5-octave units; negative sweeps down. |
| `FilterDecay` | double | 0.01 - 8 | Filter envelope decay in seconds (long = slow sweep or riser). |
| `KeyTrack` | double | 0 - 1 | How much the cutoff follows the note pitch. |
| `FilterDrive` | double | 0 - 1 | Saturation into the filter (0-1), for gritty resonant filters. |
| `FilterVowel` | double | 0 - 4 | vowel filter: vowel position 0=a 1=e 2=i 3=o 4=u; cutoff modulation shifts the formants up and down (talking wah). |

## Amp envelope

| Parameter | Type | Range | Description |
|---|---|---|---|
| `Attack` | double | 0.001 - 5 | Attack seconds. |
| `Decay` | double | 0.01 - 8 | Decay seconds. |
| `Sustain` | double | 0 - 1 | Sustain level. |
| `Release` | double | 0.005 - 8 | Release seconds. |

## Pitch and LFO

| Parameter | Type | Range | Description |
|---|---|---|---|
| `PitchEnv` | double | -48 - 48 | Pitch envelope start offset in semitones, decaying to the note (kick drops, laser zaps, risers). |
| `PitchDecay` | double | 0.002 - 4 | Pitch envelope decay seconds. |
| `LfoRate` | double | 0.05 - 40 | LFO rate in Hz. |
| `LfoPitch` | double | 0 - 1200 | LFO to pitch (vibrato) in cents. |
| `LfoFilter` | double | 0 - 1 | LFO to cutoff (wobble). |
| `LfoAmp` | double | 0 - 1 | LFO to volume (tremolo). |

## Distortion

| Parameter | Type | Range | Description |
|---|---|---|---|
| `Fold` | double | 0 - 1 | Wavefolder amount. |
| `Drive` | double | 0 - 1 | Saturation drive. |
| `Bits` | double | 2 - 16 | Bit depth (16 = clean, 4 = crunchy). |
| `Gain` | double | 0 - 1.5 | Output gain. |
| `OutputDrive` | double | 0 - 1 | Post-synth output drive (0 = clean, 1 = saturated). |

## Wavetable and vowels

| Parameter | Type | Range | Description |
|---|---|---|---|
| `Harmonics` | double | 1 - 48 | additive/vowel oscillators: number of harmonics in the stack (more = brighter, buzzier). |
| `Tilt` | double | 0 - 3 | additive: spectral tilt, harmonic k has level k^-Tilt (0 = flat and bright, 1 = saw-like, 2 = soft, 3 = nearly sine). |
| `OddEven` | double | -1 - 1 | additive: -1 keeps only even harmonics (octave and thin), 0 all, +1 odd harmonics only (hollow, clarinet, square-like). |
| `Vowel` | double | 0 - 4 | vowel oscillator: vowel position 0=a 1=e 2=i 3=o 4=u (voice formants, choirs, talking pads). |
| `VowelEnd` | double | 0 - 4 | vowel position the Morph control moves to. |
| `TiltEnd` | double | 0 - 3 | additive: tilt the Morph control moves to. |
| `Morph` | double | 0 - 1 | Wavetable morph 0..1 between the start spectrum (Tilt, Vowel) and the end spectrum (TiltEnd, VowelEnd). Modulate it with LfoMorph, Lfo2Morph and ModMorph for evolving, vocal or sweeping timbres. |
| `SyncRatio` | double | 1 - 12 | sync oscillator: hard-sync ratio (1 = plain saw; 2-8 gives the sweeping, screaming sync-lead sound when modulated). |

## Physical models

| Parameter | Type | Range | Description |
|---|---|---|---|
| `Ring` | double | 0.05 - 10 | pluck/bell oscillators: ring time in seconds (how long the string or metal keeps sounding). |
| `Damping` | double | 0 - 1 | pluck/bell: damping (0 = bright and sustained, 1 = muted and dull). Wood and felt are high, glass and steel strings are low. |
| `Inharmonic` | double | 0 - 1 | bell: inharmonicity (0 = harmonic string, 0.3 = stiff piano string, 0.5 = marimba or glockenspiel bar, 1 = clangorous church bell). |
| `Strike` | double | 0.02 - 0.5 | pluck/bell: strike or pluck position along the string (0.05 = twangy and nasal, 0.5 = round and hollow). |

## FM and ring

| Parameter | Type | Range | Description |
|---|---|---|---|
| `FmDecay` | double | 0 - 8 | FM index decay in seconds (0 = constant). Short = bright attack that mellows: electric piano, bell, pluck, brass blat. |
| `FmSustain` | double | 0 - 1 | Fraction of the FM index that remains after FmDecay. |
| `Fm2Amount` | double | 0 - 10 | Second modulator that modulates the first modulator (3-operator FM chain; 0 off). |
| `Fm2Ratio` | double | 0.25 - 16 | Second modulator frequency ratio. |
| `FmFeedback` | double | 0 - 1 | FM feedback: noisy, sawtooth-like FM tones (0.3+ metallic, 0.8+ harsh). |
| `RingAmount` | double | 0 - 1 | Ring modulation between oscillators 1 and 2 (bells, robots, clangs). |

## Modulation

| Parameter | Type | Range | Description |
|---|---|---|---|
| `LfoShape` | LfoWave | - | LFO 1 shape: sine, tri, square, saw, sh (random steps) or smooth (random glides). |
| `LfoDelay` | double | 0 - 5 | Seconds before LFO 1 starts to fade in (delayed vibrato on sustained notes). |
| `LfoPwm` | double | 0 - 1 | LFO 1 to pulse width (PWM). |
| `LfoFm` | double | 0 - 1 | LFO 1 to FM index. |
| `LfoMorph` | double | 0 - 1 | LFO 1 to wavetable morph. |
| `Lfo2Rate` | double | 0.02 - 40 | LFO 2 rate in Hz (slow = evolving, fast = ring-like). |
| `Lfo2Shape` | LfoWave | - | LFO 2 shape. |
| `Lfo2Pitch` | double | 0 - 1200 | LFO 2 to pitch in cents. |
| `Lfo2Filter` | double | 0 - 1 | LFO 2 to filter cutoff. |
| `Lfo2Fm` | double | 0 - 1 | LFO 2 to FM index. |
| `Lfo2Morph` | double | 0 - 1 | LFO 2 to wavetable morph. |
| `Lfo2Pwm` | double | 0 - 1 | LFO 2 to pulse width. |
| `Lfo2Sync` | double | 0 - 1 | LFO 2 to sync ratio. |
| `Lfo2Amp` | double | 0 - 1 | LFO 2 to volume. |
| `ModAttack` | double | 0.001 - 5 | Modulation envelope attack seconds. |
| `ModDecay` | double | 0.01 - 8 | Modulation envelope decay seconds (a second envelope for movement: use it for FM, morph, sync, noise and pitch). |
| `ModPitch` | double | -24 - 24 | Modulation envelope to pitch in semitones (blips, zaps, scoops). |
| `ModFm` | double | 0 - 1 | Modulation envelope to FM index. |
| `ModMorph` | double | -1 - 1 | Modulation envelope to wavetable morph. |
| `ModPwm` | double | -1 - 1 | Modulation envelope to pulse width. |
| `ModSync` | double | -1 - 1 | Modulation envelope to sync ratio. |
| `ModNoise` | double | 0 - 1 | Modulation envelope to noise level (breath, chiff, bow noise at the start of a note). |

## Expression

| Parameter | Type | Range | Description |
|---|---|---|---|
| `Drift` | double | 0 - 1 | Analog drift: slow random wander of pitch and cutoff so held notes feel alive. |
| `Variation` | double | 0 - 1 | Per-note variation: every repeated note differs slightly (round robin), so riffs sound played instead of machine-gun. |
| `VelFilter` | double | 0 - 1 | Velocity to filter cutoff: hard notes are brighter. |
| `VelFm` | double | 0 - 1 | Velocity to FM index. |
| `VelAttack` | double | 0 - 1 | Velocity to attack speed: hard notes start faster. |
| `VelDrive` | double | 0 - 1 | Velocity to drive: hard notes get dirtier. |

