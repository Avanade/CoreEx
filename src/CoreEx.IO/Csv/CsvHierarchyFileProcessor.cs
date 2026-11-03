namespace CoreEx.IO.Csv;

/// <summary>Routes heterogeneous records and assembles contiguous root groups behind the shared whole-file acceptance barrier.</summary>
/// <typeparam name="TGroup">The projected business unit, distinct from physical descendant records.</typeparam>
/// <remarks>Each invocation requires a fresh CsvHierarchyContext. Only the current root subtree and accepted header/trailer are retained. The immutable input revision remains the caller's responsibility.</remarks>
public class CsvHierarchyFileProcessor<TGroup> : CsvFileProcessorBase<TGroup> where TGroup : class
{
    private readonly Func<CsvHierarchyDefinition<TGroup>> _definitionFactory;

    /// <summary>Initializes a definition-specific hierarchy processor without shared mutable routing state.</summary>
    /// <param name="definitionFactory">Creates the definition and fresh route mappings for each pass.</param>
    /// <param name="options">CSV syntax and decoding.</param>
    /// <param name="validator">Optional pure projected-group validation.</param>
    /// <param name="serviceProvider">The provider from which group scopes are created.</param>
    /// <param name="handlerFactory">Resolves one scoped group handler for validation or execution.</param>
    public CsvHierarchyFileProcessor(Func<CsvHierarchyDefinition<TGroup>> definitionFactory, CsvFileOptions? options = null, IValidator<TGroup>? validator = null,
        IServiceProvider? serviceProvider = null, Func<IServiceProvider, FileUnitHandler<TGroup>>? handlerFactory = null) : base(options, validator, serviceProvider, handlerFactory)
        => _definitionFactory = definitionFactory.ThrowIfNull();

    /// <inheritdoc/>
    protected override FileProcessingContext OnCreateExecutionContext(FileProcessingContext validationContext)
        => new CsvHierarchyContext(validationContext.Identity, validationContext.Mode, validationContext.ErrorMode);

    /// <inheritdoc/>
    private protected override async Task<bool> ReadPassAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, ProcessingState state, bool execute, CancellationToken cancellationToken)
    {
        if (context is not CsvHierarchyContext hierarchyContext)
            throw new ArgumentException("Hierarchical processing requires a fresh CsvHierarchyContext for each pass.", nameof(context));

        var session = new Session(this, _definitionFactory().ThrowIfNull(), hierarchyContext, state, execute);
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
        private readonly Dictionary<string, CsvRecordRoute> _routes;
        private readonly Dictionary<string, CsvRecordRoute> _matches;
        private readonly Dictionary<string, Func<CsvReader, (object? Value, IReadOnlyList<FileDiagnostic> Diagnostics)>> _mappings;
        private readonly List<Frame> _stack = [];
        private bool _hasRecords;
        private bool _trailerSeen;
        private bool _stop;
        private bool _recordInvalid;
        private long _groupRecords;

        /// <summary>Initializes isolated mapping and recognized-count state.</summary>
        public Session(CsvHierarchyFileProcessor<TGroup> processor, CsvHierarchyDefinition<TGroup> definition, CsvHierarchyContext context, ProcessingState state, bool execute)
        {
            (_processor, _definition, _context, _state, _execute) = (processor, definition, context, state, execute);
            _routes = definition.Routes.ToDictionary(x => x.Key, StringComparer.Ordinal);
            _matches = definition.Routes.ToDictionary(x => x.Match, StringComparer.Ordinal);
            _mappings = definition.Routes.ToDictionary(x => x.Key, x => x.CreateMapping(), StringComparer.Ordinal);
            context.Initialize(_routes.Keys);
        }

        /// <summary>Gets the effective policy of the record or rule owner.</summary>
        private FileErrorMode Mode(string? key) => key is null ? _context.ErrorMode : _routes[key].ErrorMode ?? _context.ErrorMode;

        /// <summary>Reports a localized current failure and applies its rule owner's continuation policy.</summary>
        private async Task ReportAsync(FileDiagnostic diagnostic, string? policyKey, bool physical, CancellationToken cancellationToken)
        {
            if (_execute)
                throw new InvalidDataException("An accepted hierarchy could not be reconstructed on replay.");

            if (diagnostic.IsError)
            {
                _context.HasValidationErrors = true;
                if (physical && !_recordInvalid)
                {
                    _recordInvalid = true;
                    _state.Invalid = checked(_state.Invalid + 1);
                }

                if (Mode(policyKey) == FileErrorMode.FailFast)
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
            await ReportAsync(new(FileDiagnosticCategory.Structure, code, message, location: new(line), recordType: key), key, physical, cancellationToken).ConfigureAwait(false);
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
            var header = _definition.Header;
            var trailer = _definition.Trailer;
            var parentIndex = -1;

            if (key == root.Key || (trailer is not null && key == trailer.Key))
            {
                if (!await CloseToAsync(0, cancellationToken).ConfigureAwait(false))
                    return false;
            }
            else if (key is not null && key != header?.Key)
            {
                parentIndex = _stack.FindLastIndex(x => x.IndexOfChild(key) >= 0);
                if (parentIndex >= 0 && !await CloseToAsync(parentIndex + 1, cancellationToken).ConfigureAwait(false))
                    return false;
            }

            _recordInvalid = false;
            _context.EndOfFileLineNumber = line;
            if (!_execute)
            {
                _state.Records = checked(_state.Records + 1);
                _state.Update();
            }

            if (route is not null)
                _context.Count(route.Key);

            if (key == root.Key || _stack.Count > 0)
            {
                _groupRecords = key == root.Key ? 1 : checked(_groupRecords + 1);
                if (_groupRecords > _processor.MaxGroupRecords)
                {
                    await ReportAsync(new(FileDiagnosticCategory.Limit, "csv.limit.group-records", "The root group exceeds its configured record limit.",
                        location: new(line), recordType: root.Key), root.Key, false, cancellationToken).ConfigureAwait(false);

                    return false;
                }
            }

            if (_trailerSeen && trailer is not null)
                await FailAsync("csv.structure.after-trailer", "A record cannot follow the last envelope.", trailer.Key, line, true, cancellationToken).ConfigureAwait(false);

            if (header?.Cardinality == CsvRecordCardinality.One && _context.GetRecordCount(header.Key) == 0 && key != header.Key)
                await FailAsync("csv.structure.before-header", "A record cannot precede the required first envelope.", header.Key, line, true, cancellationToken).ConfigureAwait(false);

            Frame? current = null;
            if (key == header?.Key && header is not null)
            {
                if (_hasRecords)
                    await FailAsync("csv.structure.header-position", "The first envelope must precede all other records.", header.Key, line, true, cancellationToken).ConfigureAwait(false);

                if (_context.GetRecordCount(header.Key) > 1)
                    await FailAsync("csv.structure.duplicate", "An envelope may occur only once.", header.Key, line, true, cancellationToken).ConfigureAwait(false);
            }
            else if (key == trailer?.Key && trailer is not null)
            {
                if (_context.GetRecordCount(trailer.Key) > 1)
                    await FailAsync("csv.structure.duplicate", "An envelope may occur only once.", trailer.Key, line, true, cancellationToken).ConfigureAwait(false);

                _trailerSeen = true;
            }
            else if (key == root.Key)
            {
                if (_context.GetRecordCount(key) > 1 && IsSingle(root))
                    await FailAsync("csv.structure.duplicate", "The root may occur only once in this file.", key, line, true, cancellationToken).ConfigureAwait(false);

                current = new(root, line) { Valid = !_recordInvalid };
                _stack.Add(current);
            }
            else if (route is not null)
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
                    location: new(line, fieldIndex: _definition.DiscriminatorIndex)), null, true, cancellationToken).ConfigureAwait(false);
            }

            if (parsed.Diagnostic is not null)
            {
                Invalidate();
                await ReportAsync(new(parsed.Diagnostic.Category, parsed.Diagnostic.Code, parsed.Diagnostic.Message, parsed.Diagnostic.Severity,
                    parsed.Diagnostic.Location, key), key, true, cancellationToken).ConfigureAwait(false);
            }
            else if (route is not null)
            {
                var mapped = _mappings[route.Key](reader);
                foreach (var diagnostic in mapped.Diagnostics)
                {
                    Invalidate();
                    await ReportAsync(new(diagnostic.Category, diagnostic.Code, diagnostic.Message, diagnostic.Severity, diagnostic.Location, key),
                        key, true, cancellationToken).ConfigureAwait(false);
                }

                if (mapped.Value is not null)
                {
                    if (!_execute)
                        await route.ValidateAsync(mapped.Value, line, async (diagnostic, ct) =>
                        {
                            if (diagnostic.IsError)
                                Invalidate();

                            await ReportAsync(diagnostic, key, true, ct).ConfigureAwait(false);
                        }, cancellationToken).ConfigureAwait(false);

                    if (!_recordInvalid)
                    {
                        if (current is not null)
                            current.Record = mapped.Value;
                        else if (key == header?.Key || key == trailer?.Key)
                            _context.AcceptEnvelope(key == header?.Key, mapped.Value, line);
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
                    Invalidate();
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
                            if (!await _processor.ExecuteUnitAsync(group, _context, _state, cancellationToken).ConfigureAwait(false))
                                return false;
                        }
                        else
                        {
                            var valid = await _processor.ValidateUnitAsync(group, _context, _state, new(frame.Line), false, frame.Definition.Key, cancellationToken).ConfigureAwait(false);
                            _context.HasValidationErrors |= !valid;
                            if (!valid && Mode(frame.Definition.Key) == FileErrorMode.FailFast)
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

            foreach (var node in new[] { _definition.Header, _definition.Root, _definition.Trailer }.OfType<CsvHierarchyNode>())
            {
                if (IsRequired(node) && _context.GetRecordCount(node.Key) == 0)
                {
                    await FailAsync("csv.structure.required", "A required record was not found at end of file.", node.Key,
                        _context.EndOfFileLineNumber, false, cancellationToken).ConfigureAwait(false);
                    if (_stop)
                        return false;
                }
            }

            return true;
        }

        /// <summary>Gets whether at least one recognized occurrence is required.</summary>
        private static bool IsRequired(CsvHierarchyNode node) => node.Cardinality is CsvRecordCardinality.One or CsvRecordCardinality.OneOrMore;

        /// <summary>Gets whether at most one recognized occurrence is allowed.</summary>
        private static bool IsSingle(CsvHierarchyNode node) => node.Cardinality is CsvRecordCardinality.One or CsvRecordCardinality.Optional;

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
