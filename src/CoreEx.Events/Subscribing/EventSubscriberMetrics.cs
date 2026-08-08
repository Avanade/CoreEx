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
            else if (result.Error is IEventSubscriberException iex)
            {
                outcome = iex.ErrorHandling switch
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
                outcome = ErrorUnhandledOutcome;

            RecordOutcome(args, outcome);
            return result;
        }
        catch (Exception)
        {
            RecordOutcome(args, ErrorUnhandledOutcome);
            throw;
        }
    }

    /// <summary>
    /// Records the resulting <paramref name="outcome"/> against the <see cref="MessagesReceived"/> counter and the current <see cref="Activity"/>.
    /// </summary>
    /// <remarks>Where the <see cref="EventSubscriberArgs.UsesSubscribedManager"/> is <see langword="true"/> an additional <c>subscribed</c> tag is included to distinguish, orthogonally to <paramref name="outcome"/>,
    /// whether a single subscriber was matched (<see cref="EventSubscriberArgs.Subscriber"/> is not <see langword="null"/>) versus not (i.e. no match, an ambiguous match, or an instantiation failure) - this
    /// disambiguates, for example, "nobody subscribed" from "a subscriber ran and chose to complete silently" which would otherwise both report the same <c>error-complete-silent</c> outcome.</remarks>
    private static void RecordOutcome(EventSubscriberArgs args, string outcome)
    {
        if (args.UsesSubscribedManager)
        {
            var subscribed = args.Subscriber is not null;
            MessagesReceived.Add(1, new KeyValuePair<string, object?>("outcome", outcome), new KeyValuePair<string, object?>("subscribed", subscribed));
            Activity.Current?.AddTag("messaging.outcome", outcome);
            Activity.Current?.AddTag("messaging.subscribed", subscribed);
        }
        else
        {
            MessagesReceived.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
            Activity.Current?.AddTag("messaging.outcome", outcome);
        }
    }
}