# Plug-in hosting status

Humble Baker Studio's built-in synths and 113 effects are native C# and work everywhere the
engine runs. Third-party plug-in hosting is platform-specific and is tracked separately.

| Platform / build | VST3 | Audio Unit (AU) | Notes |
|---|---|---|---|
| Windows C# | Done | Not applicable | VST3 instruments/effects on tracks and graph nodes via NAudio.Vst3; 64-bit only, no VST2. |
| Windows PowerShell | Not yet | Not applicable | The studio is .NET Framework/WinForms. Hosting VST3 needs a separate native/COM or bridge process. |
| macOS app | Not yet | Planned | The early Avalonia app has no audio output yet. AU hosting follows CoreAudio. |

## Why AU/VST is not a checkbox

A plug-in host needs all of the following, not just a file picker:

1. **Discovery** - scan bundles, read manifests, cache class IDs.
2. **Loading** - load native modules and create processor/controller instances.
3. **Audio** - feed block buffers with tempo/transport and render the plug-in output.
4. **State** - save/restore opaque state blobs with the project.
5. **UI** - host/native editor windows or embed the plug-in view.
6. **Threading/lifecycle** - load/unload safely while audio is running.
7. **Packaging** - code signing and notarization on macOS; architecture matching on Windows.

The engine already has a platform-neutral `IPluginHost`/`AudioPlugins` interface. The Windows C#
app installs a VST3 implementation at startup. The Mac app will install an Audio Unit
implementation the same way; the PowerShell studio needs a bridge because it targets .NET
Framework.

## macOS Audio Unit plan

1. Add CoreAudio output and an `IAudioOutput` abstraction first.
2. Add a native (Objective-C/C# P/Invoke) AudioToolbox shim:
   - `AudioComponent` discovery for `aumu`/`aufx`
   - `AudioUnit`/`AUAudioUnit` instantiation
   - render block feeding the engine's per-track buffer
   - `AUViewController`/`NSView` hosting for editors
   - component state save/restore
3. Install the AU host through the existing `AudioPlugins.Host` interface.
4. Ship it as an optional phase after the editors and CoreAudio; unsigned plug-ins need explicit
   user approval on macOS.

AUv3 extension hosting is a separate path from AUv2 and can follow once the AUv2 host is stable.

## PowerShell VST plan

The PowerShell studio cannot load the net8 VST host assembly directly. Two viable routes:

- **Bridge process (recommended):** a small .NET 8 helper process owns the VST3 host and the
  native plug-in; the PowerShell studio communicates audio blocks and parameter/state changes
  over shared memory/named pipes. The bridge gets the same VST3 implementation as the C# app.
- **Native/COM shim:** build a .NET Framework-compatible VST3 wrapper around the VST3 SDK. This
  duplicates the host work and is harder to maintain.

The bridge route also keeps VST3 crashes away from the PowerShell UI process.

## What is in the early macOS app

The first `.app` renders the demo or a project to WAV. It does **not** have audio output or
plug-in hosting yet. AU support will land after CoreAudio, not before; see
[`MACOS.md`](MACOS.md) and [`PARITY.md`](PARITY.md).
