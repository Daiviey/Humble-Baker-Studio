# Which build should I download?

Humble Baker Studio has two Windows builds. They share the same `*.hbstudio.json` project
format, so projects open in both.

| | PowerShell studio (stable) | C# port (alpha) | C# No-AI edition |
|---|---|---|---|
| **Find it in the repo** | `Humble Baker Studio.exe` | `CSharp-Port/Humble Baker Studio (C#).exe` | build with `.\build.ps1 -NoAi`; release asset `Humble-Baker-Studio-CSharp-NoAI-...` |
| **Approx. size** | 1.9 MB | 46.7 MB | 46.7 MB |
| **Release tag** | `v1.0.23` (tags without a prefix) | `csharp-port-v0.5.1` (tags start with `csharp-port-`) | `csharp-port-v0.5.1` (separate asset) |
| **Release asset name** | `Humble-Baker-Studio-v<version>.zip` / the repo exe | `Humble-Baker-Studio-CSharp-<version>-win-x64.exe` | `Humble-Baker-Studio-CSharp-NoAI-<version>-win-x64.exe` |
| **Runtime** | Windows PowerShell 5.1 + .NET Framework 4.8 | Self-contained; no .NET install required | Self-contained; no .NET install required |
| **UI** | PowerShell/WinForms | Native .NET/WinForms | Native .NET/WinForms |
| **Best for** | The most complete feature set, including the SuperCollider live bridge and FLUX spectral photo mode | Faster native app, VST3 hosting, the newest automation painter | A deterministic workflow with no generative AI: a pre-programmed MIDI pattern library replaces the AI tab and the AI design buttons |

## How to tell them apart

- **File size**: about **1.9 MB** = PowerShell studio. About **46.7 MB** = C# port.
- **Folder**: the C# copy lives in the `CSharp-Port` folder and the file name includes `(C#)`.
- **Release tag**: C# releases always start with `csharp-port-`.
- **Release asset**: C# assets include `CSharp` in the file name; PowerShell releases do not.

## Download links

- **PowerShell studio (stable)**: [latest release](https://github.com/Daiviey/Humble-Baker-Studio/releases) - the packaged app is also committed as `Humble Baker Studio.exe`.
- **C# port v0.5.1**:
  - [Portable exe](https://github.com/Daiviey/Humble-Baker-Studio/releases/download/csharp-port-v0.5.1/Humble-Baker-Studio-CSharp-0.5.1-win-x64.exe)
  - [Zip](https://github.com/Daiviey/Humble-Baker-Studio/releases/download/csharp-port-v0.5.1/Humble-Baker-Studio-CSharp-0.5.1-win-x64.zip)
  - [No-AI edition exe](https://github.com/Daiviey/Humble-Baker-Studio/releases/download/csharp-port-v0.5.1/Humble-Baker-Studio-CSharp-NoAI-0.5.1-win-x64.exe)
  - [No-AI edition zip](https://github.com/Daiviey/Humble-Baker-Studio/releases/download/csharp-port-v0.5.1/Humble-Baker-Studio-CSharp-NoAI-0.5.1-win-x64.zip)
  - [Release notes](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.5.1)

The C# portable exe is also committed in [`CSharp-Port/`](../CSharp-Port/) as
`Humble Baker Studio (C#).exe`.

## Feature differences

Both builds have the piano roll, synths, drums, sampler, patch graph, mixing, mastering,
spectral painting, meta synth, automation painting, MIDI and local AI.

The C# port additionally has:

- the expanded automation painter with layered splines, crossing-stroke/intersection
  separation, canvases up to 4096 x 512, amplitude normalization and exact painted-canvas
  snapshots
- native VST3 instrument and effect hosting on tracks and graph nodes
- a self-contained single-file exe

The PowerShell studio has:

- a 1024 x 256 automation canvas, fast LockBits rendering and binary-search lane lookup
- the SuperCollider live session/OSC bridge
- the FLUX photo/illustration mode in the spectral painter
- the original PowerShell scripting and automation hooks

See [`PORT.md`](PORT.md) for the full port matrix and [`AUTOMATION.md`](AUTOMATION.md) for the
C# automation editor.

## Checksums

C# release checksums are attached to each C# release as `SHA256SUMS.txt`. The PowerShell
release checksums are attached to the corresponding PowerShell release.
