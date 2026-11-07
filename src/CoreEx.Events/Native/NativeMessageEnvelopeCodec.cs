namespace CoreEx.Events.Native;

/// <summary>
/// Defines the CoreEx native-representation marker carried by an internal CloudEvent.
/// </summary>
public static class NativeMessageEnvelopeCodec
{
    private static readonly string[] DefaultMappedAttributeNames = ["tenantid", "dataschemaversion", "authtype", "authid", "replyto", "partitionkey", "correlationid", "causationid", "traceparent", "tracestate", "baggage"];

    /// <summary>
    /// Gets the extension definition used to preserve the native marker's Boolean type.
    /// </summary>
    internal static CloudEventAttribute NativeCloudEventAttribute { get; } = CloudEventAttribute.CreateExtension(NativeAttributeName, CloudEventAttributeType.Boolean);

    /// <summary>
    /// Gets the CloudEvent instance used for CoreEx-formatted envelopes.
    /// </summary>
    internal static CloudEvent CreateCloudEvent() => new([NativeCloudEventAttribute]);

    /// <summary>
    /// Gets the extension definitions required when decoding CoreEx-formatted envelopes.
    /// </summary>
    internal static IReadOnlyList<CloudEventAttribute> ExtensionAttributes { get; } = [NativeCloudEventAttribute];

    /// <summary>
    /// Gets the fixed native-representation extension name.
    /// </summary>
    public const string NativeAttributeName = "coreexisdatanative";

    /// <summary>
    /// Returns whether a name is reserved for CoreEx-owned extensions.
    /// </summary>
    internal static bool IsReserved(string name) => name.StartsWith("coreex", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets and validates the message representation.
    /// </summary>
    public static MessageRepresentation GetRepresentation(CloudEvent cloudEvent)
    {
        cloudEvent.ThrowIfNull();
        foreach (var attribute in cloudEvent.GetPopulatedAttributes())
        {
            if (IsReserved(attribute.Key.Name) && attribute.Key.Name != NativeAttributeName)
                throw new InvalidOperationException($"The '{attribute.Key.Name}' extension is reserved for CoreEx protocol use.");
        }

        return cloudEvent[NativeAttributeName] switch
        {
            null or false => MessageRepresentation.CloudEvent,
            true => MessageRepresentation.Native,
            _ => throw new InvalidOperationException($"The '{NativeAttributeName}' extension must be a Boolean.")
        };
    }

    /// <summary>
    /// Reads eligible caller-defined extensions as canonical string-valued native headers.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ReadAttributes(CloudEvent cloudEvent) => ReadAttributes(cloudEvent, null);

    /// <summary>
    /// Reads eligible caller-defined extensions as canonical string-valued native headers, excluding formatter-mapped attributes.
    /// </summary>
    /// <param name="cloudEvent">The native <see cref="CloudEvent"/>.</param>
    /// <param name="isMappedAttributeName">The formatter's extension-name classification function.</param>
    /// <returns>The eligible native header values.</returns>
    internal static IReadOnlyDictionary<string, string> ReadAttributes(CloudEvent cloudEvent, Func<string, bool>? isMappedAttributeName)
    {
        cloudEvent.ThrowIfNull();
        if (GetRepresentation(cloudEvent) != MessageRepresentation.Native)
            throw new InvalidOperationException("Native attributes require native representation.");

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var attribute in cloudEvent.GetPopulatedAttributes())
        {
            var name = attribute.Key.Name;
            if (!attribute.Key.IsExtension || IsReserved(name) || IsFrameworkAttribute(name, isMappedAttributeName) || name.StartsWith('_') || name.StartsWith("ce_", StringComparison.OrdinalIgnoreCase))
                continue;

            if (cloudEvent[name] is object value)
                values.Add(name, attribute.Key.Type.Format(value));
        }

        return values;
    }

    /// <summary>
    /// Writes the native-representation marker.
    /// </summary>
    internal static void Write(EventData source, CloudEvent destination)
    {
        if (!Enum.IsDefined(source.DataRepresentation))
            throw new NotSupportedException("The message representation is not supported.");

        if (source.Attributes.Keys.Any(IsReserved))
            throw new InvalidOperationException("CoreEx-owned extension names cannot be supplied through Attributes.");

        if (source.DataRepresentation == MessageRepresentation.Native)
            destination[NativeAttributeName] = true;
    }

    /// <summary>
    /// Restores the native-representation instruction.
    /// </summary>
    internal static void Restore(CloudEvent source, EventData destination)
    {
        if (GetRepresentation(source) == MessageRepresentation.Native)
            destination.DataRepresentation = MessageRepresentation.Native;
    }

    /// <summary>
    /// Determines whether an extension is owned by CloudEvents or mapped by CoreEx.
    /// </summary>
    private static bool IsFrameworkAttribute(string name, Func<string, bool>? isMappedAttributeName)
    {
        if (CloudEventsSpecVersion.Default.AllAttributes.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return true;

        return DefaultMappedAttributeNames.Contains(name, StringComparer.OrdinalIgnoreCase)
            || (isMappedAttributeName?.Invoke(name) ?? false);
    }
}
