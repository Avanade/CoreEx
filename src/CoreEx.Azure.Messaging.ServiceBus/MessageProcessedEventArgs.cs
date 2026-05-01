namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides <see cref="ServiceBusReceiverBase.MessageProcessed"/> event data.
/// </summary>
/// <param name="result">The <see cref="Result"/>.</param>
public class MessageProcessedEventArgs(Result result) : EventArgs()
{
    /// <summary>
    /// Gets the <see cref="Result"/>.
    /// </summary>
    public Result Result { get; } = result;
}