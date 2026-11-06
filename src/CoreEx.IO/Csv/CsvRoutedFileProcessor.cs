namespace CoreEx.IO.Csv;

/// <summary>Routes unordered heterogeneous records behind whole-file acceptance, with one scoped business unit per record.</summary>
/// <typeparam name="TUnit">The common projected record type, commonly a generated abstract record base.</typeparam>
/// <remarks>Only the current record and route counts are retained. The caller must provide identical immutable bytes on replay.</remarks>
public class CsvRoutedFileProcessor<TUnit> : CsvFileProcessorBase<TUnit> where TUnit : class
{
    private readonly Func<CsvRoutedDefinition<TUnit>> _definitionFactory;

    /// <summary>Initializes independent per-pass routing with shared reporting and scoped-handler lifetimes.</summary>
    /// <param name="definitionFactory">Creates fresh route definitions and mappings per pass.</param>
    /// <param name="options">CSV syntax, strict encoding, and finite input-byte budget.</param>
    /// <param name="validator">Optional pure projected-unit validation.</param>
    /// <param name="serviceProvider">The provider from which per-record scopes are created.</param>
    /// <param name="handlerFactory">Resolves one scoped handler for each accepted record and pass.</param>
    public CsvRoutedFileProcessor(Func<CsvRoutedDefinition<TUnit>> definitionFactory, CsvFileOptions? options = null, IValidator<TUnit>? validator = null,
        IServiceProvider? serviceProvider = null, Func<IServiceProvider, FileUnitHandler<TUnit>>? handlerFactory = null) : base(options, validator, serviceProvider, handlerFactory)
        => _definitionFactory = definitionFactory.ThrowIfNull();

    /// <inheritdoc/>
    protected override FileProcessingContext OnCreateExecutionContext(FileProcessingContext validationContext)
        => new CsvRoutedContext(validationContext.Identity, validationContext.Mode, validationContext.ErrorMode);

    /// <inheritdoc/>
    private protected override async Task<bool> ReadPassAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, ProcessingState state, bool execute, CancellationToken cancellationToken)
    {
        if (context is not CsvRoutedContext routedContext)
            throw new ArgumentException("Unordered routed processing requires a fresh CsvRoutedContext for each pass.", nameof(context));

        var session = new Session(this, _definitionFactory().ThrowIfNull(), routedContext, state, execute);
        await foreach (var parsed in ReadRecordsAsync(openReadAsync, state, execute, cancellationToken).ConfigureAwait(false))
        {
            if (parsed.IsTerminal)
            {
                if (execute)
                    throw new InvalidDataException("Replay violated the accepted input bounds or encoding.");

                await state.ReportAsync(parsed.Diagnostic.ThrowIfNull(), cancellationToken).ConfigureAwait(false);
                return false;
            }

            if (!await session.ObserveAsync(parsed, cancellationToken).ConfigureAwait(false))
                return false;
        }

        if (!await session.CompleteAsync(cancellationToken).ConfigureAwait(false))
            return false;

        if (execute && state.Attempted != state.Validated)
            throw new InvalidDataException("Replay contained fewer records than the accepted input.");

        return true;
    }

    /// <summary>Owns one pass's route counts and current record without retaining record history.</summary>
    private sealed class Session
    {
        private readonly CsvRoutedFileProcessor<TUnit> _processor;
        private readonly CsvRoutedDefinition<TUnit> _definition;
        private readonly CsvRoutedContext _context;
        private readonly ProcessingState _state;
        private readonly bool _execute;
        private readonly Dictionary<string, CsvRoutedRecord<TUnit>> _matches;
        private readonly Dictionary<string, Func<CsvReader, CsvRoutedInput<TUnit>>> _mappings;
        private readonly CsvRoutedRecord<TUnit>? _first;
        private CsvRoutedRecord<TUnit>? _last;
        private bool _hasRecords;
        private bool _invalid;
        private bool _stop;

        /// <summary>Creates isolated route mappings and initializes the invocation-local counts before opening input.</summary>
        public Session(CsvRoutedFileProcessor<TUnit> processor, CsvRoutedDefinition<TUnit> definition, CsvRoutedContext context, ProcessingState state, bool execute)
        {
            (_processor, _definition, _context, _state, _execute) = (processor, definition, context, state, execute);
            _matches = definition.Records.ToDictionary(x => x.Match, StringComparer.Ordinal);
            _mappings = definition.Records.ToDictionary(x => x.Key, x => x.CreateMapping(), StringComparer.Ordinal);
            _first = definition.Records.SingleOrDefault(x => x.Position == CsvRecordPosition.First);
            context.Initialize(definition.Records.Select(x => x.Key));
        }

        /// <summary>Reports a localized failure and applies the policy of the route owning that rule.</summary>
        private async Task ReportAsync(FileDiagnostic diagnostic, CsvRoutedRecord<TUnit>? owner, bool physical, CancellationToken cancellationToken)
        {
            if (_execute)
                throw new InvalidDataException("An accepted routed record could not be reconstructed on replay.");

            if (diagnostic.IsError)
            {
                if (physical && !_invalid)
                {
                    _invalid = true;
                    _state.Invalid = checked(_state.Invalid + 1);
                }

                if ((owner?.ErrorMode ?? _context.ErrorMode) == FileErrorMode.FailFast)
                    _stop = true;
            }

            await _state.ReportAsync(diagnostic, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Reports a structural failure at the current record or explicit EOF anchor.</summary>
        private Task FailAsync(string code, string message, CsvRoutedRecord<TUnit> owner, long line, bool physical, CancellationToken cancellationToken)
            => ReportAsync(new(FileDiagnosticCategory.Structure, code, message, location: new(line), recordType: owner.Key), owner, physical, cancellationToken);

        /// <summary>Checks file structure, maps and validates the current typed record, or executes its accepted replay.</summary>
        public async Task<bool> ObserveAsync(CsvParsedRecord parsed, CancellationToken cancellationToken)
        {
            var reader = parsed.Reader;
            var line = reader.LineNumber;
            var discriminator = reader.FieldCount > _definition.DiscriminatorIndex ? reader.GetString(_definition.DiscriminatorIndex) : null;
            var route = parsed.Diagnostic is null && discriminator is not null ? _matches.GetValueOrDefault(discriminator) : null;
            _invalid = false;
            _context.EndOfFileLineNumber = line;
            if (!_execute)
            {
                _state.Records = checked(_state.Records + 1);
                _state.Update();
            }

            if (route is not null)
                _context.Count(route.Key);

            if (_last is not null)
                await FailAsync("csv.structure.after-last", "A record cannot follow the last record type.", _last, line, true, cancellationToken).ConfigureAwait(false);

            if (_first?.Cardinality == CsvRecordCardinality.One && _context.GetRecordCount(_first.Key) == 0 && route != _first)
                await FailAsync("csv.structure.before-first", "A record cannot precede the required first record type.", _first, line, true, cancellationToken).ConfigureAwait(false);

            if (route is not null)
            {
                if (_context.GetRecordCount(route.Key) > 1 && route.Cardinality is CsvRecordCardinality.One or CsvRecordCardinality.Optional)
                    await FailAsync("csv.structure.duplicate", "The record type may occur only once in this file.", route, line, true, cancellationToken).ConfigureAwait(false);

                if (route.Position == CsvRecordPosition.First && _hasRecords)
                    await FailAsync("csv.structure.first-position", "The first record type must precede all other records.", route, line, true, cancellationToken).ConfigureAwait(false);

                if (route.Position == CsvRecordPosition.Last)
                    _last = route;
            }

            _hasRecords = true;
            if (parsed.Diagnostic is not null)
                await ReportAsync(parsed.Diagnostic, null, true, cancellationToken).ConfigureAwait(false);
            else if (route is null)
            {
                var missing = string.IsNullOrWhiteSpace(discriminator);
                await ReportAsync(new(FileDiagnosticCategory.Structure, missing ? "csv.discriminator.missing" : "csv.discriminator.unknown",
                    missing ? "The record discriminator is missing." : "The record discriminator is not recognized.",
                    location: new(line, fieldIndex: _definition.DiscriminatorIndex)), null, true, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var mapped = _mappings[route.Key](reader);
                foreach (var diagnostic in mapped.Diagnostics)
                    await ReportAsync(new(diagnostic.Category, diagnostic.Code, diagnostic.Message, diagnostic.Severity, diagnostic.Location, route.Key),
                        route, true, cancellationToken).ConfigureAwait(false);

                if (mapped.Value is { } unit)
                {
                    if (!_execute)
                        await mapped.ValidateAsync(line, (diagnostic, ct) => ReportAsync(diagnostic, route, true, ct), cancellationToken).ConfigureAwait(false);

                    if (!_invalid)
                    {
                        if (_execute)
                            return await _processor.ExecuteUnitAsync(unit, _context, _state, cancellationToken).ConfigureAwait(false);

                        if (!await _processor.ValidateUnitAsync(unit, _context, _state, new(line), true, route.Key, cancellationToken).ConfigureAwait(false)
                            && (route.ErrorMode ?? _context.ErrorMode) == FileErrorMode.FailFast)
                            _stop = true;
                    }
                }
            }

            return !_stop;
        }

        /// <summary>Checks required recognized occurrences at EOF using each missing route's own policy.</summary>
        public async Task<bool> CompleteAsync(CancellationToken cancellationToken)
        {
            foreach (var route in _definition.Records)
            {
                if (_context.GetRecordCount(route.Key) == 0 && route.Cardinality is CsvRecordCardinality.One or CsvRecordCardinality.OneOrMore)
                    await FailAsync("csv.structure.required", "A required record type was not found.", route, _context.EndOfFileLineNumber, false, cancellationToken).ConfigureAwait(false);

                if (_stop)
                    return false;
            }

            return true;
        }
    }
}
