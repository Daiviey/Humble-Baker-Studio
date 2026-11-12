# Humble Baker Studio — C# port (portable)

**Version 0.1.4** · Windows 10/11 x64 · self-contained, no .NET install required.

## Run

Double-click **`Humble Baker Studio.exe`**. Windows SmartScreen may warn because the build is
unsigned; choose *More info → Run anyway*.

The app stores its settings and renders under `%LOCALAPPDATA%\HumbleBakerStudio`. Project files
use the same `*.hbstudio.json` format as the PowerShell studio.

## What is here

- `Humble Baker Studio.exe` — portable single-file build (63.5 MB).
- Source: [`../src`](../src)
- Build guide: [`../README-CSHARP.md`](../README-CSHARP.md)
- Port status: [`../docs/PORT.md`](../docs/PORT.md)

## Downloads

- Release page: https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.1.4
- SHA-256: `b2b09b1c1f6faf6d270681be956cda9e8377f317f2709bc9187ed4f09965fb79`

## Notes

- Optional: Ollama for local AI, FFmpeg for MP3/OGG/FLAC export, sample packs for the sampler.
- Headphone cueing in Live mode needs a second Windows audio output.
- Alpha software: keep backups of project files.
