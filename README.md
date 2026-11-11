# Humble Baker Studio

<p align="center"><img src="icon.png" width="180" alt="Humble Baker Studio icon"></p>

A local, offline audio studio for Windows: piano roll, synths, drums, sample tracks, a patch graph, automation painting, mastering, and AI-assisted composition. No account, no cloud dependency — generated audio and project data stay on the machine. Built with deepseek v4 flash and qwen. 

## Run

- **`Humble Baker Studio.exe`** — double-click. If Windows SmartScreen warns, choose *More info → Run anyway*.
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
| [Effect Reference](docs/EFFECTS.md) | all 98 effects, every control, range and description |
| [Synth Patch Reference](docs/SYNTH.md) | every synth parameter |
| [Settings Reference](docs/SETTINGS.md) | mastering, mix rules, spectral and meta settings |

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

## Features

- **Piano roll** with scales, snap, velocity, note lengths, and per-track drums/melody parts.
- **Synth engine**: subtractive, FM, additive, wavetable, vowel, Karplus-Strong pluck, modal bells, noise, filters, drive, bitcrush.
- **Drums** with multiple kits and per-kit character.
- **Patch graph**: tracks, FX modules, buses, groups, master out; **all 98 effects are added as graph nodes**, and shared FX nodes process all tracks routed through them.
- **MIDI input**: connect a controller, watch a live note/CC log, record quantized notes into the selected track, and optionally monitor incoming notes through that track.
- **Sampler tab**: WAV import, draggable waveform selection, normalize/reverse/trim, auto-slice at onsets, equal chop, add selection as a slice, audition and export slices, and turn slices into sampler tracks.
- **Automation painting**:
  - paint a curve on the canvas; the horizontal axis spans the whole track/song
  - vertical position sets the value; brightness is a real **Z axis** (depth/value)
  - targets: **Track**, graph **Node** (e.g. a shared REVERB), or **Bus**
  - saved lanes redraw in the canvas when you select the target or reload the project
- **Export dialog**:
  - current pattern with 1–64 seamless loops, or the whole arrangement
  - tempo override, metronome click
  - mastering chain, peak normalization, fade in/out
  - WAV 16-bit, MP3 (320/192/V0), OGG Vorbis (q6/q9), FLAC
  - 44.1 / 48 / 96 kHz, stereo or mono
- **Mastering** analysis and auto-master, AI mixing/mastering advice.
- **AI tools** for patches, drums, melody, arrangement, mixing and mastering, using a local Ollama model.
- **SuperCollider export** (`.scd`) and **MIDI export**.

## Files

| File | Purpose |
|---|---|
| `Humble Baker Studio.exe` | Packaged Windows app |
| `Humble Baker Studio.ps1` | Launcher source |
| `AudioStudioImplementation.ps1` | Canonical studio implementation |
| `Humble Baker Studio.standalone.ps1` | Single-file standalone build |
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
