namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Captures explicitly requested test-host trace observations for runtime verification.
/// </summary>
public static class ServiceBusTraceProbe
{
    private static readonly ConcurrentDictionary<string, Observation> _observations = new();

    /// <summary>
    /// Records a bounded observation for an explicitly marked test message.
    /// </summary>
    public static void Record(string messageId, Snapshot before, Snapshot after)
    {
        if (_observations.Count < 64)
            _observations.TryAdd(messageId, new Observation(before, after));
    }

    /// <summary>
    /// Removes and returns the requested observation.
    /// </summary>
    public static Observation Take(string messageId)
        => _observations.TryRemove(messageId, out var observation) ? observation : throw new NotFoundException("The trace observation is not ready.");

    /// <summary>
    /// Captures the current activity and its locally available ancestry.
    /// </summary>
    public static Snapshot Capture()
    {
        var current = Activity.Current;
        var contexts = new List<string>();
        var parents = new List<string>();
        var links = new List<string>();
        for (var activity = current; activity is not null; activity = activity.Parent)
        {
            contexts.Add($"{activity.TraceId}/{activity.SpanId}");
            parents.Add($"{activity.TraceId}/{activity.ParentSpanId}");
            links.AddRange(activity.Links.Select(link => $"{link.Context.TraceId}/{link.Context.SpanId}"));
        }

        return new Snapshot(current?.Id, contexts.ToArray(), parents.ToArray(), links.ToArray(), current?.GetBaggageItem("region"));
    }

    /// <summary>
    /// Describes activity context before and after CoreEx receive handling.
    /// </summary>
    /// <param name="Before">The host-supplied context.</param>
    /// <param name="After">The enriched context.</param>
    public sealed record Observation(Snapshot Before, Snapshot After);

    /// <summary>
    /// Describes the available ancestry and links without exporting message data.
    /// </summary>
    /// <param name="Id">The invocation activity identifier.</param>
    /// <param name="Contexts">Local activity contexts.</param>
    /// <param name="Parents">Parent references.</param>
    /// <param name="Links">Producer links.</param>
    /// <param name="Region">The test baggage value.</param>
    public sealed record Snapshot(string? Id, string[] Contexts, string[] Parents, string[] Links, string? Region);
}
