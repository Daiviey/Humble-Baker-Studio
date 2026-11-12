# Humble Baker Studio - C# port (portable)

**Version 0.4.0** - Windows 10/11 x64 - self-contained, no .NET install required.

## Run

Double-click **`Humble Baker Studio.exe`**. Windows SmartScreen may warn because the build is
unsigned; choose *More info -> Run anyway*.

The app stores its settings and renders under `%LOCALAPPDATA%\HumbleBakerStudio`. Project files
use the same `*.hbstudio.json` format as the PowerShell studio.

## What is here

- `Humble Baker Studio.exe` - portable single-file build (46.7 MiB).
- Source: [`../src`](../src)
- Build guide: [`../README-CSHARP.md`](../README-CSHARP.md)
- Port status: [`../docs/PORT.md`](../docs/PORT.md)

## Downloads

- Release page: https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.4.0
- SHA-256: `7234d4b48cfb08ebe93aad85f2b28d087b29a7c5561316b6e01f10f553a7179a`

## Notes

- Optional: Ollama for local AI, FFmpeg for MP3/OGG/FLAC export, sample packs for the sampler.
- Headphone cueing in Live mode needs a second Windows audio output.
- The SuperCollider live session/OSC bridge is available in the PowerShell studio; the C# port currently has `.scd` export only.
- Alpha software: keep backups of project files.
