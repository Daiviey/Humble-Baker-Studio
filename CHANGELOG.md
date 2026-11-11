# Changelog

## 1.0.18 — 2026-11-11

- Added `docs/SUPERCOLLIDER.md`: an in-depth SuperCollider tutorial covering the Studio bridge, the generated `.scd`, SynthDefs, Pattern/Pbind sequencing, buses and routing, effects, live coding, rendering, MIDI/OSC and debugging.
- Every external technical claim is cited; 36 official SuperCollider help, download, source and licensing references are linked.
- Linked the tutorial from the README and the manual.

## 1.0.17 — 2026-11-11

- Removed the three dots from the blue dome of the cel-shaded app icon.
- Removed the redundant `docs/icon-preview.png`.

## 1.0.16 — 2026-11-10

- Restyled the app icon with the local FLUX.1 Kontext model: cel-shaded flat colours, bold dark outlines, simplified shading and cleaner readability at small sizes.
- Regenerated the seven-size ICO, PNG previews, exe icon and standalone embedded icon.

## 1.0.15 — 2026-11-10

- UI layout pass: fixed accent buttons that collapsed to 1 px (MIDI **Connect** and **Start recording**, Sampler **Import WAV**).
- Fixed tab auto-scroll offsets that made labels drift over their fields.
- Reworked the Sampler, MIDI In, Automation Painting, Bus / FX, Composer and Spectral layouts so controls no longer overlap or sit below the visible tab.
- Added responsive anchors: logs, property grids, text boxes, lists and the Knobs/Bus knob areas now grow with the window and the bottom resizer.
- Shortened or clarified clipped labels, and removed redundant Spectral palette/visualise controls.

## 1.0.14 — 2026-11-10

- Gave the app icon a rounded-square app-tile shape with transparent corners and a subtle inner highlight.
- Regenerated all icon sizes (16-256 px), the exe icon, the standalone embedded icon and the PNG previews.

## 1.0.13 — 2026-11-10

- Added the full multi-resolution **Humble Baker Studio icon** (16, 24, 32, 48, 64, 128 and 256 px) and embedded it in the Studio exe, the window/title bar and the standalone build.
- Added an **MIT license** and a `NOTICE.md` covering original artwork/branding, content attribution and third-party software/model licenses.
- The **Studio agent now knows all 98 effects** with their use cases and a detailed mixing strategy; this knowledge is provided when it plans steps and when it designs effects.
- Added icon preview assets under `docs/`.
- Fixed the packaged exe locating its embedded studio implementation when launched from any folder.

## 1.0.12 — 2026-11-10

- Added full documentation: [Manual](docs/MANUAL.md), [Effect Reference](docs/EFFECTS.md) with all 98 effects and every control, [Synth Patch Reference](docs/SYNTH.md) with all 89 parameters, and [Settings Reference](docs/SETTINGS.md) for mastering, mix rules, spectral and meta settings.
- README now links the documentation and lists the actual export formats.

## 1.0.11 — 2026-11-10

- Expanded the FX graph to **98 modules** (25 new): Opto compressor, FET compressor, VCA compressor, Expander, Mid/Side compressor, Dynamic EQ, Pultec EQ, Overdrive, Metal drive, Speaker cab, Multiband drive, Tape flutter, Dimension, Envelope phaser, Envelope flanger, Chopper, Barber-pole, Half speed, Pitch echo, Spectral freeze, Reverse delay, Echo-verb, Bloom reverb, Non-linear reverb and Vocoder.
- All new modules expose categories, ranges and descriptions and are created only as Patch graph nodes.
- Verified 98/98 catalog entries create active nodes with no NaN/Inf on a test tone.

## 1.0.10 — 2026-11-10

- Expanded the FX graph to **73 modules** (25 new): Ladder filter, Tube preamp, Tape saturator, Diode drive, Fuzz, Chip tune, Sub bass, Bass enhancer, Frequency shifter, Air band, Stereo image, Ping-pong delay, Dub delay, Cathedral reverb, Room reverb, Ambience, Wah pedal, Talk box, Ducker, Level rider, Harmonic tremolo, Beat repeat, Granular cloud, Glitch gate and Sweep filter.
- Added the **MIDI In** tab: lists Windows MIDI inputs, connects to a controller, logs notes/CC/pitch bend, and records quantized notes into the selected track with optional monitoring.
- Added the **Sampler** tab: WAV import, waveform drag selection, normalize/reverse/trim, onset auto-slicing, equal chopping, selection-to-slice, slice audition/export, and slice-to-track creation.
- Effects remain graph-only; no rack or quick-add paths were reintroduced.

## 1.0.9 — 2026-11-10

- All 48 effects are now graph nodes; effects are added only from the Patch graph (right-click → Add/Insert FX module).
- Removed the FX Rack button, track/bus quick-add buttons and the Sound Lab FX chain editor.
- New tracks no longer start with a hidden compressor; track FX is off by default.
- Each graph effect node shows only its own parameters, with ranges and descriptions.

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
