# Humble Baker Studio

<p align="center"><img src="icon.png" width="180" alt="Humble Baker Studio icon"></p>

<p align="center">
  <a href="https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.5.1"><img alt="Download the C# port" src="https://img.shields.io/badge/Download_C%23_port-v0.5.1-6caaff?style=for-the-badge"></a>
  <a href="https://github.com/Daiviey/Humble-Baker-Studio/releases"><img alt="All releases" src="https://img.shields.io/badge/All_releases-GitHub-23272f?style=for-the-badge"></a>
</p>

## Downloads

| Build | Get it |
|---|---|
| **PowerShell studio** (stable) | `Humble Baker Studio.exe` (**1.9 MB**, PowerShell) in this repository, or the [latest release](https://github.com/Daiviey/Humble-Baker-Studio/releases) |
| **C# port** (alpha, Windows x64, no .NET install needed) | `CSharp-Port/Humble Baker Studio (C#).exe` (**46.7 MB**) - [Portable exe](https://github.com/Daiviey/Humble-Baker-Studio/releases/download/csharp-port-v0.5.1/Humble-Baker-Studio-CSharp-0.5.1-win-x64.exe) · [Zip](https://github.com/Daiviey/Humble-Baker-Studio/releases/download/csharp-port-v0.5.1/Humble-Baker-Studio-CSharp-0.5.1-win-x64.zip) · [Release notes](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.5.1) |
| **C# No-AI edition** (alpha, deterministic patterns, no generative AI) | [Portable exe](https://github.com/Daiviey/Humble-Baker-Studio/releases/download/csharp-port-v0.5.1/Humble-Baker-Studio-CSharp-NoAI-0.5.1-win-x64.exe) - [Zip](https://github.com/Daiviey/Humble-Baker-Studio/releases/download/csharp-port-v0.5.1/Humble-Baker-Studio-CSharp-NoAI-0.5.1-win-x64.zip) - [No-AI notes](CSharp-Port/README-NoAI.md) |

**Not sure which build to pick?** See [docs/DOWNLOADS.md](docs/DOWNLOADS.md) for a side-by-side comparison. In short: the root `Humble Baker Studio.exe` is the PowerShell studio (about 1.9 MB); `CSharp-Port/Humble Baker Studio (C#).exe` is the C# port (about 46.7 MB); the No-AI edition is the same C# app with the AI tab replaced by a deterministic pattern library.

The C# source is in [`src/`](src) and the port guide is [README-CSHARP.md](README-CSHARP.md). A copy of the portable exe is committed in [`CSharp-Port/`](CSharp-Port/); release assets also live under **Releases**.

A local, offline audio studio for Windows: piano roll, synths, drums, sample tracks, a patch graph, automation painting, mastering, and AI-assisted composition. No account, no cloud dependency — generated audio and project data stay on the machine. Built with deepseek v4 flash and qwen. This is honestly more vibey than i really want it to be, it started as messing around but I really like the workflow so i am uploading it for some friends, If you stumble here and try it, let me know what you would like to see, what issues you run into. I'll be developing it more seriously from her on out. {because I am loving using it, like, it may replace my actual work shop with a little more ironing out}

## Run

- **`Humble Baker Studio.exe`** (**PowerShell studio, stable, about 1.9 MB**) — double-click. If Windows SmartScreen warns, choose *More info → Run anyway*.
- **`CSharp-Port\Humble Baker Studio (C#).exe`** - C# port (alpha, about 46.7 MB). Self-contained; no .NET install required.
- **`Humble Baker Studio.ps1`** — the launcher source; loads `AudioStudioImplementation.ps1`.
- **`Humble Baker Studio.standalone.ps1`** — the single-file build (same studio, one script).

From PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File ".\Humble Baker Studio.ps1"
```

## Documentation

| Guide | Contents |
|---|---|
| [Manual](docs/MANUAL.md) | every window, tab, tool, button and workflow |
| [SuperCollider tutorial](docs/SUPERCOLLIDER.md) | live bridge, generated graph projects, 113 native effects, parity harness, SynthDefs, patterns, buses, MIDI/OSC, debugging, cited SC docs |
| [Effect Reference](docs/EFFECTS.md) | all 113 effects, every control, range and description |
| [Synth Patch Reference](docs/SYNTH.md) | every synth parameter |
| [Settings Reference](docs/SETTINGS.md) | mastering, mix rules, spectral and meta settings |
| [Automation Painting](docs/AUTOMATION.md) | curves, layers, intersections, resolution, normalization and performance |
| [Which build?](docs/DOWNLOADS.md) | PowerShell studio vs C# port, file names, sizes and release tags |
| [macOS plan](docs/MACOS.md) | what a Mac port would take, what is reusable and the recommended phases |
| [Platform parity](docs/PARITY.md) | feature matrix and workstreams for Windows, PowerShell and macOS |
| [Plug-in status](docs/PLUGINS.md) | VST3 on Windows, Audio Unit plans for macOS, and the PowerShell bridge design |

## Requirements

### Required

| Requirement | Link | Notes |
|---|---|---|
| Windows 10/11 | [microsoft.com/windows](https://www.microsoft.com/windows/) | 64-bit recommended |
| Windows PowerShell 5.1 | [Microsoft Learn — Installing Windows PowerShell](https://learn.microsoft.com/powershell/scripting/install/installing-windows-powershell) | Built into Windows 10/11; needed for the `.ps1` builds |
| .NET Framework 4.8 | [dotnet.microsoft.com — .NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48) | Included with current Windows; install if the app reports a missing runtime |
| Microsoft Visual C++ Redistributable | [Microsoft Learn — latest supported VC++ redist](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist) | Recommended if the exe or FFmpeg fails to start |

### Optional

| Requirement | Link | Notes |
|---|---|---|
| FFmpeg | [ffmpeg.org/download](https://ffmpeg.org/download.html) · [Windows builds (gyan.dev)](https://www.gyan.dev/ffmpeg/builds/) | Needed for MP3, OGG Vorbis, FLAC, non-44.1 kHz or mono exports. The Export dialog can download it, or put `ffmpeg.exe` in `Tools\` (or `Audio\studio\ffmpeg\ffmpeg.exe`). |
| SuperCollider | [supercollider.github.io/downloads](https://supercollider.github.io/downloads) | Only for live preview/render via `sclang`. The built-in synth needs no extra install. Use **Locate SuperCollider** in the app. |
| Ollama | [ollama.com/download](https://ollama.com/download) | Only for the local AI patch/drum/melody/composer/mastering tools. Runs fully locally. |
| Git | [git-scm.com/download/win](https://git-scm.com/download/win) | Only for cloning or contributing to the source. |
| PS2EXE | [github.com/MScholtes/PS2EXE](https://github.com/MScholtes/PS2EXE) | Only for rebuilding the `.exe` from the PowerShell source. |

No installer is required: the build is portable. FFmpeg and SuperCollider are discovered from the app folder or the standard install locations.

## C# port (alpha)

A native .NET port of the studio lives in `src/` and is documented in [README-CSHARP.md](README-CSHARP.md) and [docs/PORT.md](docs/PORT.md). It reads and writes the same `*.hbstudio.json` projects and includes the two-deck Live performance window, an instrument and sound library, and 64-bit VST3 effects and instruments on tracks and graph nodes (VST2 is not supported yet).

Prebuilt portable build: [C# port v0.5.1](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.5.1) (self-contained Windows x64 exe, no .NET install required). The automation editor guide is [docs/AUTOMATION.md](docs/AUTOMATION.md).

The **No-AI edition** removes the generative AI tab, toolbar button and menu entries and replaces them with a **Patterns** tab: a curated library of pre-programmed MIDI examples selected by role, style, mood, energy, complexity, density and a reproducible seed. Build it with `.\build.ps1 -NoAi` or download `Humble-Baker-Studio-CSharp-NoAI-0.5.1-win-x64.exe` from the same release.

The **macOS port** has started: the engine targets `net8.0`, a headless renderer publishes for Apple Silicon and Intel, and an early native **Avalonia `.app`** can render the demo or a project to WAV. The full editor UI and CoreAudio are the next phase. See [docs/MACOS.md](docs/MACOS.md) and [docs/PARITY.md](docs/PARITY.md).

## Features

- **Piano roll** with scales, snap, velocity, note lengths, and per-track drums/melody parts.
- **Synth engine**: subtractive, FM, additive, wavetable, vowel, Karplus-Strong pluck, modal bells, noise, filters, drive, bitcrush.
- **Drums** with multiple kits and per-kit character.
- **Patch graph**: tracks, FX modules, buses, groups, master out; **all 113 effects are added as graph nodes**, and shared FX nodes process all tracks routed through them.
- **MIDI input**: connect a controller, watch a live note/CC log, record quantized notes into the selected track, and optionally monitor incoming notes through that track.
- **Sampler tab**: WAV import, draggable waveform selection, normalize/reverse/trim, auto-slice at onsets, equal chop, add selection as a slice, audition and export slices, and turn slices into sampler tracks.
- **Automation painting**:
  - paint a curve on the canvas; the horizontal axis spans the whole track/song
  - vertical position sets the value; brightness is a real **Z axis** (depth/value)
  - targets: **Track**, graph **Node** (e.g. a shared REVERB), or **Bus**
  - canvas resolutions from 512 x 128 up to 4096 x 512, with a screen-pixel brush
  - **layered splines**: extract multiple waves, assign each its own parameter, normalize by amplitude and mix with Add/Multiply/Max/Replace
  - **crossing strokes stay separate**, so intersecting automation waves can be layered instead of merged
  - saved lanes restore an exact compressed snapshot of the painted canvas at its original resolution
  - logarithmic lane lookup keeps dense automation fast during playback
- **Export dialog**:
  - current pattern with 1–64 seamless loops, or the whole arrangement
  - tempo override, metronome click
  - mastering chain, peak normalization, fade in/out
  - WAV 16-bit, MP3 (320/192/V0), OGG Vorbis (q6/q9), FLAC
  - 44.1 / 48 / 96 kHz, stereo or mono
- **Mastering** analysis and auto-master, AI mixing/mastering advice.
- **AI tools** for patches, drums, melody, arrangement, mixing and mastering, using a local Ollama model.
- **SuperCollider export** (`.scd`) and **MIDI export**.
- **Live performance (Live mix)**: open two independent projects at once in a two-deck window with an equal-power crossfader, per-deck gain and a one-knob LP/HP filter, tempo **Sync**, per-deck queues, and a separate **cue/headphone output**. Click **Live mix** in the toolbar. Cue needs a second Windows audio output; everything else works on one device.

## Files

| File | Purpose |
|---|---|
| `Humble Baker Studio.exe` | Packaged Windows app |
| `Humble Baker Studio.ps1` | Launcher source |
| `AudioStudioImplementation.ps1` | Canonical studio implementation |
| `Humble Baker Studio.standalone.ps1` | Single-file standalone build |
| `LivePerformanceImplementation.ps1` | Live performance module source (also embedded in the app) |
| `tests/sc-parity.ps1` | Headless SuperCollider effect/parity harness |
| `docs/` | Manual and full parameter references |
| `LICENSE` | MIT license for the software |
| `NOTICE.md` | Copyright, content/brand terms and third-party attributions |

## License

The software and documentation are released under the **MIT License**; see
[LICENSE](LICENSE).

The Humble Baker name, cupcake icon/logo and other brand assets are reserved,
and third-party tools/models keep their own licenses. See [NOTICE.md](NOTICE.md)
for the full terms, attributions and redistribution checklist.

## Notes

- Projects save as `.hbstudio.json`; renders go to the studio `renders\` folder.
- Audio is rendered at 44.1 kHz internally; other sample rates are resampled on export.
- The app is PowerShell/WinForms. Build the exe with `Invoke-ps2exe` if you change the source.
- No license file is included with this repository; add one before redistributing if you intend to.
