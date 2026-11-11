# Changelog

## 1.0.8 — 2026-11-10

- Expanded the FX Rack to **48 effect modules**.
- Added: Ensemble, Rotary, Octaver, Harmonizer, Resonator, Tilt EQ, Mid/Side EQ, De-esser, Multiband Compressor, Clipper, Auto-Filter, Gated Reverb, Reverse Reverb, Multi-Tap Delay, Doubler and Formant Filter.
- All new effects expose categories, ranges and descriptions in the rack.

## 1.0.7 — 2026-11-10

- Added an **FX Rack** dialog with 32 effects, category-grouped parameters and descriptions.
- New effects: Saturator, Exciter, Rectifier, Gate, Transient Shaper, Tremolo, Auto-Pan, Vibrato, Auto-Wah, Comb Filter, Spring Reverb, Shimmer Reverb, Haas Widener, Limiter.
- New synth parameters: oscillator 2 pulse width, sub-oscillator waveform, and post-synth output drive.

## 1.0.6 — 2026-11-10

- Bus/group graph sidechain: when the shared bus has **Sidechain Mix > 0**, the **bottom-most connected source** becomes the trigger and ducks the other inputs.
- Sidechain attack/release use the bus FX settings; Mix at 0 keeps the existing mix unchanged.

## 1.0.5 — 2026-11-10

- Sample-library instruments (foley, percussion, orchestral/guitar hits) now default to **Pitch (key)** mode, so piano-roll notes transpose them.
- One-shot and Loop modes remain selectable in the track panel/Sound Lab for existing tracks.

## 1.0.4 — 2026-11-10

- Fixed false “FFmpeg could not encode” errors when the output file was actually written.
- FFmpeg success is now determined from the output file and a refreshed exit code.
- MP3/Opus continue to use the 48 kHz limit.

## 1.0.3 — 2026-11-10

- MP3 and Opus exports now limit the sample rate to 48 kHz, preventing the libmp3lame/libopus encoder error.
- Added a safety clamp in the export path for invalid rate/format combinations.

## 1.0.2 — 2026-11-10

- Rebuilt the main app and Studio executables from the current sources.
- Release bundle now includes the export-dialog fix.
- Verified both executables launch and the main app no longer overwrites the studio source.

## 1.0.1 — 2026-11-10

- Fixed the export dialog failing to open in standalone builds.
- The toolbar Export button now opens the same dialog.
- Added export diagnostics.

## 1.0.0 — 2026-11-10

Initial standalone release of Humble Baker Studio.

### Studio
- Piano roll with scales, snap, velocity and per-track parts.
- Synth engine: subtractive, FM, additive, wavetable, vowel, pluck, bell, noise.
- Drum kits, sample tracks, mixer, buses and master chain.
- Patch graph with track/FX/bus/group nodes and shared FX processing.
- MIDI export, SuperCollider export and live preview support.

### Automation painting
- Canvas spans the whole track/song.
- Vertical position sets the value; brightness is a real Z axis (depth/value).
- Targets: track, graph node (shared FX), or bus.
- Saved lanes redraw when selecting the target or reloading the project.

### Export
- Current pattern with 1–64 seamless loops, or the whole arrangement.
- Tempo override and optional metronome click.
- Mastering chain, peak normalization and fade in/out.
- WAV 16/24/32-float, MP3, OGG Vorbis, Opus, AAC/M4A, ALAC, FLAC and AIFF.
- 44.1 / 48 / 96 kHz, stereo or mono, with FFmpeg-assisted encoding.

### Packaging
- Canonical implementation, launcher and single-file standalone script.
- Portable Windows executable built with ps2exe.
