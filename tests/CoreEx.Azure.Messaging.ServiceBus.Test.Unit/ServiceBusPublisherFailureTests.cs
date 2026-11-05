namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

[TestFixture]
[NonParallelizable]
public class ServiceBusPublisherFailureTests
{
    [TestCase(false, ServiceBusFailureReason.ServiceTimeout)]
    [TestCase(false, ServiceBusFailureReason.ServiceBusy)]
    [TestCase(false, ServiceBusFailureReason.ServiceCommunicationProblem)]
    [TestCase(true, ServiceBusFailureReason.GeneralError)]
    [TestCase(false, ServiceBusFailureReason.GeneralError)]
    public async Task PublishAsync_ExhaustedTransientFailuresBecomeGenericTransientException(bool transient, ServiceBusFailureReason reason)
    {
        var message = !transient && reason == ServiceBusFailureReason.GeneralError
            ? "The connection was closed by container because it did not have any active links."
            : "Private broker diagnostic.";
        var failures = Enumerable.Range(0, 3).Select(i => new ServiceBusException(transient, $"{message} Attempt {i}.", "unit-test", reason)).ToArray();
        var sender = new StubSender((attempt, _) => failures[attempt - 1]);
        var publisher = CreatePublisher(sender);
        Func<Task> publish = () => publisher.PublishAsync();

        var exception = (await publish.Should().ThrowAsync<TransientException>()).Which;

        sender.SendAttempts.Should().Be(3);
        exception.InnerException.Should().BeSameAs(failures[2]);
        exception.InnerException.StackTrace.Should().Contain(nameof(StubSender.SendMessagesAsync));
        exception.Message.Should().Be(new TransientException().Message);
        exception.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        exception.ErrorType.Should().Be("transient");
        exception.IsTransient.Should().BeTrue();
        exception.RetryAfter.Should().Be(TransientException.DefaultRetryAfter);
        exception.ShouldBeLogged.Should().BeTrue();
        publisher.HasBeenPublished.Should().BeFalse();
    }

    [TestCase(ServiceBusFailureReason.MessagingEntityNotFound)]
    [TestCase(ServiceBusFailureReason.MessageSizeExceeded)]
    [TestCase(ServiceBusFailureReason.QuotaExceeded)]
    [TestCase(ServiceBusFailureReason.GeneralError)]
    public async Task PublishAsync_PermanentFailuresPropagateWithoutRetry(ServiceBusFailureReason reason)
    {
        var failure = new ServiceBusException(false, "Permanent broker failure.", "unit-test", reason);
        var sender = new StubSender((_, _) => failure);
        var publisher = CreatePublisher(sender);
        Func<Task> publish = () => publisher.PublishAsync();

        (await publish.Should().ThrowAsync<ServiceBusException>()).Which.Should().BeSameAs(failure);

        sender.SendAttempts.Should().Be(1);
        publisher.HasBeenPublished.Should().BeFalse();
    }

    [Test]
    public async Task PublishAsync_FinalPermanentFailureIsNotTranslated()
    {
        var transient = new ServiceBusException("Timeout.", ServiceBusFailureReason.ServiceTimeout);
        var permanent = new ServiceBusException(false, "Destination does not exist.", "unit-test", ServiceBusFailureReason.MessagingEntityNotFound);
        var sender = new StubSender((attempt, _) => attempt == 1 ? transient : permanent);
        var publisher = CreatePublisher(sender);
        Func<Task> publish = () => publisher.PublishAsync();

        (await publish.Should().ThrowAsync<ServiceBusException>()).Which.Should().BeSameAs(permanent);

        sender.SendAttempts.Should().Be(2);
    }

    [Test]
    public async Task PublishAsync_RecoveredTransientFailureSucceeds()
    {
        var sender = new StubSender((attempt, _) => attempt < 3 ? new ServiceBusException("Timeout.", ServiceBusFailureReason.ServiceTimeout) : null);
        var publisher = CreatePublisher(sender);

        await publisher.PublishAsync();

        sender.SendAttempts.Should().Be(3);
        publisher.HasBeenPublished.Should().BeTrue();
    }

    [Test]
    public async Task PublishAsync_CallerCancellationIsNotTranslatedOrRetried()
    {
        using var source = new CancellationTokenSource();
        var failure = new OperationCanceledException(source.Token);
        var sender = new StubSender((_, token) =>
        {
            token.Should().Be(source.Token);
            source.Cancel();
            return failure;
        });
        var publisher = CreatePublisher(sender);
        Func<Task> publish = () => publisher.PublishAsync(source.Token);

        (await publish.Should().ThrowAsync<OperationCanceledException>()).Which.Should().BeSameAs(failure);

        sender.SendAttempts.Should().Be(1);
        publisher.HasBeenPublished.Should().BeFalse();
    }

    [Test]
    public async Task PublishAsync_CancelledCallerDoesNotTranslateServiceBusFailure()
    {
        using var source = new CancellationTokenSource();
        var failure = new ServiceBusException("Timeout.", ServiceBusFailureReason.ServiceTimeout);
        var sender = new StubSender((_, _) =>
        {
            source.Cancel();
            return failure;
        });
        var publisher = CreatePublisher(sender);
        publisher.SendResiliency = new Polly.ResiliencePipelineBuilder<Result>().Build();
        Func<Task> publish = () => publisher.PublishAsync(source.Token);

        (await publish.Should().ThrowAsync<ServiceBusException>()).Which.Should().BeSameAs(failure);

        sender.SendAttempts.Should().Be(1);
    }

    [Test]
    public async Task PublishAsync_UnrelatedExceptionsPropagateWithoutRetry()
    {
        var failure = new InvalidOperationException("Not a broker failure.");
        var sender = new StubSender((_, _) => failure);
        var publisher = CreatePublisher(sender);
        Func<Task> publish = () => publisher.PublishAsync();

        (await publish.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);

        sender.SendAttempts.Should().Be(1);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PublishAsync_BatchCreationFailuresUseTheSameClassification(bool transient)
    {
        var failure = new ServiceBusException(transient, "Batch creation failed.", "unit-test", ServiceBusFailureReason.GeneralError);
        var sender = new StubSender((_, _) => null) { BatchException = failure };
        var publisher = CreatePublisher(sender);
        Func<Task> publish = () => publisher.PublishAsync();

        if (transient)
            (await publish.Should().ThrowAsync<TransientException>()).Which.InnerException.Should().BeSameAs(failure);
        else
            (await publish.Should().ThrowAsync<ServiceBusException>()).Which.Should().BeSameAs(failure);

        sender.SendAttempts.Should().Be(0);
    }

    [Test]
    public async Task PublishAsync_OversizedMessageRemainsPermanent()
    {
        var sender = new StubSender((_, _) => null) { BatchCapacity = 0 };
        var publisher = CreatePublisher(sender);
        Func<Task> publish = () => publisher.PublishAsync();

        await publish.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A single event is too large to fit in the Azure Service Bus message batch.");

        sender.SendAttempts.Should().Be(0);
    }

    [Test]
    public async Task PublishAsync_PartialSuccessPreservesSendMetricsAndFinalFailure()
    {
        var counts = new Dictionary<string, long>();
        var durations = 0;
        var meter = ServiceBusMetrics.Meter;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, owner) =>
            {
                if (ReferenceEquals(instrument.Meter, meter))
                    owner.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            tags[0].Key.Should().Be(ServiceBusMetrics.DestinationTagName);
            tags[0].Value.Should().Be("unit-test");
            counts[instrument.Name] = counts.GetValueOrDefault(instrument.Name) + measurement;
        });
        listener.SetMeasurementEventCallback<double>((_, _, _, _) => durations++);
        listener.Start();
        var failure = new ServiceBusException("Timeout after an earlier batch succeeded.", ServiceBusFailureReason.ServiceTimeout);
        var sender = new StubSender((attempt, _) => attempt == 1 ? null : failure) { BatchCapacity = 1 };
        var publisher = CreatePublisher(sender);
        AddEvent(publisher, "second");
        Func<Task> publish = () => publisher.PublishAsync();

        (await publish.Should().ThrowAsync<TransientException>()).Which.InnerException.Should().BeSameAs(failure);

        sender.SendAttempts.Should().Be(4);
        counts["servicebus.messages.send.sent"].Should().Be(1);
        counts["servicebus.messages.send.failed"].Should().Be(1);
        durations.Should().Be(2);
        publisher.HasBeenPublished.Should().BeFalse();
    }

    private static ServiceBusPublisher CreatePublisher(StubSender sender)
    {
        var publisher = new ServiceBusPublisher(new StubClient(sender), logger: NullLogger<ServiceBusPublisher>.Instance)
        {
            SendResiliency = ServiceBusPublisherResiliency.CreateSendRetryResiliency(TimeSpan.Zero, maxRetryAttempts: 2)
        };
        AddEvent(publisher, "first");
        return publisher;
    }

    private static void AddEvent(ServiceBusPublisher publisher, string id)
        => publisher.Add("unit-test", new CloudNative.CloudEvents.CloudEvent { Id = id, Type = "unit.test", Source = new Uri("urn:test") });

    private sealed class StubClient(StubSender sender) : ServiceBusClient
    {
        public override ServiceBusSender CreateSender(string queueOrTopicName) => sender;
    }

    private sealed class StubSender(Func<int, CancellationToken, Exception?> sendException) : ServiceBusSender
    {
        public int SendAttempts { get; private set; }

        public Exception? BatchException { get; init; }

        public int BatchCapacity { get; init; } = int.MaxValue;

        public override ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(CancellationToken cancellationToken = default)
        {
            if (BatchException is not null)
                throw BatchException;

            List<ServiceBusMessage> messages = [];
            return ValueTask.FromResult(ServiceBusModelFactory.ServiceBusMessageBatch(0, messages, tryAddCallback: _ => messages.Count < BatchCapacity));
        }

        public override Task SendMessagesAsync(ServiceBusMessageBatch messageBatch, CancellationToken cancellationToken = default)
        {
            SendAttempts++;
            var exception = sendException(SendAttempts, cancellationToken);
            if (exception is not null)
                throw exception;

            return Task.CompletedTask;
        }

        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
