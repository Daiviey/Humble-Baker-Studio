using System.Runtime.InteropServices;
using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Services;

/// <summary>
/// Drives the live mixer and writes master and cue chunks to one or two waveOut devices.
/// The master device paces the mix, so both decks advance exactly once per rendered chunk.
/// </summary>
internal sealed class LiveAudioEngine : IDisposable
{
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

    private const int WhdrPrepared = 0x2;
    private const int WhdrDone = 0x1;
    private const int Frames = 1024;
    private const int Slots = 3;

    private sealed class Slot
    {
        public IntPtr Header;
        public IntPtr Data;
    }

    private readonly LiveMixer _mixer;
    private readonly float[] _mL = new float[Frames], _mR = new float[Frames], _cL = new float[Frames], _cR = new float[Frames];
    private readonly Queue<short[]> _cueQueue = new();
    private Thread? _thread;
    private volatile bool _stop;
    private IntPtr _master, _cue;
    private bool _cueOn;

    public string LastError { get; private set; } = "";
    public bool IsRunning { get; private set; }
    public int SampleRate { get; } = Dsp.SR;

    public LiveAudioEngine(LiveMixer mixer) => _mixer = mixer;

    public bool Start(int masterDevice, int cueDevice)
    {
        Stop();
        LastError = "";
        _stop = false;
        _thread = new Thread(() => Run(masterDevice, cueDevice)) { IsBackground = true, Name = "HBS live engine", Priority = ThreadPriority.AboveNormal };
        IsRunning = true;
        _thread.Start();
        return true;
    }

    public void Stop()
    {
        _stop = true;
        var t = _thread;
        if (t != null && t.IsAlive) { try { t.Join(1500); } catch { } }
        _thread = null;
        CloseAll();
        IsRunning = false;
    }

    private void CloseAll()
    {
        if (_master != IntPtr.Zero) { try { waveOutReset(_master); } catch { } try { waveOutClose(_master); } catch { } _master = IntPtr.Zero; }
        if (_cue != IntPtr.Zero) { try { waveOutReset(_cue); } catch { } try { waveOutClose(_cue); } catch { } _cue = IntPtr.Zero; }
        lock (_cueQueue) _cueQueue.Clear();
    }

    private void Run(int masterDevice, int cueDevice)
    {
        int headerSize = Marshal.SizeOf<WaveHdr>();
        Slot[] masterSlots = CreateSlots(headerSize);
        Slot[] cueSlots = CreateSlots(headerSize);
        var fmt = new WaveFormatEx
        {
            FormatTag = 1,
            Channels = 2,
            SamplesPerSec = SampleRate,
            AvgBytesPerSec = SampleRate * 4,
            BlockAlign = 4,
            BitsPerSample = 16
        };

        try
        {
            if (waveOutOpen(out _master, masterDevice < 0 ? -1 : masterDevice, ref fmt, IntPtr.Zero, IntPtr.Zero, 0) != 0)
            {
                LastError = "Could not open the master output.";
                IsRunning = false;
                return;
            }

            _cueOn = cueDevice >= 0 && cueDevice != masterDevice;
            if (_cueOn && waveOutOpen(out _cue, cueDevice, ref fmt, IntPtr.Zero, IntPtr.Zero, 0) != 0)
            {
                _cueOn = false;
                LastError = "Cue output unavailable; cue is silent.";
            }

            while (!_stop)
            {
                Reclaim(masterSlots, _master, headerSize);
                if (_cueOn) Reclaim(cueSlots, _cue, headerSize);

                Slot? free = FreeSlot(masterSlots, headerSize);
                if (free == null) { Thread.Sleep(2); continue; }

                _mixer.Fill(_mL, _mR, _cL, _cR, Frames);
                var masterPcm = ToPcm(_mL, _mR);
                Write(_master, free, masterPcm, headerSize);

                if (_cueOn)
                {
                    lock (_cueQueue)
                    {
                        _cueQueue.Enqueue(ToPcm(_cL, _cR));
                        while (_cueQueue.Count > Slots + 1) _cueQueue.Dequeue();
                    }
                    var cueFree = FreeSlot(cueSlots, headerSize);
                    short[]? cuePcm = null;
                    lock (_cueQueue)
                    {
                        if (cueFree != null && _cueQueue.Count > 0) cuePcm = _cueQueue.Dequeue();
                    }
                    if (cueFree != null && cuePcm != null) Write(_cue, cueFree, cuePcm, headerSize);
                }
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        finally
        {
            CloseAll();
            FreeSlots(masterSlots);
            FreeSlots(cueSlots);
            IsRunning = false;
        }
    }

    private static Slot[] CreateSlots(int headerSize)
    {
        var slots = new Slot[Slots];
        for (int i = 0; i < Slots; i++)
        {
            slots[i] = new Slot
            {
                Header = Marshal.AllocHGlobal(headerSize),
                Data = Marshal.AllocHGlobal(Frames * 4)
            };
            var wh = new WaveHdr();
            Marshal.StructureToPtr(wh, slots[i].Header, false);
        }
        return slots;
    }

    private static void FreeSlots(Slot[] slots)
    {
        foreach (var s in slots)
        {
            if (s.Header != IntPtr.Zero) Marshal.FreeHGlobal(s.Header);
            if (s.Data != IntPtr.Zero) Marshal.FreeHGlobal(s.Data);
            s.Header = s.Data = IntPtr.Zero;
        }
    }

    private static void Reclaim(Slot[] slots, IntPtr device, int headerSize)
    {
        if (device == IntPtr.Zero) return;
        foreach (var slot in slots)
        {
            var hdr = Marshal.PtrToStructure<WaveHdr>(slot.Header);
            if ((hdr.Flags & WhdrDone) == 0) continue;
            waveOutUnprepareHeader(device, slot.Header, headerSize);
            hdr.Flags &= ~WhdrPrepared;
            Marshal.StructureToPtr(hdr, slot.Header, false);
        }
    }

    private static Slot? FreeSlot(Slot[] slots, int headerSize)
    {
        foreach (var slot in slots)
        {
            var hdr = Marshal.PtrToStructure<WaveHdr>(slot.Header);
            if ((hdr.Flags & WhdrPrepared) == 0) return slot;
        }
        return null;
    }

    private static short[] ToPcm(float[] l, float[] r)
    {
        var pcm = new short[l.Length * 2];
        for (int i = 0; i < l.Length; i++)
        {
            pcm[2 * i] = (short)(Math.Clamp(l[i], -1f, 1f) * 32767);
            pcm[2 * i + 1] = (short)(Math.Clamp(r[i], -1f, 1f) * 32767);
        }
        return pcm;
    }

    private static void Write(IntPtr device, Slot slot, short[] pcm, int headerSize)
    {
        Marshal.Copy(pcm, 0, slot.Data, pcm.Length);
        var hdr = new WaveHdr { Data = slot.Data, BufferLength = pcm.Length * 2 };
        Marshal.StructureToPtr(hdr, slot.Header, false);
        if (waveOutPrepareHeader(device, slot.Header, headerSize) != 0) return;
        waveOutWrite(device, slot.Header, headerSize);
    }

    public void Dispose() => Stop();
}
