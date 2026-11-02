namespace CoreEx.Events.Test.Unit;

[TestFixture]
public class EventBaggageTests
{
    private const string TraceParent = "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01";

    [TestCase(8192, true)]
    [TestCase(8193, false)]
    public void HeaderByteLimit_HasExactInclusiveBoundary(int bytes, bool accepted)
    {
        using var activity = new Activity("host").Start();
        var header = "k=" + new string('x', bytes - 2);
        Encoding.UTF8.GetByteCount(header).Should().Be(bytes);
        EventBaggage.Import(activity, EventBaggage.Parse(header));
        activity.Baggage.Count().Should().Be(accepted ? 1 : 0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task EnvelopeImport_IsOptInAndIndependentOfTraceLinking(bool enabled)
    {
        using var activity = new Activity("host").Start();
        activity.AddBaggage("existing", "local");
        var formatter = new EventFormatter { TraceParentAttributeName = "tp", TraceStateAttributeName = "ts", TraceBaggageAttributeName = "tb" };
        var cloudEvent = Event();
        cloudEvent.SetExtensionAttribute("tp", TraceParent);
        cloudEvent.SetExtensionAttribute("ts", "vendor=value");
        cloudEvent.SetExtensionAttribute("tb", "existing=remote,region=west%20coast;property=value,broken");
        var subscriber = new Subscriber(formatter) { PropagateBaggage = enabled };
        (await subscriber.ReceiveCloudEventAsync(cloudEvent)).IsSuccess.Should().BeTrue();
        subscriber.Region.Should().Be(enabled ? "west coast" : null);
        subscriber.Existing.Should().Be("local");
        activity.Links.Should().ContainSingle();
        subscriber.Received!.TraceBaggage.Should().NotBeEmpty();
        subscriber.Received.Attributes.Should().NotContainKey("tb");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task EnvelopeWithoutTraceParent_BaggageStillUsesExplicitPolicy(bool enabled)
    {
        using var activity = new Activity("host").Start();
        var cloudEvent = Event();
        cloudEvent.SetExtensionAttribute("baggage", "region=west");
        var subscriber = new Subscriber(new EventFormatter()) { PropagateBaggage = enabled };
        await subscriber.ReceiveCloudEventAsync(cloudEvent);
        subscriber.Region.Should().Be(enabled ? "west" : null);
        activity.Links.Should().BeEmpty();
    }

    [Test]
    public async Task EnvelopeHeader_IsBoundedBeforeParsing()
    {
        using var activity = new Activity("host").Start();
        var subscriber = new Subscriber(new EventFormatter()) { PropagateBaggage = true };
        var cloudEvent = Event();
        cloudEvent.SetExtensionAttribute("baggage", "region=" + new string('x', EventBaggage.MaximumBytes));
        (await subscriber.ReceiveCloudEventAsync(cloudEvent)).IsSuccess.Should().BeTrue();
        activity.Baggage.Should().BeEmpty();
        cloudEvent.SetExtensionAttribute("baggage", string.Join(",", Enumerable.Range(0, 70).Select(i => $"key{i}=value")));
        await subscriber.ReceiveCloudEventAsync(cloudEvent);
        activity.Baggage.Should().HaveCount(EventBaggage.MaximumEntries);
    }

    [Test]
    public async Task DirectEventDataImport_IsAlsoBoundedAndPreservesExistingKeys()
    {
        using var activity = new Activity("host").Start();
        var subscriber = new Subscriber(new EventFormatter()) { PropagateBaggage = true };
        await subscriber.ReceiveEventAsync(new EventData { TraceBaggage = [new("large", new string('x', EventBaggage.MaximumBytes))] });
        activity.Baggage.Should().BeEmpty();
        await subscriber.ReceiveEventAsync(new EventData { TraceBaggage = Enumerable.Range(0, 70).Select(i => new KeyValuePair<string, string?>($"key{i}", "value")) });
        activity.Baggage.Should().HaveCount(EventBaggage.MaximumEntries);
        await subscriber.ReceiveEventAsync(new EventData { TraceBaggage = [new("key0", "remote")] });
        activity.GetBaggageItem("key0").Should().Be("value");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PublishingAmbientBaggage_RequiresIndependentOptIn(bool enabled)
    {
        using var activity = new Activity("producer").SetIdFormat(ActivityIdFormat.W3C).Start();
        activity.AddBaggage("region", "west");
        var formatter = new EventFormatter { CaptureBaggage = enabled };
        var publisher = new Publisher(formatter);
        publisher.Add("destination", [Event()]);
        await publisher.PublishAsync();
        var cloudEvent = publisher.GetEvents().Single().Event;
        (cloudEvent["traceparent"] as string).Should().NotBeNullOrEmpty();
        cloudEvent["baggage"].Should().Be(enabled ? "region=west" : null);
    }

    [Test]
    public void ExplicitBaggage_IsPreservedWithoutAmbientCapture()
    {
        using var activity = new Activity("producer").SetIdFormat(ActivityIdFormat.W3C).Start();
        activity.AddBaggage("unrelated", "ambient");
        var formatter = new EventFormatter();
        var cloudEvent = Event();
        formatter.AddTracing(cloudEvent, traceBaggage: [new("region", "explicit")]);
        cloudEvent["baggage"].Should().Be("region=explicit");
        var converted = formatter.ConvertToCloudEvent(new EventData { TraceParent = TraceParent, TraceBaggage = [new("region", "data")] });
        converted["baggage"].Should().Be("region=data");
        formatter.AddTracing(converted);
        converted["baggage"].Should().Be("region=data");
    }

    [Test]
    public void RelayLinking_DoesNotImportEnvelopeBaggage()
    {
        using var activity = new Activity("relay").Start();
        var cloudEvent = Event();
        cloudEvent.SetExtensionAttribute("traceparent", TraceParent);
        cloudEvent.SetExtensionAttribute("baggage", "region=remote");
        activity.LinkTraceContext([cloudEvent]);
        activity.Links.Should().ContainSingle();
        activity.Baggage.Should().BeEmpty();
    }

    private static CloudEvent Event() => new() { Id = "event", Source = new Uri("urn:test"), Type = "example" };

    private sealed class Subscriber(IEventFormatter formatter) : EventSubscriberBase(formatter, NullLogger<EventSubscriberBase>.Instance)
    {
        public string? Region { get; private set; }
        public string? Existing { get; private set; }
        public EventData? Received { get; private set; }

        public Task<Result> ReceiveCloudEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken = default) => ReceiveAsync(cloudEvent, cancellationToken: cancellationToken);
        public Task<Result> ReceiveEventAsync(EventData @event, CancellationToken cancellationToken = default) => ReceiveAsync(@event, cancellationToken: cancellationToken);

        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
        {
            Received = @event;
            Region = Activity.Current?.GetBaggageItem("region");
            Existing = Activity.Current?.GetBaggageItem("existing");
            return Result.SuccessTask;
        }
    }

    private sealed class Publisher(IEventFormatter formatter) : EventPublisherBase(formatter: formatter)
    {
        protected override Task OnPublishAsync(DestinationEvent[] events, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
