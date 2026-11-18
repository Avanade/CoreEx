namespace CoreEx.IO.Csv;

/// <summary>Adapts one typed positional mapping and optional pure validator to a discriminator route.</summary>
/// <typeparam name="TRecord">The wire-record type.</typeparam>
public sealed class CsvRecordRoute<TRecord> : CsvRecordRoute where TRecord : class, new()
{
    private readonly Func<CsvFileMapping<TRecord>> _mappingFactory;
    private readonly IValidator<TRecord>? _validator;

    /// <summary>Initializes a typed route; mappings must be positional because routed records have different shapes.</summary>
    /// <param name="key">The logical key.</param>
    /// <param name="match">The exact discriminator.</param>
    /// <param name="mappingFactory">Creates a fresh positional mapping per pass.</param>
    /// <param name="validator">Optional pure field validation; no dependency-backed business services.</param>
    public CsvRecordRoute(string key, string match, Func<CsvFileMapping<TRecord>> mappingFactory, IValidator<TRecord>? validator = null) : base(key, match)
        => (_mappingFactory, _validator) = (mappingFactory.ThrowIfNull(), validator);

    /// <inheritdoc/>
    internal override Func<CsvReader, (object? Value, IReadOnlyList<FileDiagnostic> Diagnostics)> CreateMapping()
    {
        var definition = _mappingFactory().ThrowIfNull();
        if (definition.RequiresHeader)
            throw new InvalidOperationException("Discriminator routes require positional mappings.");

        var mapping = definition.CreateMapping();
        return reader =>
        {
            var result = mapping(reader);
            return (result.Value, result.Diagnostics);
        };
    }

    /// <inheritdoc/>
    internal override async Task ValidateAsync(object record, long line, Func<FileDiagnostic, CancellationToken, Task> reportAsync, CancellationToken cancellationToken)
    {
        if (_validator is null)
            return;

        var validation = await _validator.ValidateAsync((TRecord)record, cancellationToken).ConfigureAwait(false);
        var hasErrors = false;
        if (validation.Messages is not null)
        {
            foreach (var message in validation.Messages)
            {
                hasErrors |= message.Type == MessageType.Error;
                await reportAsync(new(FileDiagnosticCategory.Validation, "csv.validation", message.Text ?? LText.Empty, message.Type, new(line, propertyPath: message.Property), Key), cancellationToken).ConfigureAwait(false);
            }
        }

        if (validation.HasErrors && !hasErrors)
            throw new InvalidOperationException("The validator reported errors without error messages.");
    }
}
