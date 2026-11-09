namespace CoreEx.Azure.Functions;

/// <summary>
/// Provides arguments for Functions Service Bus callback processing.
/// </summary>
public sealed class FunctionsServiceBusReceiveArgs
{
    /// <summary>Gets the optional logger for raw callback correlation warnings and scopes.</summary>
    /// <remarks>Defaults to the execution context's ILoggerFactory. Invalid metadata requires a logger so warnings are not silently discarded.</remarks>
    public Microsoft.Extensions.Logging.ILogger? Logger { get; init; }

    /// <summary>Indicates that the delegated subscriber owns correlation capture and scope lifetime.</summary>
    internal bool SubscriberOwnsCorrelation { get; init; }

    /// <summary>
    /// Gets or sets whether to import bounded W3C transport baggage into the existing receive activity.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Enable only for trusted producers.</remarks>
    public bool PropagateBaggage { get; init; }
}
