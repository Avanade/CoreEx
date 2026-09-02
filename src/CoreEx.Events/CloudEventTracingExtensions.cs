namespace CoreEx.Events;

/// <summary>
/// Provides <see cref="Activity"/>/<see cref="CloudEvent"/> distributed-tracing extensions.
/// </summary>
public static class CloudEventTracingExtensions
{
    /// <summary>
    /// Links the <paramref name="activity"/> to each of the <paramref name="events"/>' originating W3C trace context (the <c>traceparent</c>/<c>tracestate</c> <see cref="CloudEvent"/> extension attributes,
    /// added as an <see cref="ActivityLink"/>).
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to link/enrich; a no-op where <see langword="null"/>.</param>
    /// <param name="events">The <see cref="CloudEvent"/>s being relayed.</param>
    /// <remarks>Used by an outbox relay to connect its own publish span back to each original producer's trace - the events being relayed were not necessarily raised within the relay's own current trace,
    /// so a plain parent/child relationship does not apply; a link is the correct W3C/OpenTelemetry mechanism for associating spans that are causally related but not nested.
    /// <para>Deliberately does <b>not</b> propagate the events' W3C <c>baggage</c> extension attribute onto <paramref name="activity"/>. A link is a one-way reference
    /// with no propagation effect, but baggage is ambient context that flows forward into whatever the current activity does next - including the relay's own outgoing publish call. A batch of events
    /// relayed together generally originates from multiple causally-unrelated operations; merging their baggage onto one shared activity would leak each event's originating context (tenant id, feature
    /// flags, anything else carried as baggage) into the outgoing call for every <i>other</i> event in the same batch. There is no merge strategy (first-wins, last-wins, de-duplicated by key) that avoids
    /// this - the fan-in shape of a batched relay is fundamentally incompatible with baggage's propagation semantics, so it is not attempted at all.</para></remarks>
    public static void LinkTraceContext(this Activity? activity, IEnumerable<CloudEvent> events)
    {
        if (activity is null)
            return;

        // De-duplicated per call - a batch can legitimately contain multiple events raised within the same originating operation (same traceparent); linking the identical context once per event would
        // add redundant, identical links and inflate span cardinality for no benefit. Lazily allocated so the (common) no-tracing-headers-at-all case costs nothing.
        HashSet<string>? seenTraceParents = null;

        foreach (var @event in events)
        {
            if (!@event.TryGetExtensionAttribute<string>("traceparent", out var traceParent) || string.IsNullOrEmpty(traceParent))
                continue;

            seenTraceParents ??= [];
            if (!seenTraceParents.Add(traceParent))
                continue;

            @event.TryGetExtensionAttribute<string>("tracestate", out var traceState);

            // isRemote: true - this context always originates from a different process (the original event producer), never the relay's own trace; confirmed empirically that the 2-arg TryParse overload
            // defaults IsRemote to false, which would otherwise mislabel every link as local-origin.
            if (ActivityContext.TryParse(traceParent, traceState, isRemote: true, out var ac))
                activity.AddLink(new ActivityLink(ac));
        }
    }
}
