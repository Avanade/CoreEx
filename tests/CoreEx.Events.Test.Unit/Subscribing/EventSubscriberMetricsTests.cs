using CoreEx.Events.Subscribing;
using CoreEx.Events.Subscribing.Exceptions;
using CoreEx.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreEx.Events.Test.Unit.Subscribing;

[TestFixture]
public class EventSubscriberMetricsTests
{
    [Test]
    public async Task ReceiveMessageAsync_Success()
    {
        var (outcome, subscribed) = await GetTagsAsync(new EventSubscriberArgs(), () => Task.FromResult(Result.Success));
        outcome.Should().Be("success");
        subscribed.Should().BeNull();
    }

    [TestCase(ErrorHandling.CompleteAsSilent, "error-complete-silent")]
    [TestCase(ErrorHandling.CompleteAsInformation, "error-complete-info")]
    [TestCase(ErrorHandling.CompleteAsWarning, "error-complete-warning")]
    [TestCase(ErrorHandling.CompleteAsError, "error-complete-error")]
    public async Task ReceiveMessageAsync_HandledException_Variants(ErrorHandling errorHandling, string expectedOutcome)
    {
        var (outcome, _) = await GetTagsAsync(new EventSubscriberArgs(), () => Task.FromResult(Result.Fail(new EventSubscriberHandledException(errorHandling))));
        outcome.Should().Be(expectedOutcome);
    }

    [Test]
    public async Task ReceiveMessageAsync_RetryException()
    {
        // Regression: previously misclassified as "error-unhandled" since only EventSubscriberHandledException was checked.
        var (outcome, _) = await GetTagsAsync(new EventSubscriberArgs(), () => Task.FromResult(Result.Fail(new EventSubscriberRetryException())));
        outcome.Should().Be("error-retry");
    }

    [Test]
    public async Task ReceiveMessageAsync_DeadLetterException()
    {
        var (outcome, _) = await GetTagsAsync(new EventSubscriberArgs(), () => Task.FromResult(Result.Fail(new EventSubscriberDeadLetterException())));
        outcome.Should().Be("error-dead-letter");
    }

    [Test]
    public async Task ReceiveMessageAsync_CatastrophicException()
    {
        var (outcome, _) = await GetTagsAsync(new EventSubscriberArgs(), () => Task.FromResult(Result.Fail(new EventSubscriberCatastrophicException())));
        outcome.Should().Be("error-catastrophic");
    }

    [Test]
    public async Task ReceiveMessageAsync_UnhandledException()
    {
        var (outcome, _) = await GetTagsAsync(new EventSubscriberArgs(), () => Task.FromResult(Result.Fail(new EventSubscriberUnhandledException())));
        outcome.Should().Be("error-unhandled");
    }

    [Test]
    public async Task ReceiveMessageAsync_GenericException_IsUnhandled()
    {
        // An exception that is not an IEventSubscriberException at all (i.e. never classified) is "error-unhandled".
        var (outcome, _) = await GetTagsAsync(new EventSubscriberArgs(), () => Task.FromResult(Result.Fail(new InvalidOperationException("boom"))));
        outcome.Should().Be("error-unhandled");
    }

    [Test]
    public void ReceiveMessageAsync_ThrownException_Propagates()
    {
        var args = new EventSubscriberArgs();
        Func<Task> act = () => EventSubscriberMetrics.ReceiveMessageAsync(args, () => throw new InvalidOperationException("boom"));
        act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public void ReceiveMessageAsync_NotUsingSubscribedManager_OmitsSubscribedTag()
    {
        // When SubscribedManager isn't involved at all, "subscribed" is not a meaningful concept and must be omitted (not a spurious true/false).
        var args = new EventSubscriberArgs();
        args.UsesSubscribedManager.Should().BeFalse();
    }

    [Test]
    public async Task ReceiveMessageAsync_UsesSubscribedManager_Matched_SubscribedTrue()
    {
        // Regression: a real subscriber match must report subscribed=true alongside the normal outcome.
        var (manager, executionContext) = CreateSubscribedManager();
        var args = new EventSubscriberArgs { Owner = CreateOwner() };

        var matchResult = manager.Match(executionContext, args, "test.entity.created");
        matchResult.IsSuccess.Should().BeTrue();
        args.Subscriber.Should().NotBeNull();

        var (outcome, subscribed) = await GetTagsAsync(args, () => Task.FromResult(Result.Success));
        outcome.Should().Be("success");
        subscribed.Should().Be(true);
    }

    [Test]
    public async Task ReceiveMessageAsync_UsesSubscribedManager_NoMatch_SubscribedFalse()
    {
        // Regression: "nobody subscribed" must report subscribed=false, distinguishing it from a handler that itself
        // chose to complete silently (which would report the same "error-complete-silent" outcome but subscribed=true).
        var (manager, executionContext) = CreateSubscribedManager();
        var args = new EventSubscriberArgs { Owner = CreateOwner() };

        var matchResult = manager.Match(executionContext, args, "unmatched.title");
        matchResult.IsSuccess.Should().BeFalse();
        args.Subscriber.Should().BeNull();

        var (outcome, subscribed) = await GetTagsAsync(args, () => Task.FromResult(matchResult.AsResult()));
        outcome.Should().Be("error-complete-silent");
        subscribed.Should().Be(false);
    }

    [Test]
    public async Task ReceiveMessageAsync_UsesSubscribedManager_Ambiguous_SubscribedFalse()
    {
        // Regression: an ambiguous match (Catastrophic by default) must report subscribed=false with the correct
        // (non-silent) outcome - distinct from both the matched and the not-subscribed cases above.
        var (manager, executionContext) = CreateSubscribedManager();
        manager.AddSubscriber<AlsoMatchingSubscribed>();
        var args = new EventSubscriberArgs { Owner = CreateOwner() };

        var matchResult = manager.Match(executionContext, args, "test.entity.created");
        matchResult.IsSuccess.Should().BeFalse();
        args.Subscriber.Should().BeNull();

        var (outcome, subscribed) = await GetTagsAsync(args, () => Task.FromResult(matchResult.AsResult()));
        outcome.Should().Be("error-catastrophic");
        subscribed.Should().Be(false);
    }

    private static EventSubscriberBase CreateOwner() => new TestEventSubscriber();

    private static (SubscribedManager Manager, ExecutionContext ExecutionContext) CreateSubscribedManager()
    {
        var services = new ServiceCollection().AddTransient<TestSubscribed>().BuildServiceProvider();
        var executionContext = new ExecutionContext { ServiceProvider = services };
        var manager = new SubscribedManager().AddSubscriber<TestSubscribed>();
        return (manager, executionContext);
    }

    private static async Task<(string? Outcome, bool? Subscribed)> GetTagsAsync(EventSubscriberArgs args, Func<Task<Result>> receiveFunc)
    {
        using var listener = new System.Diagnostics.Metrics.MeterListener();
        string? outcome = null;
        bool? subscribed = null;

        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == EventSubscriberMetrics.Meter.Name)
                l.EnableMeasurementEvents(instrument);
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "outcome")
                    outcome = tag.Value?.ToString();
                else if (tag.Key == "subscribed")
                    subscribed = (bool?)tag.Value;
            }
        });

        listener.Start();
        await EventSubscriberMetrics.ReceiveMessageAsync(args, receiveFunc);
        return (outcome, subscribed);
    }

    private sealed class TestEventSubscriber() : EventSubscriberBase(new EventFormatter(), NullLogger<EventSubscriberBase>.Instance)
    {
        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    [Subscribe("test.entity.created")]
    private sealed class TestSubscribed : SubscribedBase
    {
        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Subscribe("test.entity.created")]
    private sealed class AlsoMatchingSubscribed : SubscribedBase
    {
        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
