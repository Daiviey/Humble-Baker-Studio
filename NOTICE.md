# Notice

Copyright (c) 2026 David Burr (Daiviey). All rights reserved except where the
license below grants otherwise.

This repository combines original software with optional third-party tools and
models. This notice explains what is MIT-licensed, what is reserved, and which
third-party terms apply.

---

## 1. Software and documentation

The source code, scripts, documentation and build files in this repository are
released under the **MIT License** in [LICENSE](LICENSE).

You may use, copy, modify, merge, publish, distribute, sublicense and sell
copies of the software, provided the copyright notice and the MIT permission
notice are included in all copies or substantial portions.

## 2. Original content and brand assets

The following are original project content and are **not** covered by the
permission to rebrand:

- the **Humble Baker** name and project identity;
- the cupcake app icon, logo and icon set (`Humble Baker Studio.ico`,
  `icon.png`, `docs/icon.png`, `docs/icon-preview.png`);
- the manual, effect/synth/settings references and other documentation text;
- the bundled instrument presets, instrument profiles, demo projects and demo
  note patterns.

You may redistribute these files **unchanged** as part of this project or a
fork, and you may modify them for personal use. If you publish a modified or
derived product, you must:

1. keep this notice and the copyright notice;
2. give clear credit to **Humble Baker Studio by David Burr (Daiviey)**;
3. replace the Humble Baker name, cupcake icon/logo and other brand assets with
   your own branding, and
4. not state or imply that your product is endorsed by, affiliated with or the
   official release of Humble Baker Studio.

The documentation text may be quoted and reused with attribution. The app
icon and logo may not be used as the icon, logo or branding of a different
product.

## 3. Third-party software and services

Humble Baker Studio is a Windows PowerShell / .NET WinForms application. It
runs on, optionally uses, or can download the following components. They are
not relicensed by this project; their own terms apply.

| Component | How it is used | License / terms |
|---|---|---|
| Windows PowerShell 5.1 | Host runtime for the `.ps1` builds | MIT (Microsoft) |
| .NET Framework 4.8, Windows Forms, System.Drawing, System.Media | UI, audio playback, image/icon handling | Microsoft .NET Framework license and redistributable terms |
| Microsoft Visual C++ Redistributable | Runtime dependency for the exe and FFmpeg | Microsoft redistributable terms |
| PS2EXE (`Invoke-ps2exe`) | Optional build tool used to produce `Humble Baker Studio.exe` | MIT (Ingo Karstein, Markus Scholtes and contributors) |
| FFmpeg | Optional, downloaded on demand for MP3/OGG/FLAC and resampling | LGPL-2.1-or-later or GPL-2.0-or-later / GPL-3.0, depending on the build. See <https://ffmpeg.org/legal.html>. The app downloads the gyan.dev "release essentials" build; that build's own license applies. |
| SuperCollider | Optional live preview and `.scd` rendering | GPL-3.0 |
| Ollama | Optional local AI model server | MIT |
| Qwen models | Optional local language models used by the AI tools | Apache-2.0 for most Qwen releases; check the specific model card |
| DeepSeek models | Optional local language models used by the AI tools | Model-specific license; check the specific model card and repository |
| ACE-Step | Optional song-generation model | Apache-2.0 (check the model card and weights license) |
| Stable Audio | Optional instrumental/song generation | Stability AI model license and terms; check the model card |
| FLUX.1 and FLUX.1 Kontext | Optional image/spectral-canvas generation | Black Forest Labs FLUX license; FLUX.1 [dev] weights are non-commercial unless separately licensed. Check the model card before commercial use. |
| Git | Optional source control and cloning | GPL-2.0 |

Links:

- PowerShell: <https://github.com/PowerShell/PowerShell>
- .NET Framework: <https://dotnet.microsoft.com/download/dotnet-framework>
- PS2EXE: <https://github.com/MScholtes/PS2EXE>
- FFmpeg: <https://ffmpeg.org/> and <https://ffmpeg.org/legal.html>
- SuperCollider: <https://supercollider.github.io/>
- Ollama: <https://github.com/ollama/ollama>
- Qwen: <https://github.com/QwenLM>
- DeepSeek: <https://github.com/deepseek-ai>
- ACE-Step: <https://github.com/ace-step/ACE-Step>
- Stable Audio: <https://stability.ai/stable-audio>
- FLUX: <https://github.com/black-forest-labs/flux>

No third-party binaries, model weights or datasets are stored in this
repository. The app can find or download them at runtime, and their licenses
apply to that copy.

## 4. Originality and influences

The synthesizer, effects, sequencing, analysis, mixing and mastering code in
this repository was written for Humble Baker Studio. It is informed by general,
publicly documented knowledge of subtractive/FM/additive synthesis, physical
modelling, DSP, mixing and mastering. No source code from commercial DAWs or
plugins is included.

Style names used in the app and documentation (for example "Aphex IDM
breakbeat", "Electro / Drexciya", "Boards of Canada", "Drukqs feel", "IDM",
"techno", "jungle") are descriptive references to genres and production
styles. They do not imply affiliation with, endorsement by, or sample clearance
from those artists, labels or rights holders. All artist and product names are
the property of their respective owners.

## 5. Redistribution checklist

If you publish a build or fork:

- include [LICENSE](LICENSE) and this `NOTICE.md`;
- keep the copyright notice;
- credit **Humble Baker Studio by David Burr (Daiviey)**;
- replace the Humble Baker name and cupcake icon/logo if you present the build
  as a different product;
- do not imply endorsement;
- comply with the licenses of any optional components or models you bundle or
  download (FFmpeg, SuperCollider, Ollama, model weights, etc.).

## 6. No warranty

The software is provided "as is", without warranty of any kind. See
[LICENSE](LICENSE). Third-party components and models carry their own
warranties and disclaimers.
