namespace HumbleBakerStudio.Audio;


public class Note {
    public int Pitch; public double Start; public double Len; public int Vel;
    public Note() { }
    public Note(int p, double s, double l, int v) { Pitch = p; Start = s; Len = l; Vel = v; }
    public Note Clone() { return new Note(Pitch, Start, Len, Vel); }
}
