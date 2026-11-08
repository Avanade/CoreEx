namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Captures explicitly requested test-host trace observations for runtime verification.
/// </summary>
public static class ServiceBusTraceProbe
{
    private static readonly ConcurrentDictionary<string, Observation> _observations = new();
    private static readonly ConcurrentDictionary<string, Snapshot> _processing = new();

    /// <summary>Captures correlation inside an explicitly observed business receive boundary.</summary>
    public static void RecordProcessing(string messageId)
    {
        if (_processing.Count < 64)
            _processing.TryAdd(messageId, Capture());
    }

    /// <summary>
    /// Records a bounded observation for an explicitly marked test message.
    /// </summary>
    public static void Record(string messageId, Snapshot before, Snapshot after)
    {
        if (_observations.Count < 64)
        {
            _processing.TryRemove(messageId, out var processing);
            _observations.TryAdd(messageId, new Observation(before, after, processing));
        }
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

        return new Snapshot(current?.Id, contexts.ToArray(), parents.ToArray(), links.ToArray(), current?.GetBaggageItem("region"),
            CoreEx.ExecutionContext.TryGetCurrent(out var ec) ? ec.CorrelationId : null, current?.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName) as string, new EventData().CorrelationId);
    }

    /// <summary>
    /// Describes activity context before and after CoreEx receive handling.
    /// </summary>
    /// <param name="Before">The host-supplied context.</param>
    /// <param name="After">The enriched context.</param>
    /// <param name="Processing">The context inside the business receive boundary.</param>
    public sealed record Observation(Snapshot Before, Snapshot After, Snapshot? Processing);

    /// <summary>
    /// Describes the available ancestry and links without exporting message data.
    /// </summary>
    /// <param name="Id">The invocation activity identifier.</param>
    /// <param name="Contexts">Local activity contexts.</param>
    /// <param name="Parents">Parent references.</param>
    /// <param name="Links">Producer links.</param>
    /// <param name="Region">The test baggage value.</param>
    /// <param name="CorrelationId">The ambient workflow correlation.</param>
    /// <param name="ActivityCorrelationId">The common tracing attribute.</param>
    /// <param name="EventCorrelationId">The default correlation on a newly created event.</param>
    public sealed record Snapshot(string? Id, string[] Contexts, string[] Parents, string[] Links, string? Region, string? CorrelationId, string? ActivityCorrelationId, string? EventCorrelationId);
}
