namespace SkyWing.SkyWing.Network;

public class BandwidthStatsTracker {

    private long[] history;
    private int nextIndex;
    private long bytesSinceLast;
    public long TotalBytes { get; private set; }

    public BandwidthStatsTracker(int historySize) {
        history = new long[historySize];
        Array.Fill(history, 0, 0, historySize);
    }

    public void Add(long bytes) {
        TotalBytes += bytes;
        bytesSinceLast += bytes;
    }

    public void RotateHistory() {
        history[nextIndex] = bytesSinceLast;
        bytesSinceLast = 0;
        nextIndex = (nextIndex + 1) % history.Length;
    }

    public long AverageBytes => history.Sum() / history.Length;

    public void ResetHistory() {
        var historySize = history.Length;
        history = new long[historySize];
        Array.Fill(history, 0, 0, historySize);
    }
}

public class BidirectionalBandwidthStatsTracker {

    public BandwidthStatsTracker Send { get; }
    public BandwidthStatsTracker Receive { get; }

    public BidirectionalBandwidthStatsTracker(int historySize) {
        Send = new BandwidthStatsTracker(historySize);
        Receive = new BandwidthStatsTracker(historySize);
    }

    public void Add(long sendBytes, long recvBytes) {
        Send.Add(sendBytes);
        Receive.Add(recvBytes);
    }

    public void RotateHistory() {
        Send.RotateHistory();
        Receive.RotateHistory();
    }

    public void ResetHistory() {
        Send.ResetHistory();
        Receive.ResetHistory();
    }
}