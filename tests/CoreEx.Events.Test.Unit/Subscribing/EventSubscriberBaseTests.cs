using CoreEx.Events.Subscribing;
using CoreEx.Events.Subscribing.Exceptions;
using CoreEx.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreEx.Events.Test.Unit.Subscribing;

[TestFixture]
public class EventSubscriberBaseTests
{
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
