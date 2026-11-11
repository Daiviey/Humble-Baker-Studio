namespace HumbleBakerStudio.Audio;


// A named section of the timeline (shown on the piano roll ruler and used by the producer to shape the energy curve).
public class Marker {
    public double Beat; public string Name = ""; public int Energy;
    public Marker() { }
    public Marker(double beat, string name, int energy) { Beat = beat; Name = name; Energy = energy; }
}
