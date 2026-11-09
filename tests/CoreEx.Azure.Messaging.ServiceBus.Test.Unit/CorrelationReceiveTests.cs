namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

[NonParallelizable]
public class CorrelationReceiveTests
{
    [TearDown]
    public void Reset() => CoreEx.ExecutionContext.Reset();

    [TestCase(null)]
    [TestCase("workflow")]
    [TestCase("bad value")]
    [TestCase("bad,metadata")]
    public async Task NativeReceive_ImportsClearsWarnsAndRestores(string? supplied)
    {
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "parent" };
        CoreEx.ExecutionContext.SetCurrent(ec);
        using var activity = new Activity("receive").Start();
        var logger = new WarningLogger();
        var expected = CoreEx.Diagnostics.Correlation.IsValid(supplied) ? supplied : null;
        var subscriber = new NativeSubscriber(logger, async _ =>
        {
            ec.CorrelationId.Should().Be(expected);
            activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be(expected);
            new EventData().CorrelationId.Should().Be(expected);
            await Task.Yield();
            ec.CorrelationId.Should().Be(expected);
        });
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("\"value\""), correlationId: supplied);
        (await subscriber.ReceiveAsync(message)).IsSuccess.Should().BeTrue();
        ec.CorrelationId.Should().Be("parent");
        logger.Warnings.Should().Be(CoreEx.Diagnostics.Correlation.IsValid(supplied) ? 0 : 1);
        logger.Text.Should().NotContain(supplied is "bad value" or "bad,metadata" ? supplied : "secret");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task DisabledCorrelation_DoesNotImportTransportOrEnvelope(bool cloudEvent)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["CoreEx:Correlation:Enabled"] = "false" }).Build();
        using var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).BuildServiceProvider();
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "ambient", ServiceProvider = services };
        CoreEx.ExecutionContext.SetCurrent(ec);
        using var activity = new Activity("receive").Start();
        var logger = new WarningLogger();
        var formatter = new EventFormatter();
        var original = new EventData { Id = "message", Title = "Domain.Order.Created", Source = new Uri("urn:test"), CorrelationId = "invalid metadata" };
        var transport = formatter.ConvertToCloudEvent(original).ToServiceBusMessage();
        var message = cloudEvent
            ? ServiceBusModelFactory.ServiceBusReceivedMessage(body: transport.Body, contentType: transport.ContentType, properties: transport.ApplicationProperties, correlationId: "broker")
            : ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("\"value\""), correlationId: "invalid metadata");
        Task Check()
        {
            ec.CorrelationId.Should().Be("ambient");
            activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().BeNull();
            return Task.CompletedTask;
        }

        ServiceBusSubscriberBase subscriber = cloudEvent
            ? new EnvelopeSubscriber(formatter, logger, async ed => { ed.CorrelationId.Should().Be("invalid metadata"); await Check(); return Result.Success; })
            : new NativeSubscriber(logger, _ => Check());
        (await subscriber.ReceiveAsync(message)).IsSuccess.Should().BeTrue();
        ec.CorrelationId.Should().Be("ambient");
        logger.Warnings.Should().Be(0);
    }

    [Test]
    public async Task TypedSubscriber_UsesTransportCorrelationForBinaryCloudEvent()
    {
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "parent" };
        CoreEx.ExecutionContext.SetCurrent(ec);
        using var activity = new Activity("receive").Start();
        var subscriber = new NativeSubscriber(new WarningLogger(), _ =>
        {
            ec.CorrelationId.Should().Be("broker-correlation");
            activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("broker-correlation");
            return Task.CompletedTask;
        });
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("\"value\""),
            contentType: "application/json",
            correlationId: "broker-correlation",
            properties: new Dictionary<string, object> { [ServiceBusExtensions.CloudEventSpecVersionPropertyName] = "1.0" });

        message.IsCloudEvent().Should().BeTrue();
        (await subscriber.ReceiveAsync(message)).IsSuccess.Should().BeTrue();
        ec.CorrelationId.Should().Be("parent");
    }

    [TestCase(null)]
    [TestCase("envelope")]
    [TestCase("invalid metadata")]
    public async Task CloudEventEnvelope_IsAuthoritativeIncludingCustomNamesAndAbsence(string? supplied)
    {
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "ambient" };
        CoreEx.ExecutionContext.SetCurrent(ec);
        using var activity = new Activity("receive").Start();
        var id = activity.Id;
        var formatter = new EventFormatter { CorrelationIdAttributeName = "customcorrelation" };
        var original = new EventData { Id = "message", Title = "Domain.Order.Created", Source = new Uri("urn:test"), CorrelationId = supplied };
        var envelope = formatter.ConvertToCloudEvent(original);
        var transport = envelope.ToServiceBusMessage();
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: transport.Body, contentType: transport.ContentType,
            properties: transport.ApplicationProperties, correlationId: "broker");
        var logger = new WarningLogger();
        var expected = CoreEx.Diagnostics.Correlation.IsValid(supplied) ? supplied : null;
        var subscriber = new EnvelopeSubscriber(formatter, logger, ed =>
        {
            ed.CorrelationId.Should().Be(supplied);
            ec.CorrelationId.Should().Be(expected);
            EventData.CreateEvent("Order", "Created").CorrelationId.Should().Be(expected);
            activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be(expected);
            return Result.SuccessTask;
        });
        (await subscriber.ReceiveAsync(message)).IsSuccess.Should().BeTrue();
        activity.Id.Should().Be(id);
        logger.Warnings.Should().Be(CoreEx.Diagnostics.Correlation.IsValid(supplied) ? 0 : 1);
        ec.CorrelationId.Should().Be("ambient");
    }

    [Test]
    public async Task SequentialReceives_RefreshSameActivityIncludingAbsence()
    {
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "parent" };
        CoreEx.ExecutionContext.SetCurrent(ec);
        using var activity = new Activity("receive").Start();
        var subscriber = new NativeSubscriber(new WarningLogger(), _ => Task.CompletedTask);
        foreach (var correlation in new string?[] { "first", null, "second" })
        {
            (await subscriber.ReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("\"value\""), correlationId: correlation))).IsSuccess.Should().BeTrue();
            ec.CorrelationId.Should().Be("parent");
            activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be(correlation);
        }
    }

    [Test]
    public async Task NestedReceive_RestoresOuterContextAndSharedActivity()
    {
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "parent" };
        CoreEx.ExecutionContext.SetCurrent(ec);
        using var activity = new Activity("receive").Start();
        var inner = new NativeSubscriber(new WarningLogger(), _ =>
        {
            ec.CorrelationId.Should().Be("inner");
            return Task.CompletedTask;
        });
        var outer = new NativeSubscriber(new WarningLogger(), async _ =>
        {
            (await inner.ReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("\"value\""), correlationId: "inner"))).IsSuccess.Should().BeTrue();
            ec.CorrelationId.Should().Be("outer");
            activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("outer");
        });
        (await outer.ReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("\"value\""), correlationId: "outer"))).IsSuccess.Should().BeTrue();
        ec.CorrelationId.Should().Be("parent");
        activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("outer");
    }

    [Test]
    public async Task NestedReceive_WithOwnActivityRetainsItsExportedCorrelation()
    {
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "parent" };
        CoreEx.ExecutionContext.SetCurrent(ec);
        using var outerActivity = new Activity("outer").Start();
        var outer = new NativeSubscriber(new WarningLogger(), async _ =>
        {
            using (var innerActivity = new Activity("inner").Start())
            {
                var inner = new NativeSubscriber(new WarningLogger(), _ => Task.CompletedTask);
                (await inner.ReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("\"value\""), correlationId: "inner"))).IsSuccess.Should().BeTrue();
                ec.CorrelationId.Should().Be("outer");
                innerActivity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("inner");
            }
            outerActivity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("outer");
        });
        (await outer.ReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("\"value\""), correlationId: "outer"))).IsSuccess.Should().BeTrue();
        ec.CorrelationId.Should().Be("parent");
    }

    [Test]
    public async Task HandlerFailure_RestoresCallerWithoutChangingClassification()
    {
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "parent" };
        CoreEx.ExecutionContext.SetCurrent(ec);
        var subscriber = new NativeSubscriber(new WarningLogger(), _ => throw new InvalidOperationException("handler"));
        var result = await subscriber.ReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("\"value\""), correlationId: "received"));
        result.IsFailure.Should().BeTrue();
        ec.CorrelationId.Should().Be("parent");
    }

    private sealed class NativeSubscriber(ILogger<ServiceBusSubscriberBase> logger, Func<string, Task> handler) : ServiceBusSubscriberBase<string>(logger)
    {
        protected override async Task<Result> OnReceiveAsync(string value, ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        {
            await handler(value);
            return Result.Success;
        }
    }

    private sealed class EnvelopeSubscriber(IEventFormatter formatter, ILogger<ServiceBusSubscriberBase> logger, Func<EventData, Task<Result>> handler) : ServiceBusSubscriberBase(formatter, logger)
    {
        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken) => handler(@event);
    }

    private sealed class WarningLogger : ILogger<ServiceBusSubscriberBase>
    {
        public int Warnings { get; private set; }
        public string Text { get; private set; } = "";

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings++;
                Text += formatter(state, exception);
            }
        }
    }
}
