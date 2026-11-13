using System.IO.Compression;

namespace HumbleBakerStudio.Audio;

/// <summary>
/// Stores a painted automation canvas as a compact, display-only snapshot. The snapshot keeps
/// the exact resolution and brightness of the painting, while the lane itself stays a normal
/// one-dimensional automation curve that playback can evaluate cheaply.
/// </summary>
public static class AutomationPreview
{
    public static byte[] Encode(SpectralGrid grid)
    {
        var raw = new byte[grid.A.Length];
        for (int i = 0; i < raw.Length; i++)
            raw[i] = (byte)Math.Round(Math.Clamp(grid.A[i], 0f, 1f) * 255);

        using var memory = new MemoryStream();
        using (var deflate = new DeflateStream(memory, CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(raw, 0, raw.Length);
        return memory.ToArray();
    }

    public static byte[] EncodeLayers(IEnumerable<AutoLayer> layers, int cols, int rows)
    {
        var grid = new SpectralGrid(cols, rows);
        foreach (var layer in layers)
        {
            int count = Math.Min(grid.A.Length, layer.Data.Length);
            for (int i = 0; i < count; i++)
                if (layer.Data[i] > grid.A[i]) grid.A[i] = layer.Data[i];
        }
        return Encode(grid);
    }

    public static SpectralGrid Decode(byte[]? compressed, int cols, int rows)
    {
        var grid = new SpectralGrid(Math.Max(8, cols), Math.Max(8, rows));
        if (compressed == null || compressed.Length == 0) return grid;

        try
        {
            using var memory = new MemoryStream(compressed);
            using var deflate = new DeflateStream(memory, CompressionMode.Decompress);
            var raw = new byte[grid.A.Length];
            int read = 0;
            while (read < raw.Length)
            {
                int n = deflate.Read(raw, read, raw.Length - read);
                if (n <= 0) break;
                read += n;
            }
            for (int i = 0; i < read; i++) grid.A[i] = raw[i] / 255f;
        }
        catch
        {
            // A corrupt preview should never stop a project from opening.
        }
        return grid;
    }
}
