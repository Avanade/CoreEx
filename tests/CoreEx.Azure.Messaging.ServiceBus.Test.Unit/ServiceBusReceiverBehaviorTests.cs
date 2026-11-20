namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

[TestFixture]
public class ServiceBusReceiverBehaviorTests
{
    [Test]
    public async Task RetriesInProcess_InvokesBehaviorPerAttemptAndDispositionOnce()
    {
        var calls = 0;
        var (receiver, behavior, actions) = Create((_, _) => ++calls < 3 ? Task.FromResult<Result>(new EventSubscriberRetryException("Transient.")) : Result.SuccessTask);

        var result = await receiver.ProcessAsync(actions);

        result.IsSuccess.Should().BeTrue();
        behavior.Attempts.Should().Equal(0, 1, 2);
        actions.Settlements.Should().Equal("complete");
        behavior.Dispositions.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Settlement = SubscriberSettlement.Completed, Classification = (ErrorHandling?)null, Error = (Exception?)null });
    }

    [TestCase(MessageRepresentation.Native, false, false)]
    [TestCase(MessageRepresentation.Native, true, true)]
    [TestCase(MessageRepresentation.CloudEvent, false, true)]
    [TestCase(MessageRepresentation.CloudEvent, true, false)]
    public async Task Retries_CountAttemptsBeforePreprocessing(MessageRepresentation representation, bool withBehaviors, bool throwDuringPreprocessing)
    {
        var subscriber = new RetryCountingSubscriber(representation, throwDuringPreprocessing);
        var behavior = new RecordingBehavior();
        if (withBehaviors)
            subscriber.Behaviors.Add(behavior);

        using var services = new ServiceCollection().AddExecutionContext().AddSingleton(subscriber).BuildServiceProvider();
        var transport = new EventFormatter().ConvertToCloudEvent(new EventData { Id = "event-id", Title = "test.event", Source = new Uri("urn:test") }).ToServiceBusMessage();
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: transport.Body, messageId: transport.MessageId, subject: transport.Subject, contentType: transport.ContentType, properties: transport.ApplicationProperties, deliveryCount: 7);
        var actions = new RecordingActions(message);
        var receiver = new TestReceiver<RetryCountingSubscriber>(CreateClient(), CreateOptions(null), services);

        var result = await receiver.ProcessAsync(actions);

        result.IsSuccess.Should().BeTrue();
        subscriber.BeforeAttempts.Should().Equal(0, 1, 2);
        subscriber.HandlerAttempts.Should().Equal(1, 2);
        if (withBehaviors)
            behavior.Attempts.Should().Equal(1, 2);
        else
            behavior.Attempts.Should().BeEmpty();

        actions.Settlements.Should().Equal("complete");

        var redeliveredActions = new RecordingActions(message);
        (await receiver.ProcessAsync(redeliveredActions)).IsSuccess.Should().BeTrue();

        subscriber.BeforeAttempts.Should().Equal(0, 1, 2, 0, 1, 2);
        subscriber.HandlerAttempts.Should().Equal(1, 2, 1, 2);
        if (withBehaviors)
        {
            behavior.Attempts.Should().Equal(1, 2, 1, 2);
            behavior.Dispositions.Should().HaveCount(2);
        }

        redeliveredActions.Settlements.Should().Equal("complete");
    }

    [TestCase(ErrorHandling.Retry, "abandon", SubscriberSettlement.Abandoned, ErrorHandling.Retry)]
    [TestCase(ErrorHandling.DeadLetter, "dead-letter", SubscriberSettlement.DeadLettered, ErrorHandling.DeadLetter)]
    public async Task RetriesExhausted_SettlesPerConfiguredHandlingOnce(ErrorHandling retryHandling, string expected, SubscriberSettlement settlement, ErrorHandling classification)
    {
        var (receiver, behavior, actions) = Create((_, _) => Task.FromResult<Result>(new EventSubscriberRetryException("Transient.")), o => o.RetryErrorHandling = retryHandling);

        await receiver.ProcessAsync(actions);

        behavior.Attempts.Should().Equal(0, 1, 2, 3);
        actions.Settlements.Should().Equal(expected);
        behavior.Dispositions.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Settlement = settlement, Classification = (ErrorHandling?)classification, IsCancelled = false, ReceiverPaused = false });
    }

    [Test]
    public async Task DeadLetter_IsReportedAsDeadLettered()
    {
        var (receiver, behavior, actions) = Create((_, _) => Task.FromResult<Result>(new EventSubscriberDeadLetterException("Rejected.")));

        await receiver.ProcessAsync(actions);

        actions.Settlements.Should().Equal("dead-letter");
        behavior.Dispositions.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Settlement = SubscriberSettlement.DeadLettered, Classification = (ErrorHandling?)ErrorHandling.DeadLetter });
    }

    [Test]
    public async Task Unclassified_IsAbandonedWithNoneClassification()
    {
        var (receiver, behavior, actions) = Create((_, _) => throw new InvalidOperationException("Boom."));

        await receiver.ProcessAsync(actions);

        actions.Settlements.Should().Equal("abandon");
        var disposition = behavior.Dispositions.Should().ContainSingle().Subject;
        disposition.Settlement.Should().Be(SubscriberSettlement.Abandoned);
        disposition.Classification.Should().Be(ErrorHandling.None);
        disposition.Error.Should().BeOfType<EventSubscriberUnhandledException>();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Catastrophic_IsAbandonedAndReportsPause(bool pause)
    {
        var (receiver, behavior, actions) = Create((_, _) => Task.FromResult<Result>(new EventSubscriberCatastrophicException("Fatal.")), o => o.PauseReceiverOnCatastrophicError = pause);

        await receiver.ProcessAsync(actions);

        actions.Settlements.Should().Equal("abandon");
        behavior.Dispositions.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Settlement = SubscriberSettlement.Abandoned, Classification = (ErrorHandling?)ErrorHandling.Catastrophic, ReceiverPaused = pause });
    }

    [Test]
    public async Task SettlementFailure_LockLostIsReportedAndRethrown()
    {
        var lockLost = new ServiceBusException("Lock lost.", ServiceBusFailureReason.MessageLockLost);
        var (receiver, behavior, actions) = Create((_, _) => Task.FromResult<Result>(new EventSubscriberDeadLetterException("Rejected.")));
        actions.Failure = lockLost;

        Func<Task> run = () => receiver.ProcessAsync(actions);
        (await run.Should().ThrowAsync<ServiceBusException>()).Which.Should().BeSameAs(lockLost);

        behavior.Dispositions.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Settlement = SubscriberSettlement.SettlementFailed,
            AttemptedSettlement = (SubscriberSettlement?)SubscriberSettlement.DeadLettered,
            Classification = (ErrorHandling?)ErrorHandling.DeadLetter,
            SettlementError = lockLost,
            IsLockLost = true
        });
    }

    [Test]
    public async Task HostCancellation_IsNotSettled()
    {
        using var cts = new CancellationTokenSource();
        var (receiver, behavior, actions) = Create((_, ct) =>
        {
            cts.Cancel();
            throw new OperationCanceledException(ct);
        });

        var result = await receiver.ProcessAsync(actions, cts.Token);

        result.IsFailure.Should().BeTrue();
        actions.Settlements.Should().BeEmpty();
        behavior.Dispositions.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Settlement = SubscriberSettlement.NotSettled, IsCancelled = true, Classification = (ErrorHandling?)null });
    }

    [Test]
    public async Task DispositionCallbackFailure_DoesNotChangeSettlement()
    {
        var (receiver, behavior, actions) = Create((_, _) => Result.SuccessTask);
        behavior.ThrowOnDisposition = true;

        var result = await receiver.ProcessAsync(actions);

        result.IsSuccess.Should().BeTrue();
        actions.Settlements.Should().Equal("complete");
        behavior.Dispositions.Should().ContainSingle();
    }

    [Test]
    public async Task Dispatched_MatchedSubscriberIsVisibleToBehavior()
    {
        var (receiver, behavior, actions) = CreateDispatched("test.entity.created");

        await receiver.ProcessAsync(actions);

        actions.Settlements.Should().Equal("complete");
        behavior.Subscribers.Should().ContainSingle().Which.Should().BeOfType<CreatedSubscribed>();
        behavior.Events.Should().ContainSingle().Which!.Title.Should().Be("test.entity.created");
        behavior.Dispositions.Should().ContainSingle().Which.Settlement.Should().Be(SubscriberSettlement.Completed);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Dispatched_RetriesExposeAttemptCount(bool withBehaviors)
    {
        var attempts = new List<int>();
        var behavior = new RecordingBehavior();
        using var services = new ServiceCollection()
            .AddExecutionContext()
            .AddSingleton(attempts)
            .AddScoped<RetryingSubscribed>()
            .AddSingleton(_ =>
            {
                var subscriber = new ServiceBusSubscribedSubscriber(new SubscribedManager().AddSubscriber<RetryingSubscribed>(), new EventFormatter(), NullLogger<ServiceBusSubscriberBase>.Instance);
                if (withBehaviors)
                    subscriber.Behaviors.Add(behavior);

                return subscriber;
            })
            .BuildServiceProvider();
        var transport = new EventFormatter().ConvertToCloudEvent(new EventData { Id = "event-id", Title = "test.entity.retry", Source = new Uri("urn:test") }).ToServiceBusMessage();
        var actions = new RecordingActions(ServiceBusMessageTests.ConvertToReceivedMessage(transport));
        var receiver = new TestReceiver<ServiceBusSubscribedSubscriber>(CreateClient(), CreateOptions(null), services);

        (await receiver.ProcessAsync(actions)).IsSuccess.Should().BeTrue();

        attempts.Should().Equal(0, 1);
        if (withBehaviors)
            behavior.Attempts.Should().Equal(0, 1);
        else
            behavior.Attempts.Should().BeEmpty();

        actions.Settlements.Should().Equal("complete");
    }

    [Test]
    public async Task Dispatched_UnmatchedSubscriberSkipsBehaviorAndCallback()
    {
        var (receiver, behavior, actions) = CreateDispatched("test.entity.unknown");

        await receiver.ProcessAsync(actions);

        actions.Settlements.Should().ContainSingle();
        behavior.Attempts.Should().BeEmpty();
        behavior.Dispositions.Should().BeEmpty();
    }

    private static (TestReceiver<NativeSubscriber> Receiver, RecordingBehavior Behavior, RecordingActions Actions) Create(Func<ServiceBusReceivedMessage, CancellationToken, Task<Result>> receive, Action<ServiceBusReceiverOptions>? configure = null)
    {
        var behavior = new RecordingBehavior();
        var subscriber = new NativeSubscriber(receive);
        subscriber.Behaviors.Add(behavior);

        var services = new ServiceCollection().AddExecutionContext().AddSingleton(subscriber).BuildServiceProvider();
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("{}"), messageId: "message-id");
        return (new TestReceiver<NativeSubscriber>(CreateClient(), CreateOptions(configure), services), behavior, new RecordingActions(message));
    }

    private static (TestReceiver<ServiceBusSubscribedSubscriber> Receiver, RecordingBehavior Behavior, RecordingActions Actions) CreateDispatched(string type)
    {
        var behavior = new RecordingBehavior();
        var services = new ServiceCollection()
            .AddExecutionContext()
            .AddScoped<CreatedSubscribed>()
            .AddSingleton(sp =>
            {
                var subscriber = new ServiceBusSubscribedSubscriber(new SubscribedManager().AddSubscriber<CreatedSubscribed>(), new EventFormatter(), NullLogger<ServiceBusSubscriberBase>.Instance);
                subscriber.Behaviors.Add(behavior);
                return subscriber;
            })
            .BuildServiceProvider();

        var formatter = new EventFormatter();
        var transport = formatter.ConvertToCloudEvent(new EventData { Id = "event-id", Title = type, Source = new Uri("urn:test") }).ToServiceBusMessage();
        transport.Subject = type;
        return (new TestReceiver<ServiceBusSubscribedSubscriber>(CreateClient(), CreateOptions(null), services), behavior, new RecordingActions(ServiceBusMessageTests.ConvertToReceivedMessage(transport)));
    }

    private static ServiceBusClient CreateClient() => new("Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==");

    private static ServiceBusReceiverOptions CreateOptions(Action<ServiceBusReceiverOptions>? configure)
    {
        var options = ServiceBusReceiverOptions.CreateForQueue("queue");
        options.MessageResiliency = ServiceBusReceiverResiliency.CreateMessageRetryResiliency(TimeSpan.FromMilliseconds(1), 3, Polly.DelayBackoffType.Constant);
        configure?.Invoke(options);
        return options;
    }

    private sealed class TestReceiver<TSubscriber>(ServiceBusClient client, ServiceBusReceiverOptions options, IServiceProvider services)
        : ServiceBusReceiverBase<TSubscriber>(client, options, services, NullLogger<ServiceBusReceiverBase<TSubscriber>>.Instance) where TSubscriber : ServiceBusSubscriberBase
    {
        public Task<Result> ProcessAsync(RecordingActions actions, CancellationToken cancellationToken = default) => OnProcessMessageAsync(actions.Message, actions, cancellationToken);
        protected override Task OnStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task OnPauseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task OnResumeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NativeSubscriber : ServiceBusSubscriberBase
    {
        private readonly Func<ServiceBusReceivedMessage, CancellationToken, Task<Result>> _receive;

        public NativeSubscriber(Func<ServiceBusReceivedMessage, CancellationToken, Task<Result>> receive) : base(NullLogger<ServiceBusSubscriberBase>.Instance)
        {
            _receive = receive;
            DataRepresentation = MessageRepresentation.Native;
        }

        protected override Task<Result> OnReceiveMessageAsync(ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default) => _receive(message, cancellationToken);

        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class RetryCountingSubscriber : ServiceBusSubscriberBase
    {
        private readonly bool _throwDuringPreprocessing;

        public RetryCountingSubscriber(MessageRepresentation representation, bool throwDuringPreprocessing) : base(new EventFormatter(), NullLogger<ServiceBusSubscriberBase>.Instance)
        {
            DataRepresentation = representation;
            _throwDuringPreprocessing = throwDuringPreprocessing;
        }

        public List<int> BeforeAttempts { get; } = [];

        public List<int> HandlerAttempts { get; } = [];

        protected override Task<Result> OnBeforeReceiveAsync(ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        {
            BeforeAttempts.Add(args.AttemptCount);
            if (args.AttemptCount == 0)
            {
                if (_throwDuringPreprocessing)
                    throw new EventSubscriberRetryException("Preprocessing retry.");

                return Task.FromResult<Result>(new EventSubscriberRetryException("Preprocessing retry."));
            }

            return Result.SuccessTask;
        }

        protected override Task<Result> OnReceiveMessageAsync(ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default)
            => DataRepresentation == MessageRepresentation.Native ? RecordAttempt(args, cancellationToken) : base.OnReceiveMessageAsync(message, args, cancellationToken);

        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken) => RecordAttempt(args, cancellationToken);

        private Task<Result> RecordAttempt(EventSubscriberArgs args, CancellationToken cancellationToken)
        {
            HandlerAttempts.Add(args.AttemptCount);
            return args.AttemptCount == 1 ? Task.FromResult<Result>(new EventSubscriberRetryException("Handler retry.")) : Result.SuccessTask;
        }
    }

    private sealed class RecordingBehavior : ISubscriberBehavior
    {
        public bool ThrowOnDisposition { get; set; }

        public List<int> Attempts { get; } = [];

        public List<SubscriberDisposition> Dispositions { get; } = [];

        public List<SubscribedBase?> Subscribers { get; } = [];

        public List<EventData?> Events { get; } = [];

        public Task<Result> InvokeAsync(EventSubscriberArgs args, SubscriberBehaviorDelegate next, CancellationToken cancellationToken)
        {
            Attempts.Add(args.AttemptCount);
            Subscribers.Add(args.Subscriber);
            Events.Add(args.Event);
            return next(cancellationToken);
        }

        public Task OnDispositionAsync(EventSubscriberArgs args, SubscriberDisposition disposition, CancellationToken cancellationToken)
        {
            Dispositions.Add(disposition);
            return ThrowOnDisposition ? throw new InvalidOperationException("Callback failed.") : Task.CompletedTask;
        }
    }

    [Subscribe("test.entity.created")]
    private sealed class CreatedSubscribed : SubscribedBase
    {
        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default) => Result.SuccessTask;
    }

    [Subscribe("test.entity.retry")]
    private sealed class RetryingSubscribed(List<int> attempts) : SubscribedBase
    {
        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        {
            attempts.Add(args.AttemptCount);
            return args.AttemptCount == 0 ? Task.FromResult<Result>(new EventSubscriberRetryException("Retry.")) : Result.SuccessTask;
        }
    }

    private sealed class RecordingActions(ServiceBusReceivedMessage message) : IServiceBusMessageActions
    {
        public ServiceBusReceivedMessage Message { get; } = message;

        public List<string> Settlements { get; } = [];

        public Exception? Failure { get; set; }

        public string EntityPath => "queue";

        public global::Azure.Core.Amqp.AmqpAnnotatedMessage AmqpMessage => Message.GetRawAmqpMessage();

        public Task CompleteMessageAsync(CancellationToken cancellationToken) => Settle("complete");

        public Task AbandonMessageAsync(Exception exception, CancellationToken cancellationToken) => Settle("abandon");

        public Task DeadLetterMessageAsync(Exception exception, CancellationToken cancellationToken) => Settle("dead-letter");

        private Task Settle(string settlement)
        {
            Settlements.Add(settlement);
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }
}
