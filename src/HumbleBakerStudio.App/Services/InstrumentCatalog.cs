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
        "acid", "saw", "square", "tri", "sine", "pad", "fm", "pluck", "noise", "drums", "patch", "sample"
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
            if (!string.IsNullOrEmpty(option.Synth)) track.Synth = option.Synth;
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
