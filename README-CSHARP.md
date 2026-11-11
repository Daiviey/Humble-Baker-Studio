# Humble Baker Studio — C# port

An experimental, native .NET port of Humble Baker Studio. The shipped PowerShell app keeps
working; this tree is the clean-room restructuring of it into a typed, testable Windows
application.

**Status: usable alpha.** The project model, synthesis engine, 113-effect DSP, mixing,
mastering, piano roll, project format, WAV export and local-AI helpers are in place.
A few of the original studio's specialist tabs (SuperCollider live sessions, spectral
painting, song-model import) are not ported yet. See `docs/PORT.md` for the exact matrix.

![Sequencer](docs/screenshot-sequencer.png)

## Why C#

- The original app already embedded ~6,300 lines of C# for its audio engine. The port lifts
  that code into a real class library instead of compiling it with `Add-Type` at startup.
- No `ps2exe`: a normal build produces a normal executable, with no script-packing step and no
  antivirus false positives from a self-extracting PowerShell stub.
- Static typing, real async/await, unit tests and per-feature source files.
- Only Windows/.NET or a self-contained executable: no NuGet packages at runtime, no Node,
  no Electron, no Python. FFmpeg remains optional and only for MP3/OGG/FLAC export.

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

Pass `-FrameworkDependent` to produce a small build that uses an installed .NET 8 runtime.

## Layout

| Path | Purpose |
|---|---|
| `src/HumbleBakerStudio.Audio` | Engine + project format. No UI dependencies beyond WinForms controls that are part of the instrument editors. |
| `src/HumbleBakerStudio.Audio/Engine` | The DSP/model code imported from the PowerShell build (one file per type). |
| `src/HumbleBakerStudio.App` | WinForms application: transport, piano roll, mixer, effects, synth, graph, mastering, AI, settings. |
| `tests/HumbleBakerStudio.Audio.Tests` | Render, determinism, project round-trip, WAV and MIDI tests. |
| `tools/extract-engine.ps1` | One-way bootstrap that produced `Engine/` from the PowerShell source. Not part of the build. |
| `docs/PORT.md` | What is ported, what is left, and the design decisions. |

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
