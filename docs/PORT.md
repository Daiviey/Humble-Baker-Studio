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
| Project model, patterns, song arrangement, markers | Done |
| Project save/load (`*.hbstudio.json`) | Done, schema-compatible |
| Synth engine (subtractive, FM, additive, wavetable, vowel, pluck, modal) | Done |
| Drum kits (procedural + WAV sample kits) | Done |
| Track effects, buses, graph FX, automation | Done (engine); editors expose the full parameter set |
| Mixing, sidechain, mix rules, mastering, LUFS analysis | Done |
| Piano roll editing, scales, snap, velocity lane | Done |
| Transport/playback (winmm, no third-party audio package) | Done |
| Export WAV 16/24/32-bit, optional FFmpeg formats | Done |
| MIDI export | Done (engine) |
| Local AI melodies/drums/patch design/mix advice (Ollama) | Done |
| Settings, sample-library discovery | Done |
| Undo/redo | Not yet |
| Sample import and slicing UI | Not yet (engine support exists) |
| SuperCollider live sessions, `.scd` export, OSC | Not yet |
| Spectral painting / meta-synth / transcriber UI | Not yet (engine classes exist) |
| Song-model (ACE-Step) import | Not yet |
| MIDI input recording | Not yet (engine `MidiInPort` exists) |
| Project templates, marker editing UI | Not yet |

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

## Next steps

1. Undo/redo and a proper edit history (the model is plain C#, so this is straightforward).
2. Sample import, waveform selection and slicing UI (engine already provides `WavIn` and
   `SampleSlicer`).
3. SuperCollider bridge and `.scd` export (port `Get-StudioSc*` helpers).
4. MIDI input recording and a drum grid editor.
5. Spectral/meta-synth editors.
