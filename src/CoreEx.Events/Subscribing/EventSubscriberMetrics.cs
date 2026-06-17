namespace CoreEx.Events.Subscribing;

/// <summary>
/// Defines the <see cref="EventSubscriberBase"/> metrics.
/// </summary>
public class EventSubscriberMetrics
{
    private const string ErrorUnhandledOutcome = "error-unhandled";

    /// <summary>
    /// Gets the <see cref="Meter"/> used for recording metrics related to event subscriber operations.
    /// </summary>
    public static Meter Meter { get; } = new("CoreEx.Events.Subscribing");

    /// <summary>
    /// Gets the counter that tracks the number of messages received for processing.
    /// </summary>
    public static Counter<long> MessagesReceived { get; } = Meter.CreateCounter<long>("messages.received", unit: "{message}", description: "Number of messages received for processing.");

    /// <summary>
    /// Wraps a message receive operation with metrics recording.
    /// </summary>
    /// <param name="args">The <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="receiveFunc">The function to execute the receive operation.</param>
    /// <returns>The <see cref="Result"/> of the receive operation.</returns>
    /// <remarks>This should be used to add standardized metrics recording to a receive operation.</remarks>
    public static async Task<Result> ReceiveMessageAsync(EventSubscriberArgs args, Func<Task<Result>> receiveFunc)
    {
        try
        {
            var result = await receiveFunc().ConfigureAwait(false);

            string outcome;
            if (result.IsSuccess)
                outcome = "success";
            else if (args.UsesSubscribedManager && args.Subscriber is null)
                outcome = "not-subscribed";
            else
            {
                if (result.Error is EventSubscriberHandledException rex)
                {
                    outcome = rex.ErrorHandling switch
                    {
                        ErrorHandling.None => ErrorUnhandledOutcome,
                        ErrorHandling.CompleteAsSilent => "error-complete-silent",
                        ErrorHandling.CompleteAsInformation => "error-complete-info",
                        ErrorHandling.CompleteAsWarning => "error-complete-warning",
                        ErrorHandling.CompleteAsError => "error-complete-error",
                        ErrorHandling.Retry => "error-retry",
                        ErrorHandling.DeadLetter => "error-dead-letter",
                        ErrorHandling.Catastrophic => "error-catastrophic",
                        _ => "error-completed"
                    };
                }
                else
                    outcome = "error-unhandled";
            }

            EventSubscriberMetrics.MessagesReceived.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
            Activity.Current?.AddTag("messaging.outcome", outcome);
            return result;
        }
        catch (Exception)
        {
            EventSubscriberMetrics.MessagesReceived.Add(1, new KeyValuePair<string, object?>("outcome", ErrorUnhandledOutcome));
            Activity.Current?.AddTag("messaging.outcome", ErrorUnhandledOutcome);
            throw;
        }
    }
}