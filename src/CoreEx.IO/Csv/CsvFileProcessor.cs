namespace CoreEx.IO.Csv;

/// <summary>Validates flat CSV through EOF before optionally replaying fresh records into scoped business handlers.</summary>
/// <typeparam name="TRecord">The typed wire record and flat business unit.</typeparam>
/// <remarks>Processing is sequential within each invocation. Factories, mappings, contexts, and report resources must not share mutable invocation state. Expected content failures are diagnostics; I/O, callbacks, replay reconstruction, and programming faults propagate.</remarks>
public class CsvFileProcessor<TRecord> : FileProcessor where TRecord : class, new()
{
    private readonly Func<CsvFileMapping<TRecord>> _mappingFactory;
    private readonly CsvFileOptions _options;
    private readonly IValidator<TRecord>? _validator;
    private readonly IServiceProvider? _serviceProvider;
    private readonly Func<IServiceProvider, FileUnitHandler<TRecord>>? _handlerFactory;

    /// <summary>Initializes a flat CSV processor with explicit pure validation and optional scoped handlers.</summary>
    /// <param name="mappingFactory">Creates a fresh mapping definition for each pass.</param>
    /// <param name="options">CSV syntax and explicit encoding; header and content-error policies are owned by this processor.</param>
    /// <param name="validator">Optional pure record validator, not a file-long scoped dependency. Dependency-backed rules belong in scoped unit validation.</param>
    /// <param name="serviceProvider">The provider used to create independent unit scopes; required with a handler factory.</param>
    /// <param name="handlerFactory">Resolves a fresh handler from the supplied unit provider. Required for execution and optional for dependency-backed validation.</param>
    public CsvFileProcessor(Func<CsvFileMapping<TRecord>> mappingFactory, CsvFileOptions? options = null, IValidator<TRecord>? validator = null,
        IServiceProvider? serviceProvider = null, Func<IServiceProvider, FileUnitHandler<TRecord>>? handlerFactory = null)
    {
        _mappingFactory = mappingFactory.ThrowIfNull();
        options ??= new CsvFileOptions();
        _options = options with { Encoding = (Encoding)options.Encoding.ThrowIfNull().Clone() };

        if (handlerFactory is not null && serviceProvider is null)
            throw new ArgumentNullException(nameof(serviceProvider));

        _validator = validator;
        _serviceProvider = serviceProvider;
        _handlerFactory = handlerFactory;
    }

    /// <inheritdoc/>
    public override async Task<FileProcessingSummary> ProcessAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, CancellationToken cancellationToken = default)
    {
        openReadAsync.ThrowIfNull();
        context.ThrowIfNull();
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Mode == FileProcessingMode.ValidateThenExecute && _handlerFactory is null)
            throw new InvalidOperationException("Execution requires a scoped unit handler factory.");

        var state = new ProcessingState(this, context);
        context.Progress = state.Snapshot();
        var validationCompleted = await ReadPassAsync(openReadAsync, context, state, false, cancellationToken).ConfigureAwait(false);
        if (validationCompleted)
            await OnValidateFileAsync(context, state.ReportAsync, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        if (state.Errors > 0)
        {
            var rejected = new FileProcessingSummary(context, FileProcessingOutcome.Rejected, state.Snapshot(), validationCompleted);
            await OnRejectedAsync(context, rejected, cancellationToken).ConfigureAwait(false);
            return rejected;
        }

        if (context.Mode == FileProcessingMode.ValidateOnly)
        {
            var validated = new FileProcessingSummary(context, FileProcessingOutcome.Validated, state.Snapshot(), true);
            await OnCompletedAsync(context, validated, cancellationToken).ConfigureAwait(false);
            return validated;
        }

        var executionContext = OnCreateExecutionContext(context).ThrowIfNull();
        if (ReferenceEquals(executionContext, context) || !ReferenceEquals(executionContext.Identity, context.Identity)
            || executionContext.Mode != context.Mode || executionContext.ErrorMode != context.ErrorMode)
            throw new InvalidOperationException("Replay requires a fresh context preserving the original identity and policy.");

        await ReadPassAsync(openReadAsync, executionContext, state, true, cancellationToken).ConfigureAwait(false);

        var outcome = state.ExecutionResult.IsFailure ? FileProcessingOutcome.ExecutionFailed : FileProcessingOutcome.Executed;
        var summary = new FileProcessingSummary(context, outcome, state.Snapshot(), true, state.ExecutionResult);
        if (summary.IsSuccess)
            await OnCompletedAsync(context, summary, cancellationToken).ConfigureAwait(false);

        return summary;
    }

    /// <summary>Creates fresh execution-pass reconciliation state; override for a derived context without copying validation accumulators.</summary>
    /// <param name="validationContext">The original context whose immutable identity and policy must be preserved.</param>
    /// <returns>A distinct execution-pass context.</returns>
    protected virtual FileProcessingContext OnCreateExecutionContext(FileProcessingContext validationContext) => new(validationContext.Identity, validationContext.Mode, validationContext.ErrorMode);

    /// <summary>Configures trusted unit identity before business-service resolution; no parent state is copied by default.</summary>
    /// <param name="fileContext">The pass-local file context.</param>
    /// <param name="record">The typed input; its fields are not authenticated identity.</param>
    /// <param name="parent">The file-level ambient context, when present.</param>
    /// <param name="unitContext">The fresh registered context; configure trusted tenant/user/culture and approved metadata explicitly.</param>
    protected virtual void OnConfigureUnitContext(FileProcessingContext fileContext, TRecord record, ExecutionContext? parent, ExecutionContext unitContext) { }

    /// <summary>Reads one owned stream using isolated mapping and parser state.</summary>
    private async Task<bool> ReadPassAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext passContext, ProcessingState state, bool execute, CancellationToken cancellationToken)
    {
        var definition = _mappingFactory().ThrowIfNull();
        var mapping = definition.CreateMapping();
        var input = await openReadAsync(cancellationToken).ConfigureAwait(false);
        if (input is null)
            throw new InvalidOperationException("The stream factory returned null.");

        await using var ownedInput = input;
        if (!input.CanRead)
            throw new InvalidOperationException("The stream factory must return a readable stream at its beginning.");

        using var countingInput = new CsvReadStream(input, count =>
        {
            if (!execute)
            {
                state.Bytes = checked(state.Bytes + count);
                state.Update();
            }
        });

        using var text = new StreamReader(countingInput, _options.Encoding, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        FileDiagnostic? syntaxError = null;

        await using var parser = CsvReader.Create(text, new CsvReaderOptions
        {
            HasHeader = false,
            Separator = _options.Delimiter,
            Quote = _options.Quote,
            CommentPrefix = _options.Comment,
            TrackLineNumbers = true,
            DetectSeparatorHint = false,
            IncludeValuesInExceptions = false,
            MalformedData = CsvMalformedDataHandling.Recover,
            OnMalformedRecord = error => syntaxError = new(FileDiagnosticCategory.Parsing, "csv.record.invalid",
                "CSV syntax could not be parsed.", location: new(error.LineNumber))
        }, leaveOpen: true);

        var headerSeen = false;
        long replayed = 0;
        while (await parser.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = syntaxError is null ? mapping(parser) : new CsvInputRecord<TRecord>(null, definition.RequiresHeader && !headerSeen, [syntaxError]);
            syntaxError = null;
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

            var location = new FileLocation(parser.LineNumber);
            if (record.Value is null)
            {
                if (execute)
                    throw new InvalidDataException("An accepted CSV record could not be reconstructed on replay.");

                state.Invalid = checked(state.Invalid + 1);
                foreach (var diagnostic in record.Diagnostics)
                    await state.ReportAsync(diagnostic, cancellationToken).ConfigureAwait(false);

                if (passContext.ErrorMode == FileErrorMode.FailFast)
                    return false;

                continue;
            }

            if (execute)
            {
                replayed = checked(replayed + 1);
                if (replayed > state.Validated)
                    throw new InvalidDataException("Replay contained more units than the accepted input.");

                state.Attempted = checked(state.Attempted + 1);
                state.Update();
                passContext.Progress = state.Snapshot();
                state.ExecutionResult = await ExecuteUnitAsync(record.Value, passContext, cancellationToken).ConfigureAwait(false);
                if (state.ExecutionResult.IsFailure)
                    return false;

                state.Executed = checked(state.Executed + 1);
                state.Update();
                passContext.Progress = state.Snapshot();
            }
            else
            {
                var invalid = false;
                async Task ReportRecordAsync(FileDiagnostic diagnostic, CancellationToken cancellationToken)
                {
                    diagnostic.ThrowIfNull();
                    if (diagnostic.IsError && !invalid)
                    {
                        invalid = true;
                        state.Invalid = checked(state.Invalid + 1);
                    }

                    var located = diagnostic.Location is null
                        ? new FileDiagnostic(diagnostic.Category, diagnostic.Code, diagnostic.Message, diagnostic.Severity, location, diagnostic.RecordType)
                        : diagnostic;
                    await state.ReportAsync(located, cancellationToken).ConfigureAwait(false);
                }

                if (_validator is not null)
                {
                    var validation = await _validator.ValidateAsync(record.Value, cancellationToken).ConfigureAwait(false);
                    if (validation.Messages is not null)
                        foreach (var message in validation.Messages)
                            await ReportRecordAsync(new(FileDiagnosticCategory.Validation, "csv.validation", message.Text ?? LText.Empty, message.Type,
                                new(parser.LineNumber, propertyPath: message.Property)), cancellationToken).ConfigureAwait(false);

                    if (validation.HasErrors && !invalid)
                        throw new InvalidOperationException("The validator reported errors without error messages.");
                }

                if (!invalid && _handlerFactory is not null)
                    await ValidateUnitAsync(record.Value, passContext, ReportRecordAsync, cancellationToken).ConfigureAwait(false);

                if (!invalid)
                    state.Validated = checked(state.Validated + 1);

                state.Update();
                if (invalid && passContext.ErrorMode == FileErrorMode.FailFast)
                    return false;
            }
        }

        if (definition.RequiresHeader && !headerSeen)
        {
            if (execute)
                throw new InvalidDataException("The accepted header was absent on replay.");

            await state.ReportAsync(new(FileDiagnosticCategory.Structure, "csv.header.missing", "A column-name header is required.", location: new(1)), cancellationToken).ConfigureAwait(false);
        }

        if (execute && replayed != state.Validated)
            throw new InvalidDataException("Replay contained fewer units than the accepted input.");

        return true;
    }

    /// <summary>Validates a unit inside its own registered async scope.</summary>
    private Task ValidateUnitAsync(TRecord record, FileProcessingContext context, Func<FileDiagnostic, CancellationToken, Task> reportAsync, CancellationToken cancellationToken)
        => ExecutionContext.RunScopedAsync(_serviceProvider.ThrowIfNull(), (unit, ct) =>
            _handlerFactory.ThrowIfNull()(unit.ServiceProvider.ThrowIfNull()).ThrowIfNull().ValidateAsync(record, context, reportAsync, ct),
            (parent, unit) => OnConfigureUnitContext(context, record, parent, unit), cancellationToken);

    /// <summary>Executes a rebuilt unit inside a new registered async scope, returning the original Result.</summary>
    private Task<Result> ExecuteUnitAsync(TRecord record, FileProcessingContext context, CancellationToken cancellationToken)
        => ExecutionContext.RunScopedAsync(_serviceProvider.ThrowIfNull(), (unit, ct) =>
            _handlerFactory.ThrowIfNull()(unit.ServiceProvider.ThrowIfNull()).ThrowIfNull().ExecuteAsync(record, context, ct),
            (parent, unit) => OnConfigureUnitContext(context, record, parent, unit), cancellationToken);

    /// <summary>Contains invocation-local counters and a captured file-level callback context, never diagnostic history.</summary>
    private sealed class ProcessingState(CsvFileProcessor<TRecord> processor, FileProcessingContext context)
    {
        private readonly System.Threading.ExecutionContext _callbackContext = System.Threading.ExecutionContext.Capture()
            ?? throw new InvalidOperationException("CSV processing requires execution-context flow to be enabled.");

        /// <summary>Gets or sets the validation bytes read.</summary>
        public long Bytes { get; set; }
        /// <summary>Gets or sets the validation physical records read.</summary>
        public long Records { get; set; }
        /// <summary>Gets or sets the invalid physical record count.</summary>
        public long Invalid { get; set; }
        /// <summary>Gets or sets the validated unit count.</summary>
        public long Validated { get; set; }
        /// <summary>Gets or sets the execution attempts.</summary>
        public long Attempted { get; set; }
        /// <summary>Gets or sets the successful execution count.</summary>
        public long Executed { get; set; }
        /// <summary>Gets or sets the error diagnostic count.</summary>
        public long Errors { get; set; }
        /// <summary>Gets or sets the warning diagnostic count.</summary>
        public long Warnings { get; set; }
        /// <summary>Gets or sets the informational diagnostic count.</summary>
        public long Information { get; set; }
        /// <summary>Gets or sets the original execution Result.</summary>
        public Result ExecutionResult { get; set; }

        /// <summary>Creates an immutable progress snapshot.</summary>
        public FileProcessingProgress Snapshot() => new(Bytes, Records, Invalid, Validated, Attempted, Executed, Errors, Warnings, Information);

        /// <summary>Updates the caller-visible partial progress before callbacks and faults.</summary>
        public void Update() => context.Progress = Snapshot();

        /// <summary>Counts and awaits one diagnostic under the original file-level ambient context.</summary>
        public Task ReportAsync(FileDiagnostic diagnostic, CancellationToken cancellationToken)
        {
            diagnostic.ThrowIfNull();
            cancellationToken.ThrowIfCancellationRequested();
            switch (diagnostic.Severity)
            {
                case MessageType.Error: Errors = checked(Errors + 1); break;
                case MessageType.Warning: Warnings = checked(Warnings + 1); break;
                case MessageType.Info: Information = checked(Information + 1); break;
            }

            Update();
            Task? task = null;
            System.Threading.ExecutionContext.Run(_callbackContext, _ => task = processor.OnDiagnosticAsync(context, diagnostic, cancellationToken), null);
            return task ?? throw new InvalidOperationException("The diagnostic hook returned a null task.");
        }
    }
}
