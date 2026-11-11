namespace HumbleBakerStudio.Audio;


// A shared effects return. Tracks send into it; its FX chain runs on the summed signal before it hits the master.
public class Bus {
    public Bus() { Name = "Bus A"; }
    public Bus(string name) { Name = name; }
    public string Name = "Bus A"; public bool Enabled = true;
    public Fx Fx = new Fx();
    public List<AutoLane> Auto = new List<AutoLane>();
}
