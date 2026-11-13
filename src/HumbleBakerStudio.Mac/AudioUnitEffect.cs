using System.Runtime.InteropServices;

namespace HumbleBakerStudio.Mac;

/// <summary>
/// Offline Audio Unit effect host (AUv2). The app renders a loop, sends it through the unit in
/// 1024-frame blocks and plays/exports the result. Instantiation and rendering can only be tested
/// on macOS; callers catch failures and keep the dry signal.
/// </summary>
public sealed class AudioUnitEffect : IDisposable
{
    private const string AudioToolbox = "/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox";
    private const uint LinearPcm = 0x6C70636D;
    private const uint FloatPackedNonInterleaved = 1 | 8 | 32;
    private const uint ScopeInput = 1;
    private const uint ScopeOutput = 2;
    private const uint PropertyStreamFormat = 8;
    private const uint PropertySetRenderCallback = 23;
    private const int BlockFrames = 1024;

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioComponentDescription
    {
        public uint ComponentType;
        public uint ComponentSubType;
        public uint ComponentManufacturer;
        public uint ComponentFlags;
        public uint ComponentFlagsMask;
    }

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
    private struct AudioBuffer
    {
        public uint mNumberChannels;
        public uint mDataByteSize;
        public IntPtr mData;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AURenderCallbackStruct
    {
        public IntPtr inputProc;
        public IntPtr inputProcRefCon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SmpteTime
    {
        public short mSubframes;
        public short mSubframeDivisor;
        public uint mCounter;
        public uint mType;
        public uint mFlags;
        public short mHours;
        public short mMinutes;
        public short mSeconds;
        public short mFrames;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioTimeStamp
    {
        public double mSampleTime;
        public uint mFlags;
        public uint mReserved;
        public double mRateScalar;
        public ulong mWordClockTime;
        public SmpteTime mSMPTETime;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int AURenderCallback(IntPtr refCon, IntPtr actionFlags, IntPtr timeStamp,
        uint bus, uint frames, IntPtr ioData);

    [DllImport(AudioToolbox)]
    private static extern IntPtr AudioComponentFindNext(IntPtr component, ref AudioComponentDescription desc);

    [DllImport(AudioToolbox)]
    private static extern int AudioComponentInstanceNew(IntPtr component, out IntPtr unit);

    [DllImport(AudioToolbox)]
    private static extern int AudioUnitSetProperty(IntPtr unit, uint property, uint scope, uint element,
        ref AudioStreamBasicDescription data, uint size);

    [DllImport(AudioToolbox)]
    private static extern int AudioUnitSetProperty(IntPtr unit, uint property, uint scope, uint element,
        ref AURenderCallbackStruct data, uint size);

    [DllImport(AudioToolbox)]
    private static extern int AudioUnitInitialize(IntPtr unit);

    [DllImport(AudioToolbox)]
    private static extern int AudioUnitUninitialize(IntPtr unit);

    [DllImport(AudioToolbox)]
    private static extern int AudioComponentInstanceDispose(IntPtr unit);

    [DllImport(AudioToolbox)]
    private static extern int AudioUnitRender(IntPtr unit, ref uint actionFlags, ref AudioTimeStamp timeStamp,
        uint outputBus, uint frames, IntPtr ioData);

    private static readonly AURenderCallback InputCallback = OnInput;

    private readonly object _gate = new();
    private readonly GCHandle _self;
    private IntPtr _unit;
    private IntPtr _inputL;
    private IntPtr _inputR;
    private int _inputFrames;
    private long _processedFrames;

    public string Name { get; }
    public int SampleRate { get; }

    private AudioUnitEffect(IntPtr unit, string name, int sampleRate)
    {
        _unit = unit;
        Name = name;
        SampleRate = sampleRate;
        _self = GCHandle.Alloc(this);
    }

    public static AudioUnitEffect Create(AudioUnitInfo info, int sampleRate = 44100)
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("Audio Units are only available on macOS.");
        if (!info.IsEffect)
            throw new InvalidOperationException("Only AU effects are hosted here.");

        var desc = new AudioComponentDescription
        {
            ComponentType = info.ComponentType,
            ComponentSubType = info.SubType,
            ComponentManufacturer = info.ManufacturerCode
        };
        IntPtr component = AudioComponentFindNext(IntPtr.Zero, ref desc);
        if (component == IntPtr.Zero) throw new InvalidOperationException("Audio Unit not found: " + info.Name);

        int status = AudioComponentInstanceNew(component, out IntPtr unit);
        if (status != 0) throw new InvalidOperationException("AudioComponentInstanceNew failed: " + status);

        var effect = new AudioUnitEffect(unit, info.Name, sampleRate);
        try
        {
            effect.Configure();
            return effect;
        }
        catch
        {
            effect.Dispose();
            throw;
        }
    }

    private void Configure()
    {
        var format = new AudioStreamBasicDescription
        {
            mSampleRate = SampleRate,
            mFormatID = LinearPcm,
            mFormatFlags = FloatPackedNonInterleaved,
            mBytesPerPacket = 4,
            mFramesPerPacket = 1,
            mBytesPerFrame = 4,
            mChannelsPerFrame = 2,
            mBitsPerChannel = 32,
            mReserved = 0
        };

        int status = AudioUnitSetProperty(_unit, PropertyStreamFormat, ScopeInput, 0, ref format,
            (uint)Marshal.SizeOf<AudioStreamBasicDescription>());
        if (status != 0) throw new InvalidOperationException("AU input stream format failed: " + status);

        status = AudioUnitSetProperty(_unit, PropertyStreamFormat, ScopeOutput, 0, ref format,
            (uint)Marshal.SizeOf<AudioStreamBasicDescription>());
        if (status != 0) throw new InvalidOperationException("AU output stream format failed: " + status);

        var callback = new AURenderCallbackStruct
        {
            inputProc = Marshal.GetFunctionPointerForDelegate(InputCallback),
            inputProcRefCon = GCHandle.ToIntPtr(_self)
        };
        status = AudioUnitSetProperty(_unit, PropertySetRenderCallback, ScopeInput, 0, ref callback,
            (uint)Marshal.SizeOf<AURenderCallbackStruct>());
        if (status != 0) throw new InvalidOperationException("AU input callback failed: " + status);

        status = AudioUnitInitialize(_unit);
        if (status != 0) throw new InvalidOperationException("AudioUnitInitialize failed: " + status);
    }

    public void Process(float[] left, float[] right)
    {
        lock (_gate)
        {
            int frames = Math.Min(left.Length, right.Length);
            int position = 0;
            while (position < frames)
            {
                int block = Math.Min(BlockFrames, frames - position);
                ProcessBlock(left, right, position, block);
                position += block;
            }
        }
    }

    private void ProcessBlock(float[] left, float[] right, int position, int frames)
    {
        int bytes = frames * sizeof(float);
        _inputL = Marshal.AllocHGlobal(bytes);
        _inputR = Marshal.AllocHGlobal(bytes);
        _inputFrames = frames;
        Marshal.Copy(left, position, _inputL, frames);
        Marshal.Copy(right, position, _inputR, frames);

        IntPtr outputL = Marshal.AllocHGlobal(bytes);
        IntPtr outputR = Marshal.AllocHGlobal(bytes);
        IntPtr outputList = AllocateBufferList(outputL, outputR, frames);

        var stamp = new AudioTimeStamp { mSampleTime = _processedFrames, mFlags = 1 };
        uint flags = 0;
        try
        {
            int status = AudioUnitRender(_unit, ref flags, ref stamp, 0, (uint)frames, outputList);
            if (status != 0) throw new InvalidOperationException("AudioUnitRender failed: " + status);

            IntPtr actualL = ReadBufferData(outputList, 0, out int actualBytes);
            IntPtr actualR = ReadBufferData(outputList, 1, out _);
            int samples = Math.Min(frames, Math.Max(0, actualBytes / sizeof(float)));
            if (actualL != IntPtr.Zero) Marshal.Copy(actualL, left, position, samples);
            if (actualR != IntPtr.Zero)
            {
                int rightSamples = Math.Min(samples, frames);
                Marshal.Copy(actualR, right, position, rightSamples);
            }
            _processedFrames += frames;
        }
        finally
        {
            Marshal.FreeHGlobal(outputList);
            Marshal.FreeHGlobal(outputL);
            Marshal.FreeHGlobal(outputR);
            Marshal.FreeHGlobal(_inputL);
            Marshal.FreeHGlobal(_inputR);
            _inputL = IntPtr.Zero;
            _inputR = IntPtr.Zero;
            _inputFrames = 0;
        }
    }

    private static IntPtr AllocateBufferList(IntPtr left, IntPtr right, int frames)
    {
        int entrySize = Marshal.SizeOf<AudioBuffer>();
        IntPtr list = Marshal.AllocHGlobal(4 + entrySize * 2);
        Marshal.WriteInt32(list, 2);
        for (int i = 0; i < 2; i++)
        {
            IntPtr entry = IntPtr.Add(list, 4 + i * entrySize);
            Marshal.WriteInt32(entry, 1);
            Marshal.WriteInt32(entry + 4, frames * sizeof(float));
            Marshal.WriteIntPtr(entry + 8, i == 0 ? left : right);
        }
        return list;
    }

    private static IntPtr ReadBufferData(IntPtr list, int index, out int byteSize)
    {
        int entrySize = Marshal.SizeOf<AudioBuffer>();
        IntPtr entry = IntPtr.Add(list, 4 + index * entrySize);
        byteSize = Marshal.ReadInt32(entry + 4);
        return Marshal.ReadIntPtr(entry + 8);
    }

    private static int OnInput(IntPtr refCon, IntPtr actionFlags, IntPtr timeStamp, uint bus,
        uint frames, IntPtr ioData)
    {
        try
        {
            var effect = GCHandle.FromIntPtr(refCon).Target as AudioUnitEffect;
            effect?.FillInput(ioData, (int)frames);
        }
        catch
        {
            // Never let an exception cross the unmanaged callback.
        }
        return 0;
    }

    private unsafe void FillInput(IntPtr ioData, int frames)
    {
        int count = Marshal.ReadInt32(ioData);
        int entrySize = Marshal.SizeOf<AudioBuffer>();
        int bytes = Math.Min(frames, _inputFrames) * sizeof(float);
        for (int i = 0; i < count; i++)
        {
            IntPtr entry = IntPtr.Add(ioData, 4 + i * entrySize);
            IntPtr destination = Marshal.ReadIntPtr(entry + 8);
            IntPtr source = i == 0 ? _inputL : _inputR;
            if (source == IntPtr.Zero) continue;

            if (destination == IntPtr.Zero)
            {
                Marshal.WriteIntPtr(entry + 8, source);
                Marshal.WriteInt32(entry + 4, bytes);
            }
            else
            {
                int capacity = Marshal.ReadInt32(entry + 4);
                int copy = Math.Min(bytes, Math.Max(0, capacity));
                if (copy > 0) Buffer.MemoryCopy((void*)source, (void*)destination, capacity, copy);
                Marshal.WriteInt32(entry + 4, copy);
            }
        }
    }

    public void Dispose()
    {
        IntPtr unit = _unit;
        _unit = IntPtr.Zero;
        if (unit != IntPtr.Zero)
        {
            try { AudioUnitUninitialize(unit); } catch { }
            try { AudioComponentInstanceDispose(unit); } catch { }
        }
        if (_self.IsAllocated) _self.Free();
    }
}
