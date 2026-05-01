namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides standard extensions for Azure Service Bus.
/// </summary>
public static partial class ServiceBusExtensions
{
    private const string MessageKey = "CoreEx:ServiceBusMessage";

    extension(EventSubscriberArgs args)
    {
        /// <summary>
        /// Gets or sets the originating <see cref="ServiceBusReceivedMessage"/>.
        /// </summary>
        public ServiceBusReceivedMessage Message
        {
            get => args.Properties.TryGetValue(MessageKey, out var value) && value is ServiceBusReceivedMessage sbm ? sbm : throw new InvalidOperationException($"The {MessageKey} property has not been set.");
            internal set => args.Properties[MessageKey] = value;
        }
    }
}