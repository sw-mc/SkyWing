using System.Diagnostics;
using SkyWing.SkyWing.Network;
using SkyWing.SkyWing.Network.Mcpe;
using SkyWing.SkyWing.Utils;

namespace SkyWing.SkyWing; 

public sealed class Server {

    public MainLogger Logger { get; }

    public bool Running { get; set; } = true;

    public int TickCounter { get; private set; } = 0; 
    private float NextTick { get; set; } = 0;
    private int CurrentTps { get; set; } = 20;
    private int CurrentTpsUse { get; set; } = 0;
    private Stopwatch TickTimer { get; }

    public Network.Network? Network { get; } = null;

    public bool HasStopped { get; private set; } = false;

    private readonly Dictionary<int, int> averageTps = new();
    private readonly Dictionary<int, int> averageTpsUse = new();
    
    public string Ip { get; }
    public int Port { get; init; }

    /*
     * Order:
     * 1. Set logger
     * 2. Load properties file.
     */

    public Server(MainLogger logger) {
        Logger = logger;
        // Initialize server resources and dependencies.
        
        // Load the server properties file.
        var propertiesFile = new FileInfo("./server.properties");
        var propertiesObject = new Properties(propertiesFile);
        
        // Get network properties.
        Ip = "0.0.0.0";
        Port = 19137;

        Network = new Network.Network(Logger);
        Network.Name = "SkyWing Net"; //TODO Name from MOTD

        if (!StartupPreparedNetworkInterfaces()) {
            ForceShutdownExit();
        }

        TickTimer = new Stopwatch();
        TickHandler();
        ForceShutdown();
    }

    private bool StartupPreparedConnectableNetworkInterfaces(string ip, int port, bool ivp6) {
        bool rakLibRegistered;
        try {
            rakLibRegistered = Network?.RegisterInterface(new RakLibInterface(this, ip, port, ivp6)) ?? false;
        }
        catch (NetworkInterfaceStartException e) {
            Logger.Emergency($"Failed to start RakLib on [{ip}:{port}]: " + e.Message);
            return false;
        }

        if (rakLibRegistered) {
            Logger.Info($"RakLib booted successfully on [{ip}:{port}].");
        }
        return true;
    }

    private bool StartupPreparedNetworkInterfaces() {
        return StartupPreparedConnectableNetworkInterfaces(Ip, Port, false);
    }

    private void TickHandler() { 
        TickTimer.Start();
        NextTick = TickTimer.ElapsedMilliseconds;

        try {
            while (Running) {
                var tickTime = TickTimer.ElapsedMilliseconds;
                if (tickTime - NextTick < -0.025) {
                    Thread.Sleep(20);
                    continue;
                }

                Tick(); 
                var tickNow = TickTimer.ElapsedMilliseconds;
                CurrentTps = Math.Min(20, (int) (1 / MathF.Max(0.001f, tickNow-tickTime)));
                CurrentTpsUse = Math.Min(1, (int) ((tickNow - tickTime) / 0.05f));

                var index = TickCounter % 20;
                averageTps[index] = CurrentTps;
                averageTpsUse[index] = CurrentTpsUse;

                Thread.Sleep(20);
                /*if (NextTick - tickTime < -1) {
                    NextTick = tickTime;
                } else {
                    NextTick += 500;
                }*/
            }
        } catch (Exception e) {
            Logger.Error(e.Message);
        }
    }

    private void Tick() { 
        ++TickCounter;
    }

    public void Shutdown() {
        if (Running) Running = false;
    }

    private void ForceShutdownExit() {
        ForceShutdown();
        
        Logger.Info("Press any key to exit...");
        Console.ReadKey();
        Process.GetCurrentProcess().Kill(true);
    }

    public void ForceShutdown() {
        if (HasStopped) return;

        if (Running)
            Logger.Emergency("Forcing server shutdown...");
        try {
            HasStopped = true;
            Shutdown();

            if (Network != null) {
                Network.SessionManager.Close("Server closed");

                Logger.Debug("Stopping network interfaces.");
                foreach (var (_, networkInterface) in Network.Interfaces) {
                    Logger.Debug("Stopping network interface " + networkInterface.GetType().FullName);
                    Network.UnregisterInterface(networkInterface);
                }
            }
        }
        catch (Exception e) {
            Logger.LogException(e);
            Logger.Emergency("Crashed while crashing, killing process");
            
            Logger.Info("Press any key to exit...");
            Console.ReadKey();
            Process.GetCurrentProcess().Kill(true);
        }
    }
}