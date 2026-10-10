namespace CoreEx.Events.Publishing;

/// <summary>
/// Provides an <see cref="IDestinationProvider"/> that publishes <see cref="MessageType.Event"/> messages to a single shared <see cref="Destination"/> (i.e. topic), and <see cref="MessageType.Command"/> messages to a destination (i.e. queue) specific to the target <see cref="EventData.DomainName"/>.
/// </summary>
/// <remarks>
/// <para>Events are fire-and-forget and intended for any interested subscriber, so share a single destination. A command is intended for a single consumer (the target domain), so is given its own destination.</para>
/// <para>The destination name is created as follows:</para>
/// <list type="bullet">
/// <item><description><see cref="MessageType.Event"/>: the <see cref="Destination"/>; the <see cref="EventData.DomainName"/> is ignored.</description></item>
/// <item><description><see cref="MessageType.Command"/>: the <see cref="Destination"/> followed by the <see cref="AppendSeparatorCharacter"/> and the <see cref="EventData.DomainName"/> (the <i>target</i> domain). Where the domain name is <see langword="null"/> or empty the command is
/// <i>self-addressed</i>; that is, the <see cref="IHostSettings.DomainName"/> of the current host is used (matching the <see cref="EventFormatter"/> title defaulting). An <see cref="InvalidOperationException"/> is thrown where there is no <see cref="IHostSettings"/> available to resolve it.</description></item>
/// <item><description>Any other <see cref="MessageType"/> (i.e. <see cref="MessageType.ReplyTo"/>) is not supported and will result in a <see cref="NotSupportedException"/>.</description></item>
/// </list>
/// <para>Where a dead-letter destination is requested, the <see cref="AppendSeparatorCharacter"/> and '<c>dead-letter</c>' are appended to the resulting name.</para>
/// <para>Where not explicitly specified, the <see cref="Destination"/> will default to the value of the '<c>CoreEx:Events:Destination</c>' configuration setting, or '<c>default</c>' as a fallback.</para>
/// </remarks>
public class NamedDestinationProvider : IDestinationProvider
{
    private string? _destination = null;

    /// <summary>
    /// Gets the default <see cref="NamedDestinationProvider"/>.
    /// </summary>
    /// <remarks>The <see cref="Destination"/> is resolved from configuration (see class remarks) on first use and then retained for the lifetime of the process.</remarks>
    public static NamedDestinationProvider Default { get; } = new NamedDestinationProvider();

    /// <summary>
    /// Gets the character used to separate the <see cref="Destination"/> from the <see cref="EventData.DomainName"/> (<see cref="MessageType.Command"/> messages only) and from the '<c>dead-letter</c>' suffix. Defaults to '<c>-</c>'.
    /// </summary>
    public char AppendSeparatorCharacter { get; init; } = '-';

    /// <summary>
    /// Gets the base destination name (i.e. topic).
    /// </summary>
    /// <remarks>Where not explicitly set (initialized), the '<c>CoreEx:Events:Destination</c>' configuration setting is used (or '<c>default</c>' where not configured). The configured value is resolved on first access and then retained.
    /// <para>Setting a <see langword="null"/> or empty value will result in an <see cref="ArgumentException"/>.</para></remarks>
    public string Destination
    {
        get => _destination ??= Internal.GetConfigurationValue<string?>("CoreEx:Events:Destination", "default")!;
        init => _destination = value.ThrowIfNullOrEmpty();
    }

    /// <inheritdoc/>
    public string CreateFrom(EventData @event, bool isDeadLetter = false) => CreateNew(@event.ThrowIfNull().MessageType, @event.DomainName, isDeadLetter);

    /// <inheritdoc/>
    public string CreateFrom(string destination, bool isDeadLetter = false) => isDeadLetter ? $"{destination}{AppendSeparatorCharacter}dead-letter" : destination;

    /// <inheritdoc/>
    public string CreateNew(MessageType messageType = MessageType.Event, string? domainName = null, bool isDeadLetter = false)
    {
        return messageType switch
        {
            MessageType.Event => CreateFrom(Destination, isDeadLetter),
            MessageType.Command => CreateFrom($"{Destination}{AppendSeparatorCharacter}{GetCommandDomainName(domainName)}", isDeadLetter),
            _ => throw new NotSupportedException($"The message type '{messageType}' is not supported.")
        };
    }

    /// <summary>
    /// Gets the command target domain name; defaults to the <see cref="IHostSettings.DomainName"/> (i.e. self-addressed) where not specified.
    /// </summary>
    private static string GetCommandDomainName(string? domainName)
    {
        if (!string.IsNullOrEmpty(domainName))
            return domainName;

        return ExecutionContext.GetService<IHostSettings>()?.DomainName
            ?? throw new InvalidOperationException($"A domain name is required for '{nameof(MessageType.Command)}' messages where the {nameof(IHostSettings)}.{nameof(IHostSettings.DomainName)} is not available to default the (self-addressed) target domain.");
    }
}
