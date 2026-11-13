# macOS build: feasibility and plan

Short answer: there is no native macOS **UI** build today, and it is not a one-click rebuild.
The DSP and project format are highly reusable, but the current C# port's UI is Windows Forms
and its realtime output/plugin hosting are Windows-specific. A native Mac app is a real port,
not a repackaging.

## Current progress

The portable foundation is in place:

- `HumbleBakerStudio.Audio` now multi-targets `net8.0` and `net8.0-windows`. The project model,
  DSP, effects, automation, spectral/meta engines, pattern composer and export code build for
  plain `net8.0` with no WinForms references.
- `src/HumbleBakerStudio.Cli` is a headless cross-platform renderer:

  ```bash
  dotnet run --project src/HumbleBakerStudio.Cli -- demo out.wav
  dotnet run --project src/HumbleBakerStudio.Cli -- render project.hbstudio.json out.wav
  ```

- It publishes for Apple Silicon:

  ```powershell
  .\build-macos.ps1 -Runtime osx-arm64
  # dist-macos\osx-arm64\hbs-render
  ```

  `osx-x64` is also supported by the same script.

- The early **native shell** is now an Avalonia `.app`. It can render the demo project and open
  a `*.hbstudio.json` project and render it to WAV. Build it with:

  ```powershell
  .\build-macos-app.ps1 -Runtime osx-arm64
  # dist-macos\app-osx-arm64\Humble Baker Studio.app
  # dist-macos\Humble-Baker-Studio-macOS-osx-arm64-v0.1.0.zip
  ```

  `osx-x64` builds the same bundle for Intel. The bundles are unsigned and the full editor is
  still being ported.

An early **CLI preview** is also attached to the
[macOS CLI v0.1.0 release](https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/macos-cli-v0.1.0).

The next milestone is the full editor UI and CoreAudio output. See
[`PARITY.md`](PARITY.md) for the feature matrix and workstreams.

The PowerShell studio is Windows-only as well (Windows PowerShell 5.1, .NET Framework and
WinForms), so the C# codebase is the only realistic path to macOS.

## What can be reused

| Area | Reuse | Notes |
|---|---|---|
| Project model and `*.hbstudio.json` format | High | Keep the schema unchanged so projects remain portable. |
| Synthesizers, drums, effects and DSP | High | The render path is mostly pure C#. |
| Arrangement, automation, mastering and export logic | High | Logic is separated from the UI in `HumbleBakerStudio.Audio`. |
| Tests | High | Render, determinism, project round-trip and WAV tests should run on macOS. |
| Sample and instrument libraries | High | File-based; only path discovery changes. |
| UI | Low | WinForms-specific; needs a cross-platform rewrite. |
| Audio output | Low | `winmm`/waveOut is Windows-only. Replace with CoreAudio. |
| VST3 hosting | Low | NAudio.Vst3 and the current host are Windows-specific. |
| PowerShell studio | None | Windows PowerShell and WinForms do not run on macOS. |

## Main blockers in the current code

1. **WinForms UI.** Every tab is a `UserControl`/`Form`; the app uses `PropertyGrid`,
   `FlowLayoutPanel`, custom-drawn controls and WinForms timers.
2. **Windows audio.** Playback uses `winmm` (`waveOut`) P/Invoke.
3. **Windows-only platform calls.** Process launching, registry/path assumptions, icons,
   SmartScreen-style packaging and high-DPI behavior.
4. **Plug-in hosting.** VST3 loading and state capture use Windows modules and COM-style
   windows.
5. **`System.Drawing`.** The engine uses `Bitmap`/`Color` for spectral grids and custom
   controls; `System.Drawing.Common` is no longer cross-platform on modern .NET.

## Recommended approach

Use a cross-platform .NET UI rather than trying to shim WinForms.

1. **Extract a cross-platform core**
   - Move the reusable engine to a `net8.0` library (no WinForms).
   - Replace `System.Drawing` bitmap/color use with `SkiaSharp` or a small
     `IPixelCanvas`/`IColor` abstraction.
   - Keep the project format and the existing tests.
2. **Choose the UI**
   - **Avalonia** is the closest fit for a desktop DAW-style app and supports Windows, macOS
     and Linux.
   - Rebuild the main shell and the highest-value tabs first: transport, tracks, piano roll,
     mixer, effects, synth and project save/load.
3. **Replace audio output**
   - Implement an `IAudioOutput` interface with a CoreAudio/AudioQueue backend on macOS.
   - The existing background render-to-buffer model already fits: render the loop, queue PCM
     buffers, and keep the DSP unchanged.
   - A cross-platform library (PortAudio, SDL2 or SoLoud) is a faster first step than a
     hand-written CoreAudio layer.
4. **Platform services**
   - Settings and render folders under `~/Library/Application Support/Humble Baker Studio`.
   - FFmpeg discovery and export.
   - Ollama is already a local HTTP service and works on macOS.
5. **Postpone plug-ins**
   - Ship built-in synths/effects first.
   - Add macOS VST3 hosting as a separate phase (bundle discovery, module loading, editor
     windows, state).
6. **Package and sign**
   - `.app` bundle, code signing, notarization and a DMG.

## Phased plan

| Phase | Deliverable | Risk |
|---|---|---|
| 0 | Cross-platform `HumbleBakerStudio.Core`; tests run on macOS | Low-medium: remove WinForms/System.Drawing leaks |
| 1 | Avalonia shell, transport, project load/save, WAV export, piano roll | Medium: UI rewrite |
| 2 | Mixer, effects, synth, sampler, spectral/meta and automation painters | Medium-high: custom controls |
| 3 | Local AI, FFmpeg export, settings and sample discovery | Low |
| 4 | macOS VST3 hosting, Live mode, signing/notarization | High |

A usable alpha is possible after phases 0-2; full plug-in parity is a separate effort.

## Stopgap options

- **CrossOver / Whisky / Wine**: the self-contained C# exe may launch, but audio latency,
  WinForms theming, file dialogs and VST3 hosting will be rough. The PowerShell studio is a
  poor fit for Wine because it depends on Windows PowerShell and ps2exe.
- **Parallels / a Windows VM**: the most reliable way to run either build on Apple hardware
  today, but it is not a native Mac app.

## Contributing

The cleanest first pull request is the core extraction: remove WinForms/`System.Drawing` from
`HumbleBakerStudio.Audio`, keep the project format and tests green, and add a tiny
cross-platform smoke test. The UI can then be built against that core without touching the
DSP.
