namespace HumbleBakerStudio.Audio;


// Measurements a producer would make by listening: how busy, how bright, how noisy, how loud over time, and what pitch a note really has.
public class AudioFeatures {
    public double Seconds, OnsetsPerSec, CentroidHz, Flatness, ClipFraction, Dc, PeakDb, RmsDb;
    public double[] Curve = new double[0];   // RMS in dB for every 2 seconds
}
