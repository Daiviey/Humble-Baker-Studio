# Releases

GitHub release assets are not stored in the file tree. Use the links below or open the
[Releases page](https://github.com/Daiviey/Humble-Baker-Studio/releases).

## C# port (alpha, Windows x64)

| Version | Downloads | Notes |
|---|---|---|
| **v0.5.0** | [Portable exe](https://github.com/Daiviey/Humble-Baker-Studio/releases/download/csharp-port-v0.5.0/Humble-Baker-Studio-CSharp-0.5.0-win-x64.exe) - [Zip](https://github.com/Daiviey/Humble-Baker-Studio/releases/download/csharp-port-v0.5.0/Humble-Baker-Studio-CSharp-0.5.0-win-x64.zip) | Spectral painting, meta synth and automation painting parity. Automation adds high-resolution canvases (up to 4096 x 512), layered splines, crossing-stroke/intersection separation, amplitude normalization, Add/Multiply/Max/Replace mixing, exact painted-canvas snapshots and logarithmic lane lookup. Also includes the instrument library save/load for spectral and morph voices and the v0.4.0 arrangement/timeline work. |
| v0.4.0 | [Portable exe](https://github.com/Daiviey/Humble-Baker-Studio/releases/download/csharp-port-v0.4.0/Humble-Baker-Studio-CSharp-0.4.0-win-x64.exe) | Editable Song Order entries with a bars override, marker-driven playback energy, ruler double-click marker sketching and the timeline-wide piano roll. Includes the VST3 instrument/effect hosting from v0.3.0. |
| v0.3.0 | [Portable exe](https://github.com/Daiviey/Humble-Baker-Studio/releases/download/csharp-port-v0.3.0/Humble-Baker-Studio-CSharp-0.3.0-win-x64.exe) | 64-bit VST3 effects and instruments: choose a VST3 instrument as the track instrument, insert VST3 effects on tracks, or pick one per FX node on the patch graph. Plug-in state is saved with the project. VST2 is not included. |
| v0.2.0 | [Release page](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.2.0) | Instrument and sound library, Foley/Orchestral/Guitar sounds, drum kits, Sample lab. |
| v0.1.9 | [Release page](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.1.9) | Note previews and live loop updates land on the next pass. |
| v0.1.8 | [Release page](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.1.8) | New tracks get patch-graph nodes automatically; deleted tracks are pruned. |
| v0.1.7 | [Release page](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.1.7) | Graph module editor, note preview, live loop refresh. |
| v0.1.6 | [Release page](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.1.6) | Master/headphone outputs and cue mix. |
| v0.1.5 | [Release page](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.1.5) | Readable 7-column live deck buttons. |
| v0.1.4 | [Release page](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.1.4) | Queue-add moved into the queue panel. |
| v0.1.3 | [Release page](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.1.3) | Live deck New buttons. |
| v0.1.2 | [Release page](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.1.2) | Auto-wires the patch graph on first open. |
| v0.1.1 | [Release page](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.1.1) | UI polish: menu bar, cards, live decks, settings and AI layouts. |
| v0.1.0 | [Release page](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.1.0) | First complete C# port with the two-deck live mixer. |

C# source: [`src/`](src) - build guide: [README-CSHARP.md](README-CSHARP.md) - port status: [docs/PORT.md](docs/PORT.md).

The portable exe is also committed in the repository at [`CSharp-Port/Humble Baker Studio.exe`](CSharp-Port/Humble%20Baker%20Studio.exe).

A copy of the latest C# build is also kept locally in `Z:\LocalAI\Repo\release\csharp-v0.5.0\`
and as `Z:\LocalAI\Repo\release\Humble-Baker-Studio-CSharp-0.5.0-win-x64.zip`.

## PowerShell studio (stable)

The packaged app is tracked in this repository as `Humble Baker Studio.exe`. Previous packaged
versions are listed on the [Releases page](https://github.com/Daiviey/Humble-Baker-Studio/releases);
the current version is **v1.0.22**.
