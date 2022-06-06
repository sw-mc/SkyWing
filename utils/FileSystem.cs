using System.Diagnostics;

namespace SkyWing.SkyWing.Utils; 

public sealed class FileSystem {
    
    // Keep these files "alive" and locked.
    private static List<FileStream> lockedFileStreams = new List<FileStream>();

    public static int? CreateLockFile(string filePath) {
        if (File.Exists(filePath)) {
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            var buffer = new byte[4]; //PID is 4 bytes (int32)
            var bytesRead = fs.Read(buffer, 0, 4);
            if (bytesRead == 4) {
                return BitConverter.ToInt32(buffer, 0);
            }
                
            fs.Close();
            Thread.Sleep(100);
            CreateLockFile(filePath); // Wait for a previous process to write to lock.
        }
        
        using (var fs = File.Create(filePath)) {
            fs.Write(BitConverter.GetBytes(Environment.ProcessId), 0, 4);
            fs.Close();
            return null;
        }
    }
}