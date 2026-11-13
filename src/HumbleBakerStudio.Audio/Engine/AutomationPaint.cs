namespace HumbleBakerStudio.Audio;

/// <summary>
/// Turns a painted automation canvas into lane points. Each painted column contributes one
/// point: the vertical centroid becomes the value (logarithmic for frequency parameters) and
/// the column's brightest cell becomes the Z axis, either scaling the value toward the target's
/// neutral setting or replacing it with a direct value.
/// </summary>
public static class AutomationPaint
{
    public static List<double[]> Paint(
        SpectralGrid grid,
        string param,
        double min,
        double max,
        double loopBeats,
        string mapMode,
        string zMode,
        double zAmount,
        double neutral,
        string detail = "Curve (single value)")
    {
        var points = new List<double[]>();
        if (grid == null) return points;

        double range = max - min;
        bool isLog = param is "p:Cutoff" or "f:Cutoff" or "lp" or "hp";
        double threshold = 0.02;

        while (true)
        {
            var colVals = new List<double[]>();

            for (int c = 0; c < grid.Cols; c++)
            {
                double sum = 0, wsum = 0, peak = 0;
                int lowRow = int.MaxValue, highRow = -1;
                for (int r = 0; r < grid.Rows; r++)
                {
                    double v = grid.Get(c, r);
                    sum += v;
                    wsum += v * (r / (double)(grid.Rows - 1));
                    if (v > peak) peak = v;
                    if (v > 0.004)
                    {
                        if (r < lowRow) lowRow = r;
                        if (r > highRow) highRow = r;
                    }
                }
                if (sum < threshold) continue;

                double frac = wsum / sum;
                double value = isLog && min > 0 ? min * Math.Pow(max / min, frac) : min + frac * range;

                if (zMode != "Off (position only)" && zAmount > 0)
                {
                    double z = Math.Min(1, peak);
                    if (zMode == "Direct value")
                    {
                        value = min + range * z;
                    }
                    else
                    {
                        double strength = Math.Min(1, z * zAmount);
                        value = neutral + (value - neutral) * strength;
                    }
                }

                // Keep the painted vertical extent and brightness alongside the value so the
                // canvas can be reconstructed faithfully (and the project keeps the detail).
                double lowFrac = highRow >= lowRow ? lowRow / (double)(grid.Rows - 1) : frac;
                double highFrac = highRow >= lowRow ? highRow / (double)(grid.Rows - 1) : frac;
                double beat = (c + 0.5) / grid.Cols * loopBeats;
                colVals.Add(new[] { beat, value, Math.Min(1, peak), lowFrac, highFrac });
            }

            if (colVals.Count > 0 || threshold <= 0.002)
            {
                if (detail == "Stroke (follow rows)")
                    colVals = ExpandStroke(colVals, grid, param, min, max, loopBeats);
                points = ApplyMap(colVals, mapMode, param, min, max);
                break;
            }
            threshold = 0.002;
        }

        return points;
    }

    /// <summary>
    /// A thick painted stroke is ambiguous for one parameter: two values exist at the same time.
    /// Stroke detail resolves it by emitting both edges of the vertical run inside the column,
    /// turning the painted thickness into fast modulation instead of discarding it.
    /// </summary>
    private static List<double[]> ExpandStroke(List<double[]> columns, SpectralGrid grid, string param, double min, double max, double loopBeats)
    {
        bool isLog = param is "p:Cutoff" or "f:Cutoff" or "lp" or "hp";
        double range = max - min;
        var dense = new List<double[]>(columns.Count * 2);
        double step = 0.4 / grid.Cols * loopBeats;

        foreach (var point in columns)
        {
            double low = point[3], high = point[4];
            if (high - low <= 1.0 / Math.Max(1, grid.Rows - 1))
            {
                dense.Add(point);
                continue;
            }

            double lowValue = isLog && min > 0 ? min * Math.Pow(max / min, low) : min + low * range;
            double highValue = isLog && min > 0 ? min * Math.Pow(max / min, high) : min + high * range;
            double beat = point[0];
            dense.Add(new[] { Math.Max(0, beat - step), lowValue, point[2], low, high });
            dense.Add(new[] { beat + step, highValue, point[2], low, high });
        }
        return dense;
    }

    private static List<double[]> ApplyMap(List<double[]> values, string mode, string param, double min, double max)
    {
        double range = max - min;

        if (mode == "Smooth curve" && values.Count >= 3)
        {
            var smoothed = new List<double[]>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                double a = values[Math.Max(0, i - 1)][1];
                double b = values[i][1];
                double c = values[Math.Min(values.Count - 1, i + 1)][1];
                smoothed.Add(new[] { values[i][0], (a + b + c) / 3.0 });
            }
            values = smoothed;
        }
        else if (mode == "Quantize steps")
        {
            int steps = param switch { "p:Unison" => 7, "p:Bits" or "f:Bits" => 15, _ => 16 };
            foreach (var point in values)
                point[1] = min + Math.Round((point[1] - min) / range * steps) / steps * range;
        }
        else if (mode == "Linear ramp" && values.Count >= 2)
        {
            double v0 = values[0][1], v1 = values[^1][1];
            double b0 = values[0][0], b1 = values[^1][0];
            foreach (var point in values)
            {
                point[1] = v0 + (point[1] - v0) * 0.4
                              + (v1 - v0) * ((point[0] - b0) / Math.Max(0.001, b1 - b0)) * 0.6;
            }
        }

        // De-duplicate by beat (last write wins), then sort.
        var byBeat = new Dictionary<double, double[]>();
        foreach (var point in values) byBeat[point[0]] = point;
        return byBeat.Values.OrderBy(p => p[0]).ToList();
    }
}
