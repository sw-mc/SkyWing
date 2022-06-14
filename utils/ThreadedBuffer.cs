namespace SkyWing.utils; 

public class ThreadedBuffer<T> {

    private List<T> Buffer = new();

    public void Add(T value) {
        Buffer.Add(value);
    }

    public T? Shift() {
        if (Buffer.Count < 1) return default;

        var value = Buffer[0];
        Buffer.RemoveAt(0);
        return value;
    }
}