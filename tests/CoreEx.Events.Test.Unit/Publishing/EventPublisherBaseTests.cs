using CloudNative.CloudEvents;
using CoreEx.Events.Publishing;

namespace CoreEx.Events.Test.Unit.Publishing;

public class EventPublisherBaseTests
{
    private class TestEventPublisher() : EventPublisherBase(new FixedDestinationProvider { Destination = "fixed" }, new EventFormatter() { PartitionKeyIsRequired = false })
    {
        public DestinationEvent[]? PublishedEvents { get; private set; }
        public CancellationToken? PublishedToken { get; private set; }
        public int PublishCallCount { get; private set; }

        protected override Task OnPublishAsync(DestinationEvent[] events, CancellationToken cancellationToken = default)
        {
            PublishCallCount++;
            PublishedEvents = events;
            PublishedToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private TestEventPublisher _publisher = null!;

    [SetUp]
    public void SetUp() => _publisher = new TestEventPublisher();

    [Test]
    public void IsEmpty_ShouldBeTrue_WhenNoEventsAdded()
    {
        _publisher.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Add_EventData_ShouldQueueEvents()
    {
        var e1 = new EventData();
        var e2 = new EventData();
        _publisher.Add(e1, e2);

        _publisher.IsEmpty.Should().BeFalse();
    }

    [Test]
    public void Add_EventData_ShouldIgnoreNulls()
    {
        var e1 = new EventData();
        _publisher.Add(e1, null!);

        _publisher.IsEmpty.Should().BeFalse();
    }

    [Test]
    public void Add_WithDestination_EventData_ShouldQueueEvents()
    {
        var e1 = new EventData();
        var e2 = new EventData();
        _publisher.Add("my-dest", e1, e2);

        _publisher.IsEmpty.Should().BeFalse();
    }

    [Test]
    public void Add_WithDestination_CloudEvent_ShouldQueueEvents()
    {
        var ce1 = new CloudEvent();
        var ce2 = new CloudEvent();
        _publisher.Add("my-dest", ce1, ce2);

        _publisher.IsEmpty.Should().BeFalse();
    }

    [Test]
    public void Add_WithNullDestination_ShouldThrow()
    {
        var e1 = new EventData();
        Action act = () => _publisher.Add((string)null!, e1);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Add_WithEmptyDestination_ShouldThrow()
    {
        var e1 = new EventData();
        Action act = () => _publisher.Add("", e1);
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Add_CloudEvent_WithNullDestination_ShouldThrow()
    {
        var ce1 = new CloudEvent();
        Action act = () => _publisher.Add(null!, ce1);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Add_CloudEvent_WithEmptyDestination_ShouldThrow()
    {
        var ce1 = new CloudEvent();
        Action act = () => _publisher.Add("", ce1);
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Clear_ShouldClearQueue()
    {
        var e1 = new EventData();
        _publisher.Add(e1);
        _publisher.IsEmpty.Should().BeFalse();

        _publisher.Clear();
        _publisher.IsEmpty.Should().BeTrue();
    }

    [Test]
    public async Task Rollback_ShouldRemoveSpecifiedCountOfEvents()
    {
        var e1 = new EventData { Id = "X" };
        var e2 = new EventData();
        var e3 = new EventData();

        _publisher.Add(e1, e2, e3);
        _publisher.Count.Should().Be(3);

        Action act = () => _publisher.Rollback(4);
        act.Should().Throw<ArgumentException>();

        _publisher.Rollback(2);
        _publisher.Count.Should().Be(1);

        await _publisher.PublishAsync();

        _publisher.PublishCallCount.Should().Be(1);
        _publisher.PublishedEvents!.Length.Should().Be(1);
        _publisher.PublishedEvents![0].Event.Id.Should().Be("X");

        Action act2 = () => _publisher.Rollback(1);
        act2.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task Reset_ShouldResetHasPublished()
    {
        var e1 = new EventData();
        _publisher.Add(e1);
        _publisher.HasBeenPublished.Should().BeFalse();

        await _publisher.PublishAsync();
        _publisher.HasBeenPublished.Should().BeTrue();

        _publisher.Reset();
        _publisher.HasBeenPublished.Should().BeFalse();
    }

    [Test]
    public async Task PublishAsync_ShouldCallOnPublishAsync_AndReset()
    {
        var e1 = new EventData();
        _publisher.Add(e1);

        await _publisher.PublishAsync();

        _publisher.PublishCallCount.Should().Be(1);
        _publisher.PublishedEvents.Should().NotBeNull();
        _publisher.HasBeenPublished.Should().BeTrue();
    }

    [Test]
    public async Task PublishAsync_WhenEmpty_ShouldNotCallOnPublishAsync()
    {
        await _publisher.PublishAsync();
        _publisher.PublishCallCount.Should().Be(0);
        _publisher.HasBeenPublished.Should().BeTrue();
    }
}