namespace SkyWing.SkyWing.Network;

public class NetworkSession {
    
    public string Ip { get; set; }
    public bool Connected { get; private set; } = true;

    public void Tick() {
        
    }

    public void Disconnect(string reason) {
        
    }

    public void OnClientDisconnect(string reason) {
        
    }

    public void HandleEncoded(byte[] payload) {
        
    }

    public void UpdatePing(long pingMs) {
        
    }
}

public class NetworkSessionManager {

    public Dictionary<int, NetworkSession> Sessions { get; } = new();

    public int SessionCount => Sessions.Count;

    public void Add(NetworkSession session) {
        Sessions[session.GetHashCode()] = session;
    }

    public void Remove(NetworkSession session) {
        Sessions.Remove(session.GetHashCode());
    }

    public void Tick() {
        foreach (var (_, session) in Sessions) {
            session.Tick();
            if (!session.Connected) Remove(session);
        }
    }

    public void Close(string reason = "") {
        foreach (var (_, session) in Sessions) {
            session.Disconnect(reason);
        }
        Sessions.Clear();
    }

}