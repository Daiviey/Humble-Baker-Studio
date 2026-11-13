# Platform parity matrix

This matrix tracks feature parity between the Windows builds, the PowerShell studio and the
macOS port. It is intentionally explicit about what is shipped and what is still planned.

Status values: **Done**, **Partial**, **Planned**, **Windows-only**.

| Feature | Windows C# (standard) | Windows C# No-AI | PowerShell 1.0.24 | macOS core / CLI | macOS UI |
|---|---|---|---|---|---|
| Project model and `*.hbstudio.json` | Done | Done | Done | **Done** | Done (open/save) |
| Synths, drums, effects, mixing, mastering | Done | Done | Done | **Done** | Planned |
| Render and WAV export | Done | Done | Done | **Done** (CLI) | Done |
| Piano roll, tracks, arrangement, song order | Done | Done | Done | Done (model) | **Partial** (track list + click-to-edit roll; no arrangement editor) |
| Sample lab and sound library | Done | Done | Done | Done (model) | Planned |
| Spectral painting | Done | Done | Done (incl. FLUX) | Done (engine) | Planned |
| Meta synth | Done | Done | Done | Done (engine) | Planned |
| Automation painting (basic curves) | Done | Done | Done | Done (engine) | Planned |
| Automation layers, intersections, high-res, snapshots | Done | Done | **Partial** (high-res + fast lookup done; layers/intersections/snapshots missing) | Done (engine) | Planned |
| Instrument library save/load (spectral/morph) | Done | Done | Done | Done (engine) | Planned |
| Deterministic pattern library / No-AI edition | Done | Done | **Missing** | Done (engine) | Planned |
| VST3 instruments and effects | Done | Done | **Done** (optional `hbs-vst-bridge`; no editor window) | Planned | Planned |
| Audio Unit (AU) hosting | Not applicable | Not applicable | Not applicable | Planned | **Discovery only** |
| CoreAudio playback | Done (waveOut) | Done (waveOut) | Done | Not applicable | **Done** (AudioQueue) |
| Two-deck Live mode | Done | Done | Done | Model done | Planned |
| Local AI (Ollama) | Done | Missing by design | Done | Planned | Planned |
| MIDI input recording | Done | Done | Done | Planned (CoreMIDI) | Planned |
| SuperCollider bridge | Export only | Export only | Done (live bridge) | Export planned | Planned |

## What is already shared

The `HumbleBakerStudio.Audio` project now multi-targets `net8.0` and `net8.0-windows`:

- `net8.0-windows` keeps the WinForms editors (`PianoRoll`, `PatchGraphControl`, `Knob`,
  `KnobPanel`, `WaveView`) and the `winmm` MIDI input.
- `net8.0` contains the portable project model, DSP, effects, automation, spectral/meta engines,
  instrument library, deterministic pattern composer and export code.
- `SpectralGrid` bitmap helpers are Windows-only; the portable engine uses the grid data and
  exposes rendering helpers to the UI layer instead.

The headless renderer in `src/HumbleBakerStudio.Cli` proves the portable core:

```powershell
dotnet run --project src\HumbleBakerStudio.Cli -- demo out.wav
dotnet run --project src\HumbleBakerStudio.Cli -- render project.hbstudio.json out.wav
```

And it publishes for Apple Silicon:

```powershell
.\build-macos.ps1 -Runtime osx-arm64
# dist-macos\osx-arm64\hbs-render
```

## Parity workstreams

### P0 - shared core (done)

- Multi-target the engine.
- Headless renderer for Windows/macOS/Linux.
- osx-arm64 and osx-x64 publish path.
- Tests run against the Windows target; the portable target builds cleanly.

### P1 - native macOS UI (next)

- Avalonia shell: transport, project open/save, track list and click-to-edit piano roll (done).
- CoreAudio output via AudioQueue (done).
- Mixer, effects, automation and arrangement editors (next).
- Project-format compatibility tests on macOS.
- Publish an unsigned `.app`/binary for Apple Silicon and Intel (done).

### P2 - editing parity

- Sample lab and library.
- Spectral/meta editors using a SkiaSharp canvas.
- Automation painting with the portable layer/intersection/snapshot engine.
- Deterministic Patterns UI backed by `PatternComposer`.

### P3 - platform extras

- Local AI via Ollama.
- macOS VST3 hosting (separate from the built-in effects).
- CoreMIDI input.
- Two-deck Live window.
- Code signing, notarization and DMG packaging.

### PowerShell parity

The PowerShell studio already has spectral painting, the meta synth and the original automation
painter. The workstream to reach C# parity is:

1. ~~port the portable `AutoLane` binary-search lookup, high-resolution canvas and LockBits
   rendering into the embedded engine~~ (done in 1.0.23)
2. add automation display snapshots to `AutoLane` and the PowerShell project reader/writer
3. add per-stroke layer capture, intersection separation, amplitude normalization and mix modes
4. add the deterministic pattern library and a No-AI switch
5. validate by launching `Humble Baker Studio.ps1` from a PowerShell host and rebuilding the
   packaged exe with ps2exe

PowerShell **1.0.24** ships the high-resolution automation canvas, LockBits rendering,
binary-search lane lookup and VST3 hosting through the optional out-of-process
`hbs-vst-bridge`. The remaining steps are the snapshot/layer/intersection work and the
deterministic pattern tab; the matrix above is the source of truth for what each build
contains. Plug-in architecture, the bridge protocol and the macOS AU status are in
[`PLUGINS.md`](PLUGINS.md).
