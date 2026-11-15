# Humble Baker Studio - C# port (No-AI edition)

**Version 0.6.0** - Windows 10/11 x64 - self-contained, no .NET install required.

This is the deterministic edition of the C# port. It is the same app as the standard C# build,
without the generative AI tab, toolbar button, AI menu entries or AI design buttons in the
spectral/meta editors. The **Patterns** tab replaces them with a curated library of
pre-programmed MIDI examples.

The stable PowerShell studio is `../Humble Baker Studio.exe` (about 1.9 MB). This No-AI exe is
about 46.8 MB.

## Run

Double-click **`Humble Baker Studio (No AI).exe`**. Windows SmartScreen may warn because the
build is unsigned; choose *More info -> Run anyway*.

## What is new in 0.6.0

- **Live deck speed and pitch**: separate SPEED and PITCH faders plus a KEY LOCK switch and RESET.
- **Edit both decks while mixing**: the EDIT button opens an independent project window per deck.
- **Sustained sampler and instrument emulator**: sustain loops plus `strings`, `bowed`, `cello`,
  `pluck`, `strum` and `hybrid` engines that hold a note.
- Everything above is deterministic; no model, network or external service is used.

## Patterns instead of AI

Choose:

- role: drums, bassline, chords, lead, arpeggio or counter-melody
- style and mood
- bars, energy, complexity and density
- a seed for reproducible selection and humanisation

The composer picks a matching pre-written example, transposes it to the project key, snaps it to
the project scale, fits it to the requested bars, thins notes by density, scales velocity by
energy and applies seeded humanisation. No model, network or external service is used.

`Preview` shows the chosen example and its notes; `Write to selected track` commits them (with
an optional replace).

## Build it yourself

```powershell
.\build.ps1 -NoAi
# dist-noai\Humble Baker Studio (No AI).exe
```

The standard build (`.\build.ps1`) keeps the local-AI tools. Both come from the same codebase;
the No-AI build defines `HBS_NO_AI`.

## Downloads

- Release page: https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.6.0
- SHA-256: `eb61682bff27ecc4e9d4efaa44aad0f0bce9eeaca02049f376245a1815ec3dc3`

## Notes

- Optional: FFmpeg for MP3/OGG/FLAC export, sample packs for the sampler.
- Headphone cueing in Live mode needs a second Windows audio output.
- The SuperCollider live session/OSC bridge is PowerShell-only; the C# port has `.scd` export.
- The FLUX photo/illustration mode in the spectral painter remains PowerShell-only.
- Alpha software: keep backups of project files.
