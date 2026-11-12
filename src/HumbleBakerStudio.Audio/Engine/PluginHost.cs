namespace HumbleBakerStudio.Audio;


/// <summary>A note handed to a plugin instrument. Times are in beats so the host can honour tempo.</summary>
public struct PluginNote
{
    public double StartBeat;
    public double LenBeats;
    public int Pitch;
    public int Vel;
}

/// <summary>
/// Bridge between the offline renderer and a real plugin host. The audio engine has no dependency
/// on any VST library; the app installs an implementation at startup.
/// </summary>
public interface IPluginHost
{
    bool HasInstrument(Track track);
    bool HasEffect(Track track);

    /// <summary>Renders an instrument track. Returns [left, right] of exactly <paramref name="frames"/> samples.</summary>
    float[][]? RenderInstrument(Track track, IReadOnlyList<PluginNote> notes, double secondsPerBeat, int frames, bool loop);

    /// <summary>Processes a stereo track in place (effect plugins).</summary>
    void ProcessEffect(Track track, float[] left, float[] right, double secondsPerBeat);

    /// <summary>Stores the live plugin state back on the track (called before saving or rendering from a snapshot).</summary>
    void CaptureState(Track track) { }

    // Graph-node variants. FX state is written back on the node instead of a track.
    bool HasNodeEffect(PatchNode node) => false;
    void ProcessNodeEffect(PatchNode node, float[] left, float[] right, double secondsPerBeat) { }
    void CaptureNodeState(PatchNode node) { }
}

public static class AudioPlugins
{
    public static IPluginHost? Host;

    public static bool HasInstrument(Track? track)
    {
        if (track == null || Host == null || track.PluginBypass) return false;
        if (string.IsNullOrEmpty(track.PluginPath) || !track.PluginInstrument) return false;
        try { return Host.HasInstrument(track); } catch { return false; }
    }

    public static bool HasEffect(Track? track)
    {
        if (track == null || Host == null || track.PluginBypass) return false;
        if (string.IsNullOrEmpty(track.PluginPath) || track.PluginInstrument) return false;
        try { return Host.HasEffect(track); } catch { return false; }
    }
}
