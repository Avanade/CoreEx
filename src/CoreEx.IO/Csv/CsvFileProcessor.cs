namespace CoreEx.IO.Csv;

/// <summary>Validates flat CSV through EOF before optionally replaying fresh records into scoped business handlers.</summary>
/// <typeparam name="TRecord">The typed wire record and flat business unit.</typeparam>
/// <param name="mappingFactory">Creates a fresh mapping for each pass.</param>
/// <param name="processingOptions">Format-neutral processing budgets.</param>
/// <param name="options">CSV syntax and explicit encoding.</param>
/// <param name="validator">Optional pure record validation.</param>
/// <param name="serviceProvider">The provider used for independent unit scopes.</param>
/// <param name="handlerFactory">Resolves a scoped handler; required for execution.</param>
public class CsvFileProcessor<TRecord>(Func<CsvFileMapping<TRecord>> mappingFactory, FileProcessingOptions? processingOptions = null, CsvFileOptions? options = null, IValidator<TRecord>? validator = null,
    IServiceProvider? serviceProvider = null, Func<IServiceProvider, FileUnitHandler<TRecord>>? handlerFactory = null) : CsvFileProcessorBase<TRecord>(processingOptions, options, validator, serviceProvider, handlerFactory) where TRecord : class, new()
{
    private readonly Func<CsvFileMapping<TRecord>> _mappingFactory = mappingFactory.ThrowIfNull();

    /// <inheritdoc/>
    private protected override async Task<bool> ReadPassAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, ProcessingState state, bool execute, CancellationToken cancellationToken)
    {
        var replay = execute && context.Mode == FileProcessingMode.ValidateThenExecute;
        var definition = _mappingFactory().ThrowIfNull();
        var mapping = definition.CreateMapping();
        var headerSeen = false;
        await foreach (var parsed in ReadRecordsAsync(openReadAsync, state, replay, cancellationToken).ConfigureAwait(false))
        {
            if (parsed.IsTerminal)
            {
                if (replay)
                    throw new InvalidDataException("Replay violated the accepted input bounds or encoding.");

                await state.ReportAsync(parsed.Diagnostic.ThrowIfNull(), cancellationToken).ConfigureAwait(false);
                return false;
            }

            var isHeaderCandidate = definition.RequiresHeader && !headerSeen;
            if (!replay && !isHeaderCandidate)
            {
                state.Records = checked(state.Records + 1);
                state.UnitsRead = checked(state.UnitsRead + 1);
                state.Update();
                if (state.UnitsRead > MaxUnits)
                {
                    await state.ReportAsync(new(FileDiagnosticCategory.Limit, "file.limit.units", "The file exceeds its configured unit limit.",
                        location: new(parsed.Reader.LineNumber), unitOrdinal: state.UnitsRead), cancellationToken).ConfigureAwait(false);
                    return false;
                }
            }

            var record = parsed.Diagnostic is null ? mapping(parsed.Reader) : new CsvInputRecord<TRecord>(null, isHeaderCandidate, [parsed.Diagnostic]);
            if (record.IsHeader)
            {
                headerSeen = true;
                if (record.Diagnostics.Count > 0)
                {
                    if (replay)
                        throw new InvalidDataException("An accepted CSV header could not be reconstructed on replay.");

                    foreach (var diagnostic in record.Diagnostics)
                        await state.ReportAsync(diagnostic, cancellationToken).ConfigureAwait(false);

                    return false;
                }

                continue;
            }

            if (record.Value is null)
            {
                if (replay)
                    throw new InvalidDataException("An accepted CSV record could not be reconstructed on replay.");

                state.InvalidRecords = checked(state.InvalidRecords + 1);
                state.InvalidUnits = checked(state.InvalidUnits + 1);
                foreach (var diagnostic in record.Diagnostics)
                    await state.ReportAsync(diagnostic, cancellationToken).ConfigureAwait(false);

                if (state.LimitExceeded)
                    return false;

                if (context.ErrorMode == FileErrorMode.FailFast)
                    return false;

                continue;
            }

            if (!replay && state.UnitsRead > MaxUnits)
            {
                await state.ReportAsync(new(FileDiagnosticCategory.Limit, "file.limit.units", "The file exceeds its configured unit limit.",
                    location: new(parsed.Reader.LineNumber), unitOrdinal: state.UnitsRead), cancellationToken).ConfigureAwait(false);
                return false;
            }

            if (execute)
            {
                var info = new FileUnitInfo(replay ? state.Attempted + 1 : state.UnitsRead, new(parsed.Reader.LineNumber));
                if (!replay && !await ValidateUnitAsync(record.Value, context, state, info, true, cancellationToken).ConfigureAwait(false))
                {
                    if (state.LimitExceeded)
                        return false;

                    if (context.ErrorMode == FileErrorMode.FailFast)
                        return false;

                    continue;
                }

                if (!await ExecuteUnitAsync(record.Value, context, state, info, cancellationToken).ConfigureAwait(false))
                    return false;
            }
            else if (!await ValidateUnitAsync(record.Value, context, state, new(state.UnitsRead, new(parsed.Reader.LineNumber)), true, cancellationToken).ConfigureAwait(false))
            {
                if (state.LimitExceeded)
                    return false;

                if (context.ErrorMode == FileErrorMode.FailFast)
                    return false;
            }

            if (state.LimitExceeded)
                return false;
        }

        if (definition.RequiresHeader && !headerSeen)
        {
            if (replay)
                throw new InvalidDataException("The accepted header was absent on replay.");

            await state.ReportAsync(new(FileDiagnosticCategory.Structure, "csv.header.missing", "A column-name header is required.", location: new(1)), cancellationToken).ConfigureAwait(false);
        }

        if (replay && state.Attempted != state.Validated)
            throw new InvalidDataException("Replay contained fewer units than the accepted input.");

        return true;
    }
}
