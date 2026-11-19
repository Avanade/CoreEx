namespace CoreEx.Azure.Functions.Test.Unit;

[TestFixture]
public class FunctionsServiceBusExtensionsTests
{
    [TestCase(CloudNative.CloudEvents.ContentMode.Binary, true, true)]
    [TestCase(CloudNative.CloudEvents.ContentMode.Structured, true, true)]
    [TestCase(CloudNative.CloudEvents.ContentMode.Binary, false, false)]
    [TestCase(CloudNative.CloudEvents.ContentMode.Binary, true, false)]
    public async Task TypedCloudEventSubscriber_UsesBridgeSettlement(CloudNative.CloudEvents.ContentMode mode, bool validEnvelope, bool matches)
    {
        var subscriber = new UniformSubscriber { InvalidDataHandling = CoreEx.Events.Subscribing.ErrorHandling.DeadLetter };
        var @event = new EventData { Id = "command-id", Title = matches ? "test.command.v1" : "test.command.v2", Source = new Uri("urn:test:producer"), Data = BinaryData.FromString("\"value\"", "application/json") };
        var outgoing = new EventFormatter().ConvertToCloudEvent(@event).ToServiceBusMessage(mode);
        var message = validEnvelope
            ? ServiceBusModelFactory.ServiceBusReceivedMessage(body: outgoing.Body, messageId: outgoing.MessageId, subject: outgoing.Subject, contentType: outgoing.ContentType, properties: outgoing.ApplicationProperties)
            : ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("\"value\""), contentType: "application/json");
        var actions = new RecordingServiceBusMessageActions();

        await subscriber.FunctionReceiveAsync(message, actions, "queue");

        actions.Settlements.Should().Equal(matches ? "complete" : "dead-letter");
        actions.Message.Should().BeSameAs(message);
        subscriber.Calls.Should().Be(matches ? 1 : 0);
        if (validEnvelope && !matches)
            actions.DeadLetterReason.Should().Contain("did not match any [Subscribe] rule");
    }

    [TestCase(null)]
    [TestCase("received")]
    [TestCase("invalid metadata")]
    public async Task NativeSubscriber_CapturesCorrelationAndRestoresAfterSettlement(string? supplied)
    {
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "parent" };
        CoreEx.ExecutionContext.Reset();
        CoreEx.ExecutionContext.SetCurrent(ec);
        using var activity = new System.Diagnostics.Activity("receive").Start();
        var actions = new RecordingServiceBusMessageActions();
        var logger = new WarningLogger();
        try
        {
            var subscriber = new NativeSubscriber((_, _) =>
            {
                ec.CorrelationId.Should().Be(CoreEx.Diagnostics.Correlation.IsValid(supplied) ? supplied : null);
                new EventData().CorrelationId.Should().Be(ec.CorrelationId);
                return Result.SuccessTask;
            }, logger);

            await subscriber.FunctionReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(correlationId: supplied), actions, "queue");
            actions.Settlements.Should().Equal("complete");
            ec.CorrelationId.Should().Be("parent");
            logger.Warnings.Should().HaveCount(CoreEx.Diagnostics.Correlation.IsValid(supplied) ? 0 : 1);
            if (supplied == "invalid metadata")
                logger.Warnings.Single().Should().NotContain(supplied);
        }
        finally
        {
            CoreEx.ExecutionContext.Reset();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task NativeSubscriber_EnrichesHostActivityBeforeHandler(bool propagateBaggage)
    {
        using var activity = new System.Diagnostics.Activity("function").Start();
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(properties: new Dictionary<string, object>
        {
            ["Diagnostic-Id"] = "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01",
            ["baggage"] = "region=west"
        });
        var actions = new RecordingServiceBusMessageActions();
        var subscriber = new NativeSubscriber((_, _) =>
        {
            System.Diagnostics.Activity.Current.Should().BeSameAs(activity);
            activity.Links.Should().ContainSingle();
            activity.GetBaggageItem("region").Should().Be(propagateBaggage ? "west" : null);
            return Result.SuccessTask;
        }) { PropagateBaggage = propagateBaggage };

        await subscriber.FunctionReceiveAsync(message, actions, "queue");
        actions.Settlements.Should().Equal("complete");
    }

    [Test]
    public async Task NativePayload_DoesNotRequireCloudEventsAndFlowsToken()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("native payload"));
        var actions = new RecordingServiceBusMessageActions();
        using var cancellation = new CancellationTokenSource();
        var subscriber = new NativeSubscriber((m, ct) =>
        {
            ct.Should().Be(cancellation.Token);
            m.Body.ToString().Should().Be("native payload");
            return Result.SuccessTask;
        });

        await subscriber.FunctionReceiveAsync(message, actions, "native-queue", cancellationToken: cancellation.Token);

        actions.Settlements.Should().Equal("complete");
        actions.Token.Should().Be(cancellation.Token);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task UnclassifiedFailure_PreservesOriginalException(bool throws)
    {
        var actions = new RecordingServiceBusMessageActions();
        var error = new InvalidOperationException("Original failure.");
        var subscriber = new NativeSubscriber((_, _) => throws ? Task.FromException<Result>(error) : Task.FromResult(Result.Fail(error)));

        Func<Task> run = () => subscriber.FunctionReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(), actions, "queue");
        var assertion = await run.Should().ThrowAsync<InvalidOperationException>();
        assertion.Which.Should().BeSameAs(error);
        actions.Settlements.Should().BeEmpty();
    }

    [Test]
    public async Task CancellationDuringHandler_DoesNotCompleteSuccessfulResult()
    {
        var actions = new RecordingServiceBusMessageActions();
        using var cancellation = new CancellationTokenSource();
        var subscriber = new NativeSubscriber((_, _) =>
        {
            cancellation.Cancel();
            return Result.SuccessTask;
        });

        Func<Task> run = () => subscriber.FunctionReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(), actions, "queue", cancellationToken: cancellation.Token);
        await run.Should().ThrowAsync<OperationCanceledException>();
        actions.Settlements.Should().BeEmpty();
    }

    [Test]
    public async Task Retry_RecordsBoundedReasonAndNoSecondAttemptOnFailure()
    {
        var actions = new RecordingServiceBusMessageActions { Failure = new InvalidOperationException("Abandon failed.") };
        var subscriber = new NativeSubscriber((_, _) => Task.FromResult(Result.Fail(new EventSubscriberRetryException(new string('x', 600)))));

        Func<Task> run = () => subscriber.FunctionReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(), actions, "queue");
        await run.Should().ThrowAsync<InvalidOperationException>();
        actions.Settlements.Should().Equal("abandon");
        actions.Properties!["AbandonReason"].Should().Be(new string('x', 512));
    }

    [Test]
    public async Task DeadLetter_DoesNotPersistStackTrace()
    {
        var actions = new RecordingServiceBusMessageActions();
        var subscriber = new NativeSubscriber((_, _) => Task.FromResult(Result.Fail(new EventSubscriberDeadLetterException("Rejected.", new InvalidOperationException("Private diagnostic.")))));
        await subscriber.FunctionReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(), actions, "queue");
        actions.Settlements.Should().Equal("dead-letter");
        actions.DeadLetterReason.Should().Be("Rejected.");
        actions.DeadLetterDescription.Should().BeNull();
    }

    private sealed class NativeSubscriber : ServiceBusSubscriberBase
    {
        private readonly Func<ServiceBusReceivedMessage, CancellationToken, Task<Result>> _receive;

        public NativeSubscriber(Func<ServiceBusReceivedMessage, CancellationToken, Task<Result>> receive, ILogger<ServiceBusSubscriberBase>? logger = null) : base(logger ?? NullLogger<ServiceBusSubscriberBase>.Instance)
        {
            _receive = receive;
            DataRepresentation = MessageRepresentation.Native;
        }

        protected override Task<Result> OnReceiveMessageAsync(ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default) => _receive(message, cancellationToken);

        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    [CoreEx.Events.Subscribing.Subscribe("test.command.v1", "urn:test:producer")]
    private sealed class UniformSubscriber() : ServiceBusSubscriberBase<string>(NullLogger<ServiceBusSubscriberBase>.Instance)
    {
        public int Calls { get; private set; }

        protected override Task<Result> OnReceiveAsync(string value, ServiceBusReceivedMessage message, CoreEx.Events.Subscribing.EventSubscriberArgs args, CancellationToken cancellationToken = default)
        {
            value.Should().Be("value");
            args.CloudEvent.Should().NotBeNull();
            Calls++;
            return Result.SuccessTask;
        }
    }

    private sealed class WarningLogger : ILogger<ServiceBusSubscriberBase>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }
    }
}
