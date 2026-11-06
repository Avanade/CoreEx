namespace CoreEx.IO.Csv;

/// <summary>Shares whole-file acceptance, reporting, and scoped business-unit lifetimes across CSV processors.</summary>
/// <typeparam name="TUnit">The flat or unordered routed record, or projected root group.</typeparam>
/// <remarks>Use the flat, unordered routed, or hierarchical processor rather than implementing a separate lifecycle. Independent invocations require distinct contexts and thread-safe shared dependencies.</remarks>
public abstract class CsvFileProcessorBase<TUnit> : FileProcessor where TUnit : class
{
    private readonly CsvFileOptions _options;
    private readonly IValidator<TUnit>? _validator;
    private readonly IServiceProvider? _serviceProvider;
    private readonly Func<IServiceProvider, FileUnitHandler<TUnit>>? _handlerFactory;
    private readonly CsvFileProcessorInvoker _invoker;

    /// <summary>Initializes shared syntax, pure validation, and optional scoped business handlers.</summary>
    /// <param name="options">CSV syntax and explicit decoding.</param>
    /// <param name="validator">A pure unit validator; dependency-backed validation belongs in the scoped handler.</param>
    /// <param name="serviceProvider">The provider from which unit scopes are created.</param>
    /// <param name="handlerFactory">Resolves the unit handler after trusted context configuration.</param>
    protected CsvFileProcessorBase(CsvFileOptions? options, IValidator<TUnit>? validator, IServiceProvider? serviceProvider, Func<IServiceProvider, FileUnitHandler<TUnit>>? handlerFactory)
    {
        options ??= new CsvFileOptions();
        options.MaxInputBytes.ThrowIfLessThanOrEqualToZero();
        options.MaxGroupRecords.ThrowIfLessThanOrEqualToZero();
        if (new[] { options.Delimiter, options.Quote }.Concat(options.Comment.HasValue ? [options.Comment.Value] : Array.Empty<char>())
            .Any(c => c is '\r' or '\n' or '\0' || char.IsSurrogate(c))
            || options.Delimiter == options.Quote || options.Comment == options.Delimiter || options.Comment == options.Quote)
            throw new ArgumentException("CSV syntax characters must be distinct non-surrogate characters other than newline or null.", nameof(options));

        _options = options with { Encoding = (Encoding)options.Encoding.ThrowIfNull().Clone() };
        _options.Encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
        if (handlerFactory is not null)
            serviceProvider.ThrowIfNull();

        _validator = validator;
        _serviceProvider = serviceProvider;
        _handlerFactory = handlerFactory;
        _invoker = serviceProvider?.GetService<CsvFileProcessorInvoker>() ?? new CsvFileProcessorInvoker(serviceProvider);
    }

    /// <inheritdoc/>
    public override Task<FileProcessingSummary> ProcessAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, CancellationToken cancellationToken = default)
    {
        openReadAsync.ThrowIfNull();
        context.ThrowIfNull();
        return _invoker.InvokeFileAsync(this, context, ct => ProcessCoreAsync(openReadAsync, context, ct), cancellationToken);
    }

    /// <summary>Processes both passes inside the file-level instrumentation boundary.</summary>
    private async Task<FileProcessingSummary> ProcessCoreAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, CancellationToken cancellationToken)
    {
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
            var rejectedOutcome = context.Mode == FileProcessingMode.ValidateAndReport ? FileProcessingOutcome.ReportedWithErrors : FileProcessingOutcome.Rejected;
            var rejected = new FileProcessingSummary(context, rejectedOutcome, state.Snapshot(), validationCompleted);
            await OnRejectedAsync(context, rejected, cancellationToken).ConfigureAwait(false);
            return rejected;
        }

        if (context.Mode == FileProcessingMode.ValidateOnly)
        {
            var validated = new FileProcessingSummary(context, FileProcessingOutcome.Validated, state.Snapshot(), true);
            await OnCompletedAsync(context, validated, cancellationToken).ConfigureAwait(false);
            return validated;
        }

        if (context.Mode == FileProcessingMode.ValidateAndReport)
        {
            var reported = new FileProcessingSummary(context, FileProcessingOutcome.Reported, state.Snapshot(), true);
            await OnCompletedAsync(context, reported, cancellationToken).ConfigureAwait(false);
            return reported;
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
    /// <param name="validationContext">The original context whose identity and policies must be preserved.</param>
    /// <returns>A distinct replay context.</returns>
    protected virtual FileProcessingContext OnCreateExecutionContext(FileProcessingContext validationContext) => new(validationContext.Identity, validationContext.Mode, validationContext.ErrorMode);

    /// <summary>Configures trusted unit identity before business-service resolution; no state is copied implicitly.</summary>
    /// <param name="fileContext">The pass-local file context.</param>
    /// <param name="unit">The input unit, not an authenticated identity.</param>
    /// <param name="parent">The file ambient context, when present.</param>
    /// <param name="unitContext">The fresh registered context.</param>
    protected virtual void OnConfigureUnitContext(FileProcessingContext fileContext, TUnit unit, ExecutionContext? parent, ExecutionContext unitContext) { }

    /// <summary>Reports one fully validated unit in ValidateAndReport mode; the awaited hook is never called for invalid units.</summary>
    /// <param name="context">The file invocation context under its file-level ambient execution context.</param>
    /// <param name="unit">The mapped or projected unit that passed every unit-level validation check.</param>
    /// <param name="info">The unit's physical/logical location and optional route type.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous report operation; operational faults and cancellation propagate.</returns>
    protected virtual Task OnValidatedUnitAsync(FileProcessingContext context, TUnit unit, FileUnitInfo info, CancellationToken cancellationToken)
        => throw new NotSupportedException("ValidateAndReport requires a processor overriding OnValidatedUnitAsync.");

    /// <summary>Gets the maximum parser records retained or inspected in one root group.</summary>
    private protected long MaxGroupRecords => _options.MaxGroupRecords;

    /// <summary>Reads or reconstructs one pass with format-specific mapping and assembly.</summary>
    private protected abstract Task<bool> ReadPassAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, ProcessingState state, bool execute, CancellationToken cancellationToken);

    /// <summary>Owns one asynchronous input and parser, exposing only the current buffered record.</summary>
    private protected async IAsyncEnumerable<CsvParsedRecord> ReadRecordsAsync(Func<CancellationToken, Task<Stream>> openReadAsync, ProcessingState state, bool execute,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
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
        }, _options.MaxInputBytes);
        var encodingDiagnostic = await countingInput.InitializeAsync(_options.Encoding, cancellationToken).ConfigureAwait(false);

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

        while (true)
        {
            var read = false;
            if (encodingDiagnostic is null && !countingInput.LimitExceeded)
            {
                try
                {
                    read = await parser.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DecoderFallbackException)
                {
                    encodingDiagnostic = new(FileDiagnosticCategory.Parsing, "csv.encoding.invalid", "Input bytes are invalid for the configured encoding.");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (countingInput.LimitExceeded || encodingDiagnostic is not null)
            {
                var diagnostic = countingInput.LimitExceeded
                    ? new FileDiagnostic(FileDiagnosticCategory.Limit, "csv.limit.input-bytes", "The input exceeds its configured byte limit.")
                    : encodingDiagnostic;

                yield return new(parser, diagnostic, true);
                yield break;
            }

            if (!read)
                yield break;

            yield return new(parser, syntaxError);
            syntaxError = null;
        }
    }

    /// <summary>Validates an accepted unit, counting physical invalidity only for a flat row.</summary>
    private protected async Task<bool> ValidateUnitAsync(TUnit unit, FileProcessingContext context, ProcessingState state, FileLocation location, bool physicalRecord, string? recordType, CancellationToken cancellationToken)
    {
        var invalid = false;
        async Task ReportAsync(FileDiagnostic diagnostic, CancellationToken cancellationToken)
        {
            diagnostic.ThrowIfNull();
            if (diagnostic.IsError && !invalid)
            {
                invalid = true;
                if (physicalRecord)
                    state.Invalid = checked(state.Invalid + 1);
            }

            var located = new FileDiagnostic(diagnostic.Category, diagnostic.Code, diagnostic.Message, diagnostic.Severity,
                diagnostic.Location ?? location, diagnostic.RecordType ?? recordType);
            await state.ReportAsync(located, cancellationToken).ConfigureAwait(false);
        }

        if (_validator is not null)
        {
            var validation = await _validator.ValidateAsync(unit, cancellationToken).ConfigureAwait(false);
            if (validation.Messages is not null)
                foreach (var message in validation.Messages)
                    await ReportAsync(new(FileDiagnosticCategory.Validation, "csv.validation", message.Text ?? LText.Empty, message.Type,
                        new(location.LineNumber, propertyPath: message.Property), recordType), cancellationToken).ConfigureAwait(false);

            if (validation.HasErrors && !invalid)
                throw new InvalidOperationException("The validator reported errors without error messages.");
        }

        if (!invalid && _handlerFactory is not null)
            await ExecutionContext.RunScopedAsync(_serviceProvider.ThrowIfNull(), (unitContext, ct) =>
                _handlerFactory(unitContext.ServiceProvider.ThrowIfNull()).ThrowIfNull().ValidateAsync(unit, context, ReportAsync, ct),
                (parent, unitContext) => OnConfigureUnitContext(context, unit, parent, unitContext), cancellationToken).ConfigureAwait(false);

        if (!invalid)
        {
            state.Validated = checked(state.Validated + 1);

            if (context.Mode == FileProcessingMode.ValidateAndReport)
            {
                state.Update();
                await OnValidatedUnitAsync(context, unit, new(location, recordType), cancellationToken).ConfigureAwait(false);
                state.Reported = checked(state.Reported + 1);
            }
        }

        state.Update();
        return !invalid;
    }

    /// <summary>Executes one reconstructed unit and preserves the original application Result.</summary>
    private protected async Task<bool> ExecuteUnitAsync(TUnit unit, FileProcessingContext context, ProcessingState state, CancellationToken cancellationToken)
    {
        if (state.Attempted >= state.Validated)
            throw new InvalidDataException("Replay contained more units than the accepted input.");

        state.Attempted = checked(state.Attempted + 1);
        state.Update();
        context.Progress = state.Snapshot();
        state.ExecutionResult = await ExecutionContext.RunScopedAsync(_serviceProvider.ThrowIfNull(), (unitContext, ct) =>
            _handlerFactory.ThrowIfNull()(unitContext.ServiceProvider.ThrowIfNull()).ThrowIfNull().ExecuteAsync(unit, context, ct),
            (parent, unitContext) => OnConfigureUnitContext(context, unit, parent, unitContext), cancellationToken).ConfigureAwait(false);
        if (state.ExecutionResult.IsFailure)
            return false;

        state.Executed = checked(state.Executed + 1);
        state.Update();
        context.Progress = state.Snapshot();
        return true;
    }

    /// <summary>Contains invocation-local counters and file-level callback context, never diagnostic history.</summary>
    private protected sealed class ProcessingState(CsvFileProcessorBase<TUnit> processor, FileProcessingContext context)
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

        /// <summary>Gets or sets the number of successfully completed validated-unit reporting hooks.</summary>
        public long Reported { get; set; }

        /// <summary>Gets or sets the original execution Result.</summary>
        public Result ExecutionResult { get; set; }

        /// <summary>Creates an immutable progress snapshot.</summary>
        public FileProcessingProgress Snapshot() => new(Bytes, Records, Invalid, Validated, Attempted, Executed, Errors, Warnings, Information, Reported);

        /// <summary>Updates caller-visible partial progress before callbacks and faults.</summary>
        public void Update() => context.Progress = Snapshot();

        /// <summary>Counts and awaits one diagnostic under the original file ambient context.</summary>
        public Task ReportAsync(FileDiagnostic diagnostic, CancellationToken cancellationToken)
        {
            diagnostic.ThrowIfNull();
            cancellationToken.ThrowIfCancellationRequested();
            if (diagnostic.IsError && context is CsvHierarchyContext hierarchyContext)
                hierarchyContext.HasValidationErrors = true;

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
