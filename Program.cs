// See https://aka.ms/new-console-template for more information

using SkyWing.Logger;
using SkyWing.SkyWing;
using SkyWing.SkyWing.Utils;

public class Program {

    private static void Main(string[] args) {

        var pid = FileSystem.CreateLockFile("server.lock");
        if (pid != null) {
            CriticalError("Another " + VersionInfo.NAME + " instance (PID)" + pid + " is already using this folder.");
            CriticalError("Please stop the other server first before running a new one.\n\n");
            
            Info("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(1);
        }
        
        // TODO: Timezone setting field, preferably system default timezone.
        var logger = new MainLogger("server.log", "Server", TimeZoneInfo.Local, false);
        GlobalLogger.Logger = logger;
        
        // Check if the properties file exists.
        if(!File.Exists("server.properties")) {
            // TODO: Run setup wizard.
        }

        var server = new Server(logger);

        while (!server.Shutdown) { 
            // Wait for server to be shutdown.
        }

        // TODO: Stop other threads when we start using threads.

        logger.Shutdown = true;
        FileSystem.ReleaseLockFile("server.lock");
        
        Info("Press any key to exit...");
        Console.ReadKey();
        Environment.Exit(1);
    }

    private static void CriticalError(string message) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {message}");
        Console.ResetColor();
    }
    
    private static void Info(string message) {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"[INFO] {message}");
        Console.ResetColor();
    }
}