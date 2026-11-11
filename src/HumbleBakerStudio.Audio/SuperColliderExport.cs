using System.Globalization;
using System.Text;

namespace HumbleBakerStudio.Audio;

/// <summary>
/// Writes a self-contained SuperCollider script (<c>.scd</c>) for a project: SynthDefs for the
/// built-in voices and drums, one Pbind per track, a global delay/reverb return, and sample
/// buffers for sampler tracks. Effects are approximated by the per-voice filter and the global
/// return; the full 113-effect graph lives in the native engine, not in this export.
/// </summary>
public static class SuperColliderExport
{
    public static string ToScript(Project project, bool song, string sampleDirectory, out int sampleFiles)
    {
        var p = song && project.Song.Count > 0 ? Arranger.Flatten(project) : project;
        sampleFiles = 0;
        double spb = 60.0 / p.Bpm;
        var setup = new StringBuilder();
        var patterns = new List<string>();

        for (int i = 0; i < p.Tracks.Count; i++)
        {
            var t = p.Tracks[i];
            if (t.Mute) continue;
            if (t.Synth == "sample" && t.Sample != null && t.Sample.Length > 32)
            {
                Directory.CreateDirectory(sampleDirectory);
                var file = Path.Combine(sampleDirectory, $"sample-{i}.wav");
                Dsp.WriteWavMono(file, t.Sample);
                sampleFiles++;
                setup.AppendLine($"~buf{i} = Buffer.read(s, \"{Slash(file)}\");");
            }

            var pat = PatternFor(p, t, i, spb);
            if (!string.IsNullOrWhiteSpace(pat)) patterns.Add(pat);
        }

        var sb = new StringBuilder();
        sb.AppendLine("// Humble Baker Studio — SuperCollider export");
        sb.AppendLine($"// {p.Bpm:0.##} BPM · {p.Bars} bars · key {p.Key} {p.Scale} · {(song ? "song" : "pattern")}");
        sb.AppendLine("// Generated from a C# port of Humble Baker Studio. Audio parity is approximate;");
        sb.AppendLine("// the native engine is the reference renderer.");
        sb.AppendLine();
        sb.AppendLine("s.options.numOutputBusChannels = 2;");
        sb.AppendLine("s.waitForBoot({");
        sb.AppendLine("    ~hbmaster = Bus.audio(s, 2);");
        sb.AppendLine(Indent(SynthDefs, 4));
        sb.AppendLine(Indent(setup.ToString().TrimEnd(), 4));
        sb.AppendLine("    s.sync;");
        sb.AppendLine($"    ~hbfx = Synth(\\hbfx, [\\in, ~hbmaster, \\out, 0, \\dtime, {Num(p.DelayBeats * spb)}, \\fb, {Num(p.DelayFb)}, \\mix, {Num(p.DelayMix)}], addToTail: true);");
        sb.AppendLine($"    ~hbclock = TempoClock({Num(p.Bpm / 60.0)});");
        sb.AppendLine("    s.sync;");
        sb.AppendLine();
        if (patterns.Count == 0)
        {
            sb.AppendLine("    \"No unmuted tracks with notes.\".postln;");
        }
        else
        {
            sb.AppendLine("    ~hbplay = Ppar([");
            for (int i = 0; i < patterns.Count; i++)
                sb.AppendLine("        " + Indent(patterns[i], 8).TrimStart() + (i < patterns.Count - 1 ? "," : ""));
            sb.AppendLine("    ]);");
            sb.AppendLine("    ~hbstream = ~hbplay.play(~hbclock);");
        }
        sb.AppendLine("});");
        sb.AppendLine();
        sb.AppendLine("// Stop with: ~hbstream.stop; ~hbfx.free;");
        return sb.ToString();
    }

    private static string PatternFor(Project p, Track t, int index, double spb)
    {
        var evs = Engine.Events(p, t);
        if (evs.Count == 0) return "";
        double loop = p.LoopBeats;
        bool drums = t.Synth == "drums";
        bool sample = t.Synth == "sample" && t.Sample != null;
        string voice = sample ? $"\\hbsamp" : "\\hbvoice";

        var instruments = new List<string>();
        var freqs = new List<string>();
        var durs = new List<string>();
        var sus = new List<string>();
        var amps = new List<string>();

        void Row(string inst, string freq, double dur, double sustain, double amp)
        {
            instruments.Add(inst);
            freqs.Add(freq);
            durs.Add(Num(dur));
            sus.Add(Num(sustain));
            amps.Add(Num(amp));
        }

        if (evs[0].Start > 0.0001)
            Row(drums ? "\\hbkick" : voice, "\\rest", evs[0].Start, 0.1, 0);

        for (int i = 0; i < evs.Count; i++)
        {
            var e = evs[i];
            double next = i < evs.Count - 1 ? evs[i + 1].Start : loop;
            string inst = drums ? DrumInstrument(e.Pitch) : voice;
            double hz = 440.0 * Math.Pow(2, (e.Pitch - 69) / 12.0);
            double amp = t.Vol * e.Vel / 127.0 * 0.45;
            Row(inst, Num(hz), Math.Max(0.01, next - e.Start), e.Len, amp);
        }

        int wave = t.Synth switch
        {
            "square" => 1,
            "tri" => 2,
            "sine" => 3,
            "pluck" => 4,
            "noise" => 5,
            _ => 0
        };

        var kv = new List<string>
        {
            $"\\instrument, Pseq([{string.Join(", ", instruments)}], 1)",
            $"\\freq, Pseq([{string.Join(", ", freqs)}], 1)",
            $"\\dur, Pseq([{string.Join(", ", durs)}], 1)",
            $"\\sustain, Pseq([{string.Join(", ", sus)}], 1)",
            $"\\amp, Pseq([{string.Join(", ", amps)}], 1)",
            "\\out, ~hbmaster",
            $"\\pan, {Num(t.Pan)}",
            $"\\cutoff, {Num(t.Cutoff)}",
            $"\\res, {Num(t.Res)}",
            $"\\atk, {Num(t.Atk)}",
            $"\\rel, {Num(t.Rel)}",
            $"\\wave, {wave}"
        };
        if (sample)
        {
            kv.Add($"\\buf, ~buf{index}");
            kv.Add($"\\rootfreq, {Num(440.0 * Math.Pow(2, (t.SampleRoot - 69) / 12.0))}");
        }
        return "Pbind(\n        " + string.Join(",\n        ", kv) + "\n    )";
    }

    private static string DrumInstrument(int pitch)
    {
        return DrumSamples.PieceFor(pitch) switch
        {
            "kick" => "\\hbkick",
            "snare" => "\\hbsnare",
            "clap" => "\\hbclap",
            "rim" => "\\hbrim",
            "chat" => "\\hbhat",
            "ohat" => "\\hbhat",
            "crash" => "\\hbcrash",
            "ride" => "\\hbride",
            "tomlo" => "\\hbtom",
            "tommhi" => "\\hbtom",
            _ => "\\hbperc"
        };
    }

    private static string Num(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);
    private static string Slash(string path) => path.Replace('\\', '/').Replace("\"", "\\\"");
    private static string Indent(string text, int spaces)
    {
        var pad = new string(' ', spaces);
        return string.Join("\n", text.Replace("\r", "").Split('\n').Select(l => l.Length == 0 ? l : pad + l));
    }

    private const string SynthDefs = @"// --- Humble Baker Studio voices -------------------------------------------------
SynthDef(\hbvoice, { |freq=220, amp=0.3, dur=0.5, sustain=0.7, pan=0, cutoff=2400, res=0.3,
                     atk=0.005, rel=0.2, wave=0, out=0|
    var env = EnvGen.kr(Env.linen(atk, dur * sustain.max(0.01), rel, 1, \lin), doneAction: 2);
    var osc = Select.ar(wave,
        [
            Saw.ar(freq), Pulse.ar(freq, 0.5, 0.7), LFTri.ar(freq), SinOsc.ar(freq),
            Pluck.ar(WhiteNoise.ar(0.4), Impulse.kr(0), freq.reciprocal, freq.reciprocal, 0.9),
            WhiteNoise.ar(0.5)
        ]
    );
    var filt = RLPF.ar(osc, cutoff.clip(40, 18000), (1 - res).clip(0.05, 1));
    var sig = filt * env * amp;
    Out.ar(out, Pan2.ar(sig, pan));
}).add;

SynthDef(\hbkick, { |amp=0.8, out=0, pan=0|
    var env = EnvGen.kr(Env.perc(0.002, 0.35), doneAction: 2);
    var sig = SinOsc.ar(48 + (90 * EnvGen.kr(Env.perc(0.001, 0.08))));
    Out.ar(out, Pan2.ar((sig * env * amp).tanh, pan));
}).add;

SynthDef(\hbsnare, { |amp=0.6, out=0, pan=0|
    var env = EnvGen.kr(Env.perc(0.001, 0.18), doneAction: 2);
    var sig = (WhiteNoise.ar(0.7) + SinOsc.ar(180, 0, 0.4)) * env * amp;
    Out.ar(out, Pan2.ar(sig, pan));
}).add;

SynthDef(\hbclap, { |amp=0.5, out=0, pan=0|
    var env = EnvGen.kr(Env.perc(0.002, 0.14), doneAction: 2);
    Out.ar(out, Pan2.ar(BBandPass.ar(WhiteNoise.ar, 1200, 0.7) * env * amp, pan));
}).add;

SynthDef(\hbhat, { |amp=0.35, out=0, pan=0|
    var env = EnvGen.kr(Env.perc(0.001, 0.06), doneAction: 2);
    Out.ar(out, Pan2.ar(HPF.ar(WhiteNoise.ar, 7000) * env * amp, pan));
}).add;

SynthDef(\hbrim, { |amp=0.35, out=0, pan=0|
    var env = EnvGen.kr(Env.perc(0.001, 0.04), doneAction: 2);
    Out.ar(out, Pan2.ar(BPF.ar(WhiteNoise.ar, 1800, 3) * env * amp, pan));
}).add;

SynthDef(\hbtom, { |amp=0.5, out=0, pan=0|
    var env = EnvGen.kr(Env.perc(0.002, 0.30), doneAction: 2);
    Out.ar(out, Pan2.ar(SinOsc.ar(150, 0, env * amp), pan));
}).add;

SynthDef(\hbcrash, { |amp=0.35, out=0, pan=0|
    var env = EnvGen.kr(Env.perc(0.002, 1.6), doneAction: 2);
    Out.ar(out, Pan2.ar(HPF.ar(WhiteNoise.ar, 4000) * env * amp, pan));
}).add;

SynthDef(\hbride, { |amp=0.3, out=0, pan=0|
    var env = EnvGen.kr(Env.perc(0.002, 0.9), doneAction: 2);
    Out.ar(out, Pan2.ar(Mix(BPF.ar(WhiteNoise.ar, [3200, 5400], 4)) * env * amp, pan));
}).add;

SynthDef(\hbperc, { |amp=0.4, out=0, pan=0|
    var env = EnvGen.kr(Env.perc(0.001, 0.12), doneAction: 2);
    Out.ar(out, Pan2.ar(BPF.ar(WhiteNoise.ar, 2400, 2) * env * amp, pan));
}).add;

SynthDef(\hbsamp, { |buf, rootfreq=261.63, freq=261.63, amp=0.6, dur=1, sustain=0.8, pan=0, out=0|
    var rate = (freq / rootfreq).clip(0.25, 4);
    var env = EnvGen.kr(Env.linen(0.002, dur * sustain.max(0.01), 0.05), doneAction: 2);
    var sig = PlayBuf.ar(1, buf, rate * BufRateScale.kr(buf), 1, 0, 1);
    Out.ar(out, Pan2.ar(sig * env * amp, pan));
}).add;

SynthDef(\hbfx, { |in, out=0, dtime=0.32, fb=0.4, mix=0.2|
    var src = In.ar(in, 2);
    var wet = CombC.ar(src, 2, dtime, 4) * fb;
    var verb = FreeVerb2.ar(src[0], src[1], 0.4, 0.75, 0.3) * 0.3;
    Out.ar(out, (src + (wet + verb) * mix).tanh);
}).add;";
}
