using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Services;

/// <summary>One entry in a track's instrument selector: a built-in synthesiser or a scanned VST3 instrument.</summary>
internal sealed class InstrumentOption
{
    public string Label = "";
    public string Synth = "";
    public Vst3PluginInfo? Vst;

    public override string ToString() => Label;
}

internal static class InstrumentCatalog
{
    public static readonly string[] BuiltIn =
    {
        "strings", "bowed", "cello", "pluck", "strum", "hybrid",
        "acid", "saw", "square", "tri", "sine", "pad", "fm", "noise", "drums", "patch", "sample"
    };

    public static List<InstrumentOption> Build(Vst3Host vst)
    {
        var list = new List<InstrumentOption>();
        foreach (string s in BuiltIn)
            list.Add(new InstrumentOption { Label = s, Synth = s });
        foreach (var p in vst.Catalog)
        {
            if (!p.IsInstrument) continue;
            list.Add(new InstrumentOption { Label = "VST3 · " + p.Name, Synth = "", Vst = p });
        }
        return list;
    }

    public static void Apply(Track track, InstrumentOption option, Vst3Host vst)
    {
        if (option == null) return;
        vst.Remove(track);
        if (option.Vst != null)
        {
            track.PluginFormat = "vst3";
            track.PluginPath = option.Vst.Path;
            track.PluginClass = option.Vst.ClassId;
            track.PluginName = option.Vst.Name;
            track.PluginInstrument = true;
            track.PluginState = "";
            track.PluginBypass = false;
        }
        else
        {
            track.PluginFormat = "";
            track.PluginPath = "";
            track.PluginClass = "";
            track.PluginName = "";
            track.PluginInstrument = false;
            track.PluginState = "";
            track.PluginBypass = false;
            if (!string.IsNullOrEmpty(option.Synth))
            {
                track.Synth = option.Synth;
                ApplyVoiceDefaults(track, option.Synth);
            }
        }
    }

    /// <summary>Sensible starting articulations for the instrument-emulator engines.</summary>
    private static void ApplyVoiceDefaults(Track t, string synth)
    {
        switch (synth)
        {
            case "strings":
            case "bowed":
                t.Atk = 0.12; t.Rel = 0.55; t.Vibrato = 0.13; t.VibratoRate = 5.3;
                t.Ensemble = 0.55; t.Brightness = 0.55; t.Body = 0.55; t.Glide = 0.04;
                t.Cutoff = 4200; t.Res = 0.25;
                break;
            case "cello":
                t.Atk = 0.16; t.Rel = 0.65; t.Vibrato = 0.10; t.VibratoRate = 4.6;
                t.Ensemble = 0.45; t.Brightness = 0.42; t.Body = 0.7; t.Glide = 0.05;
                t.Cutoff = 3000; t.Res = 0.25;
                break;
            case "pluck":
                t.Atk = 0.002; t.Rel = 0.35; t.Vibrato = 0; t.Ensemble = 0.25;
                t.Brightness = 0.65; t.Body = 0.5; t.Glide = 0.0;
                break;
            case "strum":
                t.Atk = 0.002; t.Rel = 0.45; t.Vibrato = 0; t.Ensemble = 0.6;
                t.Brightness = 0.62; t.Body = 0.55; t.Glide = 0.0;
                break;
            case "hybrid":
                t.Atk = 0.01; t.Rel = 0.5; t.Vibrato = 0.08; t.VibratoRate = 5.0;
                t.Ensemble = 0.5; t.Brightness = 0.58; t.Body = 0.55; t.Glide = 0.06;
                break;
        }
    }

    public static InstrumentOption? Find(Track track, IReadOnlyList<InstrumentOption> options)
    {
        if (track == null) return null;
        if (!string.IsNullOrEmpty(track.PluginPath) && track.PluginInstrument)
        {
            var match = options.FirstOrDefault(o => o.Vst != null &&
                string.Equals(o.Vst.Path, track.PluginPath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(o.Vst.ClassId, track.PluginClass, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
            return options.FirstOrDefault(o => o.Vst != null &&
                string.Equals(o.Vst.Name, track.PluginName, StringComparison.OrdinalIgnoreCase));
        }
        return options.FirstOrDefault(o => o.Vst == null && string.Equals(o.Synth, track.Synth, StringComparison.OrdinalIgnoreCase));
    }
}
