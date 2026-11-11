# Settings Reference

These are the parameter grids behind the Master, Mix, Spectral and Meta synth tabs. Edit them directly in the property grid, or use the Knobs tab.

## Mastering chain (`MasterSettings`)

| Parameter | Type | Range | Description |
|---|---|---|---|
| `Enabled` | bool | on/off | Apply the mastering chain to previews and renders. |
| `TargetLufs` | double | -30 - -5 | Target integrated loudness in LUFS used by Auto master (streaming -14, club -9). |
| `InputGain` | double | -12 - 30 | Gain into the chain in dB (drives the compressor and limiter). |
| `LowShelfDb` | double | -9 - 9 | Low shelf gain in dB. |
| `LowShelfHz` | double | 30 - 400 | Low shelf frequency in Hz. |
| `MidDb` | double | -9 - 9 | Mid bell gain in dB (cut around 300 Hz to clear mud). |
| `MidHz` | double | 100 - 6000 | Mid bell frequency in Hz. |
| `MidQ` | double | 0.3 - 4 | Mid bell Q. |
| `HighShelfDb` | double | -9 - 9 | High shelf gain in dB (air). |
| `HighShelfHz` | double | 2000 - 16000 | High shelf frequency in Hz. |
| `Warmth` | double | 0 - 1 | Soft saturation amount (analogue warmth). |
| `CompThreshold` | double | -40 - 0 | Threshold in dB. |
| `CompRatio` | double | 1 - 10 | Ratio (1 = off). |
| `CompAttack` | double | 1 - 100 | Attack in ms. |
| `CompRelease` | double | 20 - 1000 | Release in ms. |
| `CompKnee` | double | 0 - 18 | Soft knee width in dB. |
| `Width` | double | 0 - 1.6 | Stereo width (1 = unchanged, 0 = mono). |
| `BassMonoHz` | double | 0 - 300 | Make everything below this frequency mono (0 = off). |
| `OutputGain` | double | -12 - 12 | Gain after the compressor in dB. |
| `Limiter` | bool | on/off | Brickwall look-ahead limiter on or off. |
| `Ceiling` | double | -6 - 0 | True-peak-safe ceiling in dBFS. |
| `LimiterRelease` | double | 10 - 500 | Limiter release in ms. |

## Mix rules (`MixRule`)

| Parameter | Type | Range | Description |
|---|---|---|---|
| `Enabled` | bool | on/off | Turn this rule on or off. |
| `Kind` | MixKind | - | sidechain (duck targets from the source track), highpass, lowpass, peak (EQ bell), lowshelf, highshelf, gain, pan, width, saturate, monobass (collapse low frequencies to mono). |
| `Target` | string | - | Which tracks it affects: * for all, or track names / synth names separated by commas (for example: bass, pad). |
| `Source` | string | - | Sidechain only: the track that triggers the ducking, usually the kick (matched by name). Works even if that track is muted. |
| `Depth` | double | 0 - 1 | Sidechain: how far the targets duck (1 = fully silent on each hit). Saturate: drive amount. |
| `AttackMs` | double | 0.5 - 100 | Sidechain: how fast it ducks, in milliseconds. |
| `ReleaseMs` | double | 20 - 1000 | Sidechain: how long it takes to recover, in milliseconds. Short = tight pump, long = swell. |
| `FreqHz` | double | 20 - 20000 | Frequency in Hz for filters, EQ bell, shelves and monobass. |
| `Db` | double | -24 - 12 | Gain in dB for peak, shelves and gain. |
| `Q` | double | 0.2 - 8 | Filter resonance / bell width. |
| `Width` | double | 0 - 2 | Stereo width for kind width (1 = unchanged, 0 = mono, 2 = very wide). |
| `Pan` | double | -1 - 1 | Pan position for kind pan, -1 left to 1 right. |

## Spectral canvas settings (`SpectralSettings`)

| Parameter | Type | Range | Description |
|---|---|---|---|
| `Seconds` | double | 0.5 - 30 | Length of the sound in seconds (the picture is stretched over it). |
| `MinHz` | double | 20 - 2000 | Frequency of the bottom row in Hz. |
| `MaxHz` | double | 1000 - 20000 | Frequency of the top row in Hz. |
| `Reverse` | bool | on/off | Play the picture backwards. |
| `Flip` | bool | on/off | Flip the picture upside down (low and high swap). |
| `Snap` | SpecSnap | - | Snap every row to a musical scale so images sound tonal: off, chromatic, major, minor, pentatonic, hirajoshi, wholetone, diminished. |
| `Root` | double | 0 - 11 | Scale root as a pitch class (0 = C, 9 = A). |
| `Brightness` | double | 0 - 1 | Adds 2nd and 3rd harmonics to every partial (0 pure sines, 1 buzzy). |
| `Noisiness` | double | 0 - 1 | Random pitch jitter on each partial: 0 clean sines, 1 airy noisy bands. |
| `Contrast` | double | 0.3 - 3 | Gamma on brightness: below 1 lifts faint areas, above 1 keeps only the brightest. |
| `FadeMs` | double | 0 - 200 | Fade in and out in milliseconds. |
| `Gain` | double | 0.1 - 1 | Peak level of the result. |

## Meta synth morph settings (`MetaSettings`)

| Parameter | Type | Range | Description |
|---|---|---|---|
| `X` | double | 0 - 1 | Morph position left (A, C) to right (B, D). |
| `Y` | double | 0 - 1 | Morph position top (A, B) to bottom (C, D). |
| `Seconds` | double | 1 - 30 | Length of the evolving sound in seconds. |
| `Steps` | double | 4 - 64 | Number of morph steps (more = smoother, slower to render). |
| `Pitch` | double | 24 - 96 | MIDI pitch of the sound. |
| `Path` | MetaPath | - | Journey through the four patches: tour (A to B to C to D), pingpong (A to B and back), circle, wander (smooth random). |
| `Chord` | MetaChord | - | Stack extra notes: single, octaves, fifth, minor, major, sus2, cluster. |
| `HoldSustain` | bool | on/off | Keep each step sustaining so the morph sounds continuous. |
| `Seed` | double | 0 - 9999 | Random seed for wander. |

