namespace HumbleBakerStudio.Audio;


public static class Scales {
    public static readonly string[] Names = new string[] { "minor", "major", "dorian", "phrygian", "lydian", "mixolydian", "locrian", "harmonic minor", "pentatonic minor", "pentatonic major", "blues", "whole tone", "hungarian minor", "double harmonic", "chromatic" };
    static readonly int[][] Steps = new int[][] {
        new int[] {0,2,3,5,7,8,10}, new int[] {0,2,4,5,7,9,11}, new int[] {0,2,3,5,7,9,10}, new int[] {0,1,3,5,7,8,10},
        new int[] {0,2,4,6,7,9,11}, new int[] {0,2,4,5,7,9,10}, new int[] {0,1,3,5,6,8,10}, new int[] {0,2,3,5,7,8,11},
        new int[] {0,3,5,7,10}, new int[] {0,2,4,7,9}, new int[] {0,3,5,6,7,10}, new int[] {0,2,4,6,8,10},
        new int[] {0,2,3,6,7,8,11}, new int[] {0,1,4,5,7,8,11}, new int[] {0,1,2,3,4,5,6,7,8,9,10,11} };
    public static int[] Get(string name) {
        for (int i = 0; i < Names.Length; i++) if (Names[i] == name) return Steps[i];
        return Steps[0];
    }
    public static bool[] Mask(int key, string scale) {
        bool[] m = new bool[12];
        foreach (int s in Get(scale)) m[(s + key) % 12] = true;
        return m;
    }
    public static int Snap(int pitch, bool[] mask) {
        for (int d = 0; d < 7; d++) {
            int a = pitch - d, b = pitch + d;
            if (a >= 0 && mask[((a % 12) + 12) % 12]) return a;
            if (b <= 127 && mask[((b % 12) + 12) % 12]) return b;
        }
        return pitch;
    }
}
