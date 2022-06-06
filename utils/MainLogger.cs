using System.Text;
using SkyWing.Logger;

namespace SkyWing.SkyWing.Utils;

public class MainLogger : SimpleLogger {

    public bool LogDebug { get; set; }
    private readonly string mainThreadName;
    private readonly TimeZoneInfo timezone;

    private readonly LogSendQueue logSendQueue;
    private readonly FileStream mainLogFile;
    private bool Shutdown { get; set; } = false;

    public new void Emergency(string message) {
        Send(message, "EMERGENCY", ConsoleColor.Red);
    }

    public new void Alert(string message) {
        Send(message, "ALERT", ConsoleColor.Red);
    }

    public new void Critical(string message) {
        Send(message, "CRITICAL", ConsoleColor.Red);
    }

    public new void Error(string message) {
        Send(message, "ERROR", ConsoleColor.DarkRed);
    }

    public new void Warning(string message) {
        Send(message, "WARNING", ConsoleColor.Yellow);
    }

    public new void Notice(string message) {
        Send(message, "NOTICE", ConsoleColor.Cyan);
    }

    public new void Info(string message) {
        Send(message, "INFO", ConsoleColor.White); 
    }

    public new void Debug(string message) {
        if (LogDebug) {
            Send(message, "DEBUG", ConsoleColor.Gray);
        }
    }
    
    public new void LogException(Exception e, string? trace = null) {
        Critical(e.Message);
        if (e.StackTrace != null) Console.WriteLine(e.StackTrace);
    }
    
    public new void Log(LogLevel level, string message) {
        switch (level) {
            case LogLevel.Emergency:
                Emergency(message);
                break;
            case LogLevel.Alert:
                Alert(message);
                break;
            case LogLevel.Critical:
                Critical(message);
                break;
            case LogLevel.Error:
                Error(message);
                break;
            case LogLevel.Warning:
                Warning(message);
                break;
            case LogLevel.Notice:
                Notice(message);
                break;
            case LogLevel.Info:
                Info(message);
                break;
            case LogLevel.Debug:
                Debug(message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }
    
    public MainLogger(string logFilePath, string mainThreadName, TimeZoneInfo timezone, bool logDebug) {
        this.mainThreadName = mainThreadName;
        this.timezone = timezone;
        LogDebug = logDebug;

        logSendQueue = new LogSendQueue();
        mainLogFile = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        new Thread(MainLogThread).Start();
    }

    public void Send(string message, string prefix, ConsoleColor color) {
        var time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        var threadName = Thread.CurrentThread.Name ?? mainThreadName;
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("[" + time.ToString("HH:mm:ss") + "] ");
        Console.ForegroundColor = color;
        Console.WriteLine($"[{threadName}/{prefix}] {message}");
        Console.ResetColor();
        
        logSendQueue.Add("[" + time.ToString("yy-MM-dd") + "] [" + time.ToString("HH:mm:ss") + "] " + $"[{threadName}/{prefix}] {message}" + "\n");
    }

    private void MainLogThread(object? obj) {
        while (!Shutdown) {
            var entry = logSendQueue.Get();
            if (entry == null)
                continue;
            
            mainLogFile.Write(Encoding.UTF8.GetBytes(entry));
            mainLogFile.Flush();
        }
    }

    private class LogSendQueue {

        private readonly List<string> queue = new();

        public void Add(string message) {
            queue.Add(message);
        }

        public string? Get() {
            if (queue.Count <= 0) return null;

            var next = queue.First();
            queue.RemoveAt(0);
            return next;
        }
    }
}
    