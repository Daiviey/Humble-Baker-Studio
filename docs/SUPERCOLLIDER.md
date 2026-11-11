# SuperCollider in Humble Baker Studio

A practical, in-depth guide to using SuperCollider (SC) with Humble Baker Studio: how the
integration works, how the generated `.scd` file is structured, how to write SynthDefs that the
sequencer can play, how patterns and buses are used, and how to debug the result.

This guide is written against the SuperCollider **3.14.1** documentation set and the Studio's
own SuperCollider bridge. Every external technical claim is cited; the numbered sources are at
the end. App-specific behaviour is cited as `[app]`, which points to
`AudioStudioImplementation.ps1` in this repository.

> SuperCollider is a separate, optional program. The Studio's built-in preview and renderer
> work without it [app]. This guide is for users who want SC's synthesis engine, its pattern
> language or the exported `.scd` script.

## Contents

1. [The 60-second version](#1-the-60-second-version)
2. [What SuperCollider is](#2-what-supercollider-is)
3. [How the Studio uses SuperCollider](#3-how-the-studio-uses-supercollider)
4. [Installing and configuring SC](#4-installing-and-configuring-sc)
5. [The SuperCollider code tab](#5-the-supercollider-code-tab)
6. [SynthDefs the sequencer can play](#6-synthdefs-the-sequencer-can-play)
7. [Patterns and sequencing](#7-patterns-and-sequencing)
8. [Buses, routing and order of execution](#8-buses-routing-and-order-of-execution)
9. [Live coding, routines and tasks](#9-live-coding-routines-and-tasks)
10. [Rendering and exporting](#10-rendering-and-exporting)
11. [Extending with MIDI, OSC and packages](#11-extending-with-midi-osc-and-packages)
12. [Debugging checklist](#12-debugging-checklist)
13. [A complete working example](#13-a-complete-working-example)
14. [Live bridge, graph compiler and native effects](#14-live-bridge-graph-compiler-and-native-effects)
15. [Sources and citations](#15-sources-and-citations)

---

## 1. The 60-second version

1. Install SuperCollider from the official downloads page [21].
2. In the Studio, click **Locate SuperCollider** and choose `sclang.exe`.
3. Set the toolbar **Engine** to `SuperCollider`.
4. Press **Play**. The Studio writes `live.scd` into its studio folder and starts
   `sclang -D live.scd` [app].
5. Put custom `SynthDef`s in the **SuperCollider code** tab. They are inserted into the
   generated script before the sequencer starts [app].
6. For a one-off render, use **Export .scd** to save an editable script, or the
   **Syntax-check run (render 1 bar)** button to run the current project through SC [app].

If that works, the rest of this document explains why it works and how to extend it.

---

## 2. What SuperCollider is

SuperCollider is a cross-platform environment for audio synthesis and algorithmic composition
[1]. It has three main components [1][13]:

| Component | Role |
|---|---|
| `scsynth` | The real-time audio engine, implemented as a server |
| `sclang` | The language interpreter and client that controls the server |
| ScIDE | The SuperCollider editor with integrated help |

The client and server communicate with Open Sound Control (OSC) [1][13][16]. `sclang` can run
on the same machine as `scsynth` or connect to a server over a network [1][13]. The server knows
nothing about the SuperCollider language; it receives synth definitions and node messages [3][4].

SuperCollider is free software under the GNU General Public License [1][22]. Its help files are
published under CC BY-SA 3.0 [22]. Keep those licences in mind if you copy SC documentation text
or redistribute SC itself. The source repository is on GitHub [23].

---

## 3. How the Studio uses SuperCollider

### 3.1 Two engines

The toolbar **Engine** selector chooses between:

- **Built-in preview**: the Studio synthesises and renders the project itself.
- **SuperCollider**: the Studio generates a `.scd` script and launches `sclang`, which boots
  `scsynth` and plays the script [app].

`SuperCollider` is only available after the Studio has found `sclang.exe`. The label next to the
toolbar buttons shows whether it was found [app].

### 3.2 Finding `sclang.exe`

`Find-StudioSuperCollider` checks, in order [app]:

1. the `sclang` path saved in `<studio folder>\settings.json`;
2. `sclang.exe` on `PATH`;
3. `SuperCollider*` folders under `Program Files`, `Program Files (x86)` and
   `%LOCALAPPDATA%`;
4. a `Tools` folder beside the Studio and the app's tools directory.

**Locate SuperCollider** opens a file picker and saves the chosen path to
`settings.json` [app]. If SC is installed in a non-standard place, use that button.

The studio folder is the app's `HumbleBakerAudio\studio` folder (or
`%LOCALAPPDATA%\HumbleBakerAudio\studio` in a standalone run). The generated files live there:

- `live.scd` - the live script
- `live.stop` - the stop flag watched by the running script
- `render.scd` - the render script
- `sclang.log`, `sclang.err.log` - stdout/stderr from `sclang`
- `settings.json` - the located `sclang` path

[app]

### 3.3 The `-D` launch

The Studio starts SC with `sclang -D "<script>"` [app]. In the official SC command-line parser,
`-D` is the short form of `--daemon` ("Enter daemon mode"), and a positional input file is
executed at startup; a non-empty input file also puts the process into daemon mode [24]. The
script is executed with `executeFile` [25]. That is why the Studio can write a complete `.scd`,
start it once, and keep it running until the script exits or the stop flag is seen.

The Studio runs `sclang` hidden, redirects its output to `sclang.log` and `sclang.err.log`, and
watches for the process to exit [app]. If SC exits unexpectedly, the Studio logs it and stops
the transport [app].

### 3.4 Live mode and the stop flag

In live mode the generated script starts the pattern with `Pn(..., inf).play(~hbclock, quant: 0)`
and forks a loop that checks `live.stop` every 0.25 seconds; when the file exists it quits the
server and exits [app]. Pressing **Stop** in the Studio writes that flag and, if necessary,
terminates the process [app]. This is a cooperative stop, not an SC language command.

### 3.5 Render mode

For a render, the script calls `s.record(path, numChannels: 2)`, waits for the loop plus two
seconds, calls `s.stopRecording`, waits briefly, quits the server and exits [app]. The Studio
waits up to the loop length plus 40 seconds for the file [app]. See the official
`Server` documentation for `record`/`stopRecording`, and the NRT guide for offline rendering
alternatives [12][18].

### 3.6 Per-project sessions and recompile

The Studio now gives each project its own SC session folder under its data directory:

```text
<studio>\sc\
    library\classes\HumbleBakerStable.sc
    sessions\<project-id>\
        classes\
        data\
        session.json
        recompile.scd
```

The project id is saved in the project JSON. The stable classes are `HBProject`, `HBNode`,
`HBTrack`, `HBSection` and `HBEffect` [app].

The SC tab has three controls [app]:

- **Recompile SC definitions** stops transport, writes the class library and session files,
  launches `sclang` with `--include-path` for the library and session class folders, compiles
  `recompile.scd`, and leaves transport stopped.
- **Check SC setup** reports the `sclang` path, version, sc3-plugins status and the current
  session folder.
- **Open session folder** opens the project session in Explorer.

Class compilation is explicit: the Studio does not silently recompile while the user is
working. The persistent OSC project/graph layer and the native effect library are the next
integration stages; this release establishes the session, class and recompile foundation.

### 3.7 What the generated script contains

`Get-StudioScd` builds the script in this order [app]:

1. `s.options.numOutputBusChannels = 2;` [app]
2. `s.waitForBoot({ ... })` [12]
3. `~hbbus = Bus.audio(s, 2);` - the private stereo mix bus [app][7]
4. the built-in SynthDefs (`hbacid`, `hbsaw`, `hbsquare`, `hbtri`, `hbsine`, `hbfm`, `hbpad`,
   `hbnoise`, `hbpluck`, drum voices, `hbfx`) [app]
5. your **SuperCollider code** tab contents [app]
6. `s.sync;` - a server barrier [12]
7. per-track FX SynthDefs and instances, then the global `hbfx` delay [app]
8. `~hbclock = TempoClock(bpm / 60);` [11]
9. a `Ppar` of one `Pbind` per track [8][9], played with `Pn(..., inf)` [8]
10. either the render recording block or the live stop-file loop [app]

A simplified live script looks like this (the real one also includes the built-in defs and your
custom code):

```supercollider
s.options.numOutputBusChannels = 2;
s.waitForBoot({
    ~hbbus = Bus.audio(s, 2);

    // built-in SynthDefs and your custom code here

    s.sync;

    ~hbclock = TempoClock(140 / 60);

    Pn(Ppar([
        Pbind(
            \instrument, \hbacid,
            \freq, Pseq([55, 55, 65.41, 73.42], 1),
            \dur, Pseq([0.25, 0.25, 0.5, 0.5], 1),
            \sustain, Pseq([0.2, 0.2, 0.4, 0.4], 1),
            \amp, Pseq([0.3, 0.25, 0.3, 0.3], 1),
            \out, ~hbbus,
            \pan, 0,
            \cutoff, 2400,
            \res, 0.3
        )
    ]), inf).play(~hbclock, quant: 0);

    fork {
        loop {
            0.25.wait;
            if(File.exists("live.stop")) { s.quit; 0.3.wait; 0.exit }
        }
    };
});
```

That is the shape of the file; the Studio builds the note lists and values from your project
[app].

---

## 4. Installing and configuring SC

### 4.1 Install

Download the current Windows build from the official site [21]. The Studio's **Get
SuperCollider** button opens the same downloads page [app]. Run the installer and note the
installation folder (usually under `Program Files`).

### 4.2 Locate and test

In the Studio, click **Locate SuperCollider** and select `sclang.exe` [app]. The toolbar label
should show the SC folder.

A minimal SC test, in ScIDE or in the Studio's code tab, is:

```supercollider
s.boot;
{ SinOsc.ar(440, 0, 0.1) }.play;
```

`Function: -play` is a convenience that creates a SynthDef behind the scenes, sends it to the
server and starts a synth [1][3]. `s.boot` boots the server. If you hear nothing, check the
audio device first.

### 4.3 Audio device selection

On Windows the default server uses WASAPI and the system default device; you can select an
input/output device and API through `ServerOptions` [17][30], and both sides must use the same
API and sample rate [17]. For example:

```supercollider
o = Server.default.options;
o.inDevice_("Windows WASAPI : Microphone");
o.outDevice_("Windows WASAPI : Speakers");
Server.default.reboot;
```

`ServerOptions.devices`, `inDevices` and `outDevices` list the available devices without booting
[17]. If you use ASIO, the driver usually provides both input and output [17].

The Studio sets only `numOutputBusChannels = 2` in its generated script [app]. If you need a
different device, sample rate or channel count, add the appropriate `Server.default.options...`
lines to the **SuperCollider code** tab *before* `s.waitForBoot` runs. The code tab is inserted
inside the boot block, so it is usually easier to make a local copy of the exported `.scd` and
edit it in ScIDE for device experiments.

---

## 5. The SuperCollider code tab

The **SuperCollider code** tab is a plain text editor for your own SC code [app].

- `Insert acid template` appends a working `SynthDef(\hello, ...)` example [app].
- `Syntax-check run (render 1 bar)` actually switches the engine to SuperCollider and performs a
  real render of the project [app]. It is a smoke test of the whole SC path, not only a parser
  check.
- The code is inserted after the built-in SynthDefs and `~hbbus`, and before `s.sync` and the
  sequencer [app].

That insertion point matters. It means:

- your `SynthDef(...).add` definitions are available to the patterns that follow;
- `~hbbus` already exists;
- if you want to start your own pattern or synth, wait for the server barrier. A safe pattern is:

```supercollider
SynthDef(\myVoice, { |out = 0, freq = 220, amp = 0.2, pan = 0, sustain = 0.4|
    var env = EnvGen.ar(Env.linen(0.005, sustain, 0.2, 1, -4), doneAction: 2);
    Out.ar(out, Pan2.ar(SinOsc.ar(freq) * env * amp, pan));
}).add;

fork {
    s.sync;                       // wait until the def is on the server
    Synth(\myVoice, [\out, ~hbbus, \freq, 330]);
};
```

Keep the code tab for definitions and short experiments. The Studio regenerates `live.scd` from
the project and the code tab each time playback starts [app].

---

## 6. SynthDefs the sequencer can play

### 6.1 The SynthDef model

A `SynthDef` is the client-side description of a server-side synth definition. It contains a
UGen graph function and named controls; the server uses the compiled definition as a template
for any number of `Synth` nodes [4]. `Synth` is the client-side handle to one running node [5].
The UGen graph function is evaluated **once**, when the SynthDef is built; language-side
statements inside it do not run again for each synth [3][4]. Use UGens such as `Rand` when you
want a different value per synth [3][4].

A conventional definition looks like this [3][4]:

```supercollider
SynthDef(\tutorialSine, { |out = 0, freq = 440, amp = 0.2|
    Out.ar(out, SinOsc.ar(freq, 0, amp));
}).add;
```

`Out.ar` writes to an audio bus; the first output bus is usually channel 0 [3][6]. `.add` sends
the definition to the server and registers it in the client-side synth description library;
`.store` also writes a definition file [4][29]. In the Studio, use `.add`.

### 6.2 Names and controls used by the Studio

The Studio's SC exporter names track instruments like this [app]:

| Track | SynthDef name |
|---|---|
| Built-in synth `acid`, `saw`, `square`, `tri`, `sine`, `fm`, `pad`, `noise` | `hb` + synth name (`hbacid`, `hbsaw`, ...) |
| Drum track | `hbkick`, `hbsnare`, `hbclap`, `hbhat`, `hbtom`, `hbperc` by GM pitch |
| Patch track | `hbp<track index>` (generated from the Patch) |
| Sampler track | `hbsamp` or `hbsampg` (gated) |
| Per-track FX | `hbfx<track index>` |
| Global delay | `hbfx` |

The generated `Pbind` sends these controls to the synth [app]:

`instrument`, `freq`, `dur`, `sustain`, `amp`, `out`, `pan`, `cutoff`, `res`, `envamt`, `atk`,
`rel`, `crush`.

Sampler tracks also send `buf` and `rootfreq` [app]. Your custom SynthDef can ignore any of these,
but the ones you want the sequencer to control should be declared as arguments with those exact
names. This mirrors the control-name conventions described in the `Pbind` documentation [9].

A custom voice that accepts the common controls:

```supercollider
SynthDef(\hbTutorial, { |out = 0, freq = 220, amp = 0.2, pan = 0, sustain = 0.4,
                         cutoff = 1800, res = 0.4, envamt = 1, atk = 0.005, rel = 0.25, crush = 16|
    var env = EnvGen.ar(Env.linen(atk, sustain, rel, 1, -4), doneAction: 2);
    var fenv = EnvGen.ar(Env.perc(0.001, 0.25, 1, -4));
    var sig = Saw.ar(freq) + Pulse.ar(freq * 1.005, 0.3);
    var steps = 2 ** (crush.clip(2, 16) - 1);
    sig = RLPF.ar(sig, (cutoff * (1 + (fenv * envamt * 4))).clip(40, 16000), (1 - res).clip(0.05, 1));
    sig = (sig * steps).round / steps;
    Out.ar(out, Pan2.ar(sig.tanh * env * amp, pan));
}).add;
```

`EnvGen` plays an `Env`; `doneAction: 2` frees the synth at the end [26]. `RLPF` is a resonant
low-pass filter; `Pulse` and `Saw` are oscillators. The `crush` calculation quantises the signal
to a reduced number of steps, matching the Studio's crush parameter. The official *Tour of
UGens* is a good next stop when choosing oscillators, filters and envelopes [35].

### 6.3 Assigning a custom SynthDef to a track

The Studio's instrument dropdown lists the built-in synths, drum kits and saved instruments
[app]. A custom SC SynthDef name is not added to that dropdown automatically. The supported
paths are:

- use **Design SynthDef** on the AI tab, which adds the generated definition to the code tab and
  assigns its name to the selected track [app];
- or save the project, edit the track's `"synth"` field to your SynthDef name, and reopen it
  [app].

If you are editing a standalone exported `.scd` outside the Studio, you can of course use the
SynthDef directly in a `Pbind` [9].

### 6.4 Samples and buffers

Sampler tracks are rendered to a WAV in the studio folder and loaded with
`Buffer.read(s, path)`; the generated definition uses `PlayBuf` and `BufRateScale` [app][7].
The buffer is read before the patterns start. If you write your own sample player, allocate the
buffer with `Buffer.read` and pass its index or `Buffer` object as an argument [7].

A minimal sample voice:

```supercollider
SynthDef(\hbMySampler, { |out = 0, buf = 0, freq = 440, rootfreq = 261.63,
                          amp = 0.3, pan = 0, sustain = 0.5, rel = 0.1|
    var rate = BufRateScale.kr(buf) * (freq / rootfreq);
    var sig = PlayBuf.ar(1, buf, rate, doneAction: 2);
    sig = sig * EnvGen.ar(Env([1, 1, 0], [sustain, rel]), doneAction: 2);
    Out.ar(out, Pan2.ar(sig * amp, pan));
}).add;
```

### 6.5 Effects in the generated script

The SC exporter does **not** translate the 113 Patch-graph effect modules. It generates a
SynthDef from the legacy track `Fx` fields, and only for the subset implemented by
`Get-StudioFxSynthDef` [app]: filter, wavefolder, drive, downsampling, bit crush, ring
modulation, chorus, delay, reverb and output gain. The global `hbfx` is a stereo delay [app].

This is an important limitation: if your project relies on the Patch graph, Ladder filter,
Vocoder, Opto compressor, or any of the other graph effects, the exported `.scd` will not
reproduce that processing. Use the built-in renderer for those projects, or recreate the
processing in SC with SynthDefs and buses.

> **Known exporter issue:** the current exporter emits
> `Synth(..., addToTail: true)` when it creates an FX synth. The documented `Synth.new`
> signature takes `addAction:` (for example `\addToTail`) and provides the `Synth.tail`
> convenience method [5]. If an exported script fails at that line, replace it with
> `Synth.tail(s, \hbfxN, [...])` or `Synth(..., s, \addToTail)`. The FX definitions themselves
> are valid.

If you want to build an SC effect chain, the standard pattern is: write the sources to a private
audio bus, then instantiate the effect synth with `Synth.tail` (or `addAction: \addToTail`) so it runs after the sources
[6][14][5]. The order-of-execution guide is essential reading for this [14].

---

## 7. Patterns and sequencing

### 7.1 Pbind

`Pbind` combines value patterns into an event stream; each event is played by the default event
type, which creates a synth for the given `\instrument` and passes the remaining keys as
arguments [8][9]. The basic keys are `\instrument`, `\dur`, `\amp`, `\freq`/`\midinote`,
`\sustain`/`\legato`, `\pan` and `\out` [9].

```supercollider
Pbind(
    \instrument, \hbTutorial,
    \freq, Pseq([220, 261.63, 329.63, 392], inf),
    \dur, Pseq([0.25, 0.25, 0.5, 0.5], inf),
    \sustain, Pseq([0.15, 0.15, 0.3, 0.4], inf),
    \amp, 0.2,
    \out, ~hbbus
).play(~hbclock);
```

`Pseq` returns the values in order; `Prand`, `Pxrand` and `Pwrand` choose randomly; `Pseries` and
`Pgeom` generate series; `Pwhite` and `Pexprand` generate random values [8]. The full pattern
system is described in the Pattern Guide [10].

### 7.2 Ppar, Pn and rests

The Studio wraps one `Pbind` per unmuted track in `Ppar`, and plays the result with
`Pn(..., inf)` [app]. `Ppar` runs patterns in parallel; `Pn` repeats a pattern [8]. The exporter
uses `\rest` for a leading gap before the first note [app]. `Pbind` also supports rests through
the `Rest` class [9].

### 7.3 TempoClock

The generated clock is `TempoClock(bpm / 60)`, so a tempo of 140 BPM becomes `140/60` beats per
second [app][11]. `TempoClock` schedules relative to a tempo in beats per second; `play` with
`quant` starts on a grid [11]. The exporter plays with `quant: 0` [app].

To change tempo in a custom script, set `~hbclock.tempo = newBpm / 60` [11]. To start a pattern
on the next bar, use `play(~hbclock, quant: 4)` for a 4-beat bar (or the meter you use) [11].

### 7.4 Using your own patterns in the code tab

Because the code tab runs before the Studio's `s.sync`, wait for the server before starting
your own pattern:

```supercollider
fork {
    s.sync;
    Pbind(
        \instrument, \hbTutorial,
        \freq, Pseq([220, 330, 440], inf),
        \dur, 0.25,
        \amp, 0.15,
        \out, ~hbbus
    ).play(~hbclock, quant: 0);
};
```

The Studio's own patterns will still play too. If you want only your pattern, create the project
with no unmuted tracks or use a separate exported script.

---

## 8. Buses, routing and order of execution

A bus is a numbered signal path on the server; audio buses carry audio-rate signals and control
buses carry control-rate signals [6][7]. The Studio creates `~hbbus` as a private stereo audio
bus and sends track output there [app]. It also creates `~hbt<index>` buses for per-track FX
chains, and instantiates the FX synth so it processes the sources [app].

The rules that matter:

- `Out.ar(bus, signal)` writes to a bus; multiple synths writing to the same bus are summed [6].
- `In.ar(bus, channels)` reads from a bus [6].
- `Synth` and `Group` are both `Node` subclasses; nodes are the server-side objects that run
  synths and groups [27][28].
- A synth that reads a bus must run **after** the synth that writes it; `Synth.after`,
  `Synth.before`, `Synth.head`, `Synth.tail` and the `addAction` argument place nodes in the
  server's execution order [5][14].
- `Bus.audio(s, 2)` allocates a private bus and reserves its indices; `Bus.free` releases them
  [7].
- Do not allocate buses inside a SynthDef [7].

A small effects chain that follows the Studio's routing:

```supercollider
~fxBus = Bus.audio(s, 2);

SynthDef(\hbReverb, { |in = 0, out = 0, mix = 0.25, room = 0.8, damp = 0.3|
    var dry = In.ar(in, 2);
    Out.ar(out, dry + FreeVerb2.ar(dry[0], dry[1], mix, room, damp));
}).add;

s.sync;

Synth.tail(s, \hbReverb, [\in, ~fxBus, \out, 0]);
```

Then send a source's `\out` to `~fxBus` instead of the master bus. Because the reverb is added to
the tail after the sources are created, it runs later [5][14].

---

## 9. Live coding, routines and tasks

`fork { ... }` starts a Routine on a clock; `wait` yields for a number of beats [2][11]. This is
what the generated live script uses to poll the stop file [app]. SC also has `Task`, `Routine`,
`Pdef`, `Ndef` and `Tdef` for live coding [1][8].

If you want to use `Ndef` or `Pdef`, add the definitions in the code tab and start them after
`s.sync`:

```supercollider
fork {
    s.sync;
    Ndef(\drone, { |freq = 110, amp = 0.08|
        SinOsc.ar([freq, freq * 1.005], 0, amp)
    }).play;
};
```

Remember that the Studio regenerates the script and restarts SC when you press Play again
[app]. Changes made only by typing into a running SC session are lost unless they are also in
the code tab.

---

## 10. Rendering and exporting

### 10.1 The render button

**Syntax-check run (render 1 bar)** switches the engine to SC and calls the Studio's render
path [app]. The render script records two channels with `s.record`, waits for the loop plus two
seconds, stops recording, quits the server and exits [app]. The file is written to the studio
`renders` folder [app].

### 10.2 Export `.scd`

**Export .scd** writes a complete script containing the built-in definitions, your code tab,
the track patterns and the routing [app]. Open it in ScIDE to edit or run it independently.
The SC IDE evaluates a region or line with `Ctrl+Return` (Windows/Linux) or `Cmd+Return`
(macOS), and `Ctrl+.`/`Cmd+.` stops the server and scheduled code [36]. The exact shortcut is
shown in the IDE's menus.

### 10.3 Offline/NRT rendering

The Studio's render path is real-time: it records while the server plays [app]. SC also supports
non-real-time synthesis, where the server renders a score offline and writes a file [18]. That
is the better choice for long or CPU-heavy pieces. The NRT guide explains `scsynth -N` and the
score/OSC file format [18].

### 10.4 Sample rate and channels

The generated script asks for two output bus channels [app]. If you export at another sample
rate, set the server options before boot and make sure the audio device and the requested rate
agree [17]. For offline rendering the `Server` and NRT documentation cover the server options
[12][18].

---

## 11. Extending with MIDI, OSC and packages

### 11.1 MIDI

SC's MIDI support is available through `MIDIClient`, `MIDIFunc`, `MIDIdef` and `MIDIOut` [15].
On Windows it uses PortMIDI [15]. A minimal keyboard example from the official guide:

```supercollider
s.boot;
MIDIClient.init;
MIDIIn.connectAll;

~notes = Array.newClear(128);

~on = MIDIFunc.noteOn({ |vel, num|
    ~notes[num] = Synth(\default, [\freq, num.midicps, \amp, vel * 0.00315]);
});

~off = MIDIFunc.noteOff({ |vel, num|
    ~notes[num].release;
});
```

`MIDIFunc` can filter by device, channel and message number; the `srcID` is the UID from
`MIDIClient.sources` [15]. The Studio also has its own **MIDI In** tab, which records controller
notes into the piano roll [app]; that is separate from SC's MIDI responders.

### 11.2 OSC

SC sends and receives OSC through `NetAddr` and `OSCFunc` [16]. The default language port is
`NetAddr.langPort` (usually 57120) [16]. A receiver:

```supercollider
~osc = OSCFunc({ |msg, time, addr, recvPort|
    [msg, time, addr, recvPort].postln;
}, '/hello');
```

The `OSC Communication` guide documents type tags, variable source ports, `OSCFunc.trace` and
custom dispatch [16]. Use OSC to connect the Studio's SC process to TouchOSC, Max, Processing or
another application.

### 11.3 Quarks and sc3-plugins

SC can be extended with Quarks and the sc3-plugins collection [1][32][33]. Quarks are installed
into the SC extensions directory; the official documentation covers `Quarks` and extension use
[1][33]. Third-party UGens change the set of classes available to your SynthDefs, so if you use
them in a script, make sure they are installed for the SC installation that the Studio launches.
SC also reads a startup file / library configuration; see the *Sclang Startup File* reference
[19].

### 11.4 The SuperCollider Book

For a structured, book-length treatment, see *The SuperCollider Book* (MIT Press) [31]; the
official site links to it [1].

---

## 12. Debugging checklist

| Symptom | Check |
|---|---|
| `SuperCollider was not found` | Use **Locate SuperCollider** and choose `sclang.exe` [app]. |
| No audio | Check the server audio device and API [17]; run `s.boot; { SinOsc.ar(440, 0, 0.1) }.play;` [3]. |
| `SynthDef not found` / `FAILURE` | The synth name must match a definition that was `.add`ed before the `Pbind` starts [4][9]. |
| A control does nothing | The `SynthDef` argument name must match the `Pbind` key (`\cutoff` -> `cutoff`) [9]. |
| Distorted/noisy output | Check `amp`, filter resonance, `crush`, feedback and the master level; inspect `s.meter` [12]. |
| Pattern never starts | Check `TempoClock` tempo, `quant`, and whether the server booted; `TempoClock` schedules in beats [11]. |
| Effect processes the wrong signal | Check bus indices and node order; use `addToTail` and read the order-of-execution guide [6][14]. |
| SC exits immediately | Read `sclang.log` and `sclang.err.log` in the studio folder, and the Studio's Log tab [app]. |
| Render produces no file | The Studio reports the path and the log; check `s.record` and the render timeout [app][12]. |
| Notes are in the wrong key/octave | `\freq` is Hz, `\midinote` is MIDI note number; convert with `.midicps` [9]. |
| Severe distortion at note start with a resonant filter | `EnvGen` initial-value behaviour can cause a spike at short attacks; see the EnvGen note [26]. |

The official error guide and debugging tips are useful next reads [20][34].

---

## 13. A complete working example

This script is self-contained and can be run from ScIDE. It demonstrates a synth definition, a
private effect bus, a reverb added after the sources, a tempo clock and a `Pbind`.

```supercollider
s.options.numOutputBusChannels = 2;

s.waitForBoot({
    SynthDef(\hbTutorial, { |out = 0, freq = 220, amp = 0.2, pan = 0,
                             sustain = 0.35, cutoff = 1800, res = 0.4,
                             envamt = 1, atk = 0.005, rel = 0.25, crush = 16|
        var env = EnvGen.ar(Env.linen(atk, sustain, rel, 1, -4), doneAction: 2);
        var fenv = EnvGen.ar(Env.perc(0.001, 0.22, 1, -4));
        var sig = Saw.ar(freq) + Pulse.ar(freq * 1.005, 0.3);
        var steps = 2 ** (crush.clip(2, 16) - 1);
        sig = RLPF.ar(sig, (cutoff * (1 + (fenv * envamt * 4))).clip(40, 16000),
                      (1 - res).clip(0.05, 1));
        sig = (sig * steps).round / steps;
        Out.ar(out, Pan2.ar(sig.tanh * env * amp, pan));
    }).add;

    SynthDef(\hbTutorialReverb, { |in = 0, out = 0, mix = 0.3, room = 0.8, damp = 0.3|
        var dry = In.ar(in, 2);
        Out.ar(out, dry + FreeVerb2.ar(dry[0], dry[1], mix, room, damp));
    }).add;

    ~fxBus = Bus.audio(s, 2);
    s.sync;

    Synth.tail(s, \hbTutorialReverb, [\in, ~fxBus, \out, 0]);
    ~clock = TempoClock(140 / 60);

    Pbind(
        \instrument, \hbTutorial,
        \freq, Pseq([220, 261.63, 329.63, 392, 329.63, 261.63], inf),
        \dur, Pseq([0.25, 0.25, 0.5, 0.5, 0.25, 0.25], inf),
        \sustain, Pseq([0.2, 0.2, 0.35, 0.5, 0.2, 0.2], inf),
        \amp, 0.18,
        \pan, Pseq([-0.2, 0.2], inf),
        \out, ~fxBus
    ).play(~clock, quant: 0);
});
```

Stop it with `Ctrl+.` in ScIDE, or `s.quit` in the code. In the Studio, the generated script
handles stopping through `live.stop` [app].

---

## 14. Live bridge, graph compiler and native effects

### 14.1 Per-project sessions

Each project owns a stable SuperCollider session id (`ScSessionId`), saved in the project
sidecar. Generated files live outside the git repository:

```
%LOCALAPPDATA%\HumbleBakerAudio\studio\sc\
    library\classes\           shared stable classes and startup hook
    sessions\<project-id>\     project.scd, bootstrap.scd, recompile.scd, logs
```

The stable classes are `HBProject`, `HBNode`, `HBTrack`, `HBSection`, `HBEffect` and
`HBBootstrap`. They are written to the shared library once and loaded by every session, so a
project script only describes that project.

### 14.2 The live bridge

**Start live bridge** launches a persistent `sclang` (hidden) and keeps it running for the
lifetime of the app. The app sends OSC to `sclang` on port **57120** and listens for replies on
**57121**. The bridge survives project edits; **Load session in SC** regenerates and reloads
`project.scd`, and **Play SC** / **Stop SC** control the transport.

| Message | Direction | Purpose |
|---|---|---|
| `/hb/project/load <path>` | app → SC | interpret a generated `project.scd` |
| `/hb/transport/play` | app → SC | start all track patterns |
| `/hb/transport/stop` | app → SC | stop all track patterns |
| `/hb/transport/bpm <bpm>` | app → SC | change tempo |
| `/hb/node/set <id> <param> <value>` | app → SC | set a live node parameter |
| `/hb/node/bypass <id> <0|1>` | app → SC | bypass an effect node |
| `/hb/recompile` | app → SC | stop transport, recompile definitions |
| `/hb/shutdown` | app → SC | stop, quit the audio server, exit |
| `/hb/ready` | SC → app | bridge/session ready |
| `/hb/error <text>` | SC → app | load or compile error |

`HBBootstrapStartup *initClass` registers the bridge again after any recompile. Recompiling
always stops the transport and leaves it stopped.

### 14.3 Generated project language

`Get-StudioScGraphProject` compiles the piano roll, drum tracks, sampler tracks, automation,
patch-graph modules, links and effect parameters into ordinary SuperCollider code. Drum tracks
use the built-in `hbkick`/`hbsnare`/`hbhat` voices, melodic tracks use `hbacid`, `hbsaw`,
`hbsquare`, `hbtri`, `hbsine`, `hbfm`, `hbpad`, `hbnoise` or `hbpluck`, patch tracks get a
generated instrument def, and sampler tracks get a `Buffer.read` of the track's sample. A
generated file looks like this:

```supercollider
(
~hb = HBProject.new("Humble Baker Session", 120, 4, 4);
s.waitForBoot({
    // named effect SynthDefs (hb_<type>) and routing helpers
    ~b0 = Bus.audio(s, 2);
    ~tBass = ~hb.addTrack(\Bass, \default, ~b0);
    ~tBass.pattern = Pbind(\instrument, \default, \midinote, Pseq([36, 43], inf), ...);
    ~n1 = ~hb.addNode(\n1, \ladder, Dictionary.new);
    ~n1.synth = Synth(\hb_ladder, [\in, ~b2, \out, ~b1, \mix, 1, \cutoff, 900], addAction: \addToTail);
    Synth(\hb_route, [\in, ~b0, \out, ~b2], addAction: \addToTail);
    ~hb.play;
});
~hb;
)
```

Because the result is plain SC, you can edit it by hand, add your own `Pbind`s or replace a
module. The `~hb` API is stable:

```supercollider
~hb.addTrack(\name, \default, outBus);   // HBTrack
~hb.addNode(\id, \filter, Dictionary.new); // HBNode
~hb.addSection(\intro, 8, 0.6);          // HBSection
~hb.play; ~hb.stop; ~hb.bpm_(128);
~node.set(\cutoff, 400); ~node.bypass(true);
```

### 14.4 Native effect library

All **113** effect types are native SuperCollider `SynthDef`s named `hb_<type>`
(`hb_filter`, `hb_ladder`, `hb_convreverb`, ...). They share a small set of DSP families:

| Family | Used by |
|---|---|
| `filter` | filters, EQs, wah, formant, sweep, air band |
| `drive` | saturation, tube, tape, diode, fuzz, clipper, amp sims |
| `crush` | bit crusher, chiptune |
| `mod` | chorus, flanger, phaser, tremolo, rotary, stutter, ring mod |
| `delay` | delays, ping-pong, dub, multi-tap, echo-verb |
| `reverb` | reverbs, plate, spring, shimmer, convolution-style spaces |
| `comp` | compressors, limiters, gates, de-esser, transient shaping |
| `eq` | EQ, tilt, Pultec, dynamic EQ, mid/side |
| `width` | stereo width, mono maker, phase align, shuffle |
| `pitch` | octaver, harmonizer, pitch shift, sub bass |
| `spectral` | spectral gate/blur/delay, vocoder, freeze |
| `gate` | noise gates |
| `passthrough` | unknown/unmapped types |

Each type receives its parameters from the same `Fx` fields used by the built-in renderer:
cutoff, resonance, drive, bits, rate, delay time, feedback, size, threshold, ratio, width and
pitch. Unknown module types fall back to `hb_passthrough`, so a project always loads.

Some convolution and spectral helpers sound better with **sc3-plugins** installed. The
**Check SC setup** button reports whether sc3-plugins was found; the core 113 definitions work
without it.

### 14.5 Parity harness

`tests\sc-parity.ps1` verifies the integration end to end:

```powershell
powershell -NoProfile -STA -ExecutionPolicy Bypass -File tests\sc-parity.ps1
```

It does three things:

1. Compiles the effect library in SuperCollider and asserts that all 113 named defs exist.
2. Renders every def offline through `scsynth -N` and checks that the audio is finite,
   non-silent and not clipping.
3. Compares the shared families against the app's `FxProc` on an identical test signal and
   writes correlation and RMS figures to `family-parity.csv`.

Reports are written to `%TEMP%\hb-sc-parity`. The test is headless and does not touch the
project or the audio device.

### 14.6 Troubleshooting the bridge

- **Live bridge: SuperCollider exited.** Read `bridge.out.log` / `bridge.err.log` in the
  project's session folder.
- **Port already in use.** Another `sclang` may still be running from an older build. Close
  it, or stop the bridge and retry; the app also cleans up the `scsynth` it started.
- **Load error.** The generated `project.scd` is plain SC. Open it and run it in ScIDE to see
  the full error, then click **Recompile SC definitions**.
- **No sound.** Check that the track is not muted, that its graph node is linked to the master
  out node, and that the SC audio device matches the one used by the app.

---

## 15. Sources and citations

All SuperCollider documentation links below were verified against **SuperCollider 3.14.1** on
2026-11-11. The app-specific `[app]` reference is the copy of `AudioStudioImplementation.ps1` in
this repository.

| # | Source |
|---|---|
| 1 | SuperCollider Documentation home, <https://docs.supercollider.online/> |
| 2 | *Getting Started With SC* (tutorial index), <https://docs.supercollider.online/Tutorials/Getting-Started/00-Getting-Started-With-SC.html> |
| 3 | *10. SynthDefs and Synths*, <https://docs.supercollider.online/Tutorials/Getting-Started/10-SynthDefs-and-Synths.html> |
| 4 | `SynthDef` class reference, <https://docs.supercollider.online/Classes/SynthDef.html> |
| 5 | `Synth` class reference, <https://docs.supercollider.online/Classes/Synth.html> |
| 6 | *11. Busses*, <https://docs.supercollider.online/Tutorials/Getting-Started/11-Busses.html> |
| 7 | `Bus` class reference, <https://docs.supercollider.online/Classes/Bus.html> |
| 8 | *16. Sequencing with Patterns*, <https://docs.supercollider.online/Tutorials/Getting-Started/16-Sequencing-with-Patterns.html> |
| 9 | `Pbind` class reference, <https://docs.supercollider.online/Classes/Pbind.html> |
| 10 | *Pattern Guide 01: Introduction*, <https://docs.supercollider.online/Tutorials/A-Practical-Guide/PG_01_Introduction.html> |
| 11 | `TempoClock` class reference, <https://docs.supercollider.online/Classes/TempoClock.html> |
| 12 | `Server` class reference, <https://docs.supercollider.online/Classes/Server.html> |
| 13 | *Client vs Server*, <https://docs.supercollider.online/Guides/ClientVsServer.html> |
| 14 | *Order of execution*, <https://docs.supercollider.online/Guides/Order-of-execution.html> |
| 15 | *Using MIDI*, <https://docs.supercollider.online/Guides/UsingMIDI.html> |
| 16 | *OSC Communication*, <https://docs.supercollider.online/Guides/OSC_communication.html> |
| 17 | *Audio device selection*, <https://docs.supercollider.online/Reference/AudioDeviceSelection.html> |
| 18 | *Non-Realtime Synthesis (NRT)*, <https://docs.supercollider.online/Guides/Non-Realtime-Synthesis.html> |
| 19 | *Sclang Startup File*, <https://docs.supercollider.online/Reference/StartupFile.html> |
| 20 | *Understanding errors*, <https://docs.supercollider.online/Guides/Understanding-Errors.html> |
| 21 | SuperCollider downloads, <https://supercollider.github.io/downloads> |
| 22 | *Licensing*, <https://docs.supercollider.online/Other/Licensing.html>; help-file licensing, <https://docs.supercollider.online/Other/HelpDocsLicensing.html> |
| 23 | SuperCollider source repository, <https://github.com/supercollider/supercollider> |
| 24 | `SC_CLIOptions.cpp` (`--daemon, -D` and positional input file), <https://github.com/supercollider/supercollider/blob/Version-3.14.1/lang/LangSource/SC_CLIOptions.cpp> |
| 25 | `SC_TerminalClient.cpp` (`executeFile`, `runMain`), <https://github.com/supercollider/supercollider/blob/Version-3.14.1/lang/LangSource/SC_TerminalClient.cpp> |
| 26 | `EnvGen` class reference, <https://docs.supercollider.online/Classes/EnvGen.html> |
| 27 | `Group` class reference, <https://docs.supercollider.online/Classes/Group.html> |
| 28 | `Node` class reference, <https://docs.supercollider.online/Classes/Node.html> |
| 29 | *Synth Definition File Format*, <https://docs.supercollider.online/Reference/Synth-Definition-File-Format.html> |
| 30 | `ServerOptions` class reference, <https://docs.supercollider.online/Classes/ServerOptions.html> |
| 31 | *The SuperCollider Book* (MIT Press), <https://mitpress.mit.edu/9780262049702/the-supercollider-book/> |
| 32 | sc3-plugins, <https://github.com/supercollider/sc3-plugins> |
| 33 | `Quarks` class reference, <https://docs.supercollider.online/Classes/Quarks.html> |
| 34 | *Debugging tips*, <https://docs.supercollider.online/Guides/Debugging-tips.html> |
| 35 | *Tour of UGens*, <https://docs.supercollider.online/Guides/Tour_of_UGens.html> |
| 36 | *SuperCollider IDE*, <https://docs.supercollider.online/Guides/SCIde.html> |
| app | `AudioStudioImplementation.ps1` in this repository: `Find-StudioSuperCollider`, `Get-StudioSynthDefs`, `Get-StudioScd`, `Get-StudioScdPattern`, `Get-StudioFxSynthDef`, `Start-Studio`, `Invoke-StudioRender`, `Stop-Studio`; see <../AudioStudioImplementation.ps1> |

### Licence note for citations

The links above point to SuperCollider's documentation, which is published under
CC BY-SA 3.0 [22]. This tutorial paraphrases and cites that material; it does not reproduce
large passages. SuperCollider itself is GPL-licensed [1][22]. If you redistribute SC or a
modified help file, follow those licences.
