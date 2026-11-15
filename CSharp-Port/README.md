# Humble Baker Studio - C# port (portable)

**Version 0.6.0** - Windows 10/11 x64 - self-contained, no .NET install required.

**This folder is the C# port, not the PowerShell studio.** The stable PowerShell build is
`../Humble Baker Studio.exe` (about 1.9 MB). This C# exe is about 46.8 MB and includes `(C#)`
in its file name.

## Run

Double-click **`Humble Baker Studio (C#).exe`**. Windows SmartScreen may warn because the build is
unsigned; choose *More info -> Run anyway*.

The app stores its settings and renders under `%LOCALAPPDATA%\HumbleBakerStudio`. Project files
use the same `*.hbstudio.json` format as the PowerShell studio.

## What is new in 0.6.0

- **Live deck speed and pitch**: separate SPEED and PITCH faders plus a KEY LOCK switch
  (two-grain granular time-stretch) and RESET.
- **Edit both decks while mixing**: the EDIT button opens an independent project window for a
  deck; **Update deck** re-renders without saving, and a save does the same.
- **Sustained sampler**: crossfaded sustain loop with adjustable start/end, ADSR, vibrato,
  glide, brightness and body.
- **Instrument emulator engines**: `strings`, `bowed`, `cello`, `pluck`, `strum` and `hybrid`.
- **AI arrangement crew**: drums, bass and harmony written in sequence with a reviewer pass.

## What is here

- `Humble Baker Studio (C#).exe` - portable single-file build (46.8 MiB).
- Source: [`../src`](../src)
- Build guide: [`../README-CSHARP.md`](../README-CSHARP.md)
- Port status: [`../docs/PORT.md`](../docs/PORT.md)
- Automation guide: [`../docs/AUTOMATION.md`](../docs/AUTOMATION.md)

## Downloads

- Release page: https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.6.0
- SHA-256: `40d0763a43dc92c651841c5223b3fd6bf9d04b14289c465a9f695b288df9714d`

## Notes

- Optional: Ollama for local AI, FFmpeg for MP3/OGG/FLAC export, sample packs for the sampler.
- Headphone cueing in Live mode needs a second Windows audio output.
- The SuperCollider live session/OSC bridge is available in the PowerShell studio; the C# port currently has `.scd` export only.
- The FLUX photo/illustration mode in the spectral painter remains PowerShell-only.
- Alpha software: keep backups of project files.
