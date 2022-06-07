using System.Diagnostics;
using SkyWing.SkyWing.Utils;

namespace SkyWing.SkyWing; 

public sealed class Server {

    private readonly MainLogger logger; 

    public bool Shutdown { get; set; } = false;

    public int TickCounter { get; private set; } = 0; 
    private float NextTick { get; set; } = 0;
    private int CurrentTPS { get; set; } = 20;
    private int CurrentTPSUse { get; set; } = 0;

    private readonly Dictionary<int, int> AverageTPS = new();
    private readonly Dictionary<int, int> AverageTPSUse = new();

    /*
     * Order:
     * 1. Set logger
     * 2. Load properties file.
     */

    public Server(MainLogger logger) {
        this.logger = logger;
        // Initialize server resources and dependencies.
        
        // Load the server properties file.
        var propertiesFile = new FileInfo("./server.properties");
        var propertiesObject = new Properties(propertiesFile);
        
        // Get network properties.
        var mainBindAddress = propertiesObject.GetValue("bedrock-bind-address", "0.0.0.0");
        var mainBindPort = propertiesObject.GetValue("bedrock-bind-port", "19132");

        TickHandler();
    }

    private void TickHandler() { 
        NextTick = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

        try {
            while (!Shutdown) { 
                var TickTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                if (TickTime - NextTick < -0.025) continue;

                Tick(); 
                var tickNow = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                CurrentTPS = Math.Min(20, (int) (1 / MathF.Max(0.001f, tickNow-TickTime)));
                CurrentTPSUse = Math.Min(1, (int) ((tickNow - TickTime) / 0.05f));

                var index = TickCounter % 20;
                AverageTPS[index] = CurrentTPS;
                AverageTPSUse[index] = CurrentTPSUse;

                if (NextTick - TickTime < -1) {
                    NextTick = TickTime;
                } else {
                    NextTick += 0.05F;
                }
            }
        } catch (Exception e) {
            logger.Error(e.Message);
        }
    }

    private void Tick() { 
        ++TickCounter;
    }
}