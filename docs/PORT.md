# Port status and design notes

## Source of truth

The C# port starts from the proven DSP that the PowerShell studio already embedded and passed
to `Add-Type`. `tools/extract-engine.ps1` extracts that block from
`Humble Baker Studio.standalone.ps1` and writes one file per top-level type into
`src/HumbleBakerStudio.Audio/Engine`.

The extracted code is intentionally left close to the original. Deliberate changes on top of
the import are listed here:

| File | Change | Why |
|---|---|---|
| `Engine/KnobPanel.cs` | Knobs widened to 108 px; category headers get extra top margin | Long parameter names such as `TargetLufs` were unreadable in the original |
| `Engine/PianoRoll.cs` | unchanged | |
| `GlobalUsings.cs` | shared usings | extracted files stay close to the original |

Do not run `extract-engine.ps1` again without `-Force`; it will otherwise leave the patched
files alone on purpose.

## What is ported

| Area | State |
|---|---|
| Project model, patterns, song arrangement with editable bars overrides, markers and marker energy | Done |
| Project save/load (`*.hbstudio.json`) | Done, schema-compatible |
| Synth engine (subtractive, FM, additive, wavetable, vowel, pluck, modal) | Done |
| Drum kits (procedural + WAV sample kits) | Done |
| Track effects, buses, graph FX, automation | Done (engine + painting editor for tracks, buses and graph modules) |
| VST3 instruments and effects on tracks and graph nodes | Done (64-bit; VST2 not supported) |
| Mixing, sidechain, mix rules, mastering, LUFS analysis | Done |
| Piano roll editing, scales, snap, velocity lane, timeline-wide writing area, ruler marker sketching | Done |
| Transport/playback (winmm, no third-party audio package) | Done |
| Export WAV 16/24/32-bit, optional FFmpeg formats | Done |
| MIDI export | Done (engine) |
| Local AI melodies/drums/patch design/mix advice (Ollama) | Done |
| Settings, sample-library discovery | Done |
| Undo/redo | Done (snapshot history, coalesced gestures, Ctrl+Z/Y) |
| Sample import, waveform editing and slicing UI | Done (normalize, reverse, trim, onset/equal slicing, export slices) |
| SuperCollider `.scd` export | Done (voices, drums, sample buffers, per-track patterns, global delay/reverb, song flattening) |
| Spectral painting / meta-synth | Done (7 brushes, image import, track spectrogram, harmonic stacks, undo, audition, WAV/sample-track/instrument export, AI shape drawing; four-corner meta synth with morph pad, apply/render, instrument saving and AI patch design) |
| Automation painting / layered splines | Done (high-resolution canvas, brushes, track/bus/graph targets, Z axis, mapping modes, per-stroke layers, crossing-stroke separation, amplitude normalization, Add/Multiply/Max/Replace mixing, compressed display snapshots) |
| Song import and transcription | Done (audio track + monophonic melody + drum hits + section markers) |
| MIDI input recording | Done (device selection, live log, quantized recording into the selected track) |
| Marker and song arrangement editing | Done (patterns, song order, repeats with bars override, markers and energy) |
| Live performance mode | Done (two independent decks, crossfader, cue bus, sync) |
| SuperCollider live sessions and OSC bridge | Not yet (export only) |
| Full effect-graph parity in the SC export | Approximate (global delay/reverb + per-voice filter) |
| MIDI note monitoring through the track voice | Not yet |

## Automation painting

The automation editor is documented in [`AUTOMATION.md`](AUTOMATION.md). Key design points:

- Canvas resolution is selectable from 512 x 128 to 4096 x 512. Painting is stored as a
  `SpectralGrid`; the display is sampled at screen resolution so displayed cost is independent
  of the stored resolution.
- Lane lookup uses binary search (`AutoLane.At`), keeping dense lanes (`O(log n)` per lookup)
  cheap during playback. Automation is evaluated at the audio block rate, not per sample.
- Layers are captured per mouse stroke, so crossing/intersecting waves stay separate instead
  of merging into one connected region.
- Each wrote lane can store a Deflate-compressed, display-only snapshot of the painting at its
  original resolution. The renderer ignores it; the editor uses it to restore the exact image.
- Automation points are `[beat, value, brightness, verticalLow, verticalHigh]`. Older readers
  only use the first two values.
- Parameters the renderer does not process yet are still writable, and the status bar warns
  about them.

## Architecture

- `HumbleBakerStudio.Audio` owns the domain: everything that must be true of a project and a
  render. It has no knowledge of windows, dialogs or settings files. This is what the tests
  cover.
- `HumbleBakerStudio.App` is a thin WinForms shell. Tabs talk to the same live `Project`
  object through `MainForm`, so the sidebar, piano roll, mixer and inspector cannot drift out
  of sync.
- Playback and export share one render path (`ExportService.Render`), so what you hear is what
  you get.
- AI output is untrusted: replies are parsed as JSON, clamped with `PropIO`, and never executed.

## Known limitations

- Playback renders the loop in the background before starting; very long songs take a moment.
- The mastering checkbox in the toolbar only enables the master chain for playback. The chain
  itself is switched on in the Master tab (matching the original default).
- Native scrollbars stay light themed because .NET 8 WinForms does not expose a dark scrollbar
  mode; the title bar and all custom-drawn controls are dark.
- The extracted engine exposes more features than the current UI shows. They remain reachable
  from future tabs without changing the audio library.
- The PowerShell-only FLUX photo/illustration mode in the spectral painter is not ported; the
  C# port has text-to-spectrogram AI drawing instead.
- Some graph-node parameters are display-only for now. The automation editor labels them when
  they are not processed by the renderer.
- The PowerShell studio drops automation display snapshots when it saves a project; the
  automation points and playback are unaffected.

## Live performance mode

Open **Live** in the toolbar. The window is independent of the editor and holds two complete
projects at once:

- **Deck A / Deck B** each load any `*.hbstudio.json` (or take a snapshot of the current
  project with **From main**), render it in the background, and loop or play the song.
- **Crossfader** uses an equal-power curve. **Gain** and a one-knob **filter** (low-pass to
  high-pass) are per deck.
- **Sync** matches a deck to the other by adjusting its playback rate (BPM ratio).
- **Cue** routes a deck to the headphone bus. Choose a second output in **Cue out** for real
  split-cue monitoring; with `(none)` cue stays silent. **Cue on master** is for testing.
- **Queue** on each deck holds a list of projects; the deck advances automatically when a
  song ends and the queue has a next item.
- Playback is rendered once when a track loads, then mixed live at 1024-frame blocks. Loading
  a long song takes as long as an offline render; transitions themselves are instant.

## macOS

A native macOS build is not available. The reusable core is the DSP/project model; the
WinForms UI, `winmm` playback and Windows VST3 host must be replaced. See
[`MACOS.md`](MACOS.md) for the feasibility assessment and recommended phases.

## Next steps

1. SuperCollider live sessions and the OSC bridge (the `.scd` export is in place).
2. Full effect-graph parity in the SuperCollider export.
3. MIDI note monitoring through the selected track voice while recording.
4. Drum grid editor and a dedicated sampler instrument view.
5. Undo history visualisation (the snapshot stack already records labels).
