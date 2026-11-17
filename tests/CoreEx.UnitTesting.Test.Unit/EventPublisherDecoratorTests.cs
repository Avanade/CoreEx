namespace CoreEx.UnitTesting.Test.Unit;

public class EventPublisherDecoratorTests
{
    [Test]
    public async Task PublicationsAcrossScopes_AccumulateAndPassThrough()
    {
        using var test = GenericTester.Create<EntryPoint>();
        var sent = new List<DestinationEvent>();
        test.ConfigureServices(services => services
            .AddEventPublisher("capture", sp => new RecordingPublisher(sent, sp.GetRequiredService<CoreEx.Events.IEventFormatter>()))
            .UseExpectedEventPublisher("capture"));

        for (var i = 0; i < 3; i++)
        {
            using var scope = test.Services.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredKeyedService<IEventPublisher>("capture");
            publisher.Add("commands", EventData.CreateEvent("item", "created").WithPartitionKey("item"));
            await publisher.PublishAsync();
        }

        var captured = Captured(test, "capture");
        captured.Should().HaveCount(3);
        captured.Select(e => e.Event).Should().Equal(sent.Select(e => e.Event));
    }

    [Test]
    public async Task FailureHook_RetainsEarlierSuccessAndDoesNotCaptureFailedPublication()
    {
        using var test = GenericTester.Create<EntryPoint>();
        var calls = 0;
        var failure = new IOException("Second publication failed.");
        test.OnEventPublish(EntryPoint.EventsServiceKey, () =>
        {
            if (++calls == 2)
                throw failure;
        });

        using var first = test.Services.CreateScope();
        var publisher = first.ServiceProvider.GetRequiredKeyedService<IEventPublisher>(EntryPoint.EventsServiceKey);
        publisher.Add("commands", EventData.CreateEvent("item", "created").WithPartitionKey("item"));
        await publisher.PublishAsync();
        using var second = test.Services.CreateScope();
        publisher = second.ServiceProvider.GetRequiredKeyedService<IEventPublisher>(EntryPoint.EventsServiceKey);
        publisher.Add("commands", EventData.CreateEvent("item", "created").WithPartitionKey("item"));
        Func<Task> publish = () => publisher.PublishAsync();
        (await publish.Should().ThrowAsync<IOException>()).Which.Should().BeSameAs(failure);
        Captured(test, EntryPoint.EventsServiceKey).Should().HaveCount(1);
    }

    [Test]
    public async Task Rollback_RemovesOnlyItsOwnPublications()
    {
        using var test = GenericTester.Create<EntryPoint>();
        using var first = test.Services.CreateScope();
        using var second = test.Services.CreateScope();
        var publisher = first.ServiceProvider.GetRequiredKeyedService<IEventPublisher>(EntryPoint.EventsServiceKey);
        var other = second.ServiceProvider.GetRequiredKeyedService<IEventPublisher>(EntryPoint.EventsServiceKey);
        publisher.Add("commands", EventData.CreateEvent("first", "created").WithPartitionKey("first"));
        other.Add("commands", EventData.CreateEvent("second", "created").WithPartitionKey("second"));
        await publisher.PublishAsync();
        await other.PublishAsync();
        var retained = Captured(test, EntryPoint.EventsServiceKey)[1];
        await publisher.RollbackAsync();
        Captured(test, EntryPoint.EventsServiceKey).Should().Equal(retained);
    }

    [Test]
    public async Task BypassPassThrough_DoesNotCallConfiguredPublisher()
    {
        using var test = GenericTester.Create<EntryPoint>();
        var sent = new List<DestinationEvent>();
        test.ConfigureServices(services => services
            .AddEventPublisher("capture", sp => new RecordingPublisher(sent, sp.GetRequiredService<CoreEx.Events.IEventFormatter>()))
            .UseExpectedEventPublisher("capture", bypassPassThrough: true));
        using var scope = test.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredKeyedService<IEventPublisher>("capture");
        publisher.Add("commands", EventData.CreateEvent("item", "created").WithPartitionKey("item"));
        await publisher.PublishAsync();
        sent.Should().BeEmpty();
        Captured(test, "capture").Should().ContainSingle();
    }

    private static DestinationEvent[] Captured(GenericTester<EntryPoint> test, string key)
        => (DestinationEvent[])test.SharedState.RequestStateData(null)[key].ThrowIfNull();

    private sealed class RecordingPublisher(List<DestinationEvent> sent, CoreEx.Events.IEventFormatter formatter) : EventPublisherBase(formatter: formatter)
    {
        protected override Task OnPublishAsync(DestinationEvent[] events, CancellationToken cancellationToken = default)
        {
            sent.AddRange(events);
            return Task.CompletedTask;
        }
    }
}
