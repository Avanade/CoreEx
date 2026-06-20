namespace CoreEx.Azure.Messaging.ServiceBus.Abstractions;

/// <summary>
/// Provides utility methods for classifying and interpreting a <see cref="ServiceBusException"/>.
/// </summary>
public static class ServiceBusErrorClassifier
{
    /// <summary>
    /// Classifies the specified Azure Service Bus error and logs an appropriate message based on its severity.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="args">The <see cref="ProcessErrorEventArgs"/> containing details about the error.</param>
    /// <returns><see langword="true"/> where the error was classified as a known scenario (e.g., lock lost, transient, idle connection closed); otherwise, <see langword="false"/>.</returns>
    public static bool ClassifyAndLogError(ILogger logger, ProcessErrorEventArgs args)
    {
        if (args.Exception is ServiceBusException sbex)
        {
            if (IsLockLost(sbex))
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation(sbex, "A lock lost scenario occurred on entity {EntityPath} with error source {ErrorSource}.", args.EntityPath, args.ErrorSource);

                return true;
            }
            else if (IsTransient(sbex))
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation(sbex, "A transient error occurred on entity {EntityPath} with error source {ErrorSource}.", args.EntityPath, args.ErrorSource);

                return true;
            }
            else if (IsIdleConnectionClosed(sbex))
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug(sbex, "An idle connection closed scenario occurred on entity {EntityPath} with error source {ErrorSource}.", args.EntityPath, args.ErrorSource);

                return true;
            }

            if (logger.IsEnabled(LogLevel.Error))
                logger.LogError(sbex, "An unclassified Service Bus error occurred on entity {EntityPath} with error source {ErrorSource} and reason {Reason}.", args.EntityPath, args.ErrorSource, sbex.Reason);

            return false;
        }

        if (logger.IsEnabled(LogLevel.Error))
            logger.LogError(args.Exception, "An unclassified Service Bus error occurred on entity {EntityPath} with error source {ErrorSource}", args.EntityPath, args.ErrorSource);

        return false;
    }


    /// <summary>
    /// Indicates whether the given <see cref="ServiceBusException"/> is a lock lost scenario, which typically occurs when a message lock expires before processing is completed, or when a session lock is lost.
    /// </summary>
    /// <param name="exception">The <see cref="ServiceBusException"/>.</param>
    /// <remarks>This can happen due to various reasons such as long processing times, network issues, or other transient conditions that cause the lock to be released by the Service Bus.</remarks>
    public static bool IsLockLost(ServiceBusException exception) => exception.Reason == ServiceBusFailureReason.MessageLockLost || exception.Reason == ServiceBusFailureReason.SessionLockLost;

    /// <summary>   
    /// Indicates whether the given <see cref="ServiceBusException"/> is considered transient, meaning it is likely to be resolved by retrying the operation after a delay.
    /// </summary>
    /// <param name="exception">The <see cref="ServiceBusException"/>.</param>
    /// <remarks>This typically includes exceptions that occur due to temporary issues such as network connectivity problems, service unavailability, or throttling by the Service Bus.</remarks>
    public static bool IsTransient(ServiceBusException exception)
    {
        // Best broad signal the SDK gives you.
        if (exception.IsTransient)
            return true;

        // Optional explicit belt-and-braces cases.
        return exception.Reason == ServiceBusFailureReason.ServiceTimeout
            || exception.Reason == ServiceBusFailureReason.ServiceBusy
            || exception.Reason == ServiceBusFailureReason.ServiceCommunicationProblem
            || IsIdleConnectionClosed(exception);
    }

    /// <summary>
    /// Indicates whether the given <see cref="ServiceBusException"/> is due to an idle connection being closed by the Service Bus, which can occur when a connection remains idle for an extended period and is automatically closed by the Service Bus to free up resources.
    /// </summary>
    /// <param name="exception">The <see cref="ServiceBusException"/>.</param>
    public static bool IsIdleConnectionClosed(ServiceBusException exception)
    {
        if (exception.Message is null)
            return false;

        // This is a bit of a hack as the SDK does not provide a specific reason for this scenario, but it is a known pattern that can be identified by the message content.
        return exception.Reason == ServiceBusFailureReason.GeneralError
            && exception.Message.Contains("did not have any active links", StringComparison.OrdinalIgnoreCase)
            && exception.Message.Contains("The connection was closed by container", StringComparison.OrdinalIgnoreCase);
    }
}
