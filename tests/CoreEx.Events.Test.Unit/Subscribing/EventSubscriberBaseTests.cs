using CoreEx.Events.Subscribing;
using CoreEx.Events.Subscribing.Exceptions;
using CoreEx.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreEx.Events.Test.Unit.Subscribing;

[TestFixture]
public class EventSubscriberBaseTests
{
    [TestCase("urn:test:expected", true)]
    [TestCase("urn:test:other", false)]
    public async Task ReceiveCloudEvent_EnforcesSubscriptionWithoutTransport(string source, bool matches)
    {
        var calls = 0;
        var subscriber = new FilteredEventSubscriber(() => calls++);
        var cloudEvent = new CloudEvent { Id = "event-id", Type = "test.event.v1", Source = new Uri(source) };
        var args = new EventSubscriberArgs();

        var result = await subscriber.InvokeReceiveAsync(cloudEvent, args);

        args.CloudEvent.Should().BeSameAs(cloudEvent);
        result.IsSuccess.Should().Be(matches);
        calls.Should().Be(matches ? 1 : 0);
        if (!matches)
            result.Error.Should().BeOfType<EventSubscriberDeadLetterException>();
    }

    [Test]
    public async Task ReceiveCloudEvent_CountsAttemptsBeforeSubscriptionMatch()
    {
        var calls = 0;
        var subscriber = new FilteredEventSubscriber(() => calls++) { UnmatchedCloudEventHandling = ErrorHandling.Retry };
        var cloudEvent = new CloudEvent { Id = "event-id", Type = "test.event.v1", Source = new Uri("urn:test:other") };
        var args = new EventSubscriberArgs();

        (await subscriber.InvokeReceiveAsync(cloudEvent, args)).Error.Should().BeOfType<EventSubscriberRetryException>();
        args.AttemptCount.Should().Be(0);
        (await subscriber.InvokeReceiveAsync(cloudEvent, args)).Error.Should().BeOfType<EventSubscriberRetryException>();

        args.AttemptCount.Should().Be(1);
        calls.Should().Be(0);
    }

    [Test]
    public async Task ReceiveAsync_CancellationMatchingOwnToken_BubblesUnclassified()
    {
        // Regression: a cancellation attributable to *this* receive's own cancellationToken (e.g. host/pump shutdown) must bubble up unclassified (the original exception preserved as-is), not be wrapped by ErrorHandler.
        using var cts = new CancellationTokenSource();
        OperationCanceledException? thrown = null;

        var subscriber = new TestEventSubscriber(ct =>
        {
            thrown = new OperationCanceledException("shutdown", ct);
            throw thrown;
        });

        var result = await subscriber.InvokeReceiveAsync(new EventData(), null, cts.Token);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(thrown);
    }

    [Test]
    public async Task ReceiveAsync_NestedAggregateCancellationMatchingOwnToken_BubblesUnclassified()
    {
        // Regression: multiple Result.ThrowOnError() boundaries each wrap the error in a new AggregateException; an own-token cancellation must still bubble up unclassified however deeply wrapped.
        using var cts = new CancellationTokenSource();
        Exception? thrown = null;

        var subscriber = new TestEventSubscriber(ct =>
        {
            thrown = new AggregateException(new AggregateException(new OperationCanceledException("shutdown", ct)));
            throw thrown;
        });

        var result = await subscriber.InvokeReceiveAsync(new EventData(), null, cts.Token);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(thrown);
    }

    [Test]
    public async Task ReceiveAsync_UnrelatedCancellation_IsClassifiedViaConfiguredRule()
    {
        // Regression: a cancellation from an unrelated source (e.g. an inner HttpClient timeout with its own CancellationTokenSource) must be classified normally, not excluded just because it's an OperationCanceledException.
        using var unrelatedCts = new CancellationTokenSource();
        var subscriber = new TestEventSubscriber(_ => throw new OperationCanceledException("unrelated", unrelatedCts.Token));
        subscriber.ErrorHandler.Add<OperationCanceledException>(ErrorHandling.Retry);

        var result = await subscriber.InvokeReceiveAsync(new EventData(), null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<EventSubscriberRetryException>();
    }

    [Test]
    public async Task ReceiveAsync_UnrelatedCancellation_NoRule_IsUnhandled()
    {
        // Without a configured rule, an unrelated cancellation still falls through to the default (Unhandled) classification, same as any other unclassified exception.
        using var unrelatedCts = new CancellationTokenSource();
        var subscriber = new TestEventSubscriber(_ => throw new OperationCanceledException("unrelated", unrelatedCts.Token));

        var result = await subscriber.InvokeReceiveAsync(new EventData(), null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<EventSubscriberUnhandledException>();
    }

    [Test]
    public async Task ReceiveAsync_SuccessfulRetryClearsPreviousAttemptError()
    {
        var calls = 0;
        var transient = new InvalidOperationException("Transient.");
        var subscriber = new TestEventSubscriber(_ =>
        {
            if (calls++ == 0)
                throw transient;
        });
        subscriber.ErrorHandler.Add<InvalidOperationException>(ErrorHandling.Retry);
        var args = new EventSubscriberArgs();

        (await subscriber.InvokeReceiveAsync(new EventData(), args, CancellationToken.None)).Error.Should().BeOfType<EventSubscriberRetryException>();
        args.ResultingException.Should().BeSameAs(transient);
        args.ResultingErrorHandling.Should().Be(ErrorHandling.Retry);

        var succeeded = await subscriber.InvokeReceiveAsync(new EventData(), args, CancellationToken.None);

        succeeded.IsSuccess.Should().BeTrue();
        args.AttemptCount.Should().Be(1);
        args.ResultingException.Should().BeNull();
        args.ResultingErrorHandling.Should().BeNull();
    }

    [TestCaseSource(nameof(ExtendedErrorCases))]
    public async Task ReceiveAsync_ExtendedErrors_DefaultToCompleteAsError(Exception exception)
    {
        var subscriber = new TestEventSubscriber(_ => throw exception);

        var result = await subscriber.InvokeReceiveAsync(new EventData(), null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        var handled = result.Error.Should().BeOfType<EventSubscriberHandledException>().Subject;
        handled.ErrorHandling.Should().Be(ErrorHandling.CompleteAsError);
        handled.InnerException.Should().BeSameAs(exception);
    }

    private static IEnumerable<TestCaseData> ExtendedErrorCases()
    {
        yield return new TestCaseData(new ValidationException("invalid")).SetName("ValidationException");
        yield return new TestCaseData(new BusinessException("business")).SetName("BusinessException");
    }

    [Subscribe("test.event.v1", "urn:test:expected")]
    private sealed class FilteredEventSubscriber(Action onReceive) : EventSubscriberBase(new EventFormatter(), NullLogger<EventSubscriberBase>.Instance)
    {
        public Task<Result> InvokeReceiveAsync(CloudEvent cloudEvent, EventSubscriberArgs args) => ReceiveAsync(cloudEvent, args);

        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
        {
            onReceive();
            return Result.SuccessTask;
        }
    }

    private sealed class TestEventSubscriber(Action<CancellationToken> onReceive) : EventSubscriberBase(new EventFormatter(), NullLogger<EventSubscriberBase>.Instance)
    {
        public Task<Result> InvokeReceiveAsync(EventData @event, EventSubscriberArgs? args, CancellationToken cancellationToken) => ReceiveAsync(@event, args, cancellationToken);

        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
        {
            onReceive(cancellationToken);
            return Task.FromResult(Result.Success);
        }
    }
}
