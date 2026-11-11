using System.Runtime.InteropServices;

namespace HumbleBakerStudio.Services;

internal interface IAudioSource
{
    /// <summary>Fills the frame buffers with stereo float samples in -1..1.</summary>
    void Fill(float[] left, float[] right, int frames);
}

/// <summary>
/// A small streaming waveOut output. One instance per physical output, so the live window can
/// send the master mix to the main device and the cue mix to a headphone device.
/// </summary>
internal sealed class WaveOutStream : IDisposable
{
    public const int Mapper = -1;

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveFormatEx
    {
        public short FormatTag;
        public short Channels;
        public int SamplesPerSec;
        public int AvgBytesPerSec;
        public short BlockAlign;
        public short BitsPerSample;
        public short Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHdr
    {
        public IntPtr Data;
        public int BufferLength;
        public int BytesRecorded;
        public IntPtr User;
        public int Flags;
        public int Loops;
        public IntPtr Next;
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WaveOutCaps
    {
        public ushort ManufacturerId;
        public ushort ProductId;
        public uint DriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Name;
        public uint Formats;
        public ushort Channels;
        public ushort Reserved;
        public uint Support;
    }

    [DllImport("winmm.dll")] private static extern int waveOutGetNumDevs();
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)] private static extern int waveOutGetDevCapsW(IntPtr device, ref WaveOutCaps caps, int size);
    [DllImport("winmm.dll")] private static extern int waveOutOpen(out IntPtr h, int device, ref WaveFormatEx fmt, IntPtr callback, IntPtr instance, int flags);
    [DllImport("winmm.dll")] private static extern int waveOutPrepareHeader(IntPtr h, IntPtr header, int size);
    [DllImport("winmm.dll")] private static extern int waveOutUnprepareHeader(IntPtr h, IntPtr header, int size);
    [DllImport("winmm.dll")] private static extern int waveOutWrite(IntPtr h, IntPtr header, int size);
    [DllImport("winmm.dll")] private static extern int waveOutReset(IntPtr h);
    [DllImport("winmm.dll")] private static extern int waveOutClose(IntPtr h);

    private const int WhdrDone = 0x1;
    private const int WhdrPrepared = 0x2;

    private sealed class Slot
    {
        public IntPtr Header;
        public IntPtr Data;
        public int CapacityFrames;
    }

    private readonly IAudioSource _source;
    private Thread? _thread;
    private volatile bool _stop;
    private IntPtr _device;
    private readonly int _deviceIndex;
    private readonly int _sampleRate;
    private readonly float[] _left = new float[2048];
    private readonly float[] _right = new float[2048];

    public bool IsRunning { get; private set; }
    public string LastError { get; private set; } = "";
    public int DeviceIndex => _deviceIndex;

    public WaveOutStream(IAudioSource source, int deviceIndex, int sampleRate = 44100)
    {
        _source = source;
        _deviceIndex = deviceIndex;
        _sampleRate = sampleRate;
    }

    public static string[] GetDevices()
    {
        int n = Math.Max(0, waveOutGetNumDevs());
        var names = new string[n];
        for (int i = 0; i < n; i++)
        {
            var caps = new WaveOutCaps();
            try
            {
                waveOutGetDevCapsW((IntPtr)i, ref caps, Marshal.SizeOf<WaveOutCaps>());
                names[i] = string.IsNullOrWhiteSpace(caps.Name) ? "Output " + (i + 1) : caps.Name;
            }
            catch
            {
                names[i] = "Output " + (i + 1);
            }
        }
        return names;
    }

    public bool Start()
    {
        Stop();
        _stop = false;
        IsRunning = true;
        _thread = new Thread(Run) { IsBackground = true, Name = "HBS live audio " + _deviceIndex, Priority = ThreadPriority.AboveNormal };
        _thread.Start();
        return true;
    }

    public void Stop()
    {
        _stop = true;
        var t = _thread;
        if (t != null && t.IsAlive) { try { t.Join(1200); } catch { } }
        _thread = null;
        CloseDevice();
        IsRunning = false;
    }

    private void CloseDevice()
    {
        if (_device == IntPtr.Zero) return;
        try { waveOutReset(_device); } catch { }
        try { waveOutClose(_device); } catch { }
        _device = IntPtr.Zero;
    }

    private void Run()
    {
        const int slots = 4;
        int framesPerBuffer = 1024;
        int headerSize = Marshal.SizeOf<WaveHdr>();
        var handles = new Slot[slots];
        var pending = new Queue<Slot>();
        try
        {
            var fmt = new WaveFormatEx
            {
                FormatTag = 1,
                Channels = 2,
                SamplesPerSec = _sampleRate,
                AvgBytesPerSec = _sampleRate * 4,
                BlockAlign = 4,
                BitsPerSample = 16
            };
            if (waveOutOpen(out _device, _deviceIndex, ref fmt, IntPtr.Zero, IntPtr.Zero, 0) != 0)
            {
                LastError = "Could not open audio output " + _deviceIndex;
                IsRunning = false;
                return;
            }

            for (int i = 0; i < slots; i++)
            {
                handles[i] = new Slot
                {
                    Header = Marshal.AllocHGlobal(headerSize),
                    Data = Marshal.AllocHGlobal(framesPerBuffer * 4),
                    CapacityFrames = framesPerBuffer
                };
                var wh = new WaveHdr();
                Marshal.StructureToPtr(wh, handles[i].Header, false);
            }

            while (!_stop)
            {
                while (pending.Count > 0)
                {
                    var slot = pending.Peek();
                    var hdr = Marshal.PtrToStructure<WaveHdr>(slot.Header);
                    if ((hdr.Flags & WhdrDone) == 0) break;
                    pending.Dequeue();
                    waveOutUnprepareHeader(_device, slot.Header, headerSize);
                    hdr.Flags &= ~WhdrPrepared;
                    Marshal.StructureToPtr(hdr, slot.Header, false);
                }

                Slot? free = null;
                for (int i = 0; i < slots; i++)
                {
                    if ((Marshal.PtrToStructure<WaveHdr>(handles[i].Header).Flags & WhdrPrepared) == 0)
                    {
                        free = handles[i];
                        break;
                    }
                }
                if (free == null) { Thread.Sleep(4); continue; }

                int frames = Math.Min(free.CapacityFrames, _left.Length);
                Array.Clear(_left, 0, frames);
                Array.Clear(_right, 0, frames);
                try { _source.Fill(_left, _right, frames); } catch { }

                var data = new short[frames * 2];
                for (int i = 0; i < frames; i++)
                {
                    data[2 * i] = (short)(Math.Clamp(_left[i], -1, 1) * 32767);
                    data[2 * i + 1] = (short)(Math.Clamp(_right[i], -1, 1) * 32767);
                }
                Marshal.Copy(data, 0, free.Data, data.Length);

                var header = new WaveHdr { Data = free.Data, BufferLength = frames * 4 };
                Marshal.StructureToPtr(header, free.Header, false);
                if (waveOutPrepareHeader(_device, free.Header, headerSize) != 0) continue;
                if (waveOutWrite(_device, free.Header, headerSize) != 0) continue;
                pending.Enqueue(free);
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        finally
        {
            CloseDevice();
            foreach (var s in handles)
            {
                if (s == null) continue;
                if (s.Header != IntPtr.Zero) Marshal.FreeHGlobal(s.Header);
                if (s.Data != IntPtr.Zero) Marshal.FreeHGlobal(s.Data);
            }
            IsRunning = false;
        }
    }

    public void Dispose() => Stop();
}
