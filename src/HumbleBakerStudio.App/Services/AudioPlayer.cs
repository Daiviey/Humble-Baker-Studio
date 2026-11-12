using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HumbleBakerStudio.Services;

/// <summary>
/// Minimal WASAPI-free playback through winmm (waveOut). Keeping this in-house means the studio
/// needs no audio NuGet package: only what Windows already ships.
/// </summary>
internal sealed class AudioPlayer : IDisposable
{
    private const int WaveMapper = -1;
    private const int WhdrDone = 0x00000001;
    private const int WhdrPrepared = 0x00000002;

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

    [DllImport("winmm.dll")] private static extern int waveOutOpen(out IntPtr h, int device, ref WaveFormatEx fmt, IntPtr callback, IntPtr instance, int flags);
    [DllImport("winmm.dll")] private static extern int waveOutPrepareHeader(IntPtr h, IntPtr header, int size);
    [DllImport("winmm.dll")] private static extern int waveOutUnprepareHeader(IntPtr h, IntPtr header, int size);
    [DllImport("winmm.dll")] private static extern int waveOutWrite(IntPtr h, IntPtr header, int size);
    [DllImport("winmm.dll")] private static extern int waveOutReset(IntPtr h);
    [DllImport("winmm.dll")] private static extern int waveOutClose(IntPtr h);
    [DllImport("winmm.dll")] private static extern int waveOutPause(IntPtr h);
    [DllImport("winmm.dll")] private static extern int waveOutRestart(IntPtr h);

    private sealed class Slot
    {
        public IntPtr Header;
        public IntPtr Data;
        public int Capacity;
        public int Frames;
    }

    private readonly object _gate = new();
    private Thread? _thread;
    private volatile bool _stop;
    private volatile bool _paused;
    private IntPtr _device;
    private long _doneFrames;
    private int _sampleRate = 44100;
    private int _totalFrames;
    private short[] _pcm = Array.Empty<short>();
    private volatile bool _loop;

    public bool IsPlaying { get; private set; }
    public event Action? Finished;

    public TimeSpan Position
    {
        get
        {
            long frames = Interlocked.Read(ref _doneFrames);
            if (_loop && _totalFrames > 0) frames %= _totalFrames;
            return TimeSpan.FromSeconds(_sampleRate > 0 ? (double)frames / _sampleRate : 0);
        }
    }

    public TimeSpan Duration => TimeSpan.FromSeconds(_sampleRate > 0 ? (double)_totalFrames / _sampleRate : 0);

    public void Play(short[] interleavedStereo, int sampleRate = 44100, bool loop = false)
    {
        Stop();
        if (interleavedStereo == null || interleavedStereo.Length < 4) return;

        _sampleRate = sampleRate;
        _pcm = interleavedStereo;
        _totalFrames = interleavedStereo.Length / 2;
        _loop = loop;
        Interlocked.Exchange(ref _doneFrames, 0);
        _stop = false;
        _paused = false;
        IsPlaying = true;
        _thread = new Thread(Run) { IsBackground = true, Name = "HBS audio", Priority = ThreadPriority.AboveNormal };
        _thread.Start();
    }

    /// <summary>Swaps in a freshly rendered loop without restarting playback. Apply it at a loop boundary.</summary>
    public void ReplaceBuffer(short[] interleavedStereo)
    {
        if (interleavedStereo == null || interleavedStereo.Length < 4) return;
        _pcm = interleavedStereo;
        _totalFrames = interleavedStereo.Length / 2;
    }

    public void Pause()
    {
        if (!IsPlaying || _paused || _device == IntPtr.Zero) return;
        _paused = true;
        waveOutPause(_device);
    }

    public void Resume()
    {
        if (!_paused || _device == IntPtr.Zero) return;
        _paused = false;
        waveOutRestart(_device);
    }

    public void Stop()
    {
        _stop = true;
        var t = _thread;
        if (t != null && t.IsAlive)
        {
            try { t.Join(1500); } catch { }
        }
        _thread = null;
        CloseDevice();
        IsPlaying = false;
        _paused = false;
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
        // Four short buffers keep latency low while giving the driver enough work to stay smooth.
        const int bufferCount = 4;
        int framesPerBuffer = Math.Max(256, _sampleRate / 20); // ~50 ms
        var slots = new Slot[bufferCount];
        int headerSize = Marshal.SizeOf<WaveHdr>();

        try
        {
            var fmt = new WaveFormatEx
            {
                FormatTag = 1,
                Channels = 2,
                SamplesPerSec = _sampleRate,
                AvgBytesPerSec = _sampleRate * 4,
                BlockAlign = 4,
                BitsPerSample = 16,
                Size = 0
            };
            if (waveOutOpen(out _device, WaveMapper, ref fmt, IntPtr.Zero, IntPtr.Zero, 0) != 0)
            {
                IsPlaying = false;
                return;
            }

            for (int i = 0; i < bufferCount; i++)
            {
                slots[i] = new Slot
                {
                    Header = Marshal.AllocHGlobal(headerSize),
                    Data = Marshal.AllocHGlobal(framesPerBuffer * 4),
                    Capacity = framesPerBuffer
                };
                ZeroHeader(slots[i]);
            }

            int nextFrame = 0;
            var pending = new Queue<Slot>();
            long finishedFrames = 0;

            while (!_stop)
            {
                // Reclaim any finished buffers.
                while (pending.Count > 0)
                {
                    var slot = pending.Peek();
                    var hdr = Marshal.PtrToStructure<WaveHdr>(slot.Header);
                    if ((hdr.Flags & WhdrDone) == 0) break;
                    pending.Dequeue();
                    waveOutUnprepareHeader(_device, slot.Header, headerSize);
                    hdr.Flags &= ~WhdrPrepared;
                    Marshal.StructureToPtr(hdr, slot.Header, false);
                    finishedFrames += slot.Frames;
                    Interlocked.Exchange(ref _doneFrames, finishedFrames);
                    slot.Frames = 0;
                }

                var pcm = _pcm;
                int totalFrames = pcm.Length / 2;
                if (totalFrames < 2)
                {
                    Thread.Sleep(4);
                    continue;
                }

                if (nextFrame >= totalFrames)
                {
                    if (_loop) nextFrame = 0;
                    else if (pending.Count == 0) break;
                    else { Thread.Sleep(4); continue; }
                }

                Slot? free = null;
                for (int i = 0; i < slots.Length; i++)
                {
                    if ((Marshal.PtrToStructure<WaveHdr>(slots[i].Header).Flags & WhdrPrepared) == 0) { free = slots[i]; break; }
                }
                if (free == null)
                {
                    Thread.Sleep(4);
                    continue;
                }

                int frames = Math.Min(free.Capacity, totalFrames - nextFrame);
                Marshal.Copy(pcm, nextFrame * 2, free.Data, frames * 2);
                var wh = new WaveHdr
                {
                    Data = free.Data,
                    BufferLength = frames * 4,
                    Flags = 0
                };
                Marshal.StructureToPtr(wh, free.Header, false);
                if (waveOutPrepareHeader(_device, free.Header, headerSize) != 0)
                {
                    Thread.Sleep(4);
                    continue;
                }
                if (waveOutWrite(_device, free.Header, headerSize) != 0)
                {
                    Thread.Sleep(4);
                    continue;
                }
                free.Frames = frames;
                pending.Enqueue(free);
                nextFrame += frames;
            }

            // Let the tail play out unless the user stopped us.
            var sw = Stopwatch.StartNew();
            while (!_stop && pending.Count > 0 && sw.ElapsedMilliseconds < 5000)
            {
                var slot = pending.Peek();
                var hdr = Marshal.PtrToStructure<WaveHdr>(slot.Header);
                if ((hdr.Flags & WhdrDone) == 0) { Thread.Sleep(4); continue; }
                pending.Dequeue();
                waveOutUnprepareHeader(_device, slot.Header, headerSize);
                finishedFrames += slot.Frames;
                Interlocked.Exchange(ref _doneFrames, finishedFrames);
                slot.Frames = 0;
            }
        }
        catch
        {
            // Playback failures should not take the UI down.
        }
        finally
        {
            CloseDevice();
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                if (slots[i].Header != IntPtr.Zero) Marshal.FreeHGlobal(slots[i].Header);
                if (slots[i].Data != IntPtr.Zero) Marshal.FreeHGlobal(slots[i].Data);
                slots[i].Header = IntPtr.Zero;
                slots[i].Data = IntPtr.Zero;
            }
            bool stopped = _stop;
            IsPlaying = false;
            if (!stopped)
            {
                try { Finished?.Invoke(); } catch { }
            }
        }
    }

    private static void ZeroHeader(Slot slot)
    {
        var wh = new WaveHdr();
        Marshal.StructureToPtr(wh, slot.Header, false);
    }

    public void Dispose()
    {
        Stop();
    }
}
