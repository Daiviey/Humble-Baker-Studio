using System.Text;

namespace HumbleBakerStudio.VstBridge;

/// <summary>
/// Length-prefixed binary protocol shared with the PowerShell studio (see the embedded
/// <c>VstBridgeClient</c> in AudioStudioImplementation.ps1). Both sides are little-endian .NET,
/// so <see cref="BinaryReader"/>/<see cref="BinaryWriter"/> are used directly.
/// </summary>
public static class Protocol
{
    public const int OpPing = 1;
    public const int OpScan = 2;
    public const int OpEffect = 3;
    public const int OpInstrument = 4;
    public const int OpCapture = 5;
    public const int OpRelease = 6;
    public const int OpShutdown = 7;

    public const int StatusOk = 0;
    public const int StatusError = 1;

    public static string ReadString(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length < 0 || length > 1_000_000) throw new InvalidDataException("Bad string length.");
        return Encoding.UTF8.GetString(reader.ReadBytes(length));
    }

    public static void WriteString(BinaryWriter writer, string? value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value ?? "");
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    public static float[] ReadFloats(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count < 0 || count > 200_000_000) throw new InvalidDataException("Bad buffer length.");
        var buffer = new float[count];
        for (int i = 0; i < count; i++) buffer[i] = reader.ReadSingle();
        return buffer;
    }

    public static void WriteFloats(BinaryWriter writer, float[]? values)
    {
        values ??= Array.Empty<float>();
        writer.Write(values.Length);
        foreach (float value in values) writer.Write(value);
    }

    public static void WriteOk(BinaryWriter writer, string? message = null)
    {
        writer.Write(StatusOk);
        WriteString(writer, message);
        writer.Flush();
    }

    public static void WriteError(BinaryWriter writer, string message)
    {
        writer.Write(StatusError);
        WriteString(writer, message);
        writer.Flush();
    }
}
