using Azure.Messaging.ServiceBus;
using CoreEx.Azure.Messaging.ServiceBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit.Abstractions;

public class ServiceBusErrorClassifierTests
{
    [Test]
    public void IsSessionCannotBeLocked_True()
    {
        var ex = new ServiceBusException("Session cannot be locked.", ServiceBusFailureReason.SessionCannotBeLocked);
        ServiceBusErrorClassifier.IsSessionCannotBeLocked(ex).Should().BeTrue();
    }

    [Test]
    public void IsSessionCannotBeLocked_False_ForUnrelatedReason()
    {
        var ex = new ServiceBusException("Quota exceeded.", ServiceBusFailureReason.QuotaExceeded);
        ServiceBusErrorClassifier.IsSessionCannotBeLocked(ex).Should().BeFalse();
    }

    [Test]
    public void IsLockLost_NotConfused_WithSessionCannotBeLocked()
    {
        // Regression: SessionCannotBeLocked (another receiver already holds the lock) is a distinct scenario from
        // SessionLockLost/MessageLockLost (a lock this receiver held was lost) and must not be conflated.
        var ex = new ServiceBusException("Session cannot be locked.", ServiceBusFailureReason.SessionCannotBeLocked);
        ServiceBusErrorClassifier.IsLockLost(ex).Should().BeFalse();
        ServiceBusErrorClassifier.IsSessionCannotBeLocked(ex).Should().BeTrue();
    }

    [Test]
    public void ClassifyAndLogError_SessionCannotBeLocked_LogsAsInformation_NotError()
    {
        // Regression: a routine multi-receiver session-lock race must be logged at Information (benign), not escalate to the unclassified Error branch.
        var logger = new RecordingLogger();
        var ex = new ServiceBusException("Session cannot be locked.", ServiceBusFailureReason.SessionCannotBeLocked);
        var args = new ProcessErrorEventArgs(ex, ServiceBusErrorSource.Receive, "namespace", "entity-path", CancellationToken.None);

        var classified = ServiceBusErrorClassifier.ClassifyAndLogError(logger, args);

        classified.Should().BeTrue();
        logger.Logged.Should().ContainSingle(l => l.Level == LogLevel.Information);
        logger.Logged.Should().NotContain(l => l.Level == LogLevel.Error);
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Logged { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Logged.Add((logLevel, formatter(state, exception)));
    }
}
