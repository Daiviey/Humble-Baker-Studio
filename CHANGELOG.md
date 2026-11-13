# Changelog

## macOS app 0.3.0 - 2026-11-13

- **Audio Unit effect hosting**: the `.app` can render a project loop through an AU effect selected in the transport bar, with a dry fallback if instantiation/render fails. AU discovery and CoreAudio output remain available.
- Also includes CoreAudio playback, transport, track list, click-to-edit piano roll, project open/save and WAV export from 0.2.0.
- AU instantiation/render and AU editor hosting still need a real macOS test pass.

## macOS app 0.2.0 - 2026-11-13

- **CoreAudio playback**: the `.app` renders the loop with the portable engine and streams it through AudioQueue, with Play/Stop and a live transport playhead.
- **Editor shell**: track list plus a click-to-edit piano roll (click adds a 1/16-snapped note, right-click removes it), project open/save, and WAV export.
- **Audio Unit discovery** remains available from the Audio Units window. AU instantiation/render and CoreAudio AU hosting are the next milestone.
- Everything is cross-compiled for Apple Silicon and Intel; the builds are unsigned and still need a real macOS test pass for the audio path.

## PowerShell studio 1.0.24 - 2026-11-13

- **VST3 instruments and effects in the PowerShell studio**, through the new optional out-of-process `hbs-vst-bridge`. **Tools -> VST3 plug-ins** scans installed 64-bit VST3 classes, assigns one to the selected track, bypasses or removes it, and stores the plug-in state in `*.hbstudio.json`.
- The bridge is a separate self-contained download so the studio stays about 1.9 MB; it is located next to the studio or in a `VST3` folder beside it. If it is missing, the picker explains where to get it and the studio otherwise behaves normally.
- A plug-in crash terminates the bridge instead of the studio; the next render restarts it.
- Track list shows a `[VST]`/`[VST off]` marker, and the tool remembers the current plug-in per track.
- Limitations: no plug-in editor window yet, 64-bit only, no VST2, effects must be 2-in/2-out.

## macOS app 0.1.1 - 2026-11-13

- Added an **Audio Units...** window that enumerates installed AU instruments, effects, generators and format converters with `AudioComponentFindNext`/`AudioComponentCopyName`. Instantiation, render callbacks and CoreAudio output are the next milestone; see `docs/PLUGINS.md`.

## C# port 0.5.1 - 2026-11-13

- Sample tracks now honour an envelope: the existing Attack/Release knobs fade the sample in and out, the new **Gate by note length** toggle chooses whether the release applies, and the **Sample mode** combo switches between pitch, one-shot and loop. `SampleVoice` uses `Atk` instead of a fixed 40-sample fade.

- **Arrangement and piano roll stay in sync**: editing a marker, changing the Song Order or changing a pattern length now refreshes the piano roll immediately.
- Song Order sections are drawn on the piano roll as labelled colour bands, so the arrangement structure is visible while editing notes.
- No-AI edition: the same arrangement/piano-roll sync and section bands.
- Tests: 48 passing (standard and No-AI).

## macOS app 0.1.0 - 2026-11-13

- Added the first native macOS `.app`: an Avalonia shell around the portable engine. It can render the demo project or open a `*.hbstudio.json` and render a stereo WAV.
- Added `build-macos-app.ps1`, which publishes and packages `Humble Baker Studio.app` for `osx-arm64` and `osx-x64`, preserving the executable permission in the zip.
- The full editor UI, CoreAudio playback, MIDI and VST3 hosting remain the next phases.

## 1.0.23 - 2026-11-13

- **PowerShell automation performance**: `AutoLane.At` now uses binary search, so dense lanes are `O(log n)` rather than scanning every point during playback.
- **Higher-resolution automation canvas**: the PowerShell automation painter is now **1024 x 256** (up from 160 x 64), with a larger default brush and faster LockBits bitmap rendering.
- Rebuilt the PowerShell executable and standalone build with version metadata **1.0.23**.
- The layered/intersection/snapshot automation, deterministic pattern library/No-AI edition and VST3 hosting remain C#-only or planned; see `docs/PARITY.md`.

## C# port 0.5.0 - 2026-11-13

- **Spectral paint parity**: seven brushes, image import, spectrogram from a track, harmonic stacks, undo, audition, WAV/sample-track export, AI shape drawing, the shared instrument library and full meta-synth parity (morph pad, apply/render, AI patch design, morph instrument save/load).
- **Automation painting**: a full 2D editor for tracks, buses and patch-graph FX/group/bus modules. Includes seven brushes, a screen-pixel brush size, Z-axis brightness, Exact/Smooth/Quantize/Linear mapping, shape detail modes, Clear Canvas/Lane and Reset Track Auto.
- **Layered spectral splines**: extract multiple painted waves into layers, assign each layer a parameter, normalize layers by amplitude, and combine same-parameter layers with Add, Multiply, Max or Replace.
- **Intersections**: each mouse stroke is captured separately, so crossing/intersecting waves stay as independent layers instead of merging into one connected region.
- **High-resolution automation**: canvas options from 512 x 128 to 4096 x 512. Lane lookup uses binary search (`O(log n)`), the canvas is rendered at screen resolution, and brush radii are elliptical so the brush stays round at any resolution.
- **Exact visual preservation**: every written lane stores a Deflate-compressed, display-only snapshot of the painted canvas at its original resolution. The editor restores the exact painting and resolution; playback ignores the snapshot.
- Automation points now carry optional brightness and vertical-extent values so redrawn lanes preserve stroke weight as well as the curve. Older readers use only beat/value.
- Added `docs/AUTOMATION.md` and refreshed `README-CSHARP.md`, `docs/PORT.md`, `RELEASES.md` and the portable-build README.
- **Arrangement and piano roll stay in sync**: marker edits, song-order changes and pattern-length changes now refresh the piano-roll timeline immediately. Song-order sections are drawn as labelled colour bands on the piano-roll canvas, so the arrangement structure is visible while editing notes.
- Tests: 45 passing in the standard build (48 including the No-AI pattern tests).

### No-AI edition

- Added a deterministic edition for artists who do not want generative melody/composition tools. Build with `.\build.ps1 -NoAi`; the AI tab, toolbar button and AI menu entries are replaced by a **Patterns** tab.
- Added a curated library of pre-programmed MIDI examples for drums, bass, chords, leads, arpeggios and counter-melodies. The selector scores examples by role, style, mood, energy, complexity and density; the seed makes selection and humanisation reproducible.
- The composer transposes to the project key, snaps to the project scale, fits the requested bars, thins by density and scales velocities by energy. No model or network is used.
- Release assets: `Humble-Baker-Studio-CSharp-NoAI-0.5.0-win-x64.exe` and `.zip` on the v0.5.0 release.

## 1.0.22 - 2026-11-12

- Arrangement **Song Order entries are editable**: each entry gets a **Bars override** (0 = use the pattern's own bars) next to Repeats, so one pattern can be laid out as a 2-bar fill in one place and an 8-bar section in another. Overrides are saved in the project file and honoured by song playback, export and the SuperCollider export.
- **Arrangement markers now shape song playback**: the marker in effect at each note scales its velocity (`0.6 + 0.08 × energy`, energy 5 = neutral), so Intro/Breakdown/Drop sections play softer or harder. Marker fields on the Arrangement tab (Beat, Name, Energy) now edit the selected marker instead of only feeding new ones.
- **Markers can be sketched on the piano roll**: double-click the ruler to add a marker (or remove the one under the cursor); the Arrangement tab and project file stay in sync.
- **The piano roll timeline now scrolls past the loop**: the canvas covers the loop, the whole song arrangement plus 64 beats of writing room, so notes can be written or pasted anywhere on the timeline (click the ruler where you want the paste point, then Ctrl+V).
- Added **Live mix**, a two-deck performance window: two independent projects open at once, an equal-power crossfader, per-deck gain and one-knob LP/HP filter, tempo sync, per-deck queues that advance at the end of a song, and a separate cue/headphone output device. Master and cue are metered independently.
- Live audio is rendered once per track and mixed on a dedicated thread through `winmm`, so transitions and filter sweeps are immediate. Rendering a long song still takes the usual offline render time.
- Rebuilt `Humble Baker Studio.exe` with the live module and the current studio sources.
- Live decks now include a **New** button that renders a fresh default project into that deck (with a confirmation when the deck already has audio).
- Ported all of the above to the PowerShell studio (`AudioStudioImplementation.ps1`, standalone build and exe): editable song entries with bars override, marker-driven playback energy, ruler double-click marker sketching (with a marker list and fields on the Song tab) and the timeline-wide piano roll.
- Refreshed the manual, SuperCollider guide, port notes and release pages for this version. The C# port is packaged separately as `csharp-port-v0.4.0` with the same arrangement, marker and timeline features.

## 1.0.21 - 2026-11-11

- Added the persistent SuperCollider live bridge: `sclang` stays running, the app sends OSC on port 57120 and receives status on 57121. Controls: **Start live bridge**, **Stop bridge**, **Load session in SC**, **Play SC**, **Stop SC**.
- Added the graph compiler. Piano-roll notes, tracks, patch-graph modules, links, effect parameters, bus gain/pan and master routing now generate a runnable `project.scd` with `~hb`, `HBProject`, `HBTrack`, `HBNode` and explicit audio buses.
- Added native SuperCollider definitions for all 113 effect types, named `hb_<type>` (`hb_filter`, `hb_ladder`, `hb_convreverb`, ...), generated from shared DSP families.
- Added `tests/sc-parity.ps1`: compiles all 113 defs, renders every effect offline with `scsynth -N`, checks for silence/non-finite audio, and records family correlation/RMS against the app's `FxProc`.
- Fixed OSC argument handling for PowerShell-wrapped values, the out-node bus allocation, effect bus routing, the `HBNode.synth` setter and bridge shutdown (now cleans up the `scsynth` it started).
- Documented the bridge, generated language, native effect library, parity harness and bridge troubleshooting in `docs/SUPERCOLLIDER.md`.

## 1.0.20 — 2026-11-11

- Added per-project SuperCollider session folders under the Studio data directory, with a persistent project `ScSessionId`; generated SC files stay out of git.
- Added the stable `HBProject`, `HBNode`, `HBTrack`, `HBSection` and `HBEffect` classes and a shared SC class library.
- Added SC tab controls: **Recompile SC definitions**, **Check SC setup** and **Open session folder**. Recompile stops transport, compiles the classes with `sclang` and leaves transport stopped.
- SC discovery now finds a `SuperCollider` folder beside the app (for example `Z:\LocalAI\SuperCollider`) in addition to the standard install paths.
- The setup check reports `sclang` version, sc3-plugins presence and the session path.

## 1.0.19 — 2026-11-11

- Added 15 effects (113 total): Spectral Gate, Spectral Blur, Spectral Delay, Convolution Reverb, Tank Reverb, Auto-Tune, Octave Up, Chord Generator, Upward Compressor, Transient Gate, Stereo Shuffler, Mono Maker, Phase Aligner, Tape Loop and Stutter Gate.
- All 113 catalog entries create active nodes and pass the NaN/Inf tone test.
- Regenerated the effect reference with all 113 modules and 365 effect parameters.

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
