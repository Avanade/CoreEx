namespace CoreEx.Azure.Functions.Test.Unit;

[TestFixture]
public class NativeServiceBusFunctionsTests
{
    [TestCase("success", "complete")]
    [TestCase("handled", "complete")]
    [TestCase("retry", "abandon")]
    [TestCase("reject", "dead-letter")]
    public void PlainJson_SettlesWithoutCloudEventMetadata(string outcome, string settlement)
    {
        using var test = FunctionTester.Create<NativeServiceBusFunctions>();
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString($"{{\"outcome\":\"{outcome}\"}}"), contentType: "application/json");
        var actions = new RecordingServiceBusMessageActions();
        test.Trigger<NativeServiceBusFunctions>()
            .Run(f => f.ProcessNativeMessage(message, actions, CancellationToken.None)).AssertSuccess();
        actions.Settlements.Should().Equal(settlement);
        actions.Message.Should().BeSameAs(message);
        message.Subject.Should().BeNull();
        message.ApplicationProperties.Should().BeEmpty();
    }

    [TestCase("{")]
    [TestCase("null")]
    [TestCase("{}")]
    public void InvalidJsonOrMissingOutcome_IsDeadLettered(string body)
    {
        using var test = FunctionTester.Create<NativeServiceBusFunctions>();
        var actions = new RecordingServiceBusMessageActions();
        test.Trigger<NativeServiceBusFunctions>()
            .Run(f => f.ProcessNativeMessage(ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString(body)), actions, CancellationToken.None))
            .AssertSuccess();
        actions.Settlements.Should().Equal("dead-letter");
        actions.DeadLetterDescription.Should().BeNull();
    }

    [Test]
    public void UnhandledFailure_BubblesWithoutSettlement()
    {
        using var test = FunctionTester.Create<NativeServiceBusFunctions>();
        var actions = new RecordingServiceBusMessageActions();
        test.Trigger<NativeServiceBusFunctions>()
            .Run(f => f.ProcessNativeMessage(ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("{\"outcome\":\"unexpected\"}")), actions, CancellationToken.None))
            .AssertException<EventSubscriberUnhandledException>();
        actions.Settlements.Should().BeEmpty();
    }

    [Test]
    public void CatastrophicFailure_BubblesWithoutSettlement()
    {
        using var test = FunctionTester.Create<NativeServiceBusFunctions>();
        var actions = new RecordingServiceBusMessageActions();
        test.Trigger<NativeServiceBusFunctions>()
            .Run(f => f.ProcessNativeMessage(ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("{\"outcome\":\"catastrophic\"}")), actions, CancellationToken.None))
            .AssertException<EventSubscriberCatastrophicException>();
        actions.Settlements.Should().BeEmpty();
    }
}
