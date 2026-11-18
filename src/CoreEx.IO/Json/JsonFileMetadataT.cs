namespace CoreEx.IO.Json;

/// <summary>Maps a selected JSON document metadata object to its reusable wire type.</summary>
/// <typeparam name="TMetadata">The metadata wire type.</typeparam>
public sealed class JsonFileMetadata<TMetadata> : JsonFileMetadata where TMetadata : class, new()
{
    private readonly IJsonUnitMapping _mapping;
    private readonly IValidator<TMetadata>? _validator;

    /// <summary>Initializes typed mapping, validation, and requiredness for one metadata selector.</summary>
    /// <param name="name">The logical metadata accessor key.</param>
    /// <param name="recordType">The logical metadata record type.</param>
    /// <param name="path">The JSON Pointer selecting the metadata object.</param>
    /// <param name="mapping">The declared metadata field mapping.</param>
    /// <param name="validator">An optional pure metadata validator.</param>
    /// <param name="required">Whether the object must be present.</param>
    public JsonFileMetadata(string name, string recordType, string path, JsonUnitMapping<TMetadata> mapping,
        IValidator<TMetadata>? validator = null, bool required = false) : base(name, recordType, path, required)
        => (_mapping, _validator) = (mapping.ThrowIfNull(), validator);

    /// <inheritdoc/>
    internal override async Task<object?> MapAsync(JsonDocumentCursor cursor, JsonMappingContext context, bool validate, CancellationToken cancellationToken)
    {
        var value = (TMetadata?)await _mapping.DeserializeAsync(cursor, context, Path, cancellationToken).ConfigureAwait(false);
        if (value is null || cursor.Failed || cursor.InvalidValue || context.Stopped)
            return null;

        if (validate && !context.Invalid && !context.Stopped && _validator is not null)
        {
            var validation = await _validator.ValidateAsync(value, cancellationToken).ConfigureAwait(false);
            if (validation.Messages is not null)
                foreach (var message in validation.Messages)
                {
                    if (context.Stopped)
                        break;

                    await context.ReportValidationAsync(message.Text ?? LText.Empty, message.Type,
                        string.IsNullOrEmpty(message.Property) ? null : _mapping.ResolvePropertyPath(message.Property),
                        JsonPointer.Combine(Path, _mapping.ResolveSourcePath(message.Property ?? string.Empty)), cancellationToken).ConfigureAwait(false);
                }

            if (validation.HasErrors && !context.Invalid)
                throw new InvalidOperationException("The metadata validator reported errors without error messages.");
        }

        return value;
    }
}
