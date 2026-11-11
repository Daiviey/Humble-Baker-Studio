# Changelog

## 1.0.0 — 2026-11-10

Initial standalone release of Humble Baker Studio.

### Studio
- Piano roll with scales, snap, velocity and per-track parts.
- Synth engine: subtractive, FM, additive, wavetable, vowel, pluck, bell, noise.
- Drum kits, sample tracks, mixer, buses and master chain.
- Patch graph with track/FX/bus/group nodes and shared FX processing.
- MIDI export, SuperCollider export and live preview support.

### Automation painting
- Canvas spans the whole track/song.
- Vertical position sets the value; brightness is a real Z axis (depth/value).
- Targets: track, graph node (shared FX), or bus.
- Saved lanes redraw when selecting the target or reloading the project.

### Export
- Current pattern with 1–64 seamless loops, or the whole arrangement.
- Tempo override and optional metronome click.
- Mastering chain, peak normalization and fade in/out.
- WAV 16/24/32-float, MP3, OGG Vorbis, Opus, AAC/M4A, ALAC, FLAC and AIFF.
- 44.1 / 48 / 96 kHz, stereo or mono, with FFmpeg-assisted encoding.

### Packaging
- Canonical implementation, launcher and single-file standalone script.
- Portable Windows executable built with ps2exe.
