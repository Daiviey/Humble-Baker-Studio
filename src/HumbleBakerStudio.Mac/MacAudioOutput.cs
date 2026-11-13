using System.Runtime.InteropServices;

namespace HumbleBakerStudio.Mac;

/// <summary>
/// CoreAudio playback through AudioQueue. The engine renders a loop offline (as it does for
/// export); this class streams the PCM buffer and loops it. The AudioQueue callback runs on an
/// internal CoreAudio thread, so the shared position is guarded by a lock.
/// </summary>
public sealed class MacAudioOutput : IDisposable
{
    private const string AudioToolbox = "/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox";
    private const uint LinearPcm = 0x6C70636D; // 'lpcm'
    private const uint SignedPacked16 = 0x0000000C;
    private const int BufferBytes = 65536;     // 16384 stereo frames

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioStreamBasicDescription
    {
        public double mSampleRate;
        public uint mFormatID;
        public uint mFormatFlags;
        public uint mBytesPerPacket;
        public uint mFramesPerPacket;
        public uint mBytesPerFrame;
        public uint mChannelsPerFrame;
        public uint mBitsPerChannel;
        public uint mReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioQueueBuffer
    {
        public uint mAudioDataBytesCapacity;
        public IntPtr mAudioData;
        public uint mAudioDataByteSize;
        public IntPtr mUserData;
        public uint mPacketDescriptionCapacity;
        public IntPtr mPacketDescriptions;
        public uint mPacketDescriptionCount;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AudioQueueOutputCallback(IntPtr userData, IntPtr audioQueue, IntPtr buffer);

    [DllImport(AudioToolbox)]
    private static extern int AudioQueueNewOutput(ref AudioStreamBasicDescription format,
        AudioQueueOutputCallback callback, IntPtr userData, IntPtr callbackRunLoop,
        IntPtr callbackRunLoopMode, uint flags, out IntPtr audioQueue);

    [DllImport(AudioToolbox)]
    private static extern int AudioQueueAllocateBuffer(IntPtr audioQueue, uint bufferByteSize, out IntPtr buffer);

    [DllImport(AudioToolbox)]
    private static extern int AudioQueueEnqueueBuffer(IntPtr audioQueue, IntPtr buffer,
        uint numPacketDescs, IntPtr packetDescs);

    [DllImport(AudioToolbox)]
    private static extern int AudioQueueStart(IntPtr audioQueue, IntPtr startTime);

    [DllImport(AudioToolbox)]
    private static extern int AudioQueueStop(IntPtr audioQueue, bool immediate);

    [DllImport(AudioToolbox)]
    private static extern int AudioQueueDispose(IntPtr audioQueue, bool immediate);

    private static readonly AudioQueueOutputCallback Callback = OnBuffer;
    private static readonly short[] Silence = new short[BufferBytes / 2];

    private readonly object _gate = new();
    private readonly GCHandle _self;
    private IntPtr _queue;
    private short[]? _pcm;
    private int _position;
    private bool _loop;
    private int _sampleRate = 44100;

    public MacAudioOutput()
    {
        _self = GCHandle.Alloc(this);
    }

    public bool IsPlaying => _queue != IntPtr.Zero;

    public void Play(short[] pcm, int sampleRate = 44100, bool loop = true)
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("CoreAudio playback is only available on macOS.");
        if (pcm.Length == 0) return;

        Stop();
        lock (_gate)
        {
            _pcm = pcm;
            _position = 0;
            _loop = loop;
            _sampleRate = sampleRate;
        }

        var format = new AudioStreamBasicDescription
        {
            mSampleRate = sampleRate,
            mFormatID = LinearPcm,
            mFormatFlags = SignedPacked16,
            mBytesPerPacket = 4,
            mFramesPerPacket = 1,
            mBytesPerFrame = 4,
            mChannelsPerFrame = 2,
            mBitsPerChannel = 16,
            mReserved = 0
        };

        int status = AudioQueueNewOutput(ref format, Callback, GCHandle.ToIntPtr(_self),
            IntPtr.Zero, IntPtr.Zero, 0, out _queue);
        if (status != 0)
        {
            _queue = IntPtr.Zero;
            throw new InvalidOperationException("AudioQueueNewOutput failed: " + status);
        }

        for (int i = 0; i < 3; i++)
        {
            status = AudioQueueAllocateBuffer(_queue, BufferBytes, out IntPtr buffer);
            if (status != 0) throw new InvalidOperationException("AudioQueueAllocateBuffer failed: " + status);
            Fill(buffer, _queue);
            status = AudioQueueEnqueueBuffer(_queue, buffer, 0, IntPtr.Zero);
            if (status != 0) throw new InvalidOperationException("AudioQueueEnqueueBuffer failed: " + status);
        }

        status = AudioQueueStart(_queue, IntPtr.Zero);
        if (status != 0) throw new InvalidOperationException("AudioQueueStart failed: " + status);
    }

    public void Stop()
    {
        IntPtr queue = _queue;
        _queue = IntPtr.Zero;
        if (queue != IntPtr.Zero)
        {
            try { AudioQueueStop(queue, true); } catch { }
            try { AudioQueueDispose(queue, true); } catch { }
        }
        lock (_gate)
        {
            _pcm = null;
            _position = 0;
        }
    }

    private static void OnBuffer(IntPtr userData, IntPtr audioQueue, IntPtr buffer)
    {
        try
        {
            var output = GCHandle.FromIntPtr(userData).Target as MacAudioOutput;
            output?.Fill(buffer, audioQueue);
        }
        catch
        {
            // Never let an exception cross the unmanaged AudioQueue callback.
        }
    }

    private void Fill(IntPtr bufferPtr, IntPtr audioQueue)
    {
        var buffer = Marshal.PtrToStructure<AudioQueueBuffer>(bufferPtr);
        int samples = (int)buffer.mAudioDataBytesCapacity / 2;
        int written = 0;

        lock (_gate)
        {
            if (_pcm is { Length: > 0 })
            {
                while (written < samples)
                {
                    if (_position >= _pcm.Length)
                    {
                        if (!_loop) break;
                        _position = 0;
                    }
                    int available = _pcm.Length - _position;
                    int take = Math.Min(samples - written, available);
                    Marshal.Copy(_pcm, _position, IntPtr.Add(buffer.mAudioData, written * 2), take);
                    _position += take;
                    written += take;
                }
            }
        }

        if (written < samples)
        {
            int remaining = samples - written;
            Marshal.Copy(Silence, 0, IntPtr.Add(buffer.mAudioData, written * 2), remaining);
        }

        buffer.mAudioDataByteSize = (uint)(samples * 2);
        Marshal.StructureToPtr(buffer, bufferPtr, false);
        if (audioQueue != IntPtr.Zero)
            AudioQueueEnqueueBuffer(audioQueue, bufferPtr, 0, IntPtr.Zero);
    }

    public void Dispose()
    {
        Stop();
        if (_self.IsAllocated) _self.Free();
    }
}
