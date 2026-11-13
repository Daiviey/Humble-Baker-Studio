using NAudio.Vst3;

namespace HumbleBakerStudio.VstBridge;

public sealed class BridgeNote
{
    public double StartBeat;
    public double LenBeats;
    public int Pitch;
    public int Vel;
}

public sealed class BridgePluginInfo
{
    public string Name = "";
    public string Vendor = "";
    public string Path = "";
    public string ClassId = "";
    public bool IsInstrument;
}

/// <summary>
/// Owns the VST3 modules and instances for the PowerShell studio. The audio engine talks to this
/// class through a named pipe; the process boundary keeps plug-in crashes out of the studio.
/// </summary>
public sealed class BridgeHost : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Instance> _instances = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<BridgePluginInfo> Scan()
    {
        var found = new List<BridgePluginInfo>();
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
                    found.Add(new BridgePluginInfo
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
                // One broken, 32-bit or unlicensed module must not stop the scan.
            }
        }
        found.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return found;
    }

    public float[][] ProcessEffect(string path, string classId, int sampleRate, byte[] state, bool wantState, float[] left, float[] right, out byte[]? newState)
    {
        lock (_gate)
        {
            var instance = Get(path, classId, sampleRate, state);
            if (instance.Plugin.InputChannelCount != 2 || instance.Plugin.OutputChannelCount != 2)
                throw new InvalidOperationException("The plug-in is not a stereo effect (2 in / 2 out).");
            instance.ProcessEffect(left, right);
            newState = wantState ? instance.Plugin.SaveState() : null;
            return new[] { left, right };
        }
    }

    public float[][] RenderInstrument(string path, string classId, int sampleRate, byte[] state, bool wantState, IReadOnlyList<BridgeNote> notes, double secondsPerBeat, int frames, bool loop, out byte[]? newState)
    {
        lock (_gate)
        {
            var instance = Get(path, classId, sampleRate, state);
            var result = instance.RenderInstrument(notes, secondsPerBeat, frames, loop);
            newState = wantState ? instance.Plugin.SaveState() : null;
            return result;
        }
    }

    public byte[] Capture(string path, string classId, int sampleRate)
    {
        lock (_gate)
        {
            var instance = Get(path, classId, sampleRate, Array.Empty<byte>());
            return instance.Plugin.SaveState() ?? Array.Empty<byte>();
        }
    }

    public void Release(string path, string classId)
    {
        lock (_gate)
        {
            string key = KeyFor(path, classId);
            if (_instances.Remove(key, out var instance)) instance.Dispose();
        }
    }

    private Instance Get(string path, string classId, int sampleRate, byte[] state)
    {
        string key = KeyFor(path, classId);
        if (_instances.TryGetValue(key, out var existing))
        {
            if (existing.IsValid && existing.SampleRate == sampleRate) return existing;
            existing.Dispose();
            _instances.Remove(key);
        }
        var created = Instance.Create(path, classId, sampleRate, state);
        _instances[key] = created;
        return created;
    }

    private static string KeyFor(string path, string classId) => path + "|" + classId;

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var instance in _instances.Values)
            {
                try { instance.Dispose(); } catch { }
            }
            _instances.Clear();
        }
    }

    private sealed class Instance : IDisposable
    {
        public Vst3Module Module { get; private set; }
        public Vst3Plugin Plugin { get; private set; }
        public int SampleRate { get; private set; }
        public bool IsValid => Plugin != null;

        private Instance(Vst3Module module, Vst3Plugin plugin, int sampleRate)
        {
            Module = module;
            Plugin = plugin;
            SampleRate = sampleRate;
        }

        public static Instance Create(string path, string classId, int sampleRate, byte[] state)
        {
            Vst3Module? module = null;
            try
            {
                module = Vst3Module.Load(path);
                var classes = module.GetClasses().Where(c => c.IsAudioModule).ToList();
                Vst3ClassInfo? cls = classes.FirstOrDefault(c => string.Equals(c.ClassId, classId, StringComparison.OrdinalIgnoreCase))
                    ?? classes.FirstOrDefault();
                if (cls == null) throw new InvalidOperationException("The plug-in exposes no audio class.");

                var plugin = module.CreatePlugin(cls, sampleRate, 512);
                if (state.Length > 0)
                {
                    try { plugin.LoadState(state); } catch { }
                }
                return new Instance(module, plugin, sampleRate);
            }
            catch
            {
                try { module?.Dispose(); } catch { }
                throw;
            }
        }

        public float[][] RenderInstrument(IReadOnlyList<BridgeNote> notes, double secondsPerBeat, int frames, bool loop)
        {
            var plugin = Plugin;
            if (frames <= 0) return new[] { Array.Empty<float>(), Array.Empty<float>() };

            int inCh = Math.Max(1, plugin.InputChannelCount);
            int outCh = Math.Max(1, plugin.OutputChannelCount);
            int block = Math.Max(1, Math.Min(plugin.MaxBlockSize, 512));

            plugin.ResetEventSchedule();
            long maxEnd = 0;
            foreach (var note in notes)
            {
                long start = (long)Math.Round(note.StartBeat * secondsPerBeat * SampleRate);
                long len = Math.Max(1, (long)Math.Round(note.LenBeats * secondsPerBeat * SampleRate));
                if (loop && frames > 0) start %= frames;
                long end = start + len;
                plugin.ScheduleNoteOn(start, Math.Clamp(note.Pitch, 0, 127), Math.Clamp(note.Vel, 0, 127) / 127f, 0);
                plugin.ScheduleNoteOff(end, Math.Clamp(note.Pitch, 0, 127), 0f, 0);
                if (end > maxEnd) maxEnd = end;
            }

            long tail = Math.Max(0, plugin.TailSamples);
            long extra = Math.Max(0, maxEnd - frames) + tail;
            extra = Math.Min(extra, SampleRate * 8L);
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
}
