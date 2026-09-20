using CoreEx.Events.Subscribing;
using CoreEx.Events.Subscribing.Exceptions;
using CoreEx.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

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

    [Test]
    public void Match_NoSubscriberFound_DefaultSuppressesTracing()
    {
        // Default (IsTracingEnabledForUnsubscribed = false): the current activity and its parent must be excluded from export, and the parent's Recorded flag cleared so subsequent siblings follow suit.
        using var source = new ActivitySource(nameof(Match_NoSubscriberFound_DefaultSuppressesTracing));
        using var listener = CreateAllRecordingListener(source.Name);

        using var parent = source.StartActivity("parent")!;
        using var current = source.StartActivity("current")!;

        parent.IsAllDataRequested.Should().BeTrue();
        current.IsAllDataRequested.Should().BeTrue();
        parent.Recorded.Should().BeTrue();

        var (manager, executionContext, args) = CreateManager();
        var result = manager.Match(executionContext, args, "unmatched.event.subject");

        result.IsFailure.Should().BeTrue();
        current.IsAllDataRequested.Should().BeFalse();
        current.Recorded.Should().BeFalse();
        parent.IsAllDataRequested.Should().BeFalse();
        parent.Recorded.Should().BeFalse();
    }

    [Test]
    public void Match_NoSubscriberFound_Default_SuppressesChildCreatedOnCurrentBeforeItStops()
    {
        // Regression: a transport can complete/settle a message (e.g. CoreEx's own ServiceBusReceiverBaseT calling actions.CompleteMessageAsync) *inside* the current activity's own scope, i.e. before it
        // stops - meaning that "settle" span parents on `current` (the receiver invoker span), not on `current.Parent` (the transport's native process span). A ParentBasedSampler decides that child's
        // sampling purely from its immediate parent's live Recorded flag, so clearing Recorded only on the parent (and not on current itself) would let such a child leak through unsuppressed.
        using var source = new ActivitySource(nameof(Match_NoSubscriberFound_Default_SuppressesChildCreatedOnCurrentBeforeItStops));
        using var listener = CreateParentBasedListener(source.Name);

        using var parent = source.StartActivity("parent")!;
        using var current = source.StartActivity("current")!;

        var (manager, executionContext, args) = CreateManager();
        var result = manager.Match(executionContext, args, "unmatched.event.subject");
        result.IsFailure.Should().BeTrue();

        // Simulate the transport creating a "settle" span as a child of `current`, before `current` stops.
        using var settle = source.StartActivity("settle");
        settle.Should().BeNull("a ParentBasedSampler must drop this child once current's Recorded flag has been cleared");
    }

    [Test]
    public void Match_NoSubscriberFound_OptedIn_LeavesTracingUntouched()
    {
        // IsTracingEnabledForUnsubscribed = true: nothing should be suppressed.
        using var source = new ActivitySource(nameof(Match_NoSubscriberFound_OptedIn_LeavesTracingUntouched));
        using var listener = CreateAllRecordingListener(source.Name);

        using var parent = source.StartActivity("parent")!;
        using var current = source.StartActivity("current")!;

        var (manager, executionContext, args) = CreateManager();
        manager.IsTracingEnabledForUnsubscribed = true;
        var result = manager.Match(executionContext, args, "unmatched.event.subject");

        result.IsFailure.Should().BeTrue();
        current.IsAllDataRequested.Should().BeTrue();
        parent.IsAllDataRequested.Should().BeTrue();
        parent.Recorded.Should().BeTrue();
    }

    [Test]
    public async Task Match_SubscriberFound_LeavesTracingUntouched()
    {
        // A successful match must never be affected by the unsubscribed-suppression logic.
        using var source = new ActivitySource(nameof(Match_SubscriberFound_LeavesTracingUntouched));
        using var listener = CreateAllRecordingListener(source.Name);

        using var parent = source.StartActivity("parent")!;
        using var current = source.StartActivity("current")!;

        var services = new ServiceCollection().AddScoped<TestSubscribed>(_ => new TestSubscribed(_ => { })).BuildServiceProvider();
        var executionContext = new ExecutionContext { ServiceProvider = services };
        var manager = new SubscribedManager().AddSubscriber<TestSubscribed>();
        var args = new EventSubscriberArgs { Owner = new TestEventSubscriber() };

        var result = manager.Match(executionContext, args, "test.entity.created");
        await Task.CompletedTask;

        result.IsSuccess.Should().BeTrue();
        current.IsAllDataRequested.Should().BeTrue();
        parent.IsAllDataRequested.Should().BeTrue();
        parent.Recorded.Should().BeTrue();
    }

    /// <summary>
    /// Creates an <see cref="ActivityListener"/> that samples every activity from <paramref name="sourceName"/> as <see cref="ActivitySamplingResult.AllDataAndRecorded"/> - mirroring the OpenTelemetry SDK's
    /// own <c>ActivityStopped</c> gating (see <see cref="Activity.IsAllDataRequested"/>), so these tests exercise the exact mechanism <see cref="SubscribedManager"/> relies on rather than a re-implementation.
    /// </summary>
    private static ActivityListener CreateAllRecordingListener(string sourceName)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };

        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    /// <summary>
    /// Creates an <see cref="ActivityListener"/> that mimics OpenTelemetry's own default <c>ParentBasedSampler</c>: a root activity (no parent context) is always sampled, and any child activity's sampling
    /// decision is derived purely from whether its immediate parent's <see cref="ActivityContext.TraceFlags"/> currently has <see cref="ActivityTraceFlags.Recorded"/> set - exercising the real mechanism
    /// <see cref="SubscribedManager"/> relies on to suppress not-yet-created sibling/child activities, rather than the simpler "always record" listener used by the other tests in this fixture.
    /// </summary>
    private static ActivityListener CreateParentBasedListener(string sourceName)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Parent == default || options.Parent.TraceFlags.HasFlag(ActivityTraceFlags.Recorded)
                    ? ActivitySamplingResult.AllDataAndRecorded
                    : ActivitySamplingResult.None
        };

        ActivitySource.AddActivityListener(listener);
        return listener;
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
