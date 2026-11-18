namespace CoreEx.IO.Json;

/// <summary>Streams JSON documents through the shared validation, reporting, scope, and replay lifecycle.</summary>
/// <typeparam name="TUnit">The single natural nested business wire type.</typeparam>
public class JsonFileProcessor<TUnit> : FileProcessor<TUnit> where TUnit : class, new()
{
    private readonly Func<JsonFileDefinition<TUnit>> _definitionFactory;
    private readonly JsonFileOptions _options;

    /// <summary>Initializes JSON document parsing, fresh pass definitions, and optional scoped handlers.</summary>
    /// <param name="definitionFactory">Creates fresh business and metadata mappings for each pass.</param>
    /// <param name="options">Shared JSON mapping and parser-depth settings.</param>
    /// <param name="validator">An optional pure business-unit validator.</param>
    /// <param name="serviceProvider">The provider from which unit scopes are created.</param>
    /// <param name="handlerFactory">Resolves the business handler after trusted context configuration.</param>
    public JsonFileProcessor(Func<JsonFileDefinition<TUnit>> definitionFactory, JsonFileOptions? options = null,
        IValidator<TUnit>? validator = null, IServiceProvider? serviceProvider = null, Func<IServiceProvider, FileUnitHandler<TUnit>>? handlerFactory = null)
        : base(validator, serviceProvider, handlerFactory)
    {
        _definitionFactory = definitionFactory.ThrowIfNull();
        _options = options ?? new();
        _options.Validate();
    }

    /// <inheritdoc/>
    protected override FileProcessingContext OnCreateExecutionContext(FileProcessingContext validationContext)
        => new JsonFileContext(validationContext.Identity, validationContext.Mode, validationContext.ErrorMode);

    /// <inheritdoc/>
    private protected override async Task<bool> ReadPassAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, ProcessingState state, bool execute, CancellationToken cancellationToken)
    {
        var replay = execute && context.Mode == FileProcessingMode.ValidateThenExecute;
        var definition = _definitionFactory().ThrowIfNull();
        var mapping = (IJsonUnitMapping)definition.Mapping;
        var metadataSeen = definition.Metadata.ToDictionary(x => x.Name, _ => false, StringComparer.Ordinal);
        var jsonContext = context as JsonFileContext;
        var ordinal = 0L;
        var activeValue = (SelectedValue?)null;
        var unitSelectionSeen = false;
        var unitSelectionValid = false;
        var selectedArrayDepth = -1;
        var stop = false;
        var rootSeen = false;

        await using var stream = (await openReadAsync(cancellationToken).ConfigureAwait(false)).ThrowIfNull();
        var reader = new JsonDocumentReader(stream, _options.MaxDepth, read =>
        {
            if (!replay)
            {
                state.Bytes = checked(state.Bytes + read);
                state.Update();
            }
        });

        async Task ReportAsync(FileDiagnostic diagnostic, SelectedValue? scope, CancellationToken cancellationToken)
        {
            if (replay)
                throw new InvalidDataException("An accepted JSON source could not be reconstructed on replay.");

            if (scope is not null && diagnostic.IsError && !scope.Invalid)
            {
                scope.Invalid = true;
                if (scope.IsUnit)
                {
                    state.InvalidRecords = checked(state.InvalidRecords + 1);
                    state.InvalidUnits = checked(state.InvalidUnits + 1);
                }
            }

            var located = new FileDiagnostic(diagnostic.Category, diagnostic.Code, diagnostic.Message, diagnostic.Severity, diagnostic.Location, diagnostic.RecordType, scope is { IsUnit: true } ? scope.Ordinal : null);

            await state.ReportAsync(located, cancellationToken).ConfigureAwait(false);

            stop |= diagnostic.IsError && context.ErrorMode == FileErrorMode.FailFast;
        }

        Task ReportFileErrorAsync(string code, string message, string? sourcePath, long? line, CancellationToken cancellationToken)
            => ReportAsync(new(FileDiagnosticCategory.Structure, code, message, location: new(lineNumber: line, sourcePath: sourcePath)), null, cancellationToken);

        async Task ReportSyntaxAsync(long? line, string sourcePath, CancellationToken cancellationToken)
        {
            if (jsonContext is not null)
                jsonContext.EndOfFileLineNumber = line;

            await ReportAsync(new(FileDiagnosticCategory.Parsing, "json.syntax.invalid", "The JSON document contains invalid syntax or duplicate object member names.",
                location: new(lineNumber: line, sourcePath: sourcePath), recordType: activeValue?.RecordType), activeValue, cancellationToken).ConfigureAwait(false);
        }

        var cursor = new JsonDocumentCursor(reader, ReportSyntaxAsync, () => stop, () => activeValue?.Invalid == true);

        async Task ProcessUnitAsync(SelectedValue current, CancellationToken cancellationToken)
        {
            var mappingContext = new JsonMappingContext(_options, current.LineNumber, current.Ordinal, definition.RecordType, (diagnostic, ct) => ReportAsync(diagnostic, current, ct), () => stop);
            var unit = (TUnit?)await mapping.DeserializeAsync(cursor, mappingContext, current.SourcePath, cancellationToken).ConfigureAwait(false);

            if (unit is null || current.Invalid || cursor.Failed || stop || mappingContext.Invalid)
                return;

            var info = new FileUnitInfo(current.Ordinal, new(current.LineNumber, sourcePath: current.SourcePath), definition.RecordType, current.ByteOffset)
            {
                ValidationLocation = property =>
                {
                    var source = mapping.ResolveSourcePath(property ?? string.Empty);
                    return new(current.LineNumber, propertyPath: string.IsNullOrEmpty(property) ? null : mapping.ResolvePropertyPath(property), sourcePath: JsonPointer.Combine(current.SourcePath, source));
                }
            };

            if (replay)
            {
                if (!await ExecuteUnitAsync(unit, context, state, info, cancellationToken).ConfigureAwait(false))
                    stop = true;
            }
            else if (await ValidateUnitAsync(unit, context, state, info, true, cancellationToken).ConfigureAwait(false) && execute
                && !await ExecuteUnitAsync(unit, context, state, info, cancellationToken).ConfigureAwait(false))
                stop = true;
        }

        async Task ProcessMetadataAsync(SelectedValue current, JsonFileMetadata metadata, CancellationToken cancellationToken)
        {
            var mappingContext = new JsonMappingContext(_options, current.LineNumber, null, metadata.RecordType, (diagnostic, ct) => ReportAsync(diagnostic, current, ct), () => stop);

            var value = await metadata.MapAsync(cursor, mappingContext, !replay, cancellationToken).ConfigureAwait(false);

            if (value is not null && !mappingContext.Invalid && !current.Invalid && !cursor.Failed && !stop)
                context.SetMetadata(metadata.Name, value);
        }

        while (await cursor.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var token = cursor.Token;
            var position = cursor.Position;
            rootSeen = true;

            if (token.Type == JsonTokenType.StartArray && position.IsValue && position.ObjectPath && position.Path == definition.UnitPath && !unitSelectionSeen)
            {
                unitSelectionSeen = true;
                if (definition.Kind == JsonDocumentKind.Array)
                {
                    unitSelectionValid = true;
                    selectedArrayDepth = token.Depth;
                }
                else
                    await ReportFileErrorAsync("json.unit.selection", "The selected business value has the wrong JSON kind.", definition.UnitPath, token.LineNumber, cancellationToken).ConfigureAwait(false);
            }
            else if (position.IsValue && position.ObjectPath && position.Path == definition.UnitPath && !unitSelectionSeen)
            {
                unitSelectionSeen = true;
                if (definition.Kind == JsonDocumentKind.Object && token.Type == JsonTokenType.StartObject)
                {
                    unitSelectionValid = true;
                    ordinal++;
                    if (!replay)
                    {
                        state.Records = checked(state.Records + 1);
                        state.UnitsRead = checked(state.UnitsRead + 1);
                    }

                    activeValue = new(true, ordinal, definition.RecordType, position.Path, token.LineNumber, token.ByteOffset);
                    await ProcessUnitAsync(activeValue, cancellationToken).ConfigureAwait(false);
                    activeValue = null;
                }
                else
                    await ReportFileErrorAsync("json.unit.selection", "The selected business value has the wrong JSON kind.", definition.UnitPath, token.LineNumber, cancellationToken).ConfigureAwait(false);
            }
            else if (definition.Kind == JsonDocumentKind.Array && unitSelectionValid && position.IsValue && position.ParentType == JsonTokenType.StartArray && position.ParentPath == definition.UnitPath && position.Depth == selectedArrayDepth + 1)
            {
                ordinal++;
                if (!replay)
                {
                    state.Records = checked(state.Records + 1);
                    state.UnitsRead = checked(state.UnitsRead + 1);
                }

                if (token.Type == JsonTokenType.StartObject)
                {
                    activeValue = new(true, ordinal, definition.RecordType, position.Path, token.LineNumber, token.ByteOffset);
                    await ProcessUnitAsync(activeValue, cancellationToken).ConfigureAwait(false);
                    activeValue = null;
                }
                else
                {
                    var diagnostic = new FileDiagnostic(FileDiagnosticCategory.Mapping, "json.unit.type",
                        "A selected JSON business unit must be an object.", location: new(token.LineNumber, sourcePath: position.Path),
                        recordType: definition.RecordType, unitOrdinal: ordinal);
                    await ReportAsync(diagnostic, new(true, ordinal, definition.RecordType, position.Path, token.LineNumber, token.ByteOffset), cancellationToken).ConfigureAwait(false);
                }
            }

            foreach (var metadata in definition.Metadata)
            {
                if (!metadataSeen[metadata.Name] && position.IsValue && position.ObjectPath && position.Path == metadata.Path)
                {
                    metadataSeen[metadata.Name] = true;
                    if (token.Type == JsonTokenType.StartObject)
                    {
                        activeValue = new(false, 0, metadata.RecordType, metadata.Path, token.LineNumber, token.ByteOffset);
                        await ProcessMetadataAsync(activeValue, metadata, cancellationToken).ConfigureAwait(false);
                        activeValue = null;
                    }
                    else
                        await ReportFileErrorAsync("json.metadata.type", "A selected JSON metadata value must be an object.", metadata.Path, token.LineNumber, cancellationToken).ConfigureAwait(false);

                    break;
                }
            }

            if (stop || cursor.Failed)
                return false;
        }

        if (stop || cursor.Failed)
            return false;

        var endLine = reader.EndOfFileLineNumber;
        if (jsonContext is not null)
            jsonContext.EndOfFileLineNumber = endLine;

        if (!rootSeen)
        {
            await ReportAsync(new(FileDiagnosticCategory.Parsing, "json.syntax.invalid", "The JSON document is empty.",
                location: new(lineNumber: endLine, sourcePath: string.Empty)), null, cancellationToken).ConfigureAwait(false);
            return false;
        }

        if (replay)
        {
            if (ordinal != state.Validated)
                throw new InvalidDataException("Replay contained a different number of business units than the accepted input.");

            return true;
        }

        if (!unitSelectionSeen)
            await ReportFileErrorAsync("json.unit.selection", "The configured business selector was not found.", definition.UnitPath, endLine, cancellationToken).ConfigureAwait(false);

        if (stop)
            return false;

        if (unitSelectionValid && !JsonMappingContext.Allows(definition.Cardinality, ordinal))
            await state.ReportAsync(new(FileDiagnosticCategory.Structure, "file.structure.cardinality", "The file does not satisfy its unit occurrence bounds.",
                location: new(lineNumber: endLine, sourcePath: definition.UnitPath), recordType: definition.RecordType), cancellationToken).ConfigureAwait(false);

        if (stop)
            return false;

        foreach (var metadata in definition.Metadata)
        {
            if (metadata.Required && !metadataSeen[metadata.Name])
                await state.ReportAsync(new(FileDiagnosticCategory.Structure, "file.metadata.required", "Required file metadata was not found.",
                    location: new(lineNumber: endLine, sourcePath: metadata.Path), recordType: metadata.RecordType), cancellationToken).ConfigureAwait(false);

            if (stop)
                return false;
        }

        return true;
    }

    /// <summary>Tracks provenance and error ownership without retaining the selected object's source bytes.</summary>
    private sealed class SelectedValue(bool isUnit, long ordinal, string recordType, string sourcePath, long lineNumber, long byteOffset)
    {
        /// <summary>Gets whether this value is a business unit.</summary>
        public bool IsUnit { get; } = isUnit;

        /// <summary>Gets the unit ordinal, or zero for metadata.</summary>
        public long Ordinal { get; } = ordinal;

        /// <summary>Gets the logical type associated with this object.</summary>
        public string RecordType { get; } = recordType;

        /// <summary>Gets the absolute source pointer to this object.</summary>
        public string SourcePath { get; } = sourcePath;

        /// <summary>Gets the object's one-based starting line.</summary>
        public long LineNumber { get; } = lineNumber;

        /// <summary>Gets the exact zero-based source byte offset.</summary>
        public long ByteOffset { get; } = byteOffset;

        /// <summary>Gets or sets whether an earlier content error invalidated this object.</summary>
        public bool Invalid { get; set; }
    }
}
