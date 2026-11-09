namespace CoreEx.Events.Test.Unit;

[NonParallelizable]
public class CorrelationEventTests
{
    [TearDown]
    public void Reset() => CoreEx.ExecutionContext.Reset();

    [Test]
    public void CreationFamilies_SnapshotAndAllowClear()
    {
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "workflow" };
        CoreEx.ExecutionContext.SetCurrent(ec);
        EventData[] events =
        [
            new(), EventData.CreateEvent("Order", "Created"), EventData.CreateEvent<string>("Created"),
            EventData.CreateEventWith("value", "Created"), EventData.CreateCommand("Domain", "Order", "Send"),
            EventData.CreateCommandWith("Domain", "value", "Send"), EventData.CreateNative(),
            EventData.CreateNativeWith("value"), EventData.CreateNativeWithData(BinaryData.FromString("opaque")),
            .. EventData.CreateEventsWith(new[] { "first", "second" }, "Created")
        ];
        events.Should().OnlyContain(e => e.CorrelationId == "workflow");
        ec.CorrelationId = "later";
        events.Should().OnlyContain(e => e.CorrelationId == "workflow");
        events[0].WithCorrelationId(null).CorrelationId.Should().BeNull();
        events[1].WithCorrelationId("override").CorrelationId.Should().Be("override");
        new EventFormatter { PartitionKeyIsRequired = false }.Format(events[0]);
        events[0].CorrelationId.Should().BeNull();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DisabledCorrelation_PreservesExplicitEventMetadata(bool native)
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CoreEx:Correlation:Enabled"] = "false" }).Build();
        using var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(configuration).BuildServiceProvider();
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "ambient", ServiceProvider = services };
        CoreEx.ExecutionContext.SetCurrent(ec);
        var @event = EventData.CreateEvent("Order", "Created");
        @event.CorrelationId.Should().BeNull();
        @event.CorrelationId = "explicit";
        if (native)
            @event.AsNative();

        var formatter = new EventFormatter();
        var restored = formatter.ConvertFromCloudEvent(formatter.ConvertToCloudEvent(@event));
        restored.CorrelationId.Should().Be("explicit");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EnvelopeReconstruction_NeverInheritsAmbientAbsence(bool native)
    {
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "receiver" };
        CoreEx.ExecutionContext.SetCurrent(ec);
        var formatter = new EventFormatter { CorrelationIdAttributeName = "customcorrelation" };
        var original = new EventData { Title = "Domain.Order.Created", Source = new Uri("urn:test"), CorrelationId = null };
        if (native)
            original.AsNative();

        var persisted = formatter.ConvertToCloudEvent(original);
        formatter.ConvertFromCloudEvent(persisted).CorrelationId.Should().BeNull();
        original.CorrelationId = "explicit";
        formatter.ConvertFromCloudEvent(formatter.ConvertToCloudEvent(original)).CorrelationId.Should().Be("explicit");
    }

    [TestCase("{}")]
    [TestCase("{\"CorrelationId\":null}")]
    public void JsonReconstruction_PreservesAbsence(string json)
    {
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "receiver" };
        CoreEx.ExecutionContext.SetCurrent(ec);
        System.Text.Json.JsonSerializer.Deserialize<EventData>(json)!.CorrelationId.Should().BeNull();
        System.Text.Json.JsonSerializer.Deserialize<EventData>("{\"CorrelationId\":\"stored\"}")!.CorrelationId.Should().Be("stored");
    }

    [TestCase("override", "override", false)]
    [TestCase(null, null, false)]
    [TestCase("first", "second", false)]
    [TestCase("workflow", null, false)]
    [TestCase("bad metadata", "bad metadata", false)]
    [TestCase("override", "override", true)]
    public async Task PublisherSpan_OmitsCorrelationWithoutChangingOrDecodingMessages(string? first, string? second, bool native)
    {
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.Contains("EventPublisher", StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activities.Add
        };
        ActivitySource.AddActivityListener(listener);
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "ambient" };
        CoreEx.ExecutionContext.SetCurrent(ec);
        var formatter = new NoDecodingFormatter { PartitionKeyIsRequired = false, CorrelationIdAttributeName = "customcorrelation" };
        var publisher = new CorrelationPublisher(formatter);
        var events = new[] { new EventData { CorrelationId = first }, new EventData { CorrelationId = second } };
        if (native)
            foreach (var @event in events)
                @event.AsNative();

        publisher.Add(events);
        ec.CorrelationId = "changed";
        await publisher.PublishAsync();
        publisher.GetEvents().Select(e => e.Event[native ? "correlationid" : formatter.CorrelationIdAttributeName] as string).Should().Equal(first, second);
        ec.CorrelationId.Should().Be("changed");
        activities.Should().HaveCount(2);
        activities.Should().OnlyContain(activity => activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName) == null);
    }

    private sealed class NoDecodingFormatter : EventFormatter
    {
        public override EventData ConvertFromCloudEvent(CloudNative.CloudEvents.CloudEvent cloudEvent) => throw new InvalidOperationException("Publishing must not decode the envelope.");
    }

    private sealed class CorrelationPublisher(IEventFormatter formatter) : EventPublisherBase(formatter: formatter)
    {
        protected override Task OnPublishAsync(DestinationEvent[] events, CancellationToken cancellationToken = default)
        {
            Activity.Current.Should().NotBeNull();
            Activity.Current!.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().BeNull();
            return Invoker.InvokeAsync(this, (tracer, _) =>
            {
                tracer.Activity.Should().NotBeNull();
                tracer.Activity!.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().BeNull();
                return Task.CompletedTask;
            }, cancellationToken);
        }
    }
}
