namespace SkyWing.SkyWing; 

public sealed class CoreConstants {
    
    public static string DataPath { get; }
    public static string BinPath { get; }
    public static string ResourcesPath { get; }
    public static string LocalDataPath { get; }

    static CoreConstants() {
        DataPath = Directory.GetCurrentDirectory();
        BinPath = Path.Combine(DataPath, "bin");
        ResourcesPath = Path.Combine(BinPath, "resources");
        LocalDataPath = Path.Combine(ResourcesPath, "locale");
    }
    
}