namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>
/// Represents a single event entry.
/// </summary>
public record EventEntry(DateTime Timestamp, string WorkerName, bool Success, string Message, TimeSpan Duration);

/// <summary>
/// Thread-safe circular buffer for recent events.
/// </summary>
public class RecentEventsBuffer
{
    private readonly EventEntry?[] _buffer;
    private readonly int _capacity;
    private int _index;
#if NET8_0
    private readonly object _lock = new();
#else
    private readonly Lock _lock = new();
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="RecentEventsBuffer"/> class.
    /// </summary>
    /// <param name="capacity">The maximum number of events to store.</param>
    public RecentEventsBuffer(int capacity)
    {
        _capacity = capacity;
        _buffer = new EventEntry?[capacity];
        _index = 0;
    }

    /// <summary>
    /// Adds an event to the buffer.
    /// </summary>
    public void Add(string workerName, bool success, string message, TimeSpan duration)
    {
        if (_capacity == 0)
            return;

        var entry = new EventEntry(DateTime.Now, workerName, success, message, duration);

        lock (_lock)
        {
            _buffer[_index] = entry;
            _index = (_index + 1) % _capacity;
        }
    }

    /// <summary>
    /// Gets the most recent events in chronological order (oldest to newest).
    /// </summary>
    public List<EventEntry> GetRecent()
    {
        if (_capacity == 0)
            return [];

        lock (_lock)
        {
            var result = new List<EventEntry>(_capacity);

            // Start from the oldest entry and go to the newest
            for (int i = 0; i < _capacity; i++)
            {
                var entry = _buffer[(_index + i) % _capacity];
                if (entry != null)
                    result.Add(entry);
            }

            // Reverse to show newest first
            result.Reverse();
            return result;
        }
    }
}