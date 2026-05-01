namespace CoreEx.Events.Publishing;

/// <summary>
/// Provides a <see cref="IDestinationProvider"/> where the same (fixed) <see cref="Destination"/> is used regardless of <see cref="EventData"/> contents; i.e. all messages are published to a single centralized destination.
/// </summary>
/// <remarks>The <see cref="Destination"/> will default to the value of the '<c>CoreEx.Events:Destination</c>' configuration setting or '<c>default</c>' as a fallback.</remarks>
public class FixedDestinationProvider : IDestinationProvider
{
    private string? _destination = null;

    /// <summary>
    /// Gets the default <see cref="FixedDestinationProvider"/>.
    /// </summary>
    public static FixedDestinationProvider Default { get; } = new FixedDestinationProvider();

    /// <summary>
    /// Gets or sets the fixed destination name.
    /// </summary>
    public string Destination
    {
        get => _destination ??= Internal.GetConfigurationValue<string?>("CoreEx:Events:Destination", "default")!;
        init => _destination = value.ThrowIfNullOrEmpty();
    }

    /// <inheritdoc/>
    public string CreateFrom(EventData @event, bool isDeadLetter = false) => Destination;

    /// <inheritdoc/>
    public string CreateFrom(string destination, bool isDeadLetter = false) => Destination;

    /// <inheritdoc/>
    public string CreateNew(MessageType messageType = MessageType.Event, string? domainName = null, bool isDeadLetter = false) => Destination;
}