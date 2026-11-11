# Humble Baker Studio - Manual

Version 1.0.14. This manual covers every window, tab, tool, effect slot and option in the app. For the full parameter tables see:

- [Effect Reference](EFFECTS.md) - all 98 effects with every control, range and description
- [Synth Patch Reference](SYNTH.md) - every synth parameter
- [Settings Reference](SETTINGS.md) - mastering, mix rules, spectral and meta settings

---

## 1. What it is

Humble Baker Studio is a pattern-based electronic music studio:

- piano-roll sequencing with scales, drums and per-track effects
- a modular **Patch graph** where tracks, buses, groups and **all 98 effects** live as nodes
- a subtractive/FM/additive/pluck/modal synth plus sampler, morph, spectral and drum engines
- automation painting, mixing rules, mastering, MIDI and SuperCollider export
- local-AI assistants for notes, sounds, mixing, mastering and whole songs
- WAV/MP3/OGG/FLAC export

**Everything is local.** AI features use a local Ollama model; no audio or project data is uploaded.

## 2. Quick start

1. Launch `Humble Baker Studio.exe` (or run the standalone `.ps1`).
2. Press **Play**. The starter project (Drums, Acid, Pad) loops immediately.
3. Select a track on the left. Draw notes in the piano roll.
4. Click **Patch graph**, right-click empty space, and add an FX module. Drag a cable from the track to the effect, then from the effect to `out`.
5. Open **Master**, press **Analyze mix**, then **Auto master**.
6. Press **Export...** and choose a format.

## 3. Window layout

| Area | What is there |
|---|---|
| Top toolbar | transport, tempo/time signature, key/scale, grid, project and export buttons, selected-track knobs, bus buttons, mix knobs |
| Left panel | track list, track name/instrument/preset/sample, mute/solo, bus send, bus faders |
| Centre | `Piano roll` / `Patch graph` switch, the editor, and the selected-module inspector |
| Bottom tabs | Tools, AI, SuperCollider code, Log, Sound Lab, Song, Master, Spectral, Meta synth, Mix, Knobs, Drums & melody AI, Instruments, Studio agent, Composer, Producer, Automation Painting, MIDI In, Sampler, Bus / FX |
| Status bar | last action and messages |

---

## 4. Top toolbar

### Transport and project

| Control | What it does |
|---|---|
| `Play` | Renders and plays the loop (or the song when Loop is off). Uses the built-in synth or SuperCollider. |
| `Stop` | Stops preview playback. |
| `Loop` | Loop the pattern while previewing. When off, playback runs once. |
| `BPM` | Tempo, 20-400. |
| `Time sig` | Beats per bar and beat unit (`2/4/8/16` denominator). |
| `Phrase` | Phrase length in bars, used by the composer for tension/release. |
| `Bars` | Pattern length in bars. |
| `Key` | Root note for scale snap, harmony helpers and the composer. |
| `Scale` | Scale used for snap, melody writing and chord work. |
| `Snap scale` | When on, edited/generated notes snap to the selected scale. |
| `Grid` | Piano-roll edit grid: Quarter, 8th, 16th, 32nd, 64th, 8th/16th/32nd triplet. |
| `Engine` | Preview voice: `Built-in preview` or `SuperCollider` (when located). |
| `New` | New starter project. |
| `Open...` | Open a `.hbstudio.json` project. |
| `Save...` | Save the project as `.hbstudio.json` (includes notes, sounds, effects, automation, mix and master). |
| `Export...` | Open the audio export dialog. |
| `Export MIDI` | Write the pattern as a standard MIDI file. |
| `Export .scd` | Write a SuperCollider script of the pattern. |
| `Locate SuperCollider` | Point to `sclang.exe`. |
| `Get SuperCollider` | Open the SuperCollider download page. |
| `Click (metronome)` | Add the metronome to renders and built-in previews. |

### Selected-track strip

`Volume`, `Pan`, `Cutoff`, `Reso`, `Env`, `Attack`, `Release`, `Crush` knobs edit the selected track. Knob gestures are the same everywhere:

- drag up/down to change
- hold **Shift** for fine adjustment
- mouse wheel changes the value
- double-click resets to the default
- hover shows a tooltip

### Bus and mix strip

- `A B C D` send the selected track to that shared bus (click the same bus again for direct-to-master).
- `FX rack >` opens the `Bus / FX` tab.
- `Swing` sets 16th-note swing.
- `Delay mix`, `D beats`, `Feedback` set the global dotted-eighth delay.

---

## 5. Left panel: tracks and buses

| Control | What it does |
|---|---|
| Track list | Select the track to edit. Right-click a track for **Add to patch graph**. |
| `Add` | Add a track. |
| `Copy` | Duplicate the selected track. |
| `Delete` | Delete the selected track. |
| `Add to graph` | Create a graph node for the selected track. |
| `Name` | Track name (used by mix rules and the AI). |
| `Instrument` | Unified selector: built-in synths, drum kits (`Kit: ...`) and saved instruments (`Inst: ...`). |
| `Patch preset` | Apply a synth preset to the track. |
| `Sample source` | Choose a saved sample/spectral instrument for a sampler track. |
| `Mute` / `Solo` | Standard mute; solo makes every non-soloed track silent. Sidechain sources still trigger. |
| `Send selected track to` | `Direct to master`, `Bus A-D`. |
| `Buses (shared FX + fader)` | `FX on A-D` checkboxes enable each bus chain; the sliders are the bus output faders. |

### Built-in instruments

`acid`, `saw`, `square`, `tri`, `sine`, `pad`, `fm`, `pluck`, `noise`, `drums`, `patch`, `sample`.

`patch` is the full editable synth (see [SYNTH.md](SYNTH.md)). `sample` plays a loaded WAV. `drums` uses a kit.

### Drum kits

Standard, 808, 909, Acoustic, Lo-Fi, Jazz, Rock, Techno, House, Hip-Hop, Trap, DnB, Jungle, Industrial, Electro, Latin, Ambient.

Drum-note map in the piano roll: **C2 kick, D2 snare, D#2 clap, F#2 hat, A#2 open hat** (plus the other kit pieces around them).

### Buses

Four shared stereo buses. Tracks routed into a bus are summed, processed by that bus's FX chain, then sent to the master. The `Bus / FX` tab edits the bus chain and its presets.

---

## 6. Piano roll

### Drawing and editing

| Action | Result |
|---|---|
| Click empty grid | Add a note at the current grid position and last length/velocity. |
| Drag a note body | Move it (snapped to the grid). |
| Drag the right edge | Change its length. |
| Right-click a note | Delete it. |
| Right-click and drag | Erase notes under the cursor. |
| Ctrl+drag empty space | Box-select notes. |
| Shift+click a note | Add/remove it from the selection. |
| Drag in the velocity lane | Set velocity (drag across notes to change the selection). |
| Left-click ruler | Set the playhead / paste marker. |
| Right-click ruler | Clear the playhead / marker. |

### Piano-roll keyboard

| Shortcut | Result |
|---|---|
| `Ctrl+A` | Select all notes. |
| `Ctrl+C` / `Ctrl+X` / `Ctrl+V` | Copy / cut / paste at the playhead. |
| `Ctrl+D` | Duplicate the selection after itself. |
| `Delete` / `Backspace` | Delete the selection. |
| `Up` / `Down` | Transpose one semitone; **Shift** = one octave. |
| `Left` / `Right` | Move the selection one grid step. |
| `Ctrl+wheel` | Zoom horizontally around the pointer. |
| `Shift+wheel` | Scroll horizontally. |
| `Wheel` | Scroll vertically. |
| `Space` | Play / stop. |
| `Ctrl+Z` | Undo. |

---

## 7. Patch graph

Switch with **Patch graph** in the centre bar. The graph is the only place effects are added.

### Nodes

- **Track** nodes carry a track's notes and sound.
- **FX** nodes are the 98 effects.
- **Bus** nodes are gain/pan sums.
- **Group** nodes are group masters created from selected modules.
- **Master Out** is the final output.

### Working with nodes

| Action | Result |
|---|---|
| Drag a node body | Move it. |
| Drag from an output jack to an input jack | Connect audio (or a trigger input on a shared FX node). |
| Right-click empty space | Add FX module, add Bus (gain + pan), add Master Out. |
| Right-click a wire | **Insert FX module into this wire** / **Insert Bus amp into this wire** / **Disconnect this wire**. |
| Right-click a node | **Enable/Bypass module**, **Delete module**, **Wire to Master Out**, group actions, or (on track nodes) delete the track module while keeping the track. |
| Ctrl+click | Select several nodes. |
| `Group selection` | Put the selected modules under one group master. |
| Right-click a group | **Expand group** / **Condense to component node** / **Rename group** / **Ungroup (keep the bus)** / **Remove from group**. |
| `Fit view` | Frame every module. |
| `Tidy layout` | Re-arrange modules into signal-flow columns without changing cables. |
| `Delete` key | Delete the selected module. |
| `Esc` | Cancel a wire you are dragging. |

### Shared FX and sidechain

Bus, group and track FX nodes process the sum of everything routed into them. To sidechain:

1. Select the shared FX node.
2. Set **Sidechain Mix** above `0`.
3. Connect the trigger track to the **bottom-most input** (largest Y). That input drives the ducking.

### Adding effects

Right-click the graph -> **Add FX module** -> choose any of the 98 effects. The node label shows the effect. Select it to edit **only that effect's parameters** in the inspector/knobs. See [EFFECTS.md](EFFECTS.md) for every module and control.

---

## 8. Bottom tabs

### 8.1 Tools

| Control | What it does |
|---|---|
| `Transpose +1` / `-1` / `+12` / `-12` | Shift the selected notes, or the whole track. |
| `Quantize` | Snap note starts to the grid. |
| `Snap to scale` | Force pitches into the project scale. |
| `Reverse` | Reverse the note order in time. |
| `Double time` / `Half time` | Scale note timing. |
| `Select all` | Select every note on the track. |
| `Clear track` | Delete all notes. |
| `Undo` | Undo the last edit. |
| `Roll zoom +` / `-` | Zoom the piano roll. |
| `Amount` | Strength for Humanize / Mutate. |
| `Humanize` | Randomise timing and velocity slightly. |
| `Mutate` | Randomly change pitches/rhythm by Amount. |
| `Break chop` | Chop notes into rhythmic fragments. |
| `Ratchet` | Repeat notes in rolls. |
| `Rolls` | Number of repeats for Ratchet. |
| `Chance %` | Probability applied by the glitch tools. |
| Generator: `Hits` of `steps`, `rotate`, `note` + `Euclidean rhythm` | Euclidean rhythm generator. |
| `Random-walk melody` | Rule-based melody generator. |
| `Polymeter hats` | Polymetric hat pattern. |
| `Legato` / `Staccato` | Lengthen / shorten notes to touch or detach. |
| `Velocity up` / `down` / `Random velocity` | Change velocities. |
| `Accent offbeats` | Raise off-beat velocities. |
| `Invert pitches` | Mirror pitches around the track centre. |
| `Nudge later` / `Nudge earlier` | Shift timing by a small amount. |
| `Thin out` | Remove some notes. |
| `Add octave layer` | Duplicate the selection an octave up. |

### 8.2 AI

| Control | What it does |
|---|---|
| `Model` + `Refresh models` | Choose the local Ollama model (or auto). |
| Prompt | Describe style, mood, instrument or sound. |
| `Compose notes into track` | Write notes into the selected track. |
| `Vary / mutate track` | Create a variation of the current notes. |
| `Design SynthDef` | Generate a SuperCollider SynthDef (shown for use in the SC tab). |
| `Producer notes` | Ask for arrangement/mix advice. |
| Output box | The model's reply. |

### 8.3 SuperCollider code

- Large code editor for your own SynthDefs.
- `Insert acid template` adds a working example.
- `Syntax-check run (render 1 bar)` runs the code for one bar.
- Use the `Engine` selector in the toolbar to preview through SuperCollider when `sclang.exe` is located.

### 8.4 Log

Read-only log of actions, errors and export messages. Useful when something fails.

### 8.5 Sound Lab

| Control | What it does |
|---|---|
| `Design synth` / `Refine synth` | Ask the AI for a complete patch, or tweak the current one, from the prompt. |
| `Design effects` / `Refine effects` | Ask the AI for an effects chain. Effects are still created as graph nodes. |
| `Text to sample (new sampler track)` | Generate a one-shot sample from text. |
| `Bake track to sampler` | Render the selected track's current sound and notes into a sampler track. |
| `Target` | `Synth patch` or `Effects` for the design buttons. |
| `Amount` | Strength for randomize/mutate. |
| `Randomize` / `Mutate` / `Reset` | Random patch, variation, or reset. |
| `Audition note`, `seconds`, `Audition`, `Stop` | Audition the patch at a pitch/length. |
| `Save instrument` | Save the current patch as a named instrument. |
| `Save sample WAV` | Export the selected sampler track's sample. |
| `Load WAV as sampler` | Load a WAV onto a new sampler track. |
| `Reverse` / `Normalize` | Edit the sampler buffer. |
| `Sample root note` | MIDI note the sample plays at its original pitch. |
| `Gate to note length` | Cut the sample when the note ends. |
| `Sample mode` | `Pitch (key)` (transpose with notes), `One-shot` (ignore note length), `Loop`. |
| Property grids | Every synth/effect parameter with category, range and description. |

### 8.6 Song

**Patterns (scenes)** capture the notes and mute state of every track.

| Control | What it does |
|---|---|
| `Capture current` | Save the current notes/mutes as a new pattern. |
| `Update selected` | Overwrite the selected pattern. |
| `Load to editor` | Load the pattern into the piano roll. |
| `Duplicate` / `Delete` | Copy or remove a pattern. |
| Rename box | Rename the selected pattern. |

**Arrangement**

| Control | What it does |
|---|---|
| `Add pattern` + `x reps` | Append the selected pattern, repeated N times. |
| `Remove` / `Up` / `Down` | Edit the arrangement order. |
| `Auto-arrange from tracks` | Build sections from the tracks automatically. |
| `Clear` | Empty the arrangement. |
| `Play song` / `Stop` | Play the arrangement once. |
| Arrangement info | Length, sections and timing. |

**Presets**

- Browse instrument/patch presets.
- `Add as new track`, `Apply to selected track`.

**AI song arranger**

- Prompt (default: slow intro, build, hard drop, breakdown, second drop, outro).
- `AI arrange song` builds the arrangement from the prompt and pattern names.

### 8.7 Master

| Control | What it does |
|---|---|
| Master property grid | Full mastering chain: input gain, low/mid/high EQ, warmth, compressor, stereo width, bass mono, output gain, limiter, ceiling, loudness target. See [SETTINGS.md](SETTINGS.md). |
| `Analyze mix` | Measures peak, RMS, LUFS, crest, stereo correlation and six frequency bands. |
| `Auto master` | Sets the chain from the analysis and the selected target. |
| `AI mastering assistant` | Model-driven mastering advice/settings from the prompt. |
| `Target` | `Streaming -14 LUFS`, `Loud IDM -10 LUFS`, `Club -9 LUFS`, `Dynamic -18 LUFS`. |
| `Source` | `Current pattern` or `Whole song`. |
| `Preview mastered` / `Preview dry` / `Stop` / `Reset` | Audition the master chain or clear it. |
| Report box | Analysis and AI results. |
| `Format`, `Render whole song`, `Apply mastering chain`, `Export...` | Shortcut export controls. |
| `Get FFmpeg (MP3/OGG)` | Downloads/installs FFmpeg for non-WAV formats. |
| AI prompt | Ask for mastering/mixing/sidechain changes. |

### 8.8 Spectral

Paint a spectrogram and resynthesise it as a sample.

| Control | What it does |
|---|---|
| Canvas | Drag to paint. Left = start, right = end, bottom = low pitch, top = high pitch, brightness = loudness. |
| `Brush`, `Brush type`, `Amount` | Size, shape (Hard, Soft, Smooth, Streak, Gradient, Smudge, Harmonic) and strength. |
| `Erase`, `Straight line` | Erase mode and line drawing. |
| `Clear`, `Blur`, `Smear`, `Invert`, `Flip time`, `Flip pitch` | Canvas operations. |
| `Grain`, `Threshold`, `Pitch +`, `Pitch -` | Texture and pitch tools. |
| `Harmonic stack` | Base Hz, number of partials, `Add stack`. |
| `Load image` | Turn an image into a spectrogram. |
| `From selected track sound` | Analyse the selected track's rendered sound. |
| `Save instrument` | Save the painted sound as a spectral instrument. |
| `Render and play` / `Stop` / `Render to new sample track` | Audition or commit the sound. |
| Prompt + `AI: draw this picture as a spectrogram` | Ask the AI/image model to paint the canvas. |
| `Kontext refine` / `Cancel` / `Undo refine` | Image-model refinement of the canvas. |
| `Mode` | Shapes, shapes + Kontext, or photo/illustration (FLUX). |
| `Visualize Audio` + type | Spectrogram, waveform, frequency domain or magnitude spectrum view. |
| Settings grid | FFT, pitch range, brightness, duration, etc. See [SETTINGS.md](SETTINGS.md). |
| Brush palette | Seven quick brush presets. |

### 8.9 Meta synth

Four patches at the corners of a square; the pad morphs between them.

| Control | What it does |
|---|---|
| Morph pad | Click/drag to set the blend point (X/Y between A-B-C-D). |
| `Edit A/B/C/D` | Choose which corner the patch grid edits. |
| Patch grid | Edit the selected corner patch. |
| `From track synth` | Copy the selected track's patch into the slot. |
| `Randomize` / `Mutate` / `Randomize all 4` | Generate patches. |
| `Play morph point` / `Stop` | Audition the current blend. |
| `Apply morph point to track synth` | Bake the morph into the selected track. |
| `Render evolving sound to new sample track` | Render the morph over time to a sampler track. |
| `Save instrument` | Save the 4-patch morph as an instrument. |
| AI prompt + `AI: design the 4 patches` | Generate all four corners from a journey description. |
| Settings grid | Morph rate, duration, interpolation, etc. |

### 8.10 Mix

Mix rules run top to bottom on the tracks before summing and mastering.

| Control | What it does |
|---|---|
| Rule list | Select a rule; the grid edits it. |
| `Add rule` / `Duplicate` / `Remove` / `Clear all` | Manage rules. |
| Rule grid | `Enabled`, `Kind`, `Target`, `Source`, `Depth`, `AttackMs`, `ReleaseMs`, `FreqHz`, `Db`, `Q`, `Width`, `Pan`. |
| `Sidechain everything to kick` | Adds a sidechain rule from the kick/drum track to all tracks. |
| `Play mix (rules on)` / `Stop` | Audition with the rules. |
| AI prompt + `AI: mix and master` | Model can add rules, set track volumes/pans and change the master chain. |
| `Replace existing rules` | Clear the rule list before applying the AI result. |

**Rule kinds:** `sidechain`, `highpass`, `lowpass`, `peak`, `lowshelf`, `highshelf`, `gain`, `pan`, `width`, `saturate`, `monobass`.

`Target` and `Source` accept `*`/`all`, or names matched by substring or synth name, separated by commas (e.g. `bass, pad`).

### 8.11 Knobs

Choose a target and get every parameter as a rotary knob:

- Track synth patch
- Track effects
- Master chain
- Selected mix rule
- Spectral settings
- Meta morph settings
- Meta selected patch

Knob gestures: drag up/down, **Shift** = fine, wheel, double-click = reset, hover = help.

### 8.12 Drums & melody AI

**Drums**

| Control | What it does |
|---|---|
| Style list | Free description, Aphex IDM breakbeat, Drill-n-bass, Four-on-the-floor techno, Electro/Drexciya and more. |
| `Density` / `Weird` / `Bars` | How busy, how experimental, and how long the pattern is. |
| Prompt | Describe the beat in words. |
| `Replace track notes` / `Layer on top` | Write mode. |
| `On a new drum track` | Keep the selected track and create a new one. |
| `Let AI set tempo/swing` | Allow the model to change BPM and swing. |
| `Synthesize kit (AI)` | Design each drum voice instead of using a kit. |
| `Program drums` | Generate the pattern. |

**Melody**

| Control | What it does |
|---|---|
| `Role` | Lead melody, Bassline, Arpeggio, Chords/pad, Counter-melody, Hook/motif. |
| `Style` | Melodic style preset. |
| `Octave` / `Bars` | Register and phrase length. |
| Prompt | Describe the line. |
| `Repeat across loop` | Tile the phrase through the pattern. |
| `Stay in key` | Constrain to the project scale. |
| Instrument box | Default role sound, AI pick from your instruments, design synth/morph/spectral, or a saved instrument. |
| `Replace` / `Layer`, `New track` | Write mode and destination. |
| `Write melody` | Generate the line. |

**AI direction**

| Control | What it does |
|---|---|
| `Use direction` | Make all later generators follow the planned brief. |
| `Plan direction with AI` | Produce genre, feel, structure, drum approach, chords and sound ideas. |
| Editable brief box | Change the plan freely; later steps follow it. |
| `Build song from direction` | Create song sections/patterns and optional texture layers/risers from the brief. |

**Sound design**

| Control | What it does |
|---|---|
| `Design sounds for all tracks` | Give every track a matching AI-designed sound. |
| `Depth` | 1 = simple voices, 10 = layered/evolving/heavily processed. |
| `Save sounds to library` | Store the results as instruments. |
| `Texture layers + risers` | Add evolving texture beds and transitions. |

### 8.13 Instruments

Saved patches, morphs, spectral sounds and samples.

| Control | What it does |
|---|---|
| List | Select an instrument. |
| `Delete`, `Refresh`, `Open folder` | Manage the library. |
| `Name`, `Role`, `Octave`, `Root note` | Instrument metadata. Octave `0` = automatic. |
| `Gate sampler voices to note length` | Sampler playback behaviour. |
| `Your notes for the AI` | Character/usage notes used by the generators. |
| `Update details` | Save metadata changes. |
| `Selected track sound` | Save the selected track as an instrument. |
| `Meta synth morph (4 patches)` | Save the current morph. |
| `Spectral canvas` | Save the current spectral painting. |
| `Include track effects` | Bake the track's FX into the saved patch. |
| `Apply to selected track` / `New track with it` | Use the instrument. |
| `Audition` / `Stop` | Preview it. |
| `Load into editor` | Load its patch into the Sound Lab. |
| `Write melody with it` | Generate a line using it. |
| `Import library hits` | Import the bundled one-shot libraries. |
| Profile box | What the AI knows: kind, best role, register, sound description, writing advice, your notes. |

### 8.14 Studio agent

Give a goal in plain language; the agent plans and runs studio tools.

| Control | What it does |
|---|---|
| Goal box | Describe the result. |
| `Run agent` | Plan and execute. |
| `Show the plan and ask first` | Review the plan before running. |
| `Show what the AI knows` | Display the exact context sent to the model: project, instruments, manual, parameter lists, available steps. |
| Mode | Auto (full song for song goals, plan for edits), always full song, or always plan steps. |
| Output box | Plan, steps and results. |

The agent can build whole songs, plan direction, write melody/drums/chords/bass/arps, design sounds/kits/samples, use your instruments, edit notes, add tracks, save instruments, mix/master, analyze mix, run producer/quality passes and develop sounds through the arrangement.

### 8.15 Composer

**Chord lab**

| Control | What it does |
|---|---|
| Progression | Roman numerals relative to the key (`i VI III VII`) or chord names (`Cm | Ab | Bb | G`). `|` separates bars. |
| `Voicing` | open, close, spread, power, shell. |
| `Comping` | sustain, half, offbeat, pulse8, synco, stab. |
| `Bass` | rootfifth, root, driving8, octave, synco, offbeat, sub, walking, acid16, pump. |
| `Arp` | updown, up, down, broken, trance. |
| `Oct` | Chord register. |
| `Write chords` / `Write bass` / `Write arpeggio` / `Chords + bass + arp` | Generate parts. |
| `AI progression` + prompt | Model-designed progression. |
| `Detect chords from track` | Read chords from the selected track's notes. |
| `Harmonize melody` | Pull the selected melody onto chord tones. |
| `On a new track` | Destination. |
| `Reads as` | Shows the actual notes that will be written. |

**Compose a full song**

Uses the description and direction from the Drums & melody tab, then writes harmony per section, melody, drums, chords, bass, arps, sounds, arrangement, mix and master.

Options: write melody, new drum pattern per section, design sounds, mix rules and balance, auto master, fit melody to each section's chords, use the song model first.

Buttons: `Compose full song`, `Arrange from the direction`, `Mix + master`.

**Song model**

| Control | What it does |
|---|---|
| Model | `ACE-Step song (full song, lyrics)` or `Stable Audio (instrumental music)`. |
| Duration | Length in seconds. |
| `Generate with song model` | Render a reference song. |
| `Analyze audio file...` | Analyze an existing WAV instead. |
| `Cancel` | Stop generation. |
| `Split into stems` | Separate the render for analysis. |
| `Detected chords to chord + bass tracks` | Turn detected harmony into editable notes. |
| `Rebuild melody, drums and bass as editable tracks from the stems` | Recreate the performance as notes. |
| Lyrics | Blank = instrumental. |

### 8.16 Producer

**Style templates**

| Control | What it does |
|---|---|
| Style list | Techno, House, Trance, Drum and bass, IDM, Ambient, Synthwave, Lo-fi hip hop, Dubstep, Electro, Boards of Canada, Cinematic, and your saved styles. |
| `Apply style` | Set tempo, mode, grooves, comping, bass, arp, density, colour, automation and loudness target. |
| `Save project as style...` | Save the current composition plan as a reusable style. |
| `Delete style` | Delete your own styles. |
| Info box | Style details. |

**Advanced composition**

- Harmonic colour: 7ths, 9ths, secondary dominants, turnarounds (+ amount).
- Key change for the last chorus/drop (+ semitones).
- Motif lead melody (rule-based, works without AI).
- Counter-melody under the lead.
- Transitions: drum fills, pre-drop gaps, risers.
- Automation: filter sweeps, breakdown filtering, fades (+ amount).
- Producer pass after composing.
- Complexity 1-10.
- Quality pass after composing.
- Compare with a song-model render (slow).
- Sounds develop with the song.
- Producer pass checkbox.

**Producer tools**

| Control | What it does |
|---|---|
| `Analyze mix` | Render the song and show loudness per section vs planned energy, tonal balance, width and peaks. |
| `Producer pass (fix)` | Fix levels, EQ cuts, width and the energy curve, then measure again. |
| `Ask AI producer` | Model advice for the measured mix. |
| `Quality pass (listen, compare, improve)` | Score melody/beats/harmony, detect odd sounds, compare with a reference and rebuild at higher complexity if needed. |
| `Check sounds` | Render every voice and repair noisy, harsh, off-pitch, slow or clipping sounds. |
| Report box | Results. |

### 8.17 Automation Painting

Paint a parameter curve over the whole track/song.

| Control | What it does |
|---|---|
| Canvas | Horizontal = whole track/song, vertical = value, brightness = depth (Z). |
| `Target` | Track, graph Node (any FX node), or Bus. |
| `Effect module` / `Parameter` | Which parameter the lane writes. The list follows the chosen target (filter, drive, FM, LFO, FX modules, mixer). |
| `Brush type` | Hard, Soft, Smooth, Streak, Gradient, Smudge, Smear. |
| `Brush size` | Width of the stroke. |
| `Value map` | Exact shape, Smooth curve, Quantize steps, Linear ramp. |
| `Z axis` | Depth (scale value), Direct value, Off (position only). |
| `Z depth` | Strength of the Z axis. |
| `Write to Lane` | Commit the painted shape as the automation lane. |
| `Clear Canvas` | Erase the painting. |
| `Clear Lane` | Remove the written lane. |
| `Reset Track Auto` | Remove all automation on the track. |
| `Apply to Piano Roll` | Show the painted result on the roll. |
| Brush palette | Seven quick brushes. |

Lanes are saved with the project and redraw when you select the target or reload.

### 8.18 MIDI In

Record notes from a hardware MIDI controller.

| Control | What it does |
|---|---|
| Device list + `Refresh devices` | List Windows MIDI inputs. |
| `Connect` / `Disconnect` | Open/close the selected input. |
| `Start recording` / `Stop` | Record into the selected track. |
| `Clear take` | Remove every note captured by the last take. |
| `Target` | Shows the destination track. |
| `Quantize` | Off, 1/4, 1/8, 1/16, 1/32, 1/16 triplet. |
| `Monitor incoming notes...` | Audition incoming notes through the selected track. |
| Event log | Note on/off, CC and pitch bend messages. |

Notes are timed from the project BPM. Note-on starts a note, note-off closes it; held notes are flushed when you press Stop.

### 8.19 Sampler

Import, inspect, chop and export samples.

| Control | What it does |
|---|---|
| `Import WAV...` | Load a WAV (trimmed to 120 s). |
| `Load selected track sample` | Copy the selected sampler track's buffer. |
| `Save WAV...` | Save the current buffer. |
| `Send to new sampler track` | Create a sampler track from the buffer at the chosen root note. |
| Waveform | Drag to select a region; markers show slices. |
| `Play all` / `Play selection` | Audition. |
| `Select all` | Select the whole buffer. |
| `Normalize` / `Reverse` / `Trim silence` | Buffer edits. |
| `Root note` | MIDI note used when creating tracks. |
| `Sensitivity` / `Min slice ms` / `Auto-slice at onsets` | Onset slicing. |
| `Parts` / `Chop into N parts` | Equal slicing. |
| `Add selection as slice` | Add the waveform selection. |
| `Slices to tracks` | Create one sampler track per slice (up to 64). |
| Slice list | Select a slice. |
| `Audition slice` / double-click | Preview the slice. |
| `Export slices...` | Write every slice as a WAV in a folder. |
| `Clear slices` | Remove slice markers. |

### 8.20 Bus / FX

| Control | What it does |
|---|---|
| `Edit bus A-D` | Choose the bus to edit. |
| `Bus A-D enabled` | Enable/disable the bus chain. |
| `Assign tracks to a bus` | Select a track, then use `Selected track >` or `All tracks >` buttons for None/A-D. |
| `Presets` (Space, Lo-fi, Drive, Dub, Clear bus) | Quick bus chains. These set effect modules on the bus. |
| Bus knobs | Full parameter view for the selected bus FX chain. |
| Modules | Effect modules are added in the Patch graph, not here. |

---

## 9. Export

`Export...` opens the audio export dialog.

| Option | Values |
|---|---|
| Source | `Current pattern` (loops seamlessly) or `Whole song` (arrangement). |
| Loop count | 1-64 repeats of the pattern. |
| Tempo override | 0 = project tempo, otherwise 1-300 BPM. |
| Include metronome click | On/off. |
| Apply mastering chain | On/off. |
| Normalize peak | On/off + target dBFS (-24 to 0). |
| Fade in / Fade out | 0-10000 ms. |
| Format | `WAV 16-bit`, `MP3 320 kbps`, `MP3 192 kbps`, `MP3 V0 (VBR)`, `OGG Vorbis q6`, `OGG Vorbis q9`, `FLAC lossless`. |
| Sample rate | 44100, 48000, 96000 Hz. MP3 is limited to 48 kHz. |
| Channels | Stereo or mono. |

Notes:

- WAV at 44.1 kHz stereo is written directly.
- Other formats/rates require FFmpeg (`Get FFmpeg` in the Master tab).
- The export log is `%TEMP%\hb-export-debug.log`.
- Renders go to the studio `renders` folder by default.

---

## 10. Files and folders

The studio folder is `HumbleBakerAudio\studio` under the app's Audio directory, or `%LOCALAPPDATA%\HumbleBakerAudio\studio` when running standalone. It contains:

- `projects` - saved `.hbstudio.json` projects
- `renders` - exported audio
- `instruments` - saved instruments (JSON + WAV references)
- `samples` - optional sample roots
- `ffmpeg` - local `ffmpeg.exe`

Project JSON stores tracks, notes, synth patches, effects, samples (base64), automation, patterns, arrangement, mix rules, master settings and SuperCollider code.

Environment:

- `HUMBLEBAKER_SAMPLES` - extra sample root folder.
- FFmpeg is also found on `PATH`.

---

## 11. Keyboard shortcuts

| Shortcut | Where | Result |
|---|---|---|
| `Space` | Roll / Studio | Play / stop. |
| `Ctrl+Z` | Studio (not in text boxes) | Undo. |
| `Ctrl+A/C/X/V/D` | Piano roll | Select all / copy / cut / paste / duplicate. |
| `Delete` / `Backspace` | Piano roll | Delete selection. |
| `Up/Down`, `Shift+Up/Down` | Piano roll | Transpose semitone / octave. |
| `Left/Right` | Piano roll | Move by grid. |
| `Ctrl+wheel` | Piano roll | Zoom. |
| `Shift+wheel` | Piano roll | Horizontal scroll. |
| `Ctrl+click` | Patch graph | Multi-select modules. |
| `Delete` | Patch graph | Delete selected module. |
| `Esc` | Patch graph | Cancel wire. |
| Drag / Shift-drag / wheel / double-click | Knobs | Change / fine / change / reset. |

---

## 12. AI features and models

All AI is local through Ollama:

- model list is read from the local Ollama install; `(auto: best general model)` picks a general model
- a model may need to load on first use; the status line will say so
- the Studio agent, AI synth/FX design, melody/drums, mixing/mastering, composer, producer and song-model helpers all share the same local model
- the song model (ACE-Step / Stable Audio) is used only when you press a song-model button

If an AI action fails:

1. Check `Log` for the error.
2. Confirm Ollama is running and a model is installed.
3. Try a smaller model or wait for the current one to load.
4. Rules-based tools (Euclidean, humanize, chord lab, style templates) work without AI.

---

## 13. Troubleshooting

| Problem | Fix |
|---|---|
| No sound | Press `Play`, check track volume/mute/solo, and check the track is connected to `out` in the Patch graph. |
| An effect does nothing | Select its graph node; confirm the wet/mix control is above 0 and audio is wired through it. |
| Sidechain does not duck | Set the shared FX node's `Sidechain Mix > 0` and connect the trigger to its bottom-most input. |
| MP3/OGG export fails | Install FFmpeg with `Get FFmpeg (MP3/OGG)`; WAV 16-bit at 44.1 kHz always works. |
| SuperCollider preview fails | Use `Locate SuperCollider`, or switch `Engine` back to `Built-in preview`. |
| MIDI controller missing | Plug it in, press `Refresh devices`, then `Connect`. Close other apps that may hold the port. |
| MIDI notes are late/early | Use `Quantize` in the MIDI tab, or lower the buffer/latency on the controller. |
| Sample sounds wrong pitch | Set `Sample root note` to the note at which the sample plays untransposed. |
| Project will not open | Check the JSON is intact and was saved by the Studio; the `Log` shows the parse error. |
| Old UI after an update | Close every Studio window and relaunch the rebuilt exe. |

---

## 14. Reference index

- [Effect Reference](EFFECTS.md) - 98 modules, all controls and ranges
- [Synth Patch Reference](SYNTH.md) - 89 patch parameters
- [Settings Reference](SETTINGS.md) - mastering, mix rules, spectral, meta synth
- [README](../README.md) - install requirements and quick feature list
- [CHANGELOG](../CHANGELOG.md) - version history
- [LICENSE](../LICENSE) - MIT software license
- [NOTICE](../NOTICE.md) - brand/content terms and third-party attributions
