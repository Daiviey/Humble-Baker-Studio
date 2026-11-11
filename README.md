# Humble Baker Studio

A local, offline audio studio for Windows: piano roll, synths, drums, sample tracks, a patch graph, automation painting, mastering, and AI-assisted composition. No account, no cloud dependency — generated audio and project data stay on the machine.

## Run

- **`Humble Baker Studio.exe`** — double-click. If Windows SmartScreen warns, choose *More info → Run anyway*.
- **`Humble Baker Studio.ps1`** — the launcher source; loads `AudioStudioImplementation.ps1`.
- **`Humble Baker Studio.standalone.ps1`** — the single-file build (same studio, one script).

From PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File ".\Humble Baker Studio.ps1"
```

## Requirements

- Windows 10/11
- Windows PowerShell 5.1
- .NET Framework 4.8 (included with current Windows)
- Optional: **FFmpeg** for MP3, OGG, Opus, AAC, ALAC, AIFF, 24/32-bit WAV and resampling. The Export dialog can download it, or put `ffmpeg.exe` in `Tools\`.
- Optional: **SuperCollider** for live preview via `sclang`. The built-in synth needs no extra install.

## Features

- **Piano roll** with scales, snap, velocity, note lengths, and per-track drums/melody parts.
- **Synth engine**: subtractive, FM, additive, wavetable, vowel, Karplus-Strong pluck, modal bells, noise, filters, drive, bitcrush.
- **Drums** with multiple kits and per-kit character.
- **Patch graph**: tracks, FX modules, buses, groups, master out; shared FX nodes process all tracks routed through them.
- **Automation painting**:
  - paint a curve on the canvas; the horizontal axis spans the whole track/song
  - vertical position sets the value; brightness is a real **Z axis** (depth/value)
  - targets: **Track**, graph **Node** (e.g. a shared REVERB), or **Bus**
  - saved lanes redraw in the canvas when you select the target or reload the project
- **Export dialog**:
  - current pattern with 1–64 seamless loops, or the whole arrangement
  - tempo override, metronome click
  - mastering chain, peak normalization, fade in/out
  - WAV 16/24/32-float, MP3, OGG Vorbis, Opus, AAC/M4A, ALAC, FLAC, AIFF
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

## Notes

- Projects save as `.hbstudio.json`; renders go to the studio `renders\` folder.
- Audio is rendered at 44.1 kHz internally; other sample rates are resampled on export.
- The app is PowerShell/WinForms. Build the exe with `Invoke-ps2exe` if you change the source.
- No license file is included with this repository; add one before redistributing if you intend to.
