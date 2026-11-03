namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

[TestFixture]
public class CoreExServiceBusExtensionsOpenTelemetryTests
{
    [TestCase("ServiceBusReceiver.RenewMessageLock")]
    [TestCase("ServiceBusSessionReceiver.RenewSessionLock")]
    [TestCase("ServiceBusReceiver.Receive")]
    public void IsBackgroundPollingActivity_ReturnsTrue_ForKnownBackgroundPollingActivityNames(string activityName)
        => ServiceBusTraceExtensions.IsBackgroundPollingActivity(activityName).Should().BeTrue();

    [TestCase("ServiceBusSender.Send")]
    [TestCase("ServiceBusProcessor.ProcessMessage")]
    [TestCase("ServiceBusSessionProcessor.ProcessSessionMessage")]
    [TestCase(null)]
    [TestCase("")]
    public void IsBackgroundPollingActivity_ReturnsFalse_ForOtherActivityNames(string? activityName)
        => ServiceBusTraceExtensions.IsBackgroundPollingActivity(activityName).Should().BeFalse();

    [Test]
    public void RenewMessageLockActivityName_MatchesAzureSdkDiagnosticPropertyValue()
        => ServiceBusTraceExtensions.RenewMessageLockActivityName.Should().Be("ServiceBusReceiver.RenewMessageLock");

    [Test]
    public void RenewSessionLockActivityName_MatchesAzureSdkDiagnosticPropertyValue()
        => ServiceBusTraceExtensions.RenewSessionLockActivityName.Should().Be("ServiceBusSessionReceiver.RenewSessionLock");

    [Test]
    public void ReceiveActivityName_MatchesAzureSdkDiagnosticPropertyValue()
        => ServiceBusTraceExtensions.ReceiveActivityName.Should().Be("ServiceBusReceiver.Receive");
}
