using BedrockProtocol.Packet;

namespace SkyWing.SkyWing.Network.Mcpe;

public interface PacketSender {

    public void Send(byte[] payload, bool immediate);

    public void Close(string reason = "unknown reason");
    
}

public interface PacketBroadcaster {

    public void BroadcastPackets(NetworkSession[] recipients, ClientboundPacket[] packets);
    
}

public class PacketHandlingException : Exception{

    public PacketHandlingException(string message) : base(message) {
    }
    
}
