using CoreEx.Events.Subscribing.Exceptions;

namespace CoreEx.Events.Test.Unit.Subscribing;

[TestFixture]
[NonParallelizable]
public class SubscriberBehaviorTests
{
    [SetUp]
    public void SetUp() => CoreEx.ExecutionContext.Reset();

    [Test]
    public async Task Pipeline_RunsOutermostFirstAndDispositionsInReverse()
    {
        var log = new List<string>();
        var subscriber = new TestSubscriber((_, _, _) => { log.Add("subscriber"); return Result.SuccessTask; });
        subscriber.Behaviors.Add(new LoggingBehavior("a", log)).Add(new LoggingBehavior("b", log));
        var args = new EventSubscriberArgs();
        var @event = new EventData { Id = "event-id" };

        var result = await subscriber.InvokeReceiveAsync(@event, args);
        await subscriber.NotifyDispositionAsync(args, new SubscriberDisposition { Settlement = SubscriberSettlement.Completed });

        result.IsSuccess.Should().BeTrue();
        log.Should().Equal("a:before", "b:before", "subscriber", "b:after", "a:after", "b:Completed", "a:Completed");
        args.Event.Should().BeSameAs(@event);
        args.IsNative.Should().BeFalse();
    }

    [Test]
    public async Task ShortCircuit_SkipsSubscriberAndNotifiesOnlyParticipants()
    {
        var log = new List<string>();
        var subscriber = new TestSubscriber((_, _, _) => { log.Add("subscriber"); return Result.SuccessTask; });
        subscriber.Behaviors.Add(new LoggingBehavior("a", log, (_, _) => SubscriberBehaviorResult.Skip("Duplicate.").AsTask())).Add(new LoggingBehavior("b", log));
        var args = new EventSubscriberArgs();

        var result = await subscriber.InvokeReceiveAsync(new EventData(), args);
        await subscriber.NotifyDispositionAsync(args, new SubscriberDisposition { Settlement = SubscriberSettlement.Completed });

        result.Error.Should().BeOfType<EventSubscriberHandledException>().Which.ErrorHandling.Should().Be(ErrorHandling.CompleteAsSilent);
        log.Should().Equal("a:before", "a:after", "a:Completed");
    }

    [Test]
    public async Task ShortCircuit_RetryAndDeadLetterAreClassified()
    {
        var retry = new TestSubscriber((_, _, _) => Result.SuccessTask);
        retry.Behaviors.Add(new LoggingBehavior("a", [], (_, _) => SubscriberBehaviorResult.Retry("Busy.").AsTask()));
        var deadLetter = new TestSubscriber((_, _, _) => Result.SuccessTask);
        deadLetter.Behaviors.Add(new LoggingBehavior("a", [], (_, _) => SubscriberBehaviorResult.DeadLetter("Rejected.").AsTask()));

        (await retry.InvokeReceiveAsync(new EventData(), new EventSubscriberArgs())).Error.Should().BeOfType<EventSubscriberRetryException>();
        (await deadLetter.InvokeReceiveAsync(new EventData(), new EventSubscriberArgs())).Error.Should().BeOfType<EventSubscriberDeadLetterException>().Which.Message.Should().Contain("Rejected.");
    }

    [Test]
    public async Task SubscriberFailure_IsClassifiedBeforeBehaviorsObserveIt()
    {
        Result? observed = null;
        var subscriber = new TestSubscriber((_, _, _) => throw new InvalidOperationException("Boom.")) { UnhandledErrorHandling = ErrorHandling.DeadLetter };
        subscriber.Behaviors.Add(new LoggingBehavior("a", [], async (next, ct) => { var r = await next(ct); observed = r; return r; }));

        var result = await subscriber.InvokeReceiveAsync(new EventData(), new EventSubscriberArgs());

        observed!.Value.Error.Should().BeOfType<EventSubscriberDeadLetterException>();
        result.Error.Should().BeSameAs(observed.Value.Error);
    }

    [Test]
    public async Task BehaviorThrow_IsClassifiedAsNoneRegardlessOfSubscriberPolicy()
    {
        var failure = new InvalidOperationException("Behavior failed.");
        var subscriber = new TestSubscriber((_, _, _) => Result.SuccessTask) { UnhandledErrorHandling = ErrorHandling.DeadLetter };
        subscriber.Behaviors.Add(new LoggingBehavior("a", [], (_, _) => throw failure));

        var result = await subscriber.InvokeReceiveAsync(new EventData(), new EventSubscriberArgs());

        result.Error.Should().BeOfType<EventSubscriberUnhandledException>().Which.InnerException.Should().BeSameAs(failure);
    }

    [Test]
    public async Task NextInvokedTwice_IsClassifiedAsNone()
    {
        var calls = 0;
        var subscriber = new TestSubscriber((_, _, _) => { calls++; return Result.SuccessTask; });
        subscriber.Behaviors.Add(new LoggingBehavior("a", [], async (next, ct) => { await next(ct); return await next(ct); }));

        var result = await subscriber.InvokeReceiveAsync(new EventData(), new EventSubscriberArgs());

        calls.Should().Be(1);
        result.Error.Should().BeOfType<EventSubscriberUnhandledException>().Which.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Test]
    public async Task ReentryWithSameArgs_IncrementsAttemptCountAndNotifiesOnce()
    {
        var log = new List<string>();
        var attempts = new List<int>();
        var subscriber = new TestSubscriber((_, _, _) => Result.SuccessTask);
        subscriber.Behaviors.Add(new LoggingBehavior("a", log, (next, ct) => next(ct), attempts));
        var args = new EventSubscriberArgs();

        await subscriber.InvokeReceiveAsync(new EventData(), args);
        await subscriber.InvokeReceiveAsync(new EventData(), args);
        await subscriber.NotifyDispositionAsync(args, new SubscriberDisposition { Settlement = SubscriberSettlement.Completed });
        await subscriber.NotifyDispositionAsync(args, new SubscriberDisposition { Settlement = SubscriberSettlement.Abandoned });

        attempts.Should().Equal(0, 1);
        args.AttemptCount.Should().Be(1);
        log.Count(x => x.StartsWith("a:Completed") || x.StartsWith("a:Abandoned")).Should().Be(1);
    }

    [Test]
    public async Task NoBehaviors_CountsOuterAttemptsButNotNestedReceives()
    {
        var attempts = new List<int>();
        TestSubscriber? subscriber = null;
        var depth = 0;
        subscriber = new TestSubscriber((e, a, ct) =>
        {
            attempts.Add(a.AttemptCount);
            return depth++ % 2 == 0 ? subscriber!.InvokeReceiveAsync(e, a, ct) : Result.SuccessTask;
        });
        var args = new EventSubscriberArgs();

        await subscriber.InvokeReceiveAsync(new EventData(), args);
        await subscriber.InvokeReceiveAsync(new EventData(), args);

        attempts.Should().Equal(0, 0, 1, 1);
        args.AttemptCount.Should().Be(1);
    }

    [Test]
    public async Task NativeEntryWithoutBehaviors_CountsAttempts()
    {
        var attempts = new List<int>();
        var subscriber = new TestSubscriber((_, _, _) => Result.SuccessTask);
        var args = new EventSubscriberArgs();

        await subscriber.InvokeNativeAsync(args, _ => { attempts.Add(args.AttemptCount); return Result.SuccessTask; });
        await subscriber.InvokeNativeAsync(args, _ => { attempts.Add(args.AttemptCount); return Result.SuccessTask; });

        attempts.Should().Equal(0, 1);
    }

    [Test]
    public async Task NestedReceiveWithinActiveAttempt_DoesNotReenterBehaviors()
    {
        var log = new List<string>();
        TestSubscriber? subscriber = null;
        var depth = 0;
        subscriber = new TestSubscriber((e, a, ct) => depth++ == 0 ? subscriber!.InvokeReceiveAsync(e, a, ct) : Result.SuccessTask);
        subscriber.Behaviors.Add(new LoggingBehavior("a", log));
        var args = new EventSubscriberArgs();

        var result = await subscriber.InvokeReceiveAsync(new EventData(), args);

        result.IsSuccess.Should().BeTrue();
        log.Should().Equal("a:before", "a:after");
        args.AttemptCount.Should().Be(0);
    }

    [Test]
    public async Task DispositionCallbackFailure_IsSuppressedAndOthersStillRun()
    {
        var log = new List<string>();
        var subscriber = new TestSubscriber((_, _, _) => Result.SuccessTask);
        subscriber.Behaviors.Add(new LoggingBehavior("a", log)).Add(new LoggingBehavior("b", log) { ThrowOnDisposition = true });
        var args = new EventSubscriberArgs();

        await subscriber.InvokeReceiveAsync(new EventData(), args);
        var act = () => subscriber.NotifyDispositionAsync(args, new SubscriberDisposition { Settlement = SubscriberSettlement.DeadLettered });

        await act.Should().NotThrowAsync();
        log.Should().EndWith(["b:DeadLettered", "a:DeadLettered"]);
    }

    [Test]
    public async Task NoParticipation_DoesNotNotify()
    {
        var log = new List<string>();
        var subscriber = new TestSubscriber((_, _, _) => Result.SuccessTask);
        subscriber.Behaviors.Add(new LoggingBehavior("a", log));

        await subscriber.NotifyDispositionAsync(new EventSubscriberArgs(), new SubscriberDisposition { Settlement = SubscriberSettlement.Completed });

        log.Should().BeEmpty();
    }

    [Test]
    public async Task Native_IsRepresentedWithoutEvent()
    {
        EventSubscriberArgs? observed = null;
        var subscriber = new TestSubscriber((_, _, _) => Result.SuccessTask);
        subscriber.Behaviors.Add(new LoggingBehavior("a", [], (next, ct) => next(ct), observe: a => observed = a));
        var args = new EventSubscriberArgs();

        var result = await subscriber.InvokeNativeAsync(args, _ => Result.SuccessTask);

        result.IsSuccess.Should().BeTrue();
        observed.Should().BeSameAs(args);
        args.IsNative.Should().BeTrue();
        args.Event.Should().BeNull();
        args.Owner.Should().BeSameAs(subscriber);
    }

    [Test]
    public async Task TypeRegistration_IsResolvedFromExecutionContextServiceProvider()
    {
        var log = new List<string>();
        using var services = new ServiceCollection().AddSingleton(log).AddScoped<ResolvedBehavior>().BuildServiceProvider();
        using var ec = new CoreEx.ExecutionContext { ServiceProvider = services };
        CoreEx.ExecutionContext.SetCurrent(ec);
        var subscriber = new TestSubscriber((_, _, _) => Result.SuccessTask);
        subscriber.Behaviors.Add<ResolvedBehavior>();

        var result = await subscriber.InvokeReceiveAsync(new EventData(), new EventSubscriberArgs());

        result.IsSuccess.Should().BeTrue();
        log.Should().Equal("resolved");
    }

    [Test]
    public async Task TypeRegistration_WithoutServiceProvider_IsClassifiedAsNone()
    {
        var calls = 0;
        var subscriber = new TestSubscriber((_, _, _) => { calls++; return Result.SuccessTask; });
        subscriber.Behaviors.Add<ResolvedBehavior>();

        var result = await subscriber.InvokeReceiveAsync(new EventData(), new EventSubscriberArgs());

        calls.Should().Be(0);
        result.Error.Should().BeOfType<EventSubscriberUnhandledException>().Which.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Test]
    public void MessageUniqueKey_IsSetOnce()
    {
        var args = new EventSubscriberArgs { MessageUniqueKey = "first" };

        var act = () => args.MessageUniqueKey = "second";

        act.Should().Throw<InvalidOperationException>();
        args.MessageUniqueKey.Should().Be("first");
    }

    private sealed class TestSubscriber(Func<EventData, EventSubscriberArgs, CancellationToken, Task<Result>> onReceive) : EventSubscriberBase(new EventFormatter(), NullLogger<EventSubscriberBase>.Instance)
    {
        public Task<Result> InvokeReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default) => ReceiveAsync(@event, args, cancellationToken);

        public Task<Result> InvokeNativeAsync(EventSubscriberArgs args, Func<CancellationToken, Task<Result>> receive) => InvokeBehaviorsAsync(args, null, true, receive, default);

        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken) => onReceive(@event, args, cancellationToken);
    }

    private sealed class LoggingBehavior(string name, List<string> log, Func<SubscriberBehaviorDelegate, CancellationToken, Task<Result>>? invoke = null, List<int>? attempts = null, Action<EventSubscriberArgs>? observe = null) : ISubscriberBehavior
    {
        public bool ThrowOnDisposition { get; init; }

        public async Task<Result> InvokeAsync(EventSubscriberArgs args, SubscriberBehaviorDelegate next, CancellationToken cancellationToken)
        {
            attempts?.Add(args.AttemptCount);
            observe?.Invoke(args);
            log.Add($"{name}:before");
            try
            {
                return await (invoke ?? ((n, ct) => n(ct)))(next, cancellationToken);
            }
            finally
            {
                log.Add($"{name}:after");
            }
        }

        public Task OnDispositionAsync(EventSubscriberArgs args, SubscriberDisposition disposition, CancellationToken cancellationToken)
        {
            log.Add($"{name}:{disposition.Settlement}");
            return ThrowOnDisposition ? throw new InvalidOperationException("Callback failed.") : Task.CompletedTask;
        }
    }

    private sealed class ResolvedBehavior(List<string> log) : ISubscriberBehavior
    {
        public Task<Result> InvokeAsync(EventSubscriberArgs args, SubscriberBehaviorDelegate next, CancellationToken cancellationToken)
        {
            log.Add("resolved");
            return next(cancellationToken);
        }
    }
}
