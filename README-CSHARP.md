# Humble Baker Studio — C# port

An experimental, native .NET port of Humble Baker Studio. The shipped PowerShell app keeps
working; this tree is the clean-room restructuring of it into a typed, testable Windows
application.

**Status: feature-complete alpha.** The project model, synthesis engine, 113-effect DSP,
mixing, mastering, piano roll, project format, WAV export and local-AI helpers are in place,
together with the sample lab, MIDI recording, spectral/meta synth, arrangement editor with editable Song Order bars overrides and marker-driven energy, ruler marker sketching, the timeline-wide piano roll, undo/redo,
SuperCollider export and a dual-deck live performance window. 64-bit VST3 effects and instruments are hosted on tracks and as graph FX nodes; VST2 is not supported yet. Remaining gaps are the
SuperCollider live session/OSC bridge and full effect-graph parity in the SC export.
See `docs/PORT.md` for the exact matrix.

![Sequencer](docs/screenshot-sequencer.png)

## Why C#

- The original app already embedded ~6,300 lines of C# for its audio engine. The port lifts
  that code into a real class library instead of compiling it with `Add-Type` at startup.
- No `ps2exe`: a normal build produces a normal executable, with no script-packing step and no
  antivirus false positives from a self-extracting PowerShell stub.
- Static typing, real async/await, unit tests and per-feature source files.
- Only Windows/.NET or a self-contained executable: no NuGet packages at runtime, no Node,
  no Electron, no Python. FFmpeg remains optional and only for MP3/OGG/FLAC export.

## Download

A prebuilt portable build is attached to the [C# port release](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.4.0):

- `Humble-Baker-Studio-CSharp-0.4.0-win-x64.exe` — self-contained single file, no .NET install required.
- `Humble-Baker-Studio-CSharp-0.4.0-win-x64.zip` — the same exe plus this README and the license.
- `SHA256SUMS.txt` — checksums for both.

The build is unsigned, so Windows SmartScreen may ask for confirmation.

## Build and run

```powershell
dotnet build HumbleBakerStudio.sln
dotnet test  HumbleBakerStudio.sln
dotnet run --project src\HumbleBakerStudio.App
```

Or make a portable build (no .NET install required on the target machine):

```powershell
.\build.ps1
# dist\Humble Baker Studio.exe plus its runtime files
```

Pass `-FrameworkDependent` to produce a small build that uses an installed .NET 9 runtime.

## Layout

| Path | Purpose |
|---|---|
| `src/HumbleBakerStudio.Audio` | Engine + project format. No UI dependencies beyond WinForms controls that are part of the instrument editors. |
| `src/HumbleBakerStudio.Audio/Engine` | The DSP/model code imported from the PowerShell build (one file per type). |
| `src/HumbleBakerStudio.App` | WinForms application: transport, piano roll, mixer, effects, synth, graph, mastering, AI, settings. |
| `tests/HumbleBakerStudio.Audio.Tests` | Render, determinism, project round-trip, WAV and MIDI tests. |
| `tools/extract-engine.ps1` | One-way bootstrap that produced `Engine/` from the PowerShell source. Not part of the build. |
| `docs/PORT.md` | What is ported, what is left, and the design decisions. |

## Live performance

Open **Live** in the toolbar for a two-deck performance window that holds two independent
projects at once:

- Load any project into deck A or B (or snapshot the current one with **From main**).
- Equal-power crossfader, per-deck gain and a one-knob LP/HP filter.
- **Sync** matches tempo by playback rate; **Queue** advances to the next project when a song ends.
- **Cue** sends a deck to a second Windows output for headphone pre-listening. Pick the device
  in **Cue out**; with `(none)`, cue stays silent.
- Decks render once on load, then mix live at 1024-frame blocks, so transitions are seamless.

![Live performance](docs/screenshot-live.png)

## Runtime dependencies

| Required | Notes |
|---|---|
| Windows 10/11 | 64-bit. |
| .NET 8 runtime | Not needed for the self-contained build. |
| Audio device | Playback uses `winmm` (waveOut), which ships with Windows. |

| Optional | Notes |
|---|---|
| Ollama | Local AI actions (melodies, drums, patch design, mix advice). |
| FFmpeg | MP3/OGG/FLAC export only. |
| Sample packs | `DrumKits`, `OrchestralHits`, `GuitarHits`, `FoleyPercussion` folders beside the app or configured in Settings. |
| SuperCollider | Not used by the C# port yet. |

## Project files

The port reads and writes the same `*.hbstudio.json` format as the PowerShell studio. Files
round-trip in both directions, including patch/FX parameters, patterns, automation, the patch
graph and buses.

## License

MIT, matching the rest of the repository. See `LICENSE` and `NOTICE.md`.
