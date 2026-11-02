namespace CoreEx.Azure.Functions;

/// <summary>
/// Provides arguments for Functions Service Bus callback processing.
/// </summary>
public sealed class FunctionsServiceBusReceiveArgs
{
    /// <summary>
    /// Gets or sets whether to import bounded W3C transport baggage into the existing receive activity.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Enable only for trusted producers.</remarks>
    public bool PropagateBaggage { get; init; }
}
