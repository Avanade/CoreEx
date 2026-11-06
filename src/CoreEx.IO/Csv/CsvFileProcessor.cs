namespace CoreEx.IO.Csv;

/// <summary>Validates flat CSV through EOF before optionally replaying fresh records into scoped business handlers.</summary>
/// <typeparam name="TRecord">The typed wire record and flat business unit.</typeparam>
public class CsvFileProcessor<TRecord> : CsvFileProcessorBase<TRecord> where TRecord : class, new()
{
    private readonly Func<CsvFileMapping<TRecord>> _mappingFactory;

    /// <summary>Initializes a flat CSV processor with explicit pure validation and optional scoped handlers.</summary>
    /// <param name="mappingFactory">Creates a fresh mapping for each pass.</param>
    /// <param name="options">CSV syntax and explicit encoding.</param>
    /// <param name="validator">Optional pure record validation.</param>
    /// <param name="serviceProvider">The provider used for independent unit scopes.</param>
    /// <param name="handlerFactory">Resolves a scoped handler; required for execution.</param>
    public CsvFileProcessor(Func<CsvFileMapping<TRecord>> mappingFactory, CsvFileOptions? options = null, IValidator<TRecord>? validator = null,
        IServiceProvider? serviceProvider = null, Func<IServiceProvider, FileUnitHandler<TRecord>>? handlerFactory = null) : base(options, validator, serviceProvider, handlerFactory)
        => _mappingFactory = mappingFactory.ThrowIfNull();

    /// <inheritdoc/>
    private protected override async Task<bool> ReadPassAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, ProcessingState state, bool execute, CancellationToken cancellationToken)
    {
        var definition = _mappingFactory().ThrowIfNull();
        var mapping = definition.CreateMapping();
        var headerSeen = false;
        await foreach (var parsed in ReadRecordsAsync(openReadAsync, state, execute, cancellationToken).ConfigureAwait(false))
        {
            if (parsed.IsTerminal)
            {
                if (execute)
                    throw new InvalidDataException("Replay violated the accepted input bounds or encoding.");

                await state.ReportAsync(parsed.Diagnostic.ThrowIfNull(), cancellationToken).ConfigureAwait(false);
                return false;
            }

            var record = parsed.Diagnostic is null ? mapping(parsed.Reader) : new CsvInputRecord<TRecord>(null, definition.RequiresHeader && !headerSeen, [parsed.Diagnostic]);
            if (record.IsHeader)
            {
                headerSeen = true;
                if (record.Diagnostics.Count > 0)
                {
                    if (execute)
                        throw new InvalidDataException("An accepted CSV header could not be reconstructed on replay.");

                    foreach (var diagnostic in record.Diagnostics)
                        await state.ReportAsync(diagnostic, cancellationToken).ConfigureAwait(false);

                    return false;
                }

                continue;
            }

            if (!execute)
            {
                state.Records = checked(state.Records + 1);
                state.Update();
            }

            if (record.Value is null)
            {
                if (execute)
                    throw new InvalidDataException("An accepted CSV record could not be reconstructed on replay.");

                state.Invalid = checked(state.Invalid + 1);
                foreach (var diagnostic in record.Diagnostics)
                    await state.ReportAsync(diagnostic, cancellationToken).ConfigureAwait(false);

                if (context.ErrorMode == FileErrorMode.FailFast)
                    return false;

                continue;
            }

            if (execute)
            {
                if (!await ExecuteUnitAsync(record.Value, context, state, cancellationToken).ConfigureAwait(false))
                    return false;
            }
            else if (!await ValidateUnitAsync(record.Value, context, state, new(parsed.Reader.LineNumber), true, null, cancellationToken).ConfigureAwait(false)
                && context.ErrorMode == FileErrorMode.FailFast)
                return false;
        }

        if (definition.RequiresHeader && !headerSeen)
        {
            if (execute)
                throw new InvalidDataException("The accepted header was absent on replay.");

            await state.ReportAsync(new(FileDiagnosticCategory.Structure, "csv.header.missing", "A column-name header is required.", location: new(1)), cancellationToken).ConfigureAwait(false);
        }

        if (execute && state.Attempted != state.Validated)
            throw new InvalidDataException("Replay contained fewer units than the accepted input.");

        return true;
    }
}
