namespace HumbleBakerStudio.Audio;


// One detected section of a song: where it starts (beats), how long (bars) and how loud (energy 1-10).
public class Section {
    public string Name; public double Beat; public int Bars; public int Energy;
}
