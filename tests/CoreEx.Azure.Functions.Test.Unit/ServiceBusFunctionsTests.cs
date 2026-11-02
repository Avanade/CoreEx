namespace CoreEx.Azure.Functions.Test.Unit;

[TestFixture]
public class ServiceBusFunctionsTests
{
    [TestCase("success", "complete")]
    [TestCase("handled", "complete")]
    [TestCase("retry", "abandon")]
    [TestCase("reject", "dead-letter")]
    public void ClassifiedOutcome_SettlesExactlyOnce(string outcome, string settlement)
    {
        using var test = FunctionTester.Create<ServiceBusFunctions>();
        var message = CreateMessage(outcome);
        var actions = new RecordingServiceBusMessageActions();
        test.Trigger<ServiceBusFunctions>()
            .Run(f => f.ProcessMessage(message, actions, CancellationToken.None))
            .AssertSuccess();

        actions.Settlements.Should().Equal(settlement);
        actions.Message.Should().BeSameAs(message);
        actions.DeadLetterDescription.Should().BeNull();
    }

    [TestCase("unexpected", typeof(EventSubscriberUnhandledException))]
    [TestCase("catastrophic", typeof(EventSubscriberCatastrophicException))]
    public void UnhandledOutcome_FailsInvocationWithoutSettlement(string outcome, Type exceptionType)
    {
        using var test = FunctionTester.Create<ServiceBusFunctions>();
        var actions = new RecordingServiceBusMessageActions();
        var result = test.Trigger<ServiceBusFunctions>()
            .Run(f => f.ProcessMessage(CreateMessage(outcome), actions, CancellationToken.None));

        if (exceptionType == typeof(EventSubscriberCatastrophicException))
            result.AssertException<EventSubscriberCatastrophicException>();
        else
            result.AssertException<EventSubscriberUnhandledException>();

        actions.Settlements.Should().BeEmpty();
    }

    [Test]
    public void ShutdownCancellation_DoesNotSettle()
    {
        using var test = FunctionTester.Create<ServiceBusFunctions>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var actions = new RecordingServiceBusMessageActions();
        test.Trigger<ServiceBusFunctions>()
            .Run(f => f.ProcessMessage(CreateMessage("success"), actions, cancellation.Token))
            .AssertException<OperationCanceledException>();

        actions.Settlements.Should().BeEmpty();
    }

    [Test]
    public void SettlementFailure_PropagatesWithoutSecondAttempt()
    {
        using var test = FunctionTester.Create<ServiceBusFunctions>();
        var actions = new RecordingServiceBusMessageActions { Failure = new InvalidOperationException("Settlement failed.") };
        test.Trigger<ServiceBusFunctions>()
            .Run(f => f.ProcessMessage(CreateMessage("success"), actions, CancellationToken.None))
            .AssertException<InvalidOperationException>();

        actions.Settlements.Should().Equal("complete");
    }

    [TestCase(typeof(ServiceBusFunctions), nameof(ServiceBusFunctions.ProcessMessage))]
    [TestCase(typeof(NativeServiceBusFunctions), nameof(NativeServiceBusFunctions.ProcessNativeMessage))]
    public void Trigger_DisablesAutomaticCompletion(Type functionType, string methodName)
    {
        var method = functionType.GetMethod(methodName) ?? throw new InvalidOperationException("The trigger method must exist.");
        var trigger = method.GetParameters()[0].GetCustomAttributes(typeof(ServiceBusTriggerAttribute), false).Cast<ServiceBusTriggerAttribute>().Single();
        trigger.AutoCompleteMessages.Should().BeFalse();
    }

    private static ServiceBusReceivedMessage CreateMessage(string outcome)
    {
        var @event = EventData.CreateCommand("functions", "message", "process")
            .WithTitle("coreex.functions.message.process")
            .WithKey(outcome)
            .WithSource(new Uri("urn:coreex:functions:test"));
        var outgoing = new EventFormatter().ConvertToCloudEvent(@event).ToServiceBusMessage();
        return ServiceBusModelFactory.ServiceBusReceivedMessage(body: outgoing.Body, messageId: outgoing.MessageId,
            subject: outgoing.Subject, contentType: outgoing.ContentType, properties: outgoing.ApplicationProperties.ToDictionary(x => x.Key, x => x.Value));
    }
}
