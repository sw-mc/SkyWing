using System.Text;
using System.Text.RegularExpressions;
using SkyWing.SkyWing.Network.Mcpe;
using SkyWing.SkyWing.Utils;

namespace SkyWing.SkyWing.Network; 

public class Network {
    
    public MainLogger Logger { get; init; }

    public Dictionary<int, NetworkInterface> Interfaces { get; } = new();
    public Dictionary<int, AdvancedNetworkInterface> AdvancedInterfaces { get; } = new();
    public Dictionary<int, RawPacketHandler> RawPacketHandlers { get; } = new();

    public Dictionary<string, long> BannedIps { get; } = new();

    public BidirectionalBandwidthStatsTracker BandwidthTracker { get; }

    public string Name {
        get => name;
        set {
            name = value;
            UpdateName();
        }
    }
    private string name;
    
    public NetworkSessionManager SessionManager { get; }

    public Network(MainLogger logger) {
        Logger = logger;
        BandwidthTracker = new BidirectionalBandwidthStatsTracker(5);
        SessionManager = new NetworkSessionManager();
    }

    public void Tick() {
        foreach (var (_, networkInterface) in Interfaces) {
            networkInterface.Tick();
        }
        SessionManager.Tick();
    }

    public bool RegisterInterface(NetworkInterface networkInterface) {
        networkInterface.Start();
        var hash = networkInterface.GetHashCode();
        Interfaces[hash] = networkInterface;
        if (networkInterface.GetType() == typeof(AdvancedNetworkInterface)) {
            AdvancedInterfaces[hash] = (AdvancedNetworkInterface) networkInterface;
            ((AdvancedNetworkInterface) networkInterface).SetNetwork(this);
            foreach (var (ip,_) in BannedIps) {
                ((AdvancedNetworkInterface) networkInterface).BlockAddress(ip);
            }
            foreach (var (_, handler) in RawPacketHandlers) {
                ((AdvancedNetworkInterface) networkInterface).AddRawPacketFilter(handler.GetPattern());
            }
        }
        networkInterface.SetName(Name);
        return true;
    }

    public void UnregisterInterface(NetworkInterface networkInterface) {
        var hash = networkInterface.GetHashCode();
        if (!Interfaces.ContainsKey(hash))
            throw new ArgumentException("Interface does not exist on this network.", nameof(networkInterface));

        Interfaces.Remove(hash);
        AdvancedInterfaces.Remove(hash);
        networkInterface.Shutdown();
    }

    public void UpdateName() {
        foreach (var (_, networkInterface) in Interfaces) {
            networkInterface.SetName(name);
        }
    }

    public void SendPacket(string address, int port, byte[] payload) {
        foreach (var (_, networkInterface) in AdvancedInterfaces) {
            networkInterface.SendRawPacket(address, port, payload);
        }
    }
    
    public void BlockAddress(string address, int timeout = 300) {
        BannedIps[address] =
            timeout > 0 ? new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() + timeout : Int64.MaxValue;
        foreach (var (_, networkInterface) in AdvancedInterfaces) {
            networkInterface.BlockAddress(address, timeout);
        }
    }

    public void UnblockAddress(string address) {
        BannedIps.Remove(address);
        foreach (var (_, networkInterface) in AdvancedInterfaces) {
            networkInterface.BlockAddress(address);
        }
    }

    public void RegisterRawPacketHandler(RawPacketHandler handler) {
        RawPacketHandlers[handler.GetHashCode()] = handler;

        var regex = handler.GetPattern();
        foreach (var (_, networkInterface) in AdvancedInterfaces) {
            networkInterface.AddRawPacketFilter(regex);
        }
    }

    public void UnregisterRawPacketHandler(RawPacketHandler handler) {
        RawPacketHandlers.Remove(handler.GetHashCode());
    }

    public void ProcessRawPacket(AdvancedNetworkInterface networkInterface, string address, int port, byte[] packet) {
        if (BannedIps.ContainsKey(address) &&
            new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() < BannedIps[address]) {
            Logger.Debug($"Dropped raw packet from banned address {address}:{port}");
            return;
        }

        var handled = false;
        foreach (var (_, handler) in RawPacketHandlers) {
            if (Regex.IsMatch(Encoding.ASCII.GetString(packet), handler.GetPattern())) {
                try {
                    handled = handler.Handle(networkInterface, address, port, packet);
                }
                catch (PacketHandlingException e) {
                    handled = true;
                    Logger.Error($"Bad raw packet from /{address}:{port}" + e.Message);
                    BlockAddress(address, 600);
                }
            }
        }
        if(!handled) Logger.Debug($"Unhandled raw packet from /{address}:{port}");
    }

}

public interface NetworkInterface {

    public void Start();

    public void SetName(string name);

    public void Tick();

    public void Shutdown();
}

public interface AdvancedNetworkInterface : NetworkInterface {

    public void BlockAddress(string address, int timeout = 300);

    public void UnblockAddress(string address);

    public void SetNetwork(Network network);

    public void SendRawPacket(string address, int port, byte[] payload);

    public void AddRawPacketFilter(string regex);
}

public class NetworkInterfaceStartException : Exception {

    public NetworkInterfaceStartException(string message) : base(message) {
    }
    
}

public interface RawPacketHandler {

    public string GetPattern();

    public bool Handle(AdvancedNetworkInterface advancedInterface, string address, int port, byte[] packet);
}



