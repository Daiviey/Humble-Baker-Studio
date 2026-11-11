namespace HumbleBakerStudio.Audio;


public class PatchGroup {
    public string Id = Guid.NewGuid().ToString("N");
    public string Name = "Group";
    public string NodeId;                       // the group's master bus node in the graph
    public List<string> Members = new List<string>();
    public bool Collapsed;
    public int ColorIndex;
}
