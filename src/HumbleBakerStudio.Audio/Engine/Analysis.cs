namespace HumbleBakerStudio.Audio;


public class Analysis {
    public double PeakDb, RmsDb, Lufs, Crest, Correlation, Seconds; public double[] Bands = new double[6];
    public static readonly string[] BandNames = new string[] { "sub 20-60 Hz", "bass 60-250 Hz", "low-mid 250-500 Hz", "mid 500-2k Hz", "high-mid 2-6 kHz", "air 6k+ Hz" };
    public string Report() {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        CultureInfo ci = CultureInfo.InvariantCulture;
        sb.AppendLine("Length " + Seconds.ToString("0.0", ci) + " s | peak " + PeakDb.ToString("0.0", ci) + " dBFS | RMS " + RmsDb.ToString("0.0", ci) + " dBFS");
        sb.AppendLine("Loudness (approx.) " + Lufs.ToString("0.0", ci) + " LUFS | crest factor " + Crest.ToString("0.0", ci) + " dB | stereo correlation " + Correlation.ToString("0.00", ci));
        sb.Append("Band levels (dBFS RMS): ");
        for (int i = 0; i < 6; i++) sb.Append(BandNames[i] + " " + Bands[i].ToString("0.0", ci) + (i < 5 ? "; " : ""));
        return sb.ToString();
    }
}
