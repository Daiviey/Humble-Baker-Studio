using System.Text;
using HumbleBakerStudio.Audio;
using NAudio.Vst3;

namespace HumbleBakerStudio.Services;

internal sealed class Vst3PluginInfo
{
    public string Name = "";
    public string Vendor = "";
    public string Path = "";
    public string ClassId = "";
    public bool IsInstrument;
    public string Format => "VST3";
    public string Key => Path + "|" + ClassId;

    public override string ToString() => Name;
}

/// <summary>
/// Hosts VST3 effects and instruments in the offline renderer. One plugin instance is kept per
/// track and plugin choice; the audio engine calls into this class through <see cref="IPluginHost"/>.
/// </summary>
internal sealed class Vst3Host : IPluginHost, IDisposable
{
    public event Action? CatalogChanged;

    private readonly object _gate = new();
    private readonly Dictionary<string, Vst3Instance> _instances = new(StringComparer.OrdinalIgnoreCase);
    private List<Vst3PluginInfo> _catalog = new();

    public IReadOnlyList<Vst3PluginInfo> Catalog
    {
        get { lock (_gate) return _catalog.ToList(); }
    }

    public void Scan()
    {
        var found = new List<Vst3PluginInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var moduleInfo in Vst3PluginScanner.EnumerateInstalled())
        {
            try
            {
                using var module = Vst3Module.Load(moduleInfo.Path);
                foreach (var cls in module.GetClasses())
                {
                    if (!cls.IsAudioModule) continue;
                    if (!seen.Add(moduleInfo.Path + "|" + cls.ClassId)) continue;
                    found.Add(new Vst3PluginInfo
                    {
                        Name = string.IsNullOrWhiteSpace(cls.Name) ? moduleInfo.Name : cls.Name,
                        Vendor = cls.Vendor ?? "",
                        Path = moduleInfo.Path,
                        ClassId = cls.ClassId ?? "",
                        IsInstrument = cls.IsInstrument
                    });
                }
            }
            catch
            {
                // A 32-bit, broken or unlicensed module must not stop the scan.
            }
        }
        found.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        lock (_gate) _catalog = found;
        try { CatalogChanged?.Invoke(); } catch { }
    }

    public bool HasInstrument(Track track) =>
        track != null && string.Equals(track.PluginFormat, "vst3", StringComparison.OrdinalIgnoreCase) &&
        track.PluginInstrument && !string.IsNullOrEmpty(track.PluginPath) && File.Exists(track.PluginPath);

    public bool HasEffect(Track track) =>
        track != null && string.Equals(track.PluginFormat, "vst3", StringComparison.OrdinalIgnoreCase) &&
        !track.PluginInstrument && !string.IsNullOrEmpty(track.PluginPath) && File.Exists(track.PluginPath);

    public bool HasNodeEffect(PatchNode node) =>
        node != null && string.Equals(node.PluginFormat, "vst3", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrEmpty(node.PluginPath) && File.Exists(node.PluginPath);

    public void ProcessNodeEffect(PatchNode node, float[] left, float[] right, double secondsPerBeat)
    {
        lock (_gate)
        {
            try { Get(NodeTrack(node))?.ProcessEffect(left, right); }
            catch { }
        }
    }

    public void CaptureNodeState(PatchNode node)
    {
        if (node == null) return;
        lock (_gate)
        {
            var pseudo = NodeTrack(node);
            try
            {
                string key = KeyFor(pseudo);
                if (_instances.TryGetValue(key, out var inst) && inst.Plugin != null)
                {
                    byte[] state = inst.Plugin.SaveState() ?? Array.Empty<byte>();
                    if (state.Length > 0) node.PluginState = Convert.ToBase64String(state);
                }
            }
            catch { }
        }
    }

    public void RemoveNode(PatchNode node)
    {
        if (node == null) return;
        lock (_gate)
        {
            try
            {
                string key = KeyFor(NodeTrack(node));
                if (_instances.Remove(key, out var inst)) inst.Dispose();
            }
            catch { }
        }
    }

    public System.Windows.Forms.Form? CreateNodeEditor(PatchNode node)
    {
        if (node == null) return null;
        var pseudo = NodeTrack(node);
        var form = CreateEditor(pseudo);
        if (form != null) form.FormClosed += (_, _) => node.PluginState = pseudo.PluginState;
        return form;
    }

    private static Track NodeTrack(PatchNode node) => new()
    {
        Uid = "node:" + node.Id,
        Name = node.PluginName,
        PluginFormat = node.PluginFormat,
        PluginPath = node.PluginPath,
        PluginClass = node.PluginClass,
        PluginName = node.PluginName,
        PluginState = node.PluginState,
        PluginBypass = node.PluginBypass,
        PluginInstrument = false
    };

    public float[][]? RenderInstrument(Track track, IReadOnlyList<PluginNote> notes, double secondsPerBeat, int frames, bool loop)
    {
        lock (_gate)
        {
            try
            {
                var inst = Get(track);
                return inst?.RenderInstrument(notes, secondsPerBeat, frames, loop);
            }
            catch
            {
                return null;
            }
        }
    }

    public void ProcessEffect(Track track, float[] left, float[] right, double secondsPerBeat)
    {
        lock (_gate)
        {
            try { Get(track)?.ProcessEffect(left, right); }
            catch
            {
                // Keep the mix alive if a plug-in fails.
            }
        }
    }

    public void CaptureState(Track track)
    {
        lock (_gate)
        {
            try
            {
                string key = KeyFor(track);
                if (_instances.TryGetValue(key, out var inst) && inst.Plugin != null)
                {
                    byte[] state = inst.Plugin.SaveState() ?? Array.Empty<byte>();
                    if (state.Length > 0) track.PluginState = Convert.ToBase64String(state);
                }
            }
            catch { }
        }
    }

    public void CaptureAll(Project project)
    {
        foreach (var track in project.Tracks) CaptureState(track);
        if (project.Graph != null)
            foreach (var node in project.Graph.Nodes)
                if (node.HasPlugin && !node.PluginBypass) CaptureNodeState(node);
    }

    public void Trim(Project project)
    {
        lock (_gate)
        {
            var live = new HashSet<string>(project.Tracks.Select(KeyFor), StringComparer.OrdinalIgnoreCase);
            if (project.Graph != null)
                foreach (var node in project.Graph.Nodes)
                    if (node.HasPlugin) live.Add(KeyFor(NodeTrack(node)));
            foreach (var stale in _instances.Keys.Where(k => !live.Contains(k)).ToList())
            {
                try { _instances[stale].Dispose(); } catch { }
                _instances.Remove(stale);
            }
        }
    }

    public void Remove(Track track)
    {
        lock (_gate)
        {
            string key = KeyFor(track);
            if (_instances.Remove(key, out var inst))
            {
                try { inst.Dispose(); } catch { }
            }
        }
    }

    public System.Windows.Forms.Form? CreateEditor(Track track)
    {
        Vst3Instance? inst;
        lock (_gate)
        {
            try { inst = Get(track); }
            catch { return null; }
        }
        if (inst?.Plugin is not Vst3Plugin plugin) return null;

        Vst3PluginView view;
        Vst3ViewSize size;
        try
        {
            var created = plugin.CreateView();
            if (created == null) return null;
            view = created;
            size = view.GetSize();
        }
        catch
        {
            return null;
        }

        var area = System.Windows.Forms.Screen.PrimaryScreen?.WorkingArea ?? new System.Drawing.Rectangle(0, 0, 1440, 900);
        int winW = Math.Min(Math.Max(320, size.Width), Math.Max(320, area.Width - 40));
        int winH = Math.Min(Math.Max(200, size.Height), Math.Max(200, area.Height - 80));
        var form = new System.Windows.Forms.Form
        {
            Text = string.IsNullOrEmpty(track.PluginName) ? "VST3 editor" : track.PluginName,
            StartPosition = System.Windows.Forms.FormStartPosition.Manual,
            ClientSize = new System.Drawing.Size(winW, winH),
            MinimumSize = new System.Drawing.Size(320, 200),
            Location = new System.Drawing.Point(
                area.Left + Math.Max(0, (area.Width - winW) / 2),
                area.Top + Math.Max(0, (area.Height - winH) / 2))
        };
        var panel = new System.Windows.Forms.Panel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            BackColor = Theme.Back
        };
        form.Controls.Add(panel);
        form.Shown += (_, _) =>
        {
            try
            {
                var now = view.GetSize();
                form.ClientSize = new System.Drawing.Size(Math.Max(320, now.Width), Math.Max(200, now.Height));
                view.AttachTo(panel.Handle, 1f);
            }
            catch { }
        };
        view.Resized += (_, s) =>
        {
            try { form.ClientSize = new System.Drawing.Size(Math.Max(320, s.Width), Math.Max(200, s.Height)); } catch { }
        };
        form.FormClosed += (_, _) =>
        {
            try { view.Detach(); } catch { }
            try { view.Dispose(); } catch { }
            CaptureState(track);
        };
        return form;
    }

    private Vst3Instance? Get(Track track)
    {
        string key = KeyFor(track);
        if (_instances.TryGetValue(key, out var existing) && existing.IsValid) return existing;
        if (existing != null)
        {
            try { existing.Dispose(); } catch { }
            _instances.Remove(key);
        }
        var created = Vst3Instance.Create(track);
        if (created != null) _instances[key] = created;
        return created;
    }

    private static string KeyFor(Track track) =>
        track.Uid + "|" + track.PluginPath + "|" + track.PluginClass + "|" + (track.PluginInstrument ? "i" : "e");

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var inst in _instances.Values)
            {
                try { inst.Dispose(); } catch { }
            }
            _instances.Clear();
        }
    }
}

internal sealed class Vst3Instance : IDisposable
{
    public Vst3Module Module { get; private set; }
    public Vst3Plugin Plugin { get; private set; }
    public bool IsValid => Plugin != null;

    private Vst3Instance(Vst3Module module, Vst3Plugin plugin)
    {
        Module = module;
        Plugin = plugin;
    }

    public static Vst3Instance? Create(Track track)
    {
        Vst3Module? module = null;
        try
        {
            module = Vst3Module.Load(track.PluginPath);
            var classes = module.GetClasses().Where(c => c.IsAudioModule).ToList();
            Vst3ClassInfo? cls = null;
            if (!string.IsNullOrEmpty(track.PluginClass))
                cls = classes.FirstOrDefault(c => string.Equals(c.ClassId, track.PluginClass, StringComparison.OrdinalIgnoreCase));
            if (cls == null)
                cls = classes.FirstOrDefault(c => c.IsInstrument == track.PluginInstrument) ?? classes.FirstOrDefault();
            if (cls == null) { module.Dispose(); return null; }

            var plugin = module.CreatePlugin(cls, Dsp.SR, 512);
            if (!string.IsNullOrEmpty(track.PluginState))
            {
                try { plugin.LoadState(Convert.FromBase64String(track.PluginState)); } catch { }
            }
            return new Vst3Instance(module, plugin);
        }
        catch
        {
            try { module?.Dispose(); } catch { }
            return null;
        }
    }

    public float[][]? RenderInstrument(IReadOnlyList<PluginNote> notes, double secondsPerBeat, int frames, bool loop)
    {
        var plugin = Plugin;
        if (plugin == null || frames <= 0) return null;

        int inCh = Math.Max(1, plugin.InputChannelCount);
        int outCh = Math.Max(1, plugin.OutputChannelCount);
        int block = Math.Max(1, Math.Min(plugin.MaxBlockSize, 512));

        plugin.ResetEventSchedule();
        long maxEnd = 0;
        foreach (var note in notes)
        {
            long start = (long)Math.Round(note.StartBeat * secondsPerBeat * Dsp.SR);
            long len = Math.Max(1, (long)Math.Round(note.LenBeats * secondsPerBeat * Dsp.SR));
            if (loop && frames > 0) start %= frames;
            long end = start + len;
            plugin.ScheduleNoteOn(start, Math.Clamp(note.Pitch, 0, 127), Math.Clamp(note.Vel, 0, 127) / 127f, 0);
            plugin.ScheduleNoteOff(end, Math.Clamp(note.Pitch, 0, 127), 0f, 0);
            if (end > maxEnd) maxEnd = end;
        }

        long tail = Math.Max(0, plugin.TailSamples);
        long extra = Math.Max(0, maxEnd - frames) + tail;
        extra = Math.Min(extra, Dsp.SR * 8L);
        int total = frames + (int)extra;
        var left = new float[frames];
        var right = new float[frames];
        var input = new float[block * inCh];
        var output = new float[block * outCh];

        for (int pos = 0; pos < total; pos += block)
        {
            int n = Math.Min(block, total - pos);
            Array.Clear(input, 0, n * inCh);
            plugin.Process(input.AsSpan(0, n * inCh), output.AsSpan(0, n * outCh), n);
            for (int i = 0; i < n; i++)
            {
                int idx = pos + i;
                float l = output[i * outCh];
                float r = outCh > 1 ? output[i * outCh + 1] : l;
                if (loop && idx >= frames && frames > 0) idx %= frames;
                if (idx < frames)
                {
                    left[idx] += l;
                    right[idx] += r;
                }
            }
        }
        return new[] { left, right };
    }

    public void ProcessEffect(float[] left, float[] right)
    {
        var plugin = Plugin;
        if (plugin == null) return;
        if (plugin.InputChannelCount != 2 || plugin.OutputChannelCount != 2) return;

        int frames = Math.Min(left.Length, right.Length);
        int block = Math.Max(1, Math.Min(plugin.MaxBlockSize, 512));
        var input = new float[block * 2];
        var output = new float[block * 2];
        for (int pos = 0; pos < frames; pos += block)
        {
            int n = Math.Min(block, frames - pos);
            for (int i = 0; i < n; i++)
            {
                input[i * 2] = left[pos + i];
                input[i * 2 + 1] = right[pos + i];
            }
            plugin.Process(input.AsSpan(0, n * 2), output.AsSpan(0, n * 2), n);
            for (int i = 0; i < n; i++)
            {
                left[pos + i] = output[i * 2];
                right[pos + i] = output[i * 2 + 1];
            }
        }
    }

    public void Dispose()
    {
        try { Plugin?.Dispose(); } catch { }
        Plugin = null!;
        try { Module?.Dispose(); } catch { }
        Module = null!;
    }
}
