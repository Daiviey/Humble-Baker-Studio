using System.Runtime.InteropServices;
using System.Text;

namespace HumbleBakerStudio.Mac;

public sealed record AudioUnitInfo(string Name, string Kind, string Manufacturer);

/// <summary>
/// Enumerates the Audio Units installed on macOS through the AudioToolbox component API.
/// Instantiating a unit and running its render callback is the next step; this class deliberately
/// only discovers components so the early shell can show what is available.
/// </summary>
public static class AudioUnits
{
    [StructLayout(LayoutKind.Sequential)]
    private struct AudioComponentDescription
    {
        public uint ComponentType;
        public uint ComponentSubType;
        public uint ComponentManufacturer;
        public uint ComponentFlags;
        public uint ComponentFlagsMask;
    }

    private const string AudioToolbox = "/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const uint Utf8Encoding = 0x08000100;

    [DllImport(AudioToolbox)]
    private static extern IntPtr AudioComponentFindNext(IntPtr inComponent, ref AudioComponentDescription inDesc);

    [DllImport(AudioToolbox)]
    private static extern int AudioComponentCopyName(IntPtr inComponent, out IntPtr outName);

    [DllImport(AudioToolbox)]
    private static extern int AudioComponentGetDescription(IntPtr inComponent, out AudioComponentDescription outDesc);

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(IntPtr cf);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFStringGetCStringPtr(IntPtr theString, uint encoding);

    [DllImport(CoreFoundation)]
    private static extern bool CFStringGetCString(IntPtr theString, byte[] buffer, long bufferSize, uint encoding);

    public static IReadOnlyList<AudioUnitInfo> List()
    {
        if (!OperatingSystem.IsMacOS()) return Array.Empty<AudioUnitInfo>();

        var found = new List<AudioUnitInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (type, kind) in new[]
                 {
                     ("aumu", "Instrument"),
                     ("aufx", "Effect"),
                     ("augn", "Generator"),
                     ("aumf", "Format converter")
                 })
        {
            var desc = new AudioComponentDescription { ComponentType = FourCc(type) };
            IntPtr component = IntPtr.Zero;
            while ((component = AudioComponentFindNext(component, ref desc)) != IntPtr.Zero)
            {
                string name = CopyName(component);
                if (string.IsNullOrWhiteSpace(name)) continue;
                string manufacturer = "";
                if (AudioComponentGetDescription(component, out var actual) == 0)
                    manufacturer = FourCcToString(actual.ComponentManufacturer);
                if (!seen.Add(name + "|" + kind + "|" + manufacturer)) continue;
                found.Add(new AudioUnitInfo(name, kind, manufacturer));
            }
        }
        found.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return found;
    }

    private static string CopyName(IntPtr component)
    {
        if (AudioComponentCopyName(component, out IntPtr cf) != 0 || cf == IntPtr.Zero) return "";
        try
        {
            IntPtr direct = CFStringGetCStringPtr(cf, Utf8Encoding);
            if (direct != IntPtr.Zero) return Marshal.PtrToStringUTF8(direct) ?? "";
            var buffer = new byte[1024];
            if (!CFStringGetCString(cf, buffer, buffer.Length, Utf8Encoding)) return "";
            return Encoding.UTF8.GetString(buffer).TrimEnd('\0');
        }
        finally
        {
            CFRelease(cf);
        }
    }

    private static uint FourCc(string value) =>
        ((uint)value[0] << 24) | ((uint)value[1] << 16) | ((uint)value[2] << 8) | value[3];

    private static string FourCcToString(uint value)
    {
        Span<char> chars = stackalloc char[4]
        {
            (char)((value >> 24) & 0xFF),
            (char)((value >> 16) & 0xFF),
            (char)((value >> 8) & 0xFF),
            (char)(value & 0xFF)
        };
        foreach (char c in chars)
            if (c < ' ' || c > '~') return value.ToString("X8");
        return new string(chars);
    }
}
