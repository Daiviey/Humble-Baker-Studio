namespace HumbleBakerStudio.Audio;


// Repeat and finalise PCM for export (fast enough for long loop counts).
public static class ExportDsp {
    public static short[] Repeat(short[] pcm, int loops) {
        loops = Math.Max(1, loops);
        if (loops == 1) return pcm;
        short[] o = new short[pcm.Length * loops];
        for (int i = 0; i < loops; i++) Array.Copy(pcm, 0, o, i * pcm.Length, pcm.Length);
        return o;
    }
    public static void NormalizeFade(short[] pcm, bool normalize, double targetDb, int fadeInMs, int fadeOutMs, int sr) {
        int n = pcm.Length / 2;
        if (normalize) {
            double peak = 0;
            for (int i = 0; i < pcm.Length; i++) { double a = Math.Abs((double)pcm[i]); if (a > peak) peak = a; }
            if (peak > 0) {
                double target = Math.Pow(10, targetDb / 20.0) * 32767.0, g = target / peak;
                for (int i = 0; i < pcm.Length; i++) { double v = Math.Round(pcm[i] * g); pcm[i] = (short)Math.Max(-32767, Math.Min(32767, v)); }
            }
        }
        if (fadeInMs > 0) { int k = Math.Min(n, (int)(fadeInMs * sr / 1000.0)); for (int i = 0; i < k; i++) { double g = (double)i / k; pcm[2 * i] = (short)(pcm[2 * i] * g); pcm[2 * i + 1] = (short)(pcm[2 * i + 1] * g); } }
        if (fadeOutMs > 0) { int k = Math.Min(n, (int)(fadeOutMs * sr / 1000.0)); for (int i = 0; i < k; i++) { double g = (double)i / k; int idx = n - 1 - i; pcm[2 * idx] = (short)(pcm[2 * idx] * g); pcm[2 * idx + 1] = (short)(pcm[2 * idx + 1] * g); } }
    }
}
