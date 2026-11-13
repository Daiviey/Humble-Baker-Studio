namespace HumbleBakerStudio.Audio;

/// <summary>A disconnected painted region on the automation canvas, treated as one spline layer.</summary>
public sealed class AutoLayer
{
    public string Name = "";
    public string Param = "";
    public int Cols;
    public int Rows;
    public float[] Data = Array.Empty<float>();
    public double Amplitude;       // peak brightness in the region
    public double CentroidY;       // 0 bottom .. 1 top, used to order layers
    public int PixelCount;
    /// <summary>Normalised centreline points: [x 0..1, y 0..1] for drawing the layer.</summary>
    public List<double[]> Contour = new();

    public AutoLayer Clone()
    {
        var copy = new AutoLayer
        {
            Name = Name,
            Param = Param,
            Cols = Cols,
            Rows = Rows,
            Data = (float[])Data.Clone(),
            Amplitude = Amplitude,
            CentroidY = CentroidY,
            PixelCount = PixelCount,
            Contour = Contour.Select(p => (double[])p.Clone()).ToList()
        };
        return copy;
    }
}

/// <summary>
/// Splits a painted automation canvas into separate wave layers and combines layered lanes.
/// A single parameter is one-dimensional, so layering is expressed as normalised deviations
/// from the target's neutral value; each layer is weighted by its painted amplitude.
/// </summary>
public static class AutomationLayers
{
    public static List<AutoLayer> Extract(SpectralGrid grid, double threshold = 0.02, int maxLayers = 8)
    {
        var layers = new List<AutoLayer>();
        if (grid == null || grid.A.Length == 0) return layers;

        var seen = new bool[grid.A.Length];
        var stack = new Stack<int>();
        int cols = grid.Cols, rows = grid.Rows;

        for (int start = 0; start < grid.A.Length; start++)
        {
            if (seen[start] || grid.A[start] <= threshold) continue;

            var cells = new List<int>();
            stack.Clear();
            stack.Push(start);
            seen[start] = true;

            while (stack.Count > 0)
            {
                int index = stack.Pop();
                cells.Add(index);
                int c = index / rows, r = index % rows;
                for (int dc = -1; dc <= 1; dc++)
                {
                    int nc = c + dc;
                    if (nc < 0 || nc >= cols) continue;
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        int nr = r + dr;
                        if (nr < 0 || nr >= rows) continue;
                        int ni = nc * rows + nr;
                        if (seen[ni] || grid.A[ni] <= threshold) continue;
                        seen[ni] = true;
                        stack.Push(ni);
                    }
                }
            }

            if (cells.Count < 4) continue;

            var data = new float[grid.A.Length];
            foreach (int index in cells) data[index] = grid.A[index];
            var layer = FromData(data, cols, rows, 1e-6);
            if (layer != null) layers.Add(layer);
        }

        // Keep the largest regions, then present them top-to-bottom so "Layer 1" is the highest wave.
        layers = layers.OrderByDescending(l => l.PixelCount).Take(Math.Max(1, maxLayers))
                       .OrderByDescending(l => l.CentroidY).ToList();
        for (int i = 0; i < layers.Count; i++) layers[i].Name = "Layer " + (i + 1);
        return layers;
    }

    /// <summary>
    /// Builds one layer from a stroke buffer. Because each mouse stroke gets its own buffer,
    /// two waves that cross at the same pixel still remain separate layers.
    /// </summary>
    public static AutoLayer? FromData(float[] data, int cols, int rows, double threshold = 0.004)
    {
        if (data == null || cols < 2 || rows < 2 || data.Length < cols * rows) return null;

        int pixels = 0;
        double peak = 0;
        for (int i = 0; i < cols * rows; i++)
        {
            double v = data[i];
            if (v <= threshold) continue;
            pixels++;
            if (v > peak) peak = v;
        }
        if (pixels < 4) return null;

        var contour = new List<double[]>();
        double centroidSum = 0, centroidWeight = 0;
        for (int c = 0; c < cols; c++)
        {
            double sum = 0, wsum = 0;
            for (int r = 0; r < rows; r++)
            {
                double v = data[c * rows + r];
                if (v <= 0) continue;
                sum += v;
                wsum += v * (r / (double)(rows - 1));
            }
            if (sum <= 1e-4) continue;
            double y = wsum / sum;
            contour.Add(new[] { c / (double)(cols - 1), y });
            centroidSum += y * sum;
            centroidWeight += sum;
        }
        if (contour.Count == 0) return null;

        return new AutoLayer
        {
            Cols = cols,
            Rows = rows,
            Data = (float[])data.Clone(),
            Amplitude = Math.Max(1e-6, peak),
            CentroidY = centroidWeight > 0 ? centroidSum / centroidWeight : 0.5,
            PixelCount = pixels,
            Contour = contour
        };
    }

    public static List<double[]> Combine(
        List<AutoLayer> layers,
        string param,
        double min,
        double max,
        double loopBeats,
        string mapMode,
        double neutral,
        string detail,
        bool normalizeAmplitude,
        double amplitudeGain,
        string mixMode)
    {
        var output = new List<double[]>();
        if (layers == null || layers.Count == 0) return output;

        bool isLog = param is "p:Cutoff" or "f:Cutoff" or "lp" or "hp";
        double range = max - min;
        int cols = layers[0].Cols;
        double neutralFrac = Math.Clamp(Fraction(neutral, isLog, min, max), 0, 1);

        var add = new double[cols];
        var multiply = Enumerable.Repeat(1.0, cols).ToArray();
        var strongest = new double[cols];
        var last = new double[cols];
        var has = new bool[cols];
        var strength = new double[cols];
        var low = Enumerable.Repeat(1.0, cols).ToArray();
        var high = new double[cols];

        foreach (var layer in layers)
        {
            var temp = new SpectralGrid(layer.Cols, layer.Rows);
            Array.Copy(layer.Data, temp.A, Math.Min(layer.Data.Length, temp.A.Length));

            // Amplitude weighting is this mode's normalisation, so per-point Z scaling is off.
            var points = AutomationPaint.Paint(temp, param, min, max, loopBeats, mapMode,
                "Off (position only)", 1, neutral, detail);

            double amp = Math.Max(1e-6, layer.Amplitude);
            double weight = normalizeAmplitude
                ? Math.Clamp(amplitudeGain / Math.Max(0.05, amp), 0.25, 4.0)
                : amplitudeGain * amp;

            foreach (var point in points)
            {
                int c = (int)Math.Round(point[0] / loopBeats * cols - 0.5);
                if (c < 0 || c >= cols) continue;
                double frac = Math.Clamp(Fraction(point[1], isLog, min, max), 0, 1);
                double delta = (frac - neutralFrac) * weight;

                if (!has[c])
                {
                    has[c] = true;
                    last[c] = neutralFrac + delta;
                    low[c] = 1;
                    high[c] = 0;
                }

                switch (mixMode)
                {
                    case "Multiply": multiply[c] *= 1 + delta; break;
                    case "Max": if (Math.Abs(delta) > Math.Abs(strongest[c])) strongest[c] = delta; break;
                    case "Replace": last[c] = neutralFrac + delta; break;
                    default: add[c] += delta; break;
                }

                strength[c] = Math.Max(strength[c], point.Length >= 3 ? point[2] : 1);
                if (point.Length >= 5)
                {
                    low[c] = Math.Min(low[c], point[3]);
                    high[c] = Math.Max(high[c], point[4]);
                }
            }
        }

        for (int c = 0; c < cols; c++)
        {
            if (!has[c]) continue;

            double frac = mixMode switch
            {
                "Multiply" => neutralFrac + (multiply[c] - 1),
                "Max" => neutralFrac + strongest[c],
                "Replace" => last[c],
                _ => neutralFrac + add[c]
            };
            frac = Math.Clamp(frac, 0, 1);
            double value = isLog && min > 0 ? min * Math.Pow(max / min, frac) : min + frac * range;
            double lowFrac = low[c] <= high[c] ? low[c] : frac;
            double highFrac = low[c] <= high[c] ? high[c] : frac;
            output.Add(new[] { (c + 0.5) / cols * loopBeats, value, Math.Clamp(strength[c], 0, 1), lowFrac, highFrac });
        }

        return output;
    }

    private static double Fraction(double value, bool isLog, double min, double max)
    {
        if (isLog && min > 0 && value > 0)
        {
            double denom = Math.Log(max / min);
            if (denom > 1e-9) return Math.Log(value / min) / denom;
        }
        return max - min > 1e-9 ? (value - min) / (max - min) : 0;
    }
}
