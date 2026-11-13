# Plug-in hosting status

Humble Baker Studio's built-in synths and 113 effects are native C# and work everywhere the
engine runs. Third-party plug-in hosting is platform-specific and is tracked separately.

| Platform / build | VST3 | Audio Unit (AU) | Notes |
|---|---|---|---|
| Windows C# | Done | Not applicable | VST3 instruments/effects on tracks and graph nodes via NAudio.Vst3; 64-bit only, no VST2. |
| Windows PowerShell | Done, optional bridge | Not applicable | Pick a VST3 in **Tools -> VST3 plug-ins**; `hbs-vst-bridge.exe` hosts it out-of-process. 64-bit only, effects and instruments, state saved in the project. No editor window or VST2 yet. |
| macOS app | Not applicable | Discovery only | The `.app` lists installed AUs (`AudioComponent`). Instantiation, render and CoreAudio output are the next milestone. |

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

## macOS Audio Unit status

**Implemented:** `src/HumbleBakerStudio.Mac/AudioUnits.cs` calls `AudioComponentFindNext` /
`AudioComponentCopyName` / `AudioComponentGetDescription` and the **Audio Units...** button
lists `aumu`, `aufx`, `augn` and `aumf` components installed on the Mac.

**Still to do (needs a macOS build and test machine):**

1. CoreAudio output and an `IAudioOutput` abstraction.
2. `AudioComponentInstanceNew` + stream-format setup.
3. An input render callback (AUv2) or `AudioUnitProcess` (AUv3) feeding the engine buffer.
4. `AudioUnitSetProperty` for component class/state save and restore.
5. `AUViewController`/`NSView` hosting for plug-in editors.
6. Install the host through `AudioPlugins.Host` so it looks like the Windows implementation.
7. Code signing/notarization; unsigned plug-ins need explicit user approval on macOS.

AUv3 extension hosting is a separate path from AUv2 and can follow once the AUv2 host is stable.

## PowerShell VST bridge (implemented)

The PowerShell studio targets .NET Framework, so it cannot load the net9 `NAudio.Vst3` host.
`src/HumbleBakerStudio.VstBridge` is a self-contained .NET 9 console process that owns the
VST3 modules and speaks a small binary protocol over a named pipe:

| Op | Purpose |
|---|---|
| 1 | Ping/version |
| 2 | Scan installed VST3 classes |
| 3 | Process a stereo effect buffer |
| 4 | Render an instrument from a note list |
| 5 | Capture the opaque plug-in state |
| 6 | Release an instance |
| 7 | Shut down |

Each request is `int32 op`, then length-prefixed UTF-8 strings, 32-bit integers, doubles and
`float[]` buffers written one sample at a time. The studio embeds a matching
`HbStudio.VstBridgeClient` in its Add-Type block. Audio is passed a whole track at a time (the
engine renders a loop offline), so the pipe is not on a real-time per-block path.

The bridge is a separate download so the PowerShell studio stays about 1.9 MB. Unzip
`hbs-vst-bridge.exe` next to `Humble Baker Studio.exe`, then **Tools -> VST3 plug-ins ->
Choose plug-in...**. If the bridge is missing, the button explains how to install it and the
studio otherwise works normally. A plug-in crash terminates the bridge, not the studio; the
next render starts a fresh process.

Limitations: no plug-in editor windows yet (parameters keep their defaults/state), 64-bit only,
no VST2, and effects must be 2-in/2-out.

## What is in the early macOS app

The `.app` renders the demo or a project to WAV and lists Audio Units. It does **not** have
CoreAudio output or AU audio rendering yet. See [`MACOS.md`](MACOS.md) and
[`PARITY.md`](PARITY.md).
