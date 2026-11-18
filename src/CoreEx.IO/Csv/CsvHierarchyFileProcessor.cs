namespace CoreEx.IO.Csv;

/// <summary>Routes heterogeneous records and assembles contiguous root groups behind the shared whole-file acceptance barrier.</summary>
/// <typeparam name="TGroup">The projected business unit, distinct from physical descendant records.</typeparam>
/// <remarks>Each invocation requires a fresh CsvHierarchyContext. Only the current root subtree and declared metadata are retained. The immutable input revision remains the caller's responsibility.</remarks>
/// <param name="definitionFactory">Creates the definition and fresh route mappings for each pass.</param>
/// <param name="options">CSV syntax and decoding.</param>
/// <param name="validator">Optional pure projected-group validation.</param>
/// <param name="serviceProvider">The provider from which group scopes are created.</param>
/// <param name="handlerFactory">Resolves one scoped group handler for validation or execution.</param>
public class CsvHierarchyFileProcessor<TGroup>(Func<CsvHierarchyDefinition<TGroup>> definitionFactory, CsvFileOptions? options = null, IValidator<TGroup>? validator = null,
    IServiceProvider? serviceProvider = null, Func<IServiceProvider, FileUnitHandler<TGroup>>? handlerFactory = null) : CsvFileProcessorBase<TGroup>(options, validator, serviceProvider, handlerFactory) where TGroup : class
{
    private readonly Func<CsvHierarchyDefinition<TGroup>> _definitionFactory = definitionFactory.ThrowIfNull();

    /// <inheritdoc/>
    protected override FileProcessingContext OnCreateExecutionContext(FileProcessingContext validationContext)
        => new CsvHierarchyContext(validationContext.Identity, validationContext.Mode, validationContext.ErrorMode);

    /// <inheritdoc/>
    private protected override async Task<bool> ReadPassAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, ProcessingState state, bool execute, CancellationToken cancellationToken)
    {
        var replay = execute && context.Mode == FileProcessingMode.ValidateThenExecute;
        if (context is not CsvHierarchyContext hierarchyContext)
            throw new ArgumentException("Hierarchical processing requires a fresh CsvHierarchyContext for each pass.", nameof(context));

        var session = new Session(this, _definitionFactory().ThrowIfNull(), hierarchyContext, state, execute, replay);
        await foreach (var parsed in ReadRecordsAsync(openReadAsync, state, replay, cancellationToken).ConfigureAwait(false))
        {
            if (parsed.IsTerminal)
            {
                if (replay)
                    throw new InvalidDataException("Replay violated the accepted encoding.");

                await state.ReportAsync(parsed.Diagnostic.ThrowIfNull(), cancellationToken).ConfigureAwait(false);
                return false;
            }

            if (!await session.ObserveAsync(parsed, cancellationToken).ConfigureAwait(false))
                return false;
        }

        if (!await session.CompleteAsync(cancellationToken).ConfigureAwait(false))
            return false;

        if (replay && state.Attempted != state.Validated)
            throw new InvalidDataException("Replay contained fewer groups than the accepted input.");

        return true;
    }

    /// <summary>Owns one pass's routing, structural stack, and current group, never file-wide group history.</summary>
    private sealed class Session
    {
        private readonly CsvHierarchyFileProcessor<TGroup> _processor;
        private readonly CsvHierarchyDefinition<TGroup> _definition;
        private readonly CsvHierarchyContext _context;
        private readonly ProcessingState _state;
        private readonly bool _execute;
        private readonly bool _replay;
        private readonly Dictionary<string, CsvRecordRoute> _matches;
        private readonly Dictionary<string, CsvMetadataDefinition> _metadataByKey;
        private readonly Dictionary<string, Func<CsvReader, (object? Value, IReadOnlyList<FileDiagnostic> Diagnostics)>> _mappings;
        private readonly List<Frame> _stack = [];
        private bool _hasRecords;
        private bool _lastMetadataSeen;
        private bool _stop;
        private bool _recordInvalid;

        /// <summary>Initializes isolated mapping and recognized-count state.</summary>
        /// <param name="processor">The processor owning the shared lifecycle.</param>
        /// <param name="definition">The immutable hierarchy definition.</param>
        /// <param name="context">The pass-local context and error policy.</param>
        /// <param name="state">The shared processing state.</param>
        /// <param name="execute">Whether this pass executes accepted groups.</param>
        /// <param name="replay">Whether this pass replays an accepted validation pass.</param>
        public Session(CsvHierarchyFileProcessor<TGroup> processor, CsvHierarchyDefinition<TGroup> definition, CsvHierarchyContext context, ProcessingState state, bool execute, bool replay)
        {
            (_processor, _definition, _context, _state, _execute, _replay) = (processor, definition, context, state, execute, replay);
            _matches = definition.Routes.ToDictionary(x => x.Match, StringComparer.Ordinal);
            _metadataByKey = definition.Metadata.ToDictionary(x => x.RecordKey, StringComparer.Ordinal);
            _mappings = definition.Routes.ToDictionary(x => x.Key, x => x.CreateMapping(), StringComparer.Ordinal);
            context.Initialize(definition.Routes.Select(x => x.Key));
        }

        /// <summary>Reports a localized current failure and applies the invocation's continuation policy.</summary>
        private async Task ReportAsync(FileDiagnostic diagnostic, bool physical, CancellationToken cancellationToken)
        {
            if (_replay)
                throw new InvalidDataException("An accepted hierarchy could not be reconstructed on replay.");

            if (diagnostic.IsError)
            {
                _context.HasValidationErrors = true;
                if (physical && !_recordInvalid)
                {
                    _recordInvalid = true;
                    _state.InvalidRecords = checked(_state.InvalidRecords + 1);
                }

                if (_context.ErrorMode == FileErrorMode.FailFast)
                    _stop = true;
            }

            await _state.ReportAsync(diagnostic, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Marks active ancestors invalid without counting an additional physical failure.</summary>
        private void Invalidate()
        {
            foreach (var frame in _stack)
                frame.Valid = false;
        }

        /// <summary>Reports a structural rule violation at a physical record or explicit parent/EOF anchor.</summary>
        private async Task FailAsync(string code, string message, string key, long line, bool physical, CancellationToken cancellationToken)
        {
            Invalidate();
            await ReportAsync(new(FileDiagnosticCategory.Structure, code, message, location: new(line), recordType: key), physical, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Processes the previous group's boundary before mapping the new record.</summary>
        public async Task<bool> ObserveAsync(CsvParsedRecord parsed, CancellationToken cancellationToken)
        {
            var reader = parsed.Reader;
            var line = reader.LineNumber;
            var discriminator = reader.FieldCount > _definition.DiscriminatorIndex ? reader.GetString(_definition.DiscriminatorIndex) : null;
            var route = parsed.Diagnostic is null && discriminator is not null ? _matches.GetValueOrDefault(discriminator) : null;
            var key = route?.Key;
            var root = _definition.Root;
            var metadata = key is null ? null : _metadataByKey.GetValueOrDefault(key);
            var parentIndex = -1;

            if (key == root.Key || metadata is not null)
            {
                if (!await CloseToAsync(0, cancellationToken).ConfigureAwait(false))
                    return false;
            }
            else if (key is not null)
            {
                parentIndex = _stack.FindLastIndex(x => x.IndexOfChild(key) >= 0);
                if (parentIndex >= 0 && !await CloseToAsync(parentIndex + 1, cancellationToken).ConfigureAwait(false))
                    return false;
            }

            _recordInvalid = false;
            _context.EndOfFileLineNumber = line;
            if (!_replay)
            {
                _state.Records = checked(_state.Records + 1);
                if (key == root.Key)
                    _state.UnitsRead = checked(_state.UnitsRead + 1);
                _state.Update();
            }

            if (route is not null)
                _context.Count(route.Key);

            if (_lastMetadataSeen && metadata?.Position != FileRecordPosition.Last)
                await FailAsync("csv.metadata.after-last", "A record cannot follow metadata restricted to the last position.", _definition.Metadata.Single(x => x.Position == FileRecordPosition.Last).RecordKey, line, true, cancellationToken).ConfigureAwait(false);

            Frame? current = null;
            if (metadata is not null)
            {
                if (metadata.Position == FileRecordPosition.First && _hasRecords)
                    await FailAsync("csv.metadata.first-position", "Metadata restricted to the first position must precede all other records.", metadata.RecordKey, line, true, cancellationToken).ConfigureAwait(false);

                if (_context.GetRecordCount(metadata.RecordKey) > 1)
                    await FailAsync("csv.metadata.duplicate", "Metadata may occur only once.", metadata.RecordKey, line, true, cancellationToken).ConfigureAwait(false);

                if (metadata.Position == FileRecordPosition.Last)
                    _lastMetadataSeen = true;
            }
            else if (key == root.Key)
            {
                if (_context.GetRecordCount(key) > 1 && IsSingle(root))
                    await FailAsync("csv.structure.duplicate", "The root may occur only once in this file.", key, line, true, cancellationToken).ConfigureAwait(false);

                current = new(root, line) { Valid = !_recordInvalid };
                _stack.Add(current);
            }
            else if (metadata is null && route is not null)
            {
                if (parentIndex < 0)
                    await FailAsync("csv.structure.orphan", "The record has no active parent.", route.Key, line, true, cancellationToken).ConfigureAwait(false);
                else
                {
                    var parent = _stack[parentIndex];
                    var index = parent.IndexOfChild(route.Key);
                    var child = parent.Definition.Children[index];
                    if (index < parent.Cursor)
                        await FailAsync("csv.structure.child-order", "A child subtree is out of declared order.", route.Key, line, true, cancellationToken).ConfigureAwait(false);

                    for (var i = parent.Cursor + 1; i < index; i++)
                    {
                        var skipped = parent.Definition.Children[i];
                        if (IsRequired(skipped) && parent.Counts.GetValueOrDefault(skipped.Key) == 0)
                        {
                            await FailAsync("csv.structure.required", "A required child record was not found.", skipped.Key, parent.Line, false, cancellationToken).ConfigureAwait(false);
                            parent.MissingReported.Add(skipped.Key);
                        }
                    }

                    parent.Cursor = Math.Max(parent.Cursor, index);
                    var count = parent.Counts.GetValueOrDefault(route.Key);
                    parent.Counts[route.Key] = checked(count + 1);

                    if (count > 0 && IsSingle(child))
                        await FailAsync("csv.structure.duplicate", "The child may occur only once per parent.", route.Key, line, true, cancellationToken).ConfigureAwait(false);

                    current = new(child, line) { Valid = !_recordInvalid };
                    _stack.Add(current);
                }
            }

            _hasRecords = true;
            if (route is null && parsed.Diagnostic is null)
            {
                Invalidate();
                var missing = string.IsNullOrWhiteSpace(discriminator);
                await ReportAsync(new(FileDiagnosticCategory.Structure, missing ? "csv.discriminator.missing" : "csv.discriminator.unknown",
                    missing ? "The record discriminator is missing." : "The record discriminator is not recognized.",
                    location: new(line, fieldIndex: _definition.DiscriminatorIndex)), true, cancellationToken).ConfigureAwait(false);
            }

            if (parsed.Diagnostic is not null)
            {
                Invalidate();
                await ReportAsync(new(parsed.Diagnostic.Category, parsed.Diagnostic.Code, parsed.Diagnostic.Message, parsed.Diagnostic.Severity,
                    parsed.Diagnostic.Location, key), true, cancellationToken).ConfigureAwait(false);
            }
            else if (route is not null)
            {
                var mapped = _mappings[route.Key](reader);
                foreach (var diagnostic in mapped.Diagnostics)
                {
                    Invalidate();
                    await ReportAsync(new(diagnostic.Category, diagnostic.Code, diagnostic.Message, diagnostic.Severity, diagnostic.Location, key), true, cancellationToken).ConfigureAwait(false);
                }

                if (mapped.Value is not null)
                {
                    if (!_replay)
                        await route.ValidateAsync(mapped.Value, line, async (diagnostic, ct) =>
                        {
                            if (diagnostic.IsError)
                                Invalidate();

                            await ReportAsync(diagnostic, true, ct).ConfigureAwait(false);
                        }, cancellationToken).ConfigureAwait(false);

                    if (!_recordInvalid)
                    {
                        if (current is not null)
                            current.Record = mapped.Value;
                        else if (metadata is not null)
                            _context.SetMetadata(metadata.Name, mapped.Value, line);
                    }
                }
            }

            return !_stop;
        }

        /// <summary>Closes descendants from the leaves upwards and dispatches only accepted root groups.</summary>
        private async Task<bool> CloseToAsync(int depth, CancellationToken cancellationToken)
        {
            while (_stack.Count > depth)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var frame = _stack[^1];
                foreach (var child in frame.Definition.Children)
                {
                    if (IsRequired(child) && frame.Counts.GetValueOrDefault(child.Key) == 0 && !frame.MissingReported.Contains(child.Key))
                        await FailAsync("csv.structure.required", "A required child record was not found.", child.Key, frame.Line, false, cancellationToken).ConfigureAwait(false);
                }

                _stack.RemoveAt(_stack.Count - 1);
                if (!frame.Valid || frame.Record is null)
                {
                    if (_stack.Count == 0 && !_replay)
                        _state.InvalidUnits = checked(_state.InvalidUnits + 1);

                    Invalidate();
                }
                else
                {
                    var node = new CsvAggregateNode(frame.Definition.Key, frame.Record, frame.Line, frame.Children.AsReadOnly());
                    if (_stack.Count > 0)
                        _stack[^1].Children.Add(node);
                    else if (!_stop)
                    {
                        var group = _definition.Project(node).ThrowIfNull();
                        if (_execute)
                        {
                            var info = new FileUnitInfo(_replay ? _state.Attempted + 1 : _state.UnitsRead, new(frame.Line), frame.Definition.Key);
                            if (!_replay && !await _processor.ValidateUnitAsync(group, _context, _state, info, false, cancellationToken).ConfigureAwait(false))
                            {
                                if (_context.ErrorMode == FileErrorMode.FailFast)
                                    return false;

                                continue;
                            }

                            if (!await _processor.ExecuteUnitAsync(group, _context, _state, info, cancellationToken).ConfigureAwait(false))
                                return false;
                        }
                        else
                        {
                            var valid = await _processor.ValidateUnitAsync(group, _context, _state, new(_state.UnitsRead, new(frame.Line), frame.Definition.Key), false, cancellationToken).ConfigureAwait(false);

                            _context.HasValidationErrors |= !valid;
                            if (!valid && _context.ErrorMode == FileErrorMode.FailFast)
                                _stop = true;
                        }
                    }
                }

                if (_stop)
                    return false;
            }

            return true;
        }

        /// <summary>Completes the final root and required file-level occurrences at their documented EOF anchor.</summary>
        public async Task<bool> CompleteAsync(CancellationToken cancellationToken)
        {
            if (!await CloseToAsync(0, cancellationToken).ConfigureAwait(false))
                return false;

            foreach (var node in new[] { _definition.Root })
            {
                if (IsRequired(node) && _context.GetRecordCount(node.Key) == 0)
                {
                    await FailAsync("csv.structure.required", "A required record was not found at end of file.", node.Key, _context.EndOfFileLineNumber, false, cancellationToken).ConfigureAwait(false);
                    if (_stop)
                        return false;
                }
            }

            foreach (var metadata in _definition.Metadata)
            {
                if (metadata.Required && _context.GetRecordCount(metadata.RecordKey) == 0)
                {
                    await FailAsync("csv.metadata.required", "Required metadata was not found at end of file.", metadata.RecordKey, _context.EndOfFileLineNumber, false, cancellationToken).ConfigureAwait(false);
                    if (_stop)
                        return false;
                }
            }

            return true;
        }

        /// <summary>Gets whether at least one recognized occurrence is required.</summary>
        private static bool IsRequired(CsvHierarchyNode node) => node.Cardinality is FileCardinality.One or FileCardinality.OneOrMore;

        /// <summary>Gets whether at most one recognized occurrence is allowed.</summary>
        private static bool IsSingle(CsvHierarchyNode node) => node.Cardinality is FileCardinality.One or FileCardinality.Optional;

        /// <summary>Holds only one open subtree's wire record, children, and structural counters.</summary>
        private sealed class Frame(CsvHierarchyNode definition, long line)
        {
            /// <summary>Gets the declared position.</summary>
            public CsvHierarchyNode Definition { get; } = definition;

            /// <summary>Gets the parent's physical starting line for missing-child diagnostics.</summary>
            public long Line { get; } = line;

            /// <summary>Gets or sets the accepted wire record.</summary>
            public object? Record { get; set; }

            /// <summary>Gets or sets whether the subtree remains structurally/field valid.</summary>
            public bool Valid { get; set; } = true;

            /// <summary>Gets or sets the highest encountered child position.</summary>
            public int Cursor { get; set; } = -1;

            /// <summary>Gets recognized child occurrences independent of field validity.</summary>
            public Dictionary<string, long> Counts { get; } = new(StringComparer.Ordinal);

            /// <summary>Gets missing-child keys already reported while advancing sibling order.</summary>
            public HashSet<string> MissingReported { get; } = new(StringComparer.Ordinal);

            /// <summary>Gets accepted completed children retained only until the root is dispatched.</summary>
            public List<CsvAggregateNode> Children { get; } = [];

            /// <summary>Finds a child's declared position without allocating a collection for each input row.</summary>
            public int IndexOfChild(string key)
            {
                for (var i = 0; i < Definition.Children.Count; i++)
                    if (Definition.Children[i].Key == key)
                        return i;

                return -1;
            }
        }
    }
}
