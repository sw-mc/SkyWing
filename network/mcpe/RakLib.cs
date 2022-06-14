using System.Diagnostics;
using System.Net.Sockets;
using SkyWing.RakLib;
using SkyWing.RakLib.Generic;
using SkyWing.RakLib.Ipc;
using SkyWing.RakLib.server;
using SkyWing.RakLib.Server;
using RakServer = SkyWing.RakLib.Server.Server;
using SkyWing.SkyWing.Utils;
using SkyWing.utils;
using ThreadState = System.Threading.ThreadState;

namespace SkyWing.SkyWing.Network.Mcpe;

public class RakLibInterface : ServerEventListener, AdvancedNetworkInterface {
    
    private const int MCPE_RAKNET_PROTOCOL_VERSION = 10;
    private const byte MCPE_RAKNET_PACKET_ID = 0xfe;

    public Server Server { get; }
    public Network Network { get; private set; }
    
    public int RakServerId { get; }
    private RakLibServer RakLib { get; }
    

    private readonly Dictionary<int, NetworkSession> sessions = new();
    
    private RakLibToUserThreadMessageReceiver EventReceiver { get; }
    private UserToRakLibThreadMessageSender ThreadInterface { get; }
    

    public RakLibInterface(Server server, string ip, int port, bool ipv6) {
        Server = server;
        RakServerId = new Random().Next(0, int.MaxValue);

        var mainToThreadBuffer = new ThreadedBuffer<byte[]>();
        var threadToMainBuffer = new ThreadedBuffer<byte[]>();

        RakLib = new RakLibServer(
            server.Logger,
            mainToThreadBuffer,
            threadToMainBuffer,
            new InternetAddress(ip, port, ipv6 ? InternetVersion.Ipv6 : InternetVersion.Ipv4),
            RakServerId,
            1492, //TODO: SkyWing.yml setting
            MCPE_RAKNET_PROTOCOL_VERSION
        );

        EventReceiver = new RakLibToUserThreadMessageReceiver(new RakLibChannelReader(threadToMainBuffer));
        ThreadInterface = new UserToRakLibThreadMessageSender(new RakLibChannelWriter(mainToThreadBuffer));
    }

    public void OnClientConnect(int sessionId, string address, int port, long clientId) {
        var session = new NetworkSession();
        sessions[sessionId] = session;
    }

    public void OnClientDisconnect(int sessionId, string reason) {
        if (!sessions.ContainsKey(sessionId)) return;
        var session = sessions[sessionId];
        sessions.Remove(sessionId);
        session.OnClientDisconnect(reason);
    }

    public void Close(int sessionId) {
        if (!sessions.ContainsKey(sessionId)) return;
        sessions.Remove(sessionId);
        ThreadInterface.CloseSession(sessionId);
    }

    public void OnPacketReceive(int sessionId, byte[] packet) {
        if (!sessions.ContainsKey(sessionId)) return;
        
        if (packet.Length == 0 || packet[0] == MCPE_RAKNET_PACKET_ID) {
            Server.Logger.Debug("Non-FE packet received");
            return;
        }

        var session = sessions[sessionId];
        var address = session.Ip;
        try {
            session.HandleEncoded(packet.ToList().GetRange(1, packet.Length - 1).ToArray());
        }
        catch (PacketHandlingException e) {
            var b = new byte[6]; new Random().NextBytes(b);
            var id = Convert.ToHexString(b);
            Server.Logger.Error($"Bad packet (error ID {id}): " + e.Message);
                
            session.Disconnect($"Packet processing error (Error ID: {id})");
            ThreadInterface.BlockAddress(address, 5);
        }
    }

    public void OnRawPacketReceive(string address, int port, byte[] payload) {
        Network.ProcessRawPacket(this, address, port, payload);
    }

    public void OnPacketAck(int sessionId, int identifierAck) {
        //NOOP
    }

    public void OnBandwidthStatsUpdate(long bytesSentDiff, long bytesReceivedDiff) {
        Network.BandwidthTracker.Add(bytesSentDiff, bytesReceivedDiff);
    }

    public void OnPingMeasure(int sessionId, long pingMs) {
        if (sessions.ContainsKey(sessionId))
            sessions[sessionId].UpdatePing(pingMs);
    }

    public void Start() {
        Server.Logger.Debug("Waiting for RakLib to start...");
        try {
            RakLib.StartAndWait();
        }
        catch (SocketException e) {
            throw new NetworkInterfaceStartException(e.Message);
        }
        Server.Logger.Debug("RakLib booted successfully.");
    }

    public void SetName(string name) {
        ThreadInterface.SetName(name);
    }

    public void Tick() {
        if (RakLib.Shutdown || RakLib.Thread is not {ThreadState: ThreadState.Running}) {
            if (RakLib.CrashInfo != null)
                throw new RakLibException("RakLib crashed: " + RakLib.CrashInfo.MakePrettyMessage());
        }
        else {
            throw new RakLibException("RakLib crashed without crash information");
        }

        //EventReceiver.Handle(this);
    }

    public void Shutdown() {
        RakLib.Shutdown = true;
    }

    public void BlockAddress(string address, int timeout = 300) {
        ThreadInterface.BlockAddress(address, timeout);
    }

    public void UnblockAddress(string address) {
        ThreadInterface.UnblockAddress(address);
    }

    public void SetNetwork(Network net) {
        Network = net;
    }

    public void SendRawPacket(string address, int port, byte[] payload) {
        ThreadInterface.SendRaw(address, port, payload);
    }

    public void AddRawPacketFilter(string regex) {
        ThreadInterface.AddRawPacketFilter(regex);
    }
    
}

public sealed class RakLibServer {

    private InternetAddress Address { get; }
    private MainLogger Logger { get; }
    
    public bool Shutdown { get; set; } = false;
    public bool CleanShutdown { get; private set; } = false;
    public bool Ready { get; private set; } = false;

    public Thread? Thread { get; private set; } = null;

    private readonly int serverId;
    private readonly int maxMtuSize;
    private readonly int protocolVersion;

    private ThreadedBuffer<byte[]> MainToThreadBuffer { get; }
    private ThreadedBuffer<byte[]> ThreadToMainBuffer { get; }

    public RakLibServerCrashInfo? CrashInfo { get; private set; }

    public RakLibServer(MainLogger logger, ThreadedBuffer<byte[]> mainToThreadBuffer,
        ThreadedBuffer<byte[]> threadToMainBuffer, InternetAddress address, int serverId, int maxMtuSize,
        int protocolVersion) {
        Logger = logger;
        Address = address;

        this.serverId = serverId;
        this.maxMtuSize = maxMtuSize;
        this.protocolVersion = protocolVersion;

        MainToThreadBuffer = mainToThreadBuffer;
        ThreadToMainBuffer = threadToMainBuffer;
    }

    public void StartAndWait() {
        Thread = new Thread(RakLibServerThread);
        Thread.Start();
        while (!Ready && CrashInfo == null) {
            // Wait till raklib is ready or an error occurs
        }

        if (CrashInfo == null) return;
        
        CleanShutdown = false;
        Shutdown = true;
        throw new RakLibException("RakLib failed to start: " + CrashInfo.MakePrettyMessage());
    }

    private void RakLibServerThread() {
        try {
            var socket = new RakNetSocket(Address);

            var manager = new RakServer(Logger, serverId, socket, maxMtuSize,
                new SimpleProtocolAcceptor(protocolVersion),
                new UserToRakLibThreadMessageReceiver(new RakLibChannelReader(MainToThreadBuffer)),
                new RakLibToUserThreadMessageSender(new RakLibChannelWriter(ThreadToMainBuffer)));

            Ready = true;

            while (!Shutdown) {
                manager.TickProcessor();
            }
            manager.WaitShutdown();
            CleanShutdown = true;
        }
        catch (SocketException e) {
            CrashInfo = RakLibServerCrashInfo.From(e);

        }
        catch (Exception e) {
            CrashInfo = RakLibServerCrashInfo.From(e);
            Logger.LogException(e);

            CleanShutdown = false;
            Shutdown = true;
        }
    }

    public sealed class RakLibServerCrashInfo {

        public StackTrace StackTrace { get; init; }
        public string? Class { get; init; }
        public int Line { get; init; }
        public string? File { get; init; }
        public string? Source { get; set; }
        public string? Message { get; init; }
        public string? HelpLink { get; init; }

        public RakLibServerCrashInfo() {
        }

        public RakLibServerCrashInfo(StackTrace stackTrace, string? targetClass, int line, string? file, string? source, string? message, string? helpLink) {
            StackTrace = stackTrace;
            Class = targetClass;
            Line = line;
            File = file;
            Source = source;
            Message = message;
            HelpLink = helpLink;
        }

        public static RakLibServerCrashInfo From(Exception e) {
            var trace = new StackTrace(e, true);
            return new RakLibServerCrashInfo {
                StackTrace = trace,
                Class = trace.GetFrame(0)?.GetMethod()?.ReflectedType?.FullName,
                Line = trace.GetFrame(0)?.GetFileLineNumber() ?? 0,
                File = trace.GetFrame(0)?.GetFileName(),
                Source = e.Source,
                Message = e.Message,
                HelpLink = e.HelpLink
            };
        }

        public string MakePrettyMessage() {
            return Class ?? "Fatal Error" + ": " + Message + " in " + File + " on line " + Line;
        }
    }
    
}

public sealed class RakLibChannelReader : InterThreadChannelReader {
    
    private readonly ThreadedBuffer<byte[]> buffer;

    public RakLibChannelReader(ThreadedBuffer<byte[]> buffer) {
        this.buffer = buffer;
    }

    public byte[]? Read() {
        return buffer.Shift();
    }
    
}

public sealed class RakLibChannelWriter : InterThreadChannelWriter {

    private readonly ThreadedBuffer<byte[]> buffer;

    public RakLibChannelWriter(ThreadedBuffer<byte[]> buffer) {
        this.buffer = buffer;
    }

    public void Write(byte[] str) {
        buffer.Add(str);
    }
    
}

public class RakLibException : Exception {

    public RakLibException(string message) : base(message) {
    }
    
}