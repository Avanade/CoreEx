namespace CoreEx.IO.Csv;

/// <summary>Defines an exact unordered route and its file-level occurrence, position, and error policy.</summary>
/// <typeparam name="TUnit">The common business-unit type produced by typed route projections.</typeparam>
public abstract class CsvRoutedRecord<TUnit> where TUnit : class
{
    /// <summary>Initializes immutable route metadata.</summary>
    private protected CsvRoutedRecord(string key, string match, CsvRecordCardinality cardinality, CsvRecordPosition position, FileErrorMode? errorMode)
    {
        Key = key.ThrowIfNullOrWhiteSpace();
        Match = match.ThrowIfNullOrWhiteSpace();
        if (!Enum.IsDefined(cardinality))
            throw new ArgumentOutOfRangeException(nameof(cardinality));

        if (!Enum.IsDefined(position))
            throw new ArgumentOutOfRangeException(nameof(position));

        if (errorMode.HasValue && !Enum.IsDefined(errorMode.Value))
            throw new ArgumentOutOfRangeException(nameof(errorMode));

        if (position != CsvRecordPosition.Any && cardinality is not (CsvRecordCardinality.One or CsvRecordCardinality.Optional))
            throw new ArgumentException("First and Last positions require One or Optional cardinality.", nameof(cardinality));

        Cardinality = cardinality;
        Position = position;
        ErrorMode = errorMode;
    }

    /// <summary>Gets the logical key, unique ignoring case within a definition.</summary>
    public string Key { get; }

    /// <summary>Gets the exact case-sensitive discriminator, without trimming.</summary>
    public string Match { get; }

    /// <summary>Gets the recognized occurrences allowed per file.</summary>
    public CsvRecordCardinality Cardinality { get; }

    /// <summary>Gets the optional file-boundary restriction.</summary>
    public CsvRecordPosition Position { get; }

    /// <summary>Gets the override for content failures and structural rules owned by this route.</summary>
    public FileErrorMode? ErrorMode { get; }

    /// <summary>Creates a fresh positional mapper and typed validation dispatch for one pass.</summary>
    internal abstract Func<CsvReader, CsvRoutedInput<TUnit>> CreateMapping();
}

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
    /// <param name="errorMode">The optional continuation override.</param>
    public CsvRoutedRecord(string key, string match, Func<CsvFileMapping<TRecord>> mappingFactory, Func<TRecord, TUnit> project, IValidator<TRecord>? validator = null,
        CsvRecordCardinality cardinality = CsvRecordCardinality.Many, CsvRecordPosition position = CsvRecordPosition.Any, FileErrorMode? errorMode = null) : base(key, match, cardinality, position, errorMode)
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
                await reportAsync(new(FileDiagnosticCategory.Validation, "csv.validation", message.Text ?? LText.Empty, message.Type,
                    new(line, propertyPath: message.Property), Key), cancellationToken).ConfigureAwait(false);
            }
        }

        if (validation.HasErrors && !hasErrors)
            throw new InvalidOperationException("The validator reported errors without error messages.");
    }
}
