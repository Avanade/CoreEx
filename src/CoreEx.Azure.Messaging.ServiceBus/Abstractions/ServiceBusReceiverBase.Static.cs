namespace CoreEx.Azure.Messaging.ServiceBus.Abstractions;

public abstract partial class ServiceBusReceiverBase
{
    /// <summary>
    /// Gets or sets the maximum text length used by <see cref="FormatText(string?, string?)"/>.
    /// </summary>
    /// <remarks>Default is '<c>512</c>'.</remarks>
    public static int MaxFormatTextLength { get; set; } = 512;

    /// <summary>
    /// Formats the <paramref name="text"/> for the likes of logging, etc., truncating to <see cref="MaxFormatTextLength"/> characters.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <param name="default">The default where the <paramref name="text"/> is <see langword="null"/>.</param>
    /// <returns>The formatted text.</returns>
    public static string? FormatText(string? text, string? @default = null) => text?[..Math.Min(text.Length, MaxFormatTextLength)] ?? @default;

    /// <summary>
    /// Provides a standardized/reusable means for actioning known exceptions in a consistent manner.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="actions">The <see cref="IServiceBusMessageActions"/> to perform message actions.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result"/> of the error handling operation.</returns>
    /// <remarks>Where the <paramref name="exception"/> has been actioned then the returned <see cref="Result"/> will be <see cref="Result.IsSuccess"/>; otherwise, <see cref="Result.IsFailure"/>.</remarks>
    public static async Task<Result> MessageErrorActionAsync(Exception exception, IServiceBusMessageActions actions, CancellationToken cancellationToken)
    {
        if (exception.ThrowIfNull() is IEventSubscriberException esex)
        {
            switch (esex.ErrorHandling)
            {
                case ErrorHandling.CompleteAsSilent:
                case ErrorHandling.CompleteAsInformation:
                case ErrorHandling.CompleteAsWarning:
                case ErrorHandling.CompleteAsError:
                    await actions.CompleteMessageAsync(cancellationToken).ConfigureAwait(false);
                    return Result.Success;

                case ErrorHandling.Retry:
                    await actions.AbandonMessageAsync(exception, cancellationToken).ConfigureAwait(false);
                    return Result.Success;

                case ErrorHandling.DeadLetter:
                    await actions.DeadLetterMessageAsync(exception, cancellationToken).ConfigureAwait(false);
                    return Result.Success;

                default:
                    return exception;
            }
        }
        else
            return exception;
    }

    /// <summary>
    /// Logs the conversion of an exception to a different handling as per the configured options; e.g. retry to dead-letter, etc.
    /// </summary>
    private static Exception LogConversion(ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(exception, "{ConversionMessage}", exception.Message);

        return exception;
    }

    /// <summary>
    /// Determine the final handling of an exhausted retry error based on the configured options.
    /// </summary>
    protected static Result MessageRetryErrorDetermination(Result result, ServiceBusReceiverOptionsBase options, ILogger logger)
    {
        static Result HandleAsPerConfiguredOptions(Exception exception, ServiceBusReceiverOptionsBase options, ILogger logger)
        {
            return options.RetryErrorHandling switch
            {
                ErrorHandling.DeadLetter => LogConversion(logger, new EventSubscriberDeadLetterException("Service bus receiver has converted Retry error to DeadLetter (as configured).", exception)),
                ErrorHandling.Catastrophic => LogConversion(logger, new EventSubscriberCatastrophicException("Service bus receiver has converted Retry error to Catastrophic (as configured).", exception)),
                _ => exception
            };
        }

        return result.OnFailure(r =>
        {
            if (r.Error is IEventSubscriberException esex)
                return esex.ErrorHandling == ErrorHandling.Retry ? HandleAsPerConfiguredOptions(r.Error, options, logger) : r.Error;
            else
                return HandleAsPerConfiguredOptions(r.Error, options, logger);
        });
    }

    /// <summary>
    /// Determine the final handling of an unhandled error based on the configured options.
    /// </summary>
    protected static Result MessageUnhandledErrorDetermination(Result result, ServiceBusReceiverOptionsBase options, ILogger logger)
    {
        static Result HandleAsPerConfiguredOptions(Exception exception, ServiceBusReceiverOptionsBase options, ILogger logger)
        {
            return options.UnhandledErrorHandling switch
            {
                ErrorHandling.Retry => LogConversion(logger, new EventSubscriberRetryException("Service bus receiver has converted Unhandled to Retry error (as configured).", exception)),
                ErrorHandling.DeadLetter => LogConversion(logger, new EventSubscriberDeadLetterException("Service bus receiver has converted Unhandled error to DeadLetter (as configured).", exception)),
                ErrorHandling.Catastrophic => LogConversion(logger, new EventSubscriberCatastrophicException("Service bus receiver has converted Unhandled error to Catastrophic (as configured).", exception)),
                _ => exception
            };
        }

        return result.OnFailure(r =>
        {
            if (r.Error is IEventSubscriberException esex)
                return esex.ErrorHandling == ErrorHandling.None ? HandleAsPerConfiguredOptions(r.Error, options, logger) : r.Error;
            else
                return HandleAsPerConfiguredOptions(r.Error, options, logger);
        });
    }
}