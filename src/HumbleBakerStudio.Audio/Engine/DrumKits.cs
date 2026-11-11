namespace HumbleBakerStudio.Audio;


public static class DrumKits {
    public static DrumKitParams For(string name) {
        var p = new DrumKitParams();
        if (name == null) return p;
        switch (name) {
            case "808":
                p.KickPitch = 110; p.KickDecay = 3.5; p.KickLen = 1.1; p.KickClick = 0.15; p.KickSub = 0.9; p.KickSnap = 0.4; p.SnareDecay = 9; p.SnareTone = 150; p.SnareNoise = 0.6; p.HatDecay = 30; p.HatBright = 0.6; p.OpenDecay = 4; p.CrashLen = 0.5; p.TomDecay = 6; p.DrumDrive = 0.15; p.Room = 0.1; break;
            case "909":
                p.KickPitch = 155; p.KickDecay = 6; p.KickLen = 0.38; p.KickDrive = 0.35; p.KickClick = 0.9; p.KickSub = 0.35; p.SnareTone = 215; p.SnareSnap = 0.9; p.HatBright = 1.5; p.HatDecay = 85; p.OpenDecay = 12; p.CrashBright = 1.3; p.TomDecay = 11; p.Room = 0.08; break;
            case "Acoustic":
                p.KickPitch = 78; p.KickDecay = 13; p.KickLen = 0.28; p.KickClick = 1.1; p.KickSub = 0.3; p.SnareTone = 172; p.SnareNoise = 1.2; p.SnareBody = 0.8; p.SnareSnap = 0.9; p.HatBright = 0.9; p.HatDecay = 42; p.OpenDecay = 8; p.Room = 0.55; p.TomDecay = 14; break;
            case "Lo-Fi":
                p.KickPitch = 92; p.KickDecay = 9; p.SnareDecay = 24; p.SnareTone = 148; p.SnareNoise = 1.0; p.HatDecay = 26; p.HatBright = 0.45; p.DrumCrush = 7; p.DrumDrive = 0.45; p.Room = 0.25; p.Humanize = 0.5; break;
            case "Jazz":
                p.KickPitch = 72; p.KickDecay = 15; p.KickLen = 0.2; p.KickClick = 1.2; p.SnareTone = 185; p.RimFreq = 980; p.SnareSnap = 0.8; p.HatBright = 1.15; p.HatDecay = 48; p.Room = 0.6; p.RideCha = 0.45; p.RidePing = 0.7; p.Humanize = 0.45; break;
            case "Rock":
                p.KickPitch = 98; p.KickDrive = 0.4; p.KickClick = 1.0; p.SnareTone = 168; p.SnareNoise = 1.05; p.SnareSnap = 0.85; p.HatBright = 0.85; p.OpenDecay = 7; p.Room = 0.4; p.TomDecay = 13; break;
            case "Techno":
                p.KickPitch = 135; p.KickDecay = 5.5; p.KickDrive = 0.6; p.KickClick = 0.7; p.KickSub = 0.5; p.SnareTone = 205; p.SnareSnap = 0.7; p.HatBright = 1.35; p.HatDecay = 72; p.OpenDecay = 10; p.Room = 0.06; break;
            case "House":
                p.KickPitch = 138; p.KickDecay = 4.5; p.KickDrive = 0.2; p.KickClick = 0.5; p.SnareTone = 222; p.SnareNoise = 0.8; p.HatBright = 1.25; p.HatDecay = 58; p.OpenDecay = 11; p.Room = 0.12; break;
            case "Hip-Hop":
                p.KickPitch = 86; p.KickDecay = 8; p.KickLen = 0.5; p.KickClick = 0.6; p.SnareTone = 142; p.SnareDecay = 21; p.SnareNoise = 0.95; p.HatDecay = 44; p.HatBright = 0.8; p.Room = 0.3; p.Humanize = 0.4; break;
            case "Trap":
                p.KickPitch = 58; p.KickDecay = 2.5; p.KickLen = 0.8; p.KickClick = 0.3; p.KickSub = 1.0; p.SnareTone = 208; p.HatDecay = 95; p.HatBright = 1.6; p.HatOpen = 0.7; p.Room = 0.05; p.VelCurve = 0.85; break;
            case "DnB":
                p.KickPitch = 112; p.KickDecay = 9; p.KickClick = 0.8; p.KickSub = 0.45; p.SnareTone = 195; p.SnareNoise = 1.0; p.SnareSnap = 0.75; p.HatDecay = 78; p.HatBright = 1.25; p.Room = 0.38; p.Humanize = 0.35; break;
            case "Jungle":
                p.KickPitch = 102; p.KickDecay = 10; p.KickClick = 0.9; p.SnareTone = 200; p.SnareNoise = 1.3; p.SnareSnap = 1.0; p.SnareTight = 0.4; p.HatDecay = 68; p.Room = 0.5; p.Humanize = 0.55; break;
            case "Industrial":
                p.KickPitch = 108; p.KickDrive = 0.7; p.KickClick = 1.3; p.DrumDrive = 0.65; p.DrumCrush = 8; p.SnareTone = 245; p.SnareNoise = 1.25; p.HatBright = 1.7; p.HatDecay = 55; p.OpenDecay = 6; p.Room = 0.22; p.Humanize = 0.3; break;
            case "Electro":
                p.KickPitch = 122; p.KickDrive = 0.35; p.KickClick = 0.65; p.SnareTone = 228; p.SnareSnap = 0.55; p.HatBright = 1.45; p.HatDecay = 62; p.HatOpen = 0.35; p.Room = 0.08; break;
            case "Latin":
                p.KickPitch = 88; p.KickDecay = 11; p.KickClick = 0.95; p.SnareTone = 152; p.RimFreq = 610; p.RimFreq = 610; p.HatBright = 1.0; p.Room = 0.38; p.Humanize = 0.4; p.PercDecay = 8; break;
            case "Ambient":
                p.KickPitch = 68; p.KickDecay = 16; p.KickClick = 0.4; p.KickSub = 0.6; p.SnareTone = 138; p.SnareNoise = 0.5; p.SnareBody = 0.5; p.HatDecay = 28; p.HatBright = 0.55; p.Room = 0.85; p.CrashLen = 2.2; p.CrashBright = 0.6; break;
        }
        // kits get distinct saturation/crush defaults
        if (name == "808" || name == "Ambient" || name == "Jazz") p.DrumCrush = Math.Max(p.DrumCrush, 12);
        if (name == "Industrial") p.DrumCrush = 7;
        return p;
    }
    public static string[] Names = new string[] { "Standard", "808", "909", "Acoustic", "Lo-Fi", "Jazz", "Rock", "Techno", "House", "Hip-Hop", "Trap", "DnB", "Jungle", "Industrial", "Electro", "Latin", "Ambient" };
}
