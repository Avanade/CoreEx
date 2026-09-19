using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Extensions;
using CoreEx.Events.Publishing;
using System.Diagnostics;

namespace CoreEx.Events.Test.Unit;

[TestFixture]
public class CloudEventTracingExtensionsTests
{
    private static CloudEvent CreateCloudEvent(string id, string? traceParent = null, string? traceState = null)
    {
        var ce = new CloudEvent { Id = id, Type = "test.event", Source = new Uri("urn:test"), Time = DateTimeOffset.UtcNow };

        if (traceParent is not null)
            ce.SetExtensionAttribute("traceparent", traceParent);

        if (traceState is not null)
            ce.SetExtensionAttribute("tracestate", traceState);

        return ce;
    }

    /// <summary>
    /// Starts a standalone, listened-to <see cref="Activity"/> on its own uniquely-named <see cref="ActivitySource"/> - simulating an originating trace (e.g. an API request, or a batch-level relay span)
    /// that is entirely independent of the test's own ambient activity.
    /// </summary>
    private static ActivityScope StartActivity(string activityName) => new(activityName);

    private sealed class ActivityScope : IDisposable
    {
        private readonly ActivitySource _source;
        private readonly ActivityListener _listener;

        public ActivityScope(string activityName)
        {
            _source = new ActivitySource($"test.{activityName}.{Guid.NewGuid()}");
            _listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == _source.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(_listener);

            Activity = _source.StartActivity(activityName)!;
            Activity.Should().NotBeNull();
        }

        public Activity Activity { get; }

        public void Dispose()
        {
            Activity.Dispose();
            _listener.Dispose();
            _source.Dispose();
        }
    }

    private static List<Activity> ListenForMarkers()
    {
        var markers = new List<Activity>();
        var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == CloudEventTracingExtensions.RelayMarkerActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => { lock (markers) markers.Add(a); }
        };
        ActivitySource.AddActivityListener(listener);
        return markers;
    }

    [Test]
    public void LinkTraceContext_NullActivity_IsNoOp()
    {
        // Must not throw - the extension is called unconditionally from every relay implementation regardless of whether instrumentation is enabled.
        ((Activity?)null).LinkTraceContext([CreateCloudEvent("evt-1", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01")]);
    }

    [Test]
    public void LinkTraceContext_AddsOneLinkPerDistinctTraceParent()
    {
        using var producer = StartActivity("original-request");

        var events = new[]
        {
            CreateCloudEvent("evt-1", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"),
            CreateCloudEvent("evt-2", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"), // same traceparent as evt-1 - must not add a second, redundant link.
            CreateCloudEvent("evt-3", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"),
            CreateCloudEvent("evt-4") // no traceparent at all - must be skipped without error.
        };

        producer.Activity.LinkTraceContext(events);

        producer.Activity.Links.Should().HaveCount(2);
    }

    [Test]
    public void EmitRelayMarkers_NoTraceParent_EmitsNoMarker()
    {
        var markers = ListenForMarkers();

        new[] { new DestinationEvent("dest", CreateCloudEvent("evt-1")) }.EmitRelayMarkers();

        markers.Should().BeEmpty();
    }

    [Test]
    public void EmitRelayMarkers_PerEvent_IsParentedToItsOwnOriginatingTrace_AndLinkedToTheBatchActivity()
    {
        using var producer = StartActivity("original-request");
        using var batch = StartActivity("relay-batch");
        var markers = ListenForMarkers();

        var destinationEvents = new[] { new DestinationEvent("test-destination", CreateCloudEvent("evt-1", producer.Activity.Id!)) };

        destinationEvents.EmitRelayMarkers(batch.Activity);

        markers.Should().ContainSingle();
        var marker = markers[0];
        marker.TraceId.Should().Be(producer.Activity.TraceId);
        marker.ParentSpanId.Should().Be(producer.Activity.SpanId);
        marker.Kind.Should().Be(ActivityKind.Producer);
        marker.GetTagItem("outbox.destination").Should().Be("test-destination");
        marker.GetTagItem("outbox.event.id").Should().Be("evt-1");
        marker.GetTagItem("outbox.event.type").Should().Be("test.event");
        marker.Links.Should().ContainSingle(l => l.Context.SpanId == batch.Activity.SpanId);
    }

    [Test]
    public void EmitRelayMarkers_MultipleEvents_EachGetsItsOwnMarkerInItsOwnTrace()
    {
        using var producer1 = StartActivity("original-request-1");
        using var producer2 = StartActivity("original-request-2");
        var markers = ListenForMarkers();

        // Two events from two entirely unrelated originating traces, relayed together in the same physical batch.
        var destinationEvents = new[]
        {
            new DestinationEvent("dest-1", CreateCloudEvent("evt-1", producer1.Activity.Id!)),
            new DestinationEvent("dest-2", CreateCloudEvent("evt-2", producer2.Activity.Id!))
        };

        destinationEvents.EmitRelayMarkers();

        markers.Should().HaveCount(2);
        markers.Should().ContainSingle(m => m.TraceId == producer1.Activity.TraceId && (string?)m.GetTagItem("outbox.event.id") == "evt-1");
        markers.Should().ContainSingle(m => m.TraceId == producer2.Activity.TraceId && (string?)m.GetTagItem("outbox.event.id") == "evt-2");
    }
}
