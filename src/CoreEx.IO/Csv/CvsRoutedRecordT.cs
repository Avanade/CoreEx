namespace CoreEx.IO.Csv;

/// <summary>Maps and validates a concrete wire record, projecting it into the common unit type without runtime casts.</summary>
/// <typeparam name="TRecord">The concrete wire-record type.</typeparam>
/// <typeparam name="TUnit">The common business-unit type.</typeparam>
public sealed class CsvRoutedRecord<TRecord, TUnit> : CsvRoutedRecord<TUnit> where TRecord : class, new() where TUnit : class
{
    private readonly Func<CsvFileMapping<TRecord>> _mappingFactory;
    private readonly Func<TRecord, TUnit> _project;
    private readonly IValidator<TRecord>? _validator;

    /// <summary>Initializes one typed route; mapping and projection must not perform business mutations.</summary>
    /// <param name="key">The logical key.</param>
    /// <param name="match">The exact discriminator.</param>
    /// <param name="mappingFactory">Creates a fresh positional mapping per pass.</param>
    /// <param name="project">Pure projection into the shared unit type, commonly a base/interface conversion or typed wrapper.</param>
    /// <param name="validator">Optional wire-record validation; skipped on replay.</param>
    /// <param name="cardinality">The file-level recognized occurrences.</param>
    /// <param name="position">The optional First or Last boundary restriction.</param>
    public CsvRoutedRecord(string key, string match, Func<CsvFileMapping<TRecord>> mappingFactory, Func<TRecord, TUnit> project, IValidator<TRecord>? validator = null,
        FileCardinality cardinality = FileCardinality.Many, FileRecordPosition position = FileRecordPosition.Any) : base(key, match, cardinality, position)
        => (_mappingFactory, _project, _validator) = (mappingFactory.ThrowIfNull(), project.ThrowIfNull(), validator);

    /// <inheritdoc/>
    internal override Func<CsvReader, CsvRoutedInput<TUnit>> CreateMapping()
    {
        var definition = _mappingFactory().ThrowIfNull();
        if (definition.RequiresHeader)
            throw new InvalidOperationException("Discriminator routes require positional mappings.");

        var mapping = definition.CreateMapping();
        return reader =>
        {
            var record = mapping(reader);
            return record.Value is { } value
                ? new(_project(value).ThrowIfNull(), record.Diagnostics, (line, report, ct) => ValidateAsync(value, line, report, ct))
                : new(null, record.Diagnostics, (_, _, _) => Task.CompletedTask);
        };
    }

    /// <summary>Emits the concrete record validator's messages without object casts or per-content exceptions.</summary>
    private async Task ValidateAsync(TRecord record, long line, Func<FileDiagnostic, CancellationToken, Task> reportAsync, CancellationToken cancellationToken)
    {
        if (_validator is null)
            return;

        var validation = await _validator.ValidateAsync(record, cancellationToken).ConfigureAwait(false);
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
