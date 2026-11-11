namespace CoreEx.IO.JsonLines;

/// <summary>Reads UTF-8 object lines using the shared lifecycle, scopes, reporting, and immutable replay.</summary>
/// <typeparam name="TUnit">The single business wire type, including nested occurrences.</typeparam>
public class JsonLinesFileProcessor<TUnit> : FileProcessor<TUnit> where TUnit : class, new()
{
    private readonly Func<JsonLinesDefinition<TUnit>> _definitionFactory;
    private readonly JsonLinesFileOptions _options;

    /// <summary>Initializes JSONL framing, fresh pass definitions, and optional scoped handlers.</summary>
    /// <param name="definitionFactory">Creates fresh business and metadata mappings for each pass.</param>
    /// <param name="options">JSONL framing, mapping, and route-selection options.</param>
    /// <param name="validator">An optional pure business-unit validator.</param>
    /// <param name="serviceProvider">The provider from which unit scopes are created.</param>
    /// <param name="handlerFactory">Resolves the business handler after trusted context configuration.</param>
    public JsonLinesFileProcessor(Func<JsonLinesDefinition<TUnit>> definitionFactory, JsonLinesFileOptions? options = null,
        IValidator<TUnit>? validator = null, IServiceProvider? serviceProvider = null, Func<IServiceProvider, FileUnitHandler<TUnit>>? handlerFactory = null)
        : base(validator, serviceProvider, handlerFactory)
    {
        _definitionFactory = definitionFactory.ThrowIfNull();
        _options = options ?? new();
        _options.Json.ThrowIfNull().Validate();
        if (_options.Discriminator is not null)
            _options.Discriminator.ThrowIfNullOrWhiteSpace();

        if (!Enum.IsDefined(_options.BlankLinePolicy))
            throw new ArgumentOutOfRangeException(nameof(options));
    }

    /// <inheritdoc/>
    protected override FileProcessingContext OnCreateExecutionContext(FileProcessingContext validationContext)
        => new JsonLinesContext(validationContext.Identity, validationContext.Mode, validationContext.ErrorMode);

    /// <inheritdoc/>
    private protected override async Task<bool> ReadPassAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, ProcessingState state, bool execute, CancellationToken cancellationToken)
    {
        var replay = execute && context.Mode == FileProcessingMode.ValidateThenExecute;
        var definition = _definitionFactory().ThrowIfNull();
        var mapping = (CoreEx.IO.Json.IJsonUnitMapping)definition.Mapping;
        var routed = _options.Discriminator is not null;
        if (routed ? string.IsNullOrWhiteSpace(definition.Match) : definition.Match is not null || definition.Metadata.Count != 0)
            throw new InvalidOperationException("JSONL routes require an explicit discriminator and business match; plain input cannot declare routes.");

        var counts = definition.Metadata.ToDictionary(x => x.Name, _ => 0L, StringComparer.Ordinal);
        var ordinal = 0L;
        var records = 0L;
        var lastSeen = false;
        var jsonContext = context as JsonLinesContext;

        jsonContext?.Initialize(definition.Metadata.Select(x => x.RecordType).Prepend(definition.RecordType));
        var first = definition.Metadata.SingleOrDefault(x => x.Position == FileRecordPosition.First && x.Required);
        await using var stream = (await openReadAsync(cancellationToken).ConfigureAwait(false)).ThrowIfNull();
        var reader = new JsonLinesReader(stream,
            read => { if (!replay) { state.Bytes = checked(state.Bytes + read); state.Update(); } });

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false) is { } frame)
        {
            var blank = IsBlank(frame.Bytes.Span);
            if (blank && _options.BlankLinePolicy == JsonLinesBlankLinePolicy.Skip)
                continue;

            var invalid = false;
            var stop = false;
            var isUnit = !routed;
            JsonLinesMetadata? metadata = null;
            string recordType = definition.RecordType;
            records++;
            if (jsonContext is not null)
                jsonContext.EndOfFileLineNumber = frame.Line;

            if (!replay)
                state.Records = checked(state.Records + 1);

            string? code = null;
            using var document = blank ? null : Parse(frame.Bytes, out code);
#if !NET10_0_OR_GREATER
            if (document is not null)
                code = CoreEx.IO.Json.JsonSyntax.Check(document.RootElement);
#endif
            string? match = null;
            if (_options.Discriminator is { } discriminatorName && document?.RootElement.ValueKind == JsonValueKind.Object && code != "jsonl.record.invalid")
            {
                if (document.RootElement.TryGetProperty(discriminatorName, out var discriminator)
                    && CoreEx.IO.Json.JsonFieldConverters.String(discriminator, out var value))
                    match = value;
            }

            if (match is not null)
            {
                isUnit = match == definition.Match;
                metadata = definition.Metadata.SingleOrDefault(x => x.Match == match);
                if (metadata is not null)
                    recordType = metadata.RecordType;
            }

            if (isUnit)
            {
                ordinal++;
                if (!replay)
                    state.UnitsRead = checked(state.UnitsRead + 1);
            }

            if (isUnit || metadata is not null)
                jsonContext?.Count(recordType);

            state.Update();

            async Task ReportAsync(FileDiagnostic diagnostic, CancellationToken cancellationToken)
            {
                if (replay)
                    throw new InvalidDataException("An accepted JSONL source could not be reconstructed on replay.");

                if (stop)
                    return;

                if (diagnostic.IsError && !invalid)
                {
                    invalid = true;
                    state.InvalidRecords = checked(state.InvalidRecords + 1);
                    if (isUnit)
                        state.InvalidUnits = checked(state.InvalidUnits + 1);
                }

                await state.ReportAsync(diagnostic, cancellationToken).ConfigureAwait(false);
                stop |= diagnostic.IsError && context.ErrorMode == FileErrorMode.FailFast;
            }

            Task FailAsync(string code, CancellationToken cancellationToken) => ReportAsync(new(
                FileDiagnosticCategory.Parsing,
                code, "The JSONL source does not satisfy its declared contract.",
                location: new(frame.Line, sourcePath: string.Empty), recordType: metadata?.RecordType ?? (isUnit ? recordType : null), unitOrdinal: isUnit ? ordinal : null), cancellationToken);


            if (lastSeen)
                await FailAsync("jsonl.metadata.after-last", cancellationToken).ConfigureAwait(false);

            if (first is not null && counts[first.Name] == 0 && metadata != first)
                await FailAsync("jsonl.metadata.first-position", cancellationToken).ConfigureAwait(false);

            if (metadata is not null)
            {
                counts[metadata.Name]++;
                if (counts[metadata.Name] > 1)
                    await FailAsync("jsonl.metadata.duplicate", cancellationToken).ConfigureAwait(false);

                if (metadata.Position == FileRecordPosition.First && records != 1)
                    await FailAsync("jsonl.metadata.first-position", cancellationToken).ConfigureAwait(false);

                lastSeen |= metadata.Position == FileRecordPosition.Last;
            }

            if (code is not null)
            {
                await FailAsync(code, cancellationToken).ConfigureAwait(false);
                if (code == "jsonl.encoding.bom")
                    return false;
            }
            else if (blank)
                await FailAsync("jsonl.line.blank", cancellationToken).ConfigureAwait(false);
            else if (routed && !isUnit && metadata is null)
                await FailAsync(match is null ? "jsonl.discriminator.invalid" : "jsonl.discriminator.unknown", cancellationToken).ConfigureAwait(false);

            if (stop)
                return false;

            if (invalid)
                continue;

            var root = document.ThrowIfNull().RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                await FailAsync("json.unit.type", cancellationToken).ConfigureAwait(false);
                if (stop)
                    return false;

                continue;
            }

            var mappingContext = new CoreEx.IO.Json.JsonMappingContext(_options.Json, frame.Line, isUnit ? ordinal : null, recordType, ReportAsync, () => stop);
            if (metadata is not null)
            {
                var value = await metadata.MapAsync(root, mappingContext, _options.Discriminator, !replay, cancellationToken).ConfigureAwait(false);
                if (!invalid)
                    context.SetMetadata(metadata.Name, value);
            }
            else
            {
                var unit = (TUnit)await mapping.MapAsync(root, mappingContext, string.Empty, string.Empty, _options.Discriminator, cancellationToken).ConfigureAwait(false);
                if (!invalid)
                {
                    var info = new FileUnitInfo(ordinal, new(frame.Line, sourcePath: string.Empty), recordType, frame.Offset)
                    {
                        ValidationLocation = property => new(frame.Line, propertyPath: string.IsNullOrEmpty(property) ? null : mapping.ResolvePropertyPath(property),
                            sourcePath: mapping.ResolveSourcePath(property ?? string.Empty))
                    };

                    if (!replay && !await ValidateUnitAsync(unit, context, state, info, true, cancellationToken).ConfigureAwait(false))
                    {
                        if (context.ErrorMode == FileErrorMode.FailFast)
                            return false;
                    }
                    else if (execute && !await ExecuteUnitAsync(unit, context, state, info, cancellationToken).ConfigureAwait(false))
                        return false;
                }
            }

            if (stop)
                return false;
        }

        if (replay)
        {
            if (ordinal != state.Validated)
                throw new InvalidDataException("Replay contained fewer units than the accepted input.");

            return true;
        }

        foreach (var metadata in definition.Metadata)
        {
            if (metadata.Required && counts[metadata.Name] == 0)
            {
                await state.ReportAsync(new(FileDiagnosticCategory.Structure, "file.metadata.required", "Required file metadata was not found.",
                    location: new(sourcePath: string.Empty), recordType: metadata.RecordType), cancellationToken).ConfigureAwait(false);

                if (context.ErrorMode == FileErrorMode.FailFast)
                    return false;
            }
        }

        if (!CoreEx.IO.Json.JsonMappingContext.Allows(definition.Cardinality, ordinal))
            await state.ReportAsync(new(FileDiagnosticCategory.Structure, "file.structure.cardinality", "The file does not satisfy its unit occurrence bounds.",
                location: new(sourcePath: string.Empty), recordType: definition.RecordType), cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <summary>Uses the built-in UTF-8 parser once per line without exhaustively decoding ignored content.</summary>
    /// <param name="bytes">The raw line content excluding its separator.</param>
    /// <param name="error">The encoding or syntax diagnostic code when parsing fails.</param>
    /// <returns>The parsed document, or null when the line is invalid.</returns>
    /// <remarks>Built-in duplicate checking can throw InvalidOperationException for an unpaired surrogate in a member name.</remarks>
    private JsonDocument? Parse(ReadOnlyMemory<byte> bytes, out string? error)
    {
        error = null;
        if (bytes.Span.StartsWith<byte>([0xef, 0xbb, 0xbf]))
            error = "jsonl.encoding.bom";
        else
        {
            try
            {
                return JsonDocument.Parse(bytes, new JsonDocumentOptions
                {
                    MaxDepth = _options.Json.MaxDepth,
#if NET10_0_OR_GREATER
                    AllowDuplicateProperties = false
#endif
                });
            }
            catch (JsonException) { error = "jsonl.record.invalid"; }
            catch (InvalidOperationException) { error = "jsonl.record.invalid"; }
        }

        return null;
    }

    /// <summary>Recognizes only JSON whitespace; CR within a line remains part of its raw byte span.</summary>
    /// <param name="bytes">The raw line content.</param>
    /// <returns>Whether the content consists entirely of JSON whitespace.</returns>
    private static bool IsBlank(ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
            if (value is not (9 or 13 or 32))
                return false;

        return true;
    }
}
