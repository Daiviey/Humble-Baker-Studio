namespace HumbleBakerStudio.Audio;


// Splits a mono buffer at detected onsets; returns start sample indices (always includes 0).
public static class SampleSlicer {
    public static int[] Boundaries(float[] s, double sensitivity, int minGapSamples) {
        List<int> points = new List<int>();
        points.Add(0);
        if (s != null && s.Length > 2048) {
            double[] sec = Transcriber.OnsetSeconds(s, Math.Max(0, Math.Min(1, sensitivity)));
            int last = 0;
            foreach (double t in sec) {
                int p = (int)(t * Dsp.SR);
                if (p > 64 && p < s.Length - 64 && p - last >= minGapSamples) { points.Add(p); last = p; }
            }
        }
        return points.ToArray();
    }
}
