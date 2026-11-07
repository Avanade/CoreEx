namespace CoreEx.Events.Native;

/// <summary>
/// Defines the durable CoreEx native-message protocol carried by an internal CloudEvent.
/// </summary>
public static class NativeMessageEnvelopeCodec
{
    /// <summary>
    /// Gets the fixed representation extension name.
    /// </summary>
    public const string RepresentationAttributeName = "datarepresentation";

    /// <summary>
    /// Gets the fixed versioned native metadata extension name.
    /// </summary>
    public const string MetadataAttributeName = "coreexnative";

    /// <summary>
    /// Returns whether a name is owned by the internal publishing protocol.
    /// </summary>
    public static bool IsReserved(string name) => string.Equals(name, RepresentationAttributeName, StringComparison.OrdinalIgnoreCase) || string.Equals(name, MetadataAttributeName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets and validates the requested representation.
    /// </summary>
    public static MessageRepresentation GetRepresentation(CloudEvent cloudEvent)
    {
        cloudEvent.ThrowIfNull();
        foreach (var attribute in cloudEvent.GetPopulatedAttributes())
        {
            if (IsReserved(attribute.Key.Name) && attribute.Key.Name != RepresentationAttributeName && attribute.Key.Name != MetadataAttributeName)
                throw new InvalidOperationException("CoreEx control attribute names must use their canonical lowercase spelling.");
        }

        return cloudEvent[RepresentationAttributeName] switch
        {
            null or "cloudevent" => cloudEvent[MetadataAttributeName] is null ? MessageRepresentation.CloudEvent : throw new InvalidOperationException("Native metadata requires native representation."),
            "native" => MessageRepresentation.Native,
            _ => throw new NotSupportedException("The message datarepresentation is not supported.")
        };
    }

    /// <summary>
    /// Validates and reads the resolved native subject, returning null when omitted or for ordinary CloudEvents.
    /// </summary>
    public static string? ReadSubject(CloudEvent cloudEvent) => Read(cloudEvent)?.Subject;

    /// <summary>
    /// Gets the durable reply destination independently of formatter attribute aliases.
    /// </summary>
    public static string? ReadReplyTo(CloudEvent cloudEvent) => Read(cloudEvent)?.ReplyTo;

    /// <summary>
    /// Determines whether the internal payload must be encoded as opaque bytes.
    /// </summary>
    public static bool IsOpaque(CloudEvent cloudEvent) => Read(cloudEvent)?.Opaque == true;

    /// <summary>
    /// Reads caller-declared publishable attributes as canonical string headers, excluding framework metadata.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ReadAttributes(CloudEvent cloudEvent)
    {
        var metadata = Read(cloudEvent) ?? throw new InvalidOperationException("Native attributes require native representation.");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in metadata.Attributes)
        {
            ValidateAttributeName(name);
            var attribute = cloudEvent.GetAttribute(name);
            if (attribute is null || !attribute.IsExtension || cloudEvent[name] is not object value)
                throw new InvalidOperationException($"Native attribute '{name}' is missing or is not an extension.");

            values.Add(name, attribute.Type.Format(value));
        }

        return values;
    }

    /// <summary>
    /// Writes a validated snapshot of native metadata.
    /// </summary>
    internal static void Write(EventData source, CloudEvent destination, bool includeSubject = true)
    {
        if (!Enum.IsDefined(source.DataRepresentation))
            throw new NotSupportedException("The message representation is not supported.");

        if (source.Attributes.Keys.Any(IsReserved))
            throw new InvalidOperationException("CoreEx native control attributes cannot be supplied through Attributes.");

        if (source.DataRepresentation != MessageRepresentation.Native)
            return;

        if (!Enum.IsDefined(source.MessageType))
            throw new NotSupportedException("The native message intent is not supported.");

        var attributes = source.Attributes.Where(x => !x.Key.StartsWith('_') && x.Value is not null).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        foreach (var name in attributes)
            ValidateAttributeName(name);

        destination[RepresentationAttributeName] = "native";
        destination[MetadataAttributeName] = JsonSerializer.Serialize(new
        {
            version = 1,
            intent = (int)source.MessageType,
            opaque = source.IsDataOpaque,
            subject = source.NativeSubjectIsResolved ? source.NativeSubject : includeSubject ? source.Title : null,
            replyTo = source.ReplyTo,
            attributes
        });
    }

    /// <summary>
    /// Restores native metadata without exposing control attributes as business attributes.
    /// </summary>
    internal static void Restore(CloudEvent source, EventData destination)
    {
        var metadata = Read(source);
        if (metadata is null)
            return;

        destination.DataRepresentation = MessageRepresentation.Native;
        destination.MessageType = metadata.Intent;
        destination.IsDataOpaque = metadata.Opaque;
        destination.NativeSubject = metadata.Subject;
        destination.NativeSubjectIsResolved = true;
        destination.ReplyTo = metadata.ReplyTo;
    }

    /// <summary>
    /// Reads the versioned metadata using strict field and attribute-name validation.
    /// </summary>
    private static Metadata? Read(CloudEvent cloudEvent)
    {
        if (GetRepresentation(cloudEvent) != MessageRepresentation.Native)
            return null;

        if (cloudEvent[MetadataAttributeName] is not string json)
            throw new InvalidOperationException("Native representation requires versioned coreexnative metadata.");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.GetProperty("version").GetInt32() != 1)
            throw new NotSupportedException("The native metadata version is not supported.");

        var intent = (MessageType)root.GetProperty("intent").GetInt32();
        if (!Enum.IsDefined(intent))
            throw new NotSupportedException("The native message intent is not supported.");

        var subject = root.GetProperty("subject").GetString();

        var attributes = root.GetProperty("attributes").EnumerateArray().Select(x => x.GetString() ?? throw new InvalidOperationException("Native attribute names cannot be null.")).ToArray();
        foreach (var name in attributes)
            ValidateAttributeName(name);

        return new Metadata(intent, root.GetProperty("opaque").GetBoolean(), root.GetProperty("replyTo").GetString(), subject, attributes);
    }

    /// <summary>
    /// Validates caller attribute names against framework control and tracing fields.
    /// </summary>
    private static void ValidateAttributeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.StartsWith('_') || IsReserved(name) || name.StartsWith("ce_", StringComparison.OrdinalIgnoreCase)
            || CloudEventsSpecVersion.Default.AllAttributes.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            || new[] { "tenantid", "authtype", "authid", "replyto", "partitionkey", "dataschemaversion", "correlationid", "causationid" }.Contains(name, StringComparer.OrdinalIgnoreCase)
            || name.Equals("traceparent", StringComparison.OrdinalIgnoreCase) || name.Equals("tracestate", StringComparison.OrdinalIgnoreCase)
            || name.Equals("baggage", StringComparison.OrdinalIgnoreCase) || name.Equals("Diagnostic-Id", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Native attribute name '{name}' is empty or reserved.");
    }

    /// <summary>
    /// Represents validated internal metadata.
    /// </summary>
    /// <param name="Intent">The semantic message intent.</param>
    /// <param name="Opaque">Whether the payload is byte-exact.</param>
    /// <param name="ReplyTo">The reply destination.</param>
    /// <param name="Subject">The resolved native subject, or null when omitted.</param>
    /// <param name="Attributes">The caller-declared extension names to publish as native headers.</param>
    private sealed record Metadata(MessageType Intent, bool Opaque, string? ReplyTo, string? Subject, string[] Attributes);
}
