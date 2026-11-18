namespace CoreEx.IO.JsonLines;

/// <summary>Maps a metadata route to its independently reusable wire type.</summary>
/// <typeparam name="TMetadata">The metadata wire type.</typeparam>
public sealed class JsonLinesMetadata<TMetadata> : JsonLinesMetadata where TMetadata : class, new()
{
    private readonly CoreEx.IO.Json.IJsonUnitMapping _mapping;
    private readonly IValidator<TMetadata>? _validator;

    /// <summary>Initializes typed mapping, validation, and occurrence restrictions.</summary>
    /// <param name="name">The logical metadata accessor key.</param>
    /// <param name="recordType">The logical metadata record type.</param>
    /// <param name="match">The exact discriminator value selecting this route.</param>
    /// <param name="mapping">The declared metadata field mapping.</param>
    /// <param name="validator">An optional pure metadata validator.</param>
    /// <param name="required">Whether exactly one occurrence is required.</param>
    /// <param name="position">The source-order restriction for this route.</param>
    public JsonLinesMetadata(string name, string recordType, string match, CoreEx.IO.Json.JsonUnitMapping<TMetadata> mapping,
        IValidator<TMetadata>? validator = null, bool required = false, FileRecordPosition position = FileRecordPosition.Any) : base(name, recordType, match, required, position)
        => (_mapping, _validator) = (mapping.ThrowIfNull(), validator);

    /// <inheritdoc/>
    internal override async Task<object?> MapAsync(JsonElement element, CoreEx.IO.Json.JsonMappingContext context, string? discriminator, bool validate, CancellationToken cancellationToken)
    {
        var value = (TMetadata?)await _mapping.MapAsync(element, context, string.Empty, string.Empty, discriminator, cancellationToken).ConfigureAwait(false);
        if (value is null)
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
                        string.IsNullOrEmpty(message.Property) ? null : _mapping.ResolvePropertyPath(message.Property), _mapping.ResolveSourcePath(message.Property ?? string.Empty), cancellationToken).ConfigureAwait(false);
                }

            if (validation.HasErrors && !context.Invalid)
                throw new InvalidOperationException("The metadata validator reported errors without error messages.");
        }

        return value;
    }
}
