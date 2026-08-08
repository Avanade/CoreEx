using CoreEx.Events.Subscribing;
using CoreEx.Events.Subscribing.Exceptions;
using CoreEx.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreEx.Events.Test.Unit.Subscribing;

[TestFixture]
public class SubscribedManagerTests
{
    [Test]
    public async Task ReceiveAsync_CancellationMatchingOwnToken_BubblesUnclassified()
    {
        // Regression: a cancellation attributable to *this* receive's own cancellationToken (e.g. host/pump shutdown) must bubble up unclassified, not be wrapped by the subscribed ErrorHandler.
        using var cts = new CancellationTokenSource();
        OperationCanceledException? thrown = null;

        var subscribed = new TestSubscribed(ct =>
        {
            thrown = new OperationCanceledException("shutdown", ct);
            throw thrown;
        }) { ErrorHandler = new ErrorHandler() };

        var (manager, executionContext, args) = CreateManager();
        var result = await manager.ReceiveAsync(executionContext, subscribed, new EventData(), args, cts.Token);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(thrown);
    }

    [Test]
    public async Task ReceiveAsync_UnrelatedCancellation_IsClassifiedViaConfiguredRule()
    {
        // Regression: a cancellation from an unrelated source must be classified normally via the subscribed ErrorHandler, not excluded just because it's an OperationCanceledException.
        using var unrelatedCts = new CancellationTokenSource();
        var subscribed = new TestSubscribed(_ => throw new OperationCanceledException("unrelated", unrelatedCts.Token))
        {
            ErrorHandler = new ErrorHandler().Add<OperationCanceledException>(ErrorHandling.DeadLetter)
        };

        var (manager, executionContext, args) = CreateManager();
        var result = await manager.ReceiveAsync(executionContext, subscribed, new EventData(), args, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<EventSubscriberDeadLetterException>();
    }

    [Test]
    public async Task ReceiveAsync_UnrelatedCancellation_NoErrorHandler_Propagates()
    {
        // Without a subscribed ErrorHandler at all, the catch guard's "subscribed.ErrorHandler is not null" condition already excludes it - the exception is simply returned as a failed Result (existing behavior, unrelated to the cancellation fix).
        using var unrelatedCts = new CancellationTokenSource();
        var subscribed = new TestSubscribed(_ => throw new OperationCanceledException("unrelated", unrelatedCts.Token));

        var (manager, executionContext, args) = CreateManager();
        var result = await manager.ReceiveAsync(executionContext, subscribed, new EventData(), args, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<OperationCanceledException>();
    }

    private static (SubscribedManager Manager, ExecutionContext ExecutionContext, EventSubscriberArgs Args) CreateManager()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var executionContext = new ExecutionContext { ServiceProvider = services };
        var manager = new SubscribedManager();
        var args = new EventSubscriberArgs { Owner = new TestEventSubscriber() };
        return (manager, executionContext, args);
    }

    private sealed class TestEventSubscriber() : EventSubscriberBase(new EventFormatter(), NullLogger<EventSubscriberBase>.Instance)
    {
        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    [Subscribe("test.entity.created")]
    private sealed class TestSubscribed(Action<CancellationToken> onReceive) : SubscribedBase
    {
        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        {
            onReceive(cancellationToken);
            return Task.FromResult(Result.Success);
        }
    }
}
