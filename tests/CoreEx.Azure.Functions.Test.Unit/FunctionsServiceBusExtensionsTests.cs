namespace CoreEx.Azure.Functions.Test.Unit;

[TestFixture]
public class FunctionsServiceBusExtensionsTests
{
    [TestCase(null)]
    [TestCase("received")]
    [TestCase("invalid metadata")]
    public async Task RawCallback_CapturesCorrelationAndRestoresAfterSettlement(string? supplied)
    {
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "parent" };
        CoreEx.ExecutionContext.Reset();
        CoreEx.ExecutionContext.SetCurrent(ec);
        using var activity = new System.Diagnostics.Activity("receive").Start();
        var actions = new RecordingServiceBusMessageActions();
        var logger = new WarningLogger();
        try
        {
            await actions.FunctionReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(correlationId: supplied), "queue", _ =>
            {
                ec.CorrelationId.Should().Be(CoreEx.Diagnostics.Correlation.IsValid(supplied) ? supplied : null);
                new EventData().CorrelationId.Should().Be(ec.CorrelationId);
                return Result.SuccessTask;
            }, new FunctionsServiceBusReceiveArgs { Logger = logger });
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

    [Test]
    public async Task DisabledCorrelation_RawCallbackIgnoresInvalidMetadataWithoutLogger()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["CoreEx:Correlation:Enabled"] = "false" }).Build();
        using var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).BuildServiceProvider();
        using var ec = new CoreEx.ExecutionContext { CorrelationId = "ambient", ServiceProvider = services };
        CoreEx.ExecutionContext.Reset();
        CoreEx.ExecutionContext.SetCurrent(ec);
        using var activity = new System.Diagnostics.Activity("receive").Start();
        var actions = new RecordingServiceBusMessageActions();
        try
        {
            await actions.FunctionReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(correlationId: "invalid value"), "queue", _ =>
            {
                ec.CorrelationId.Should().Be("ambient");
                activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().BeNull();
                return Result.SuccessTask;
            });
            actions.Settlements.Should().Equal("complete");
        }
        finally
        {
            CoreEx.ExecutionContext.Reset();
        }
    }

    [TestCase(null)]
    [TestCase(false)]
    [TestCase(true)]
    public async Task NativeCallback_EnrichesHostActivityBeforeHandler(bool? propagateBaggage)
    {
        using var activity = new System.Diagnostics.Activity("function").Start();
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(properties: new Dictionary<string, object>
        {
            ["Diagnostic-Id"] = "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01",
            ["baggage"] = "region=west"
        });
        var actions = new RecordingServiceBusMessageActions();
        await actions.FunctionReceiveAsync(message, "queue", _ =>
        {
            System.Diagnostics.Activity.Current.Should().BeSameAs(activity);
            activity.Links.Should().ContainSingle();
            activity.GetBaggageItem("region").Should().Be(propagateBaggage == true ? "west" : null);
            return Result.SuccessTask;
        }, args: propagateBaggage.HasValue ? new FunctionsServiceBusReceiveArgs { PropagateBaggage = propagateBaggage.Value } : null);
        actions.Settlements.Should().Equal("complete");
    }

    [Test]
    public void ReceiveArgs_DisableBaggageByDefault() => new FunctionsServiceBusReceiveArgs().PropagateBaggage.Should().BeFalse();

    [Test]
    public async Task NativePayload_DoesNotRequireCloudEventsAndFlowsToken()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("native payload"));
        var actions = new RecordingServiceBusMessageActions();
        using var cancellation = new CancellationTokenSource();
        await actions.FunctionReceiveAsync(message, "native-queue", ct =>
        {
            ct.Should().Be(cancellation.Token);
            message.Body.ToString().Should().Be("native payload");
            return Result.SuccessTask;
        }, cancellationToken: cancellation.Token);

        actions.Settlements.Should().Equal("complete");
        actions.Token.Should().Be(cancellation.Token);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task UnclassifiedFailure_PreservesOriginalException(bool throws)
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage();
        var actions = new RecordingServiceBusMessageActions();
        var error = new InvalidOperationException("Original failure.");
        Func<Task> run = () => actions.FunctionReceiveAsync(message, "queue", _ => throws ? Task.FromException<Result>(error) : Task.FromResult(Result.Fail(error)));
        var assertion = await run.Should().ThrowAsync<InvalidOperationException>();
        assertion.Which.Should().BeSameAs(error);
        actions.Settlements.Should().BeEmpty();
    }

    [Test]
    public async Task CancellationDuringHandler_DoesNotCompleteSuccessfulResult()
    {
        var actions = new RecordingServiceBusMessageActions();
        using var cancellation = new CancellationTokenSource();
        Func<Task> run = () => actions.FunctionReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(), "queue", _ =>
        {
            cancellation.Cancel();
            return Result.SuccessTask;
        }, cancellationToken: cancellation.Token);
        await run.Should().ThrowAsync<OperationCanceledException>();
        actions.Settlements.Should().BeEmpty();
    }

    [Test]
    public async Task Retry_RecordsBoundedReasonAndNoSecondAttemptOnFailure()
    {
        var actions = new RecordingServiceBusMessageActions { Failure = new InvalidOperationException("Abandon failed.") };
        Func<Task> run = () => actions.FunctionReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(), "queue",
            _ => Task.FromResult(Result.Fail(new EventSubscriberRetryException(new string('x', 600)))));
        await run.Should().ThrowAsync<InvalidOperationException>();
        actions.Settlements.Should().Equal("abandon");
        actions.Properties!["AbandonReason"].Should().Be(new string('x', 512));
    }

    [Test]
    public async Task DeadLetter_DoesNotPersistStackTrace()
    {
        var actions = new RecordingServiceBusMessageActions();
        await actions.FunctionReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(), "queue",
            _ => Task.FromResult(Result.Fail(new EventSubscriberDeadLetterException("Rejected.", new InvalidOperationException("Private diagnostic.")))));
        actions.Settlements.Should().Equal("dead-letter");
        actions.DeadLetterReason.Should().Be("Rejected.");
        actions.DeadLetterDescription.Should().BeNull();
    }

    private sealed class WarningLogger : ILogger
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
