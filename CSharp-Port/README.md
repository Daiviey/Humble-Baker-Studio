# Humble Baker Studio - C# port (portable)

**Version 0.5.1** - Windows 10/11 x64 - self-contained, no .NET install required.

**This folder is the C# port, not the PowerShell studio.** The stable PowerShell build is
`../Humble Baker Studio.exe` (about 1.9 MB). This C# exe is about 46.7 MB and includes `(C#)`
in its file name.

## Run

Double-click **`Humble Baker Studio (C#).exe`**. Windows SmartScreen may warn because the build is
unsigned; choose *More info -> Run anyway*.

The app stores its settings and renders under `%LOCALAPPDATA%\HumbleBakerStudio`. Project files
use the same `*.hbstudio.json` format as the PowerShell studio.

## What is here

- `Humble Baker Studio (C#).exe` - portable single-file build (46.7 MiB).
- Source: [`../src`](../src)
- Build guide: [`../README-CSHARP.md`](../README-CSHARP.md)
- Port status: [`../docs/PORT.md`](../docs/PORT.md)
- Automation guide: [`../docs/AUTOMATION.md`](../docs/AUTOMATION.md)

## Downloads

- Release page: https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.5.1
- SHA-256: `d098816435e2bb5c6f2348631919de17f66a5adf0b6822430fc3af19401970f8`

## Notes

- Automation painting now supports high-resolution canvases, layered splines, crossing-stroke
  separation, amplitude normalization, mix modes and exact painted-canvas restoration.
- Optional: Ollama for local AI, FFmpeg for MP3/OGG/FLAC export, sample packs for the sampler.
- Headphone cueing in Live mode needs a second Windows audio output.
- The SuperCollider live session/OSC bridge is available in the PowerShell studio; the C# port currently has `.scd` export only.
- The FLUX photo/illustration mode in the spectral painter remains PowerShell-only.
- Alpha software: keep backups of project files.
