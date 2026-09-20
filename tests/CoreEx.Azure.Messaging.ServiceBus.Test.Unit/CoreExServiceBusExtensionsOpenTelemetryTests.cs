using OpenTelemetry.Trace;

namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

[TestFixture]
public class CoreExServiceBusExtensionsOpenTelemetryTests
{
    [TestCase("ServiceBusReceiver.RenewMessageLock")]
    [TestCase("ServiceBusSessionReceiver.RenewSessionLock")]
    [TestCase("ServiceBusReceiver.Receive")]
    public void IsBackgroundPollingActivity_ReturnsTrue_ForKnownBackgroundPollingActivityNames(string activityName)
        => CoreExServiceBusExtensions.IsBackgroundPollingActivity(activityName).Should().BeTrue();

    [TestCase("ServiceBusSender.Send")]
    [TestCase("ServiceBusProcessor.ProcessMessage")]
    [TestCase("ServiceBusSessionProcessor.ProcessSessionMessage")]
    [TestCase(null)]
    [TestCase("")]
    public void IsBackgroundPollingActivity_ReturnsFalse_ForOtherActivityNames(string? activityName)
        => CoreExServiceBusExtensions.IsBackgroundPollingActivity(activityName).Should().BeFalse();

    [Test]
    public void RenewMessageLockActivityName_MatchesAzureSdkDiagnosticPropertyValue()
        => CoreExServiceBusExtensions.RenewMessageLockActivityName.Should().Be("ServiceBusReceiver.RenewMessageLock");

    [Test]
    public void RenewSessionLockActivityName_MatchesAzureSdkDiagnosticPropertyValue()
        => CoreExServiceBusExtensions.RenewSessionLockActivityName.Should().Be("ServiceBusSessionReceiver.RenewSessionLock");

    [Test]
    public void ReceiveActivityName_MatchesAzureSdkDiagnosticPropertyValue()
        => CoreExServiceBusExtensions.ReceiveActivityName.Should().Be("ServiceBusReceiver.Receive");
}
