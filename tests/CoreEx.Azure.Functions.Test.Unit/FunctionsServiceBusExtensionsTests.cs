namespace CoreEx.Azure.Functions.Test.Unit;

[TestFixture]
public class FunctionsServiceBusExtensionsTests
{
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
}
