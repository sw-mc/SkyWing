using System.Diagnostics;

namespace SkyWing.SkyWing.Utils; 

public sealed class FileSystem {

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
    
    public static void ReleaseLockFile(string filePath) {
        if (File.Exists(filePath)) {
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            var buffer = new byte[4]; //PID is 4 bytes (int32)
            var bytesRead = fs.Read(buffer, 0, 4);
            if (bytesRead == 4) {
                var pid = BitConverter.ToInt32(buffer, 0);
                if (pid == Environment.ProcessId) {
                    fs.Close();
                    File.Delete(filePath);
                }
                else {
                    throw new LockFileException("Lock file is not owned by this process.");
                }
            }
            else {
                throw new LockFileException("Lock file does not contain a PID.");
            }
        }
        else {
            throw new LockFileException("Lock file does not exist.");
        }
    }
    
    private class LockFileException : Exception {
        public LockFileException(string message) : base(message) { }
    }
}