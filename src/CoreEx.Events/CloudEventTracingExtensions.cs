namespace CoreEx.Events;

/// <summary>
/// Provides <see cref="Activity"/>/<see cref="CloudEvent"/> distributed-tracing extensions.
/// </summary>
public static class CloudEventTracingExtensions
{
    /// <summary>
    /// Gets the name of the dedicated <see cref="ActivitySource"/> used by <see cref="EmitRelayMarkers(IEnumerable{DestinationEvent}, Activity?, string)"/>: '<c>CoreEx.Events.Outbox.Relay</c>'.
    /// </summary>
    /// <remarks>Registered as an OpenTelemetry tracing source via <see cref="OpenTelemetry.Trace.CoreExEventsExtensions.WithCoreExEventsSources(OpenTelemetryBuilder)"/>.
    /// <para>Named to sit alongside the relay batch-span sources <c>CoreEx.Database.Outbox.Relay</c> (<c>CoreEx.Database.Outbox.DatabaseOutboxRelayInvoker</c>) and
    /// <c>CoreEx.Cosmos.Outbox.Relay</c> (<c>CoreEx.Cosmos.Outbox.CosmosDbOutboxRelayInvoker</c>) - all three form the <c>*.Outbox.Relay</c> family for outbox-relay telemetry.</para></remarks>
    public const string RelayMarkerActivitySourceName = "CoreEx.Events.Outbox.Relay";

    private static readonly ActivitySource _relayMarkerActivitySource = new(RelayMarkerActivitySourceName);

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
            if (!TryGetTraceContext(@event, out var traceParent, out var ac))
                continue;

            seenTraceParents ??= [];
            if (!seenTraceParents.Add(traceParent))
                continue;

            activity.AddLink(new ActivityLink(ac));
        }
    }

    /// <summary>
    /// Emits a short marker <see cref="Activity"/> per <paramref name="events"/> entry, started as a child of that event's own originating W3C trace context (the <c>traceparent</c>/<c>tracestate</c>
    /// <see cref="CloudEvent"/> extension attributes) - a no-op for any event with no trace context (or where <see cref="RelayMarkerActivitySourceName"/> has no listener).
    /// </summary>
    /// <param name="events">The <see cref="DestinationEvent"/>s being relayed.</param>
    /// <param name="relayActivity">The optional batch-level relay <see cref="Activity"/> (see <see cref="LinkTraceContext(Activity?, IEnumerable{CloudEvent})"/>) to link back to from each marker, so the
    /// batch's own trace remains reachable from any individual originating trace.</param>
    /// <param name="activityName">The marker <see cref="Activity"/> name; defaults to '<c>outbox.relay.publish</c>'.</param>
    /// <remarks>Deliberately the inverse of <see cref="LinkTraceContext(Activity?, IEnumerable{CloudEvent})"/>: that method links the relay's <i>one</i> batch-level span back to <i>many</i> originating
    /// traces (correct for the relay's own fan-in operation), whereas this emits <i>one small marker per originating trace</i> so every producer (e.g. the API request that raised the event) gets a
    /// deterministic, visible "this event was relayed" node regardless of how many other, unrelated events happened to share the same physical batch. Reparenting the relay's own batch span into a single
    /// originating trace was considered and rejected - it would only work when a batch happens to contain events from exactly one trace, making the relay's visibility a runtime accident rather than a
    /// guaranteed outcome.
    /// <para><b>Callers must only invoke this after the corresponding publish has completed successfully</b> (see the call sites in <c>DatabaseOutboxRelayBase</c> and <c>CosmosDbOutboxRelayProcessor</c>),
    /// never beforehand. A marker denotes "this event was relayed" - emitting it before the publish call risks a false-positive marker for an event whose publish subsequently throws and is retried
    /// (as part of the whole batch being cancelled/re-claimed, or redelivered by the Change Feed Processor), since the marker would already show as a completed, successful span.</para>
    /// <para>Each marker is started and immediately disposed regardless - no per-event duration can be meaningfully attributed even post-publish, since a single batched publish call covers every event in
    /// the batch; the marker exists purely to make the relay hop visible within the originating trace, not to time it.</para></remarks>
    public static void EmitRelayMarkers(this IEnumerable<DestinationEvent> events, Activity? relayActivity = null, string activityName = "outbox.relay.publish")
    {
        var relayLinks = relayActivity is null ? null : new[] { new ActivityLink(relayActivity.Context) };

        foreach (var de in events)
        {
            if (!TryGetTraceContext(de.Event, out _, out var ac))
                continue;

            using var marker = _relayMarkerActivitySource.StartActivity(activityName, ActivityKind.Producer, ac, links: relayLinks);
            if (marker is null)
                continue;

            marker.SetTag("outbox.destination", de.Destination);
            marker.SetTag("outbox.event.id", de.Event.Id);
            marker.SetTag("outbox.event.type", de.Event.Type);
        }
    }

    /// <summary>
    /// Attempts to parse the <paramref name="event"/>'s W3C <c>traceparent</c>/<c>tracestate</c> <see cref="CloudEvent"/> extension attributes into an <see cref="ActivityContext"/>.
    /// </summary>
    private static bool TryGetTraceContext(CloudEvent @event, out string traceParent, out ActivityContext context)
    {
        context = default;
        if (!@event.TryGetExtensionAttribute<string>("traceparent", out traceParent!) || string.IsNullOrEmpty(traceParent))
            return false;

        @event.TryGetExtensionAttribute<string>("tracestate", out var traceState);

        // isRemote: true - this context always originates from a different process (the original event producer), never the relay's own trace; confirmed empirically that the 2-arg TryParse overload
        // defaults IsRemote to false, which would otherwise mislabel every link/marker as local-origin.
        return ActivityContext.TryParse(traceParent, traceState, isRemote: true, out context);
    }
}
