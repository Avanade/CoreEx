namespace CoreEx.IO;

/// <summary>Coordinates shared file validation, reporting, scoped unit execution, and replay independently of format parsing.</summary>
/// <typeparam name="TUnit">The flat record or assembled business unit.</typeparam>
public abstract class FileProcessor<TUnit> : FileProcessor where TUnit : class
{
    private readonly FileProcessingOptions _processingOptions;
    private readonly IValidator<TUnit>? _validator;
    private readonly IServiceProvider? _serviceProvider;
    private readonly Func<IServiceProvider, FileUnitHandler<TUnit>>? _handlerFactory;
    private readonly FileProcessorInvoker _invoker;

    /// <summary>Initializes common processing budgets, validation, and scoped business handlers.</summary>
    /// <param name="processingOptions">Format-neutral processing budgets.</param>
    /// <param name="validator">A pure unit validator; dependency-backed validation belongs in the scoped handler.</param>
    /// <param name="serviceProvider">The provider from which unit scopes are created.</param>
    /// <param name="handlerFactory">Resolves the unit handler after trusted context configuration.</param>
    protected FileProcessor(FileProcessingOptions? processingOptions, IValidator<TUnit>? validator, IServiceProvider? serviceProvider, Func<IServiceProvider, FileUnitHandler<TUnit>>? handlerFactory)
    {
        processingOptions ??= new FileProcessingOptions();
        processingOptions.MaxInputBytes.ThrowIfLessThanOrEqualToZero();
        processingOptions.MaxUnits.ThrowIfLessThanOrEqualToZero();
        processingOptions.MaxDiagnostics.ThrowIfLessThanOrEqualToZero();
        if (handlerFactory is not null)
            serviceProvider.ThrowIfNull();

        _processingOptions = processingOptions;
        _validator = validator;
        _serviceProvider = serviceProvider;
        _handlerFactory = handlerFactory;
        _invoker = serviceProvider?.GetService<FileProcessorInvoker>() ?? new FileProcessorInvoker(serviceProvider);
    }

    /// <summary>Gets the provider supplied when constructing this processor.</summary>
    /// <remarks>Its lifetime follows the processor's registration; it is not the fresh unit scope. Resolve unit business dependencies through the handler factory instead.</remarks>
    /// <exception cref="InvalidOperationException">The processor was constructed without a service provider.</exception>
    protected IServiceProvider Services => _serviceProvider ?? throw new InvalidOperationException("This processor was constructed without a service provider.");

    /// <summary>Opens, validates, optionally observes validated units, and executes according to the requested mode.</summary>
    /// <inheritdoc/>
    public override Task<FileProcessingSummary> ProcessAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, CancellationToken cancellationToken = default)
    {
        openReadAsync.ThrowIfNull();
        context.ThrowIfNull();
        return _invoker.InvokeFileAsync(this, context, ct => ProcessCoreAsync(openReadAsync, context, ct), cancellationToken);
    }

    /// <summary>Processes the validation pass and, only after acceptance, optional replay.</summary>
    private async Task<FileProcessingSummary> ProcessCoreAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if ((context.Mode is FileProcessingMode.ValidateThenExecute or FileProcessingMode.ValidateAndExecute) && _handlerFactory is null)
            throw new InvalidOperationException("Execution requires a scoped unit handler factory.");

        var state = new ProcessingState(this, context);
        context.Progress = state.Snapshot();
        var singlePassExecution = context.Mode == FileProcessingMode.ValidateAndExecute;

        var validationCompleted = await ReadPassAsync(openReadAsync, context, state, singlePassExecution, cancellationToken).ConfigureAwait(false);
        if (validationCompleted)
            await OnValidateFileAsync(new(context, state.ReportAsync), cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        if (state.Errors > 0 && !state.ExecutionResult.IsFailure)
        {
            var failed = new FileProcessingSummary(context, FileProcessingOutcome.ContentFailed, state.Snapshot(), validationCompleted);
            await OnContentFailedAsync(new(context, failed), cancellationToken).ConfigureAwait(false);
            return failed;
        }

        if (context.Mode == FileProcessingMode.ValidateOnly)
        {
            context.FreezeAcceptedMetadata();
            var validated = new FileProcessingSummary(context, FileProcessingOutcome.Succeeded, state.Snapshot(), true);
            await OnCompletedAsync(new(context, validated), cancellationToken).ConfigureAwait(false);
            return validated;
        }

        if (context.Mode == FileProcessingMode.ValidateThenExecute)
        {
            context.FreezeAcceptedMetadata();
            var executionContext = OnCreateExecutionContext(context).ThrowIfNull();
            if (ReferenceEquals(executionContext, context) || !ReferenceEquals(executionContext.Identity, context.Identity)
                || executionContext.Mode != context.Mode || executionContext.ErrorMode != context.ErrorMode)
                throw new InvalidOperationException("Replay requires a fresh context preserving the original identity and policy.");

            executionContext.SetAcceptedMetadata(context.AcceptedMetadata);
            await ReadPassAsync(openReadAsync, executionContext, state, true, cancellationToken).ConfigureAwait(false);
        }

        var outcome = state.ExecutionResult.IsFailure ? FileProcessingOutcome.ExecutionFailed : FileProcessingOutcome.Succeeded;
        if (state.ExecutionResult.IsFailure)
            await OnExecutionFailedAsync(new(context, state.FailedInfo.ThrowIfNull(), state.ExecutionResult), cancellationToken).ConfigureAwait(false);

        var summary = new FileProcessingSummary(context, outcome, state.Snapshot(), validationCompleted, state.ExecutionResult);
        if (summary.IsSuccess)
        {
            if (context.Mode == FileProcessingMode.ValidateAndExecute)
                context.FreezeAcceptedMetadata();

            await OnCompletedAsync(new(context, summary), cancellationToken).ConfigureAwait(false);
        }

        return summary;
    }

    /// <summary>Creates fresh execution-pass reconciliation state without copying validation accumulators.</summary>
    /// <param name="validationContext">The original context whose identity and policies must be preserved.</param>
    /// <returns>A distinct replay context.</returns>
    protected virtual FileProcessingContext OnCreateExecutionContext(FileProcessingContext validationContext) => new(validationContext.Identity, validationContext.Mode, validationContext.ErrorMode);

    /// <summary>Configures trusted unit identity before business-service resolution.</summary>
    /// <param name="args">The unit, provenance, pass-local file state, and ambient contexts.</param>
    protected virtual void OnConfigureUnitContext(FileUnitContextArgs<TUnit> args) { }

    /// <summary>Optionally observes or reports one fully validated unit before the next unit is read.</summary>
    /// <param name="args">The validated unit, provenance, and file context.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous observation/reporting operation; operational faults and cancellation propagate.</returns>
    protected virtual Task OnValidatedUnitAsync(FileUnitArgs<TUnit> args, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>Gets the maximum source bytes per pass.</summary>
    private protected long MaxInputBytes => _processingOptions.MaxInputBytes;

    /// <summary>Gets the maximum candidate business units per file.</summary>
    private protected long MaxUnits => _processingOptions.MaxUnits;

    /// <summary>Reads or reconstructs one pass with format-specific parsing and assembly.</summary>
    private protected abstract Task<bool> ReadPassAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, ProcessingState state, bool execute, CancellationToken cancellationToken);

    /// <summary>Validates one mapped unit and records its provenance and accounting.</summary>
    private protected async Task<bool> ValidateUnitAsync(TUnit unit, FileProcessingContext context, ProcessingState state, FileUnitInfo info, bool physicalRecord, CancellationToken cancellationToken)
    {
        var invalid = false;
        async Task ReportAsync(FileDiagnostic diagnostic, CancellationToken cancellationToken)
        {
            diagnostic.ThrowIfNull();
            if (diagnostic.IsError && !invalid)
            {
                invalid = true;
                if (physicalRecord)
                    state.InvalidRecords = checked(state.InvalidRecords + 1);
            }

            var located = new FileDiagnostic(diagnostic.Category, diagnostic.Code, diagnostic.Message, diagnostic.Severity, diagnostic.Location ?? info.Location, diagnostic.RecordType ?? info.RecordType, info.Ordinal);
            await state.ReportAsync(located, cancellationToken).ConfigureAwait(false);
        }

        if (_validator is not null)
        {
            var validation = await _validator.ValidateAsync(unit, cancellationToken).ConfigureAwait(false);
            if (validation.Messages is not null)
                foreach (var message in validation.Messages)
                    await ReportAsync(new(FileDiagnosticCategory.Validation, "file.validation", message.Text ?? LText.Empty, message.Type, new(info.Location.LineNumber, propertyPath: message.Property), info.RecordType), cancellationToken).ConfigureAwait(false);

            if (validation.HasErrors && !invalid)
                throw new InvalidOperationException("The validator reported errors without error messages.");
        }

        if (!invalid && !state.LimitExceeded && _handlerFactory is not null)
            await ExecutionContext.RunScopedAsync(_serviceProvider.ThrowIfNull(), (unitContext, ct) =>
                _handlerFactory(unitContext.ServiceProvider.ThrowIfNull()).ThrowIfNull().ValidateAsync(unit, info, context, ReportAsync, ct),
                (parent, unitContext) => OnConfigureUnitContext(new(unit, info, context, parent, unitContext)), cancellationToken).ConfigureAwait(false);

        invalid |= state.LimitExceeded;
        if (invalid)
            state.InvalidUnits = checked(state.InvalidUnits + 1);
        else
            state.Validated = checked(state.Validated + 1);

        state.Update();
        if (!invalid)
            await OnValidatedUnitAsync(new(unit, info, context), cancellationToken).ConfigureAwait(false);

        return !invalid;
    }

    /// <summary>Executes one complete unit in a fresh scope and preserves the original application Result.</summary>
    private protected async Task<bool> ExecuteUnitAsync(TUnit unit, FileProcessingContext context, ProcessingState state, FileUnitInfo info, CancellationToken cancellationToken)
    {
        if (state.Attempted >= state.Validated)
            throw new InvalidDataException("Execution contained more units than validation accepted.");

        state.Attempted = checked(state.Attempted + 1);
        context.Progress = state.Update();
        state.ExecutionResult = await ExecutionContext.RunScopedAsync(_serviceProvider.ThrowIfNull(), (unitContext, ct) =>
            _handlerFactory.ThrowIfNull()(unitContext.ServiceProvider.ThrowIfNull()).ThrowIfNull().ExecuteAsync(unit, info, context, ct),
            (parent, unitContext) => OnConfigureUnitContext(new(unit, info, context, parent, unitContext)), cancellationToken).ConfigureAwait(false);

        if (state.ExecutionResult.IsFailure)
        {
            state.FailedInfo = info;
            return false;
        }

        state.Executed = checked(state.Executed + 1);
        context.Progress = state.Update();
        return true;
    }

    /// <summary>Contains invocation-local counters and callback context, never diagnostic history.</summary>
    private protected sealed class ProcessingState(FileProcessor<TUnit> processor, FileProcessingContext context)
    {
        private readonly System.Threading.ExecutionContext _callbackContext = System.Threading.ExecutionContext.Capture()
            ?? throw new InvalidOperationException("File processing requires execution-context flow to be enabled.");

        /// <summary>Gets or sets the validation bytes read.</summary>
        public long Bytes { get; set; }

        /// <summary>Gets or sets the validation source records read.</summary>
        public long Records { get; set; }

        /// <summary>Gets or sets the candidate business-unit count.</summary>
        public long UnitsRead { get; set; }

        /// <summary>Gets or sets the invalid source-record count.</summary>
        public long InvalidRecords { get; set; }

        /// <summary>Gets or sets the invalid candidate business-unit count.</summary>
        public long InvalidUnits { get; set; }

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

        /// <summary>Gets or sets the original execution result.</summary>
        public Result ExecutionResult { get; set; }

        /// <summary>Gets or sets the failed unit's provenance.</summary>
        public FileUnitInfo? FailedInfo { get; set; }

        /// <summary>Gets whether a terminal shared diagnostic budget was reached.</summary>
        public bool LimitExceeded { get; private set; }

        private long _diagnostics;

        /// <summary>Creates an immutable progress snapshot.</summary>
        public FileProcessingProgress Snapshot() => new(bytesRead: Bytes, recordsRead: Records, invalidRecords: InvalidRecords, unitsRead: UnitsRead, invalidUnits: InvalidUnits,
            unitsValidated: Validated, unitsAttempted: Attempted, unitsExecuted: Executed, errorCount: Errors, warningCount: Warnings, informationCount: Information);

        /// <summary>Updates caller-visible partial progress before callbacks and faults.</summary>
        /// <returns>The published snapshot, also usable by the replay context.</returns>
        public FileProcessingProgress Update() => context.Progress = Snapshot();

        /// <summary>Counts and awaits one diagnostic under the original file ambient context.</summary>
        public Task ReportAsync(FileDiagnostic diagnostic, CancellationToken cancellationToken)
        {
            diagnostic.ThrowIfNull();
            cancellationToken.ThrowIfCancellationRequested();
            if (LimitExceeded)
                return Task.CompletedTask;

            if (_diagnostics >= processor._processingOptions.MaxDiagnostics && diagnostic.Category != FileDiagnosticCategory.Limit)
            {
                diagnostic = new(FileDiagnosticCategory.Limit, "file.limit.diagnostics", "The file exceeds its configured diagnostic limit.", location: diagnostic.Location, recordType: diagnostic.RecordType, unitOrdinal: diagnostic.UnitOrdinal);
                LimitExceeded = true;
            }
            else if (diagnostic.Category != FileDiagnosticCategory.Limit)
                _diagnostics = checked(_diagnostics + 1);

            switch (diagnostic.Severity)
            {
                case MessageType.Error: Errors = checked(Errors + 1); break;
                case MessageType.Warning: Warnings = checked(Warnings + 1); break;
                case MessageType.Info: Information = checked(Information + 1); break;
            }

            Update();
            Task? task = null;
            System.Threading.ExecutionContext.Run(_callbackContext, _ => task = processor.OnDiagnosticAsync(new(context, diagnostic), cancellationToken), null);
            return task ?? throw new InvalidOperationException("The diagnostic hook returned a null task.");
        }
    }
}
