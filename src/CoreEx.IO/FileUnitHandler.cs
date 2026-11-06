namespace CoreEx.IO;

/// <summary>Defines scoped custom validation and execution for a flat row or assembled business root group.</summary>
/// <typeparam name="TUnit">The typed business execution unit, not necessarily a physical record.</typeparam>
/// <remarks>Resolve a fresh handler in each applicable unit scope. Store file-wide accumulators in the explicit context, not handler fields. The coordinator owns diagnostics, lifecycle, and whole-file acceptance.</remarks>
public abstract class FileUnitHandler<TUnit> where TUnit : class
{
    /// <summary>Invokes custom unit validation without creating exceptions for content errors.</summary>
    /// <param name="unit">The mapped flat row or structurally accepted root group.</param>
    /// <param name="context">The validation-pass context.</param>
    /// <param name="reportDiagnosticAsync">The awaited diagnostic sink; the coordinator applies locations, counts, and continuation policy.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous validation operation; operational faults propagate.</returns>
    public Task ValidateAsync(TUnit unit, FileProcessingContext context, Func<FileDiagnostic, CancellationToken, Task> reportDiagnosticAsync, CancellationToken cancellationToken = default)
    {
        unit.ThrowIfNull();
        context.ThrowIfNull();
        reportDiagnosticAsync.ThrowIfNull();
        cancellationToken.ThrowIfCancellationRequested();
        return OnValidateAsync(unit, context, reportDiagnosticAsync, cancellationToken);
    }

    /// <summary>Invokes execution for a rebuilt unit only after whole-file acceptance.</summary>
    /// <param name="unit">The fresh execution-pass flat row or assembled root group.</param>
    /// <param name="context">The execution-pass context, without mutable validation accumulators.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The application's Result, preserved without throw/catch conversion. Failure stops subsequent units without rollback.</returns>
    public Task<Result> ExecuteAsync(TUnit unit, FileProcessingContext context, CancellationToken cancellationToken = default)
    {
        unit.ThrowIfNull();
        context.ThrowIfNull();
        cancellationToken.ThrowIfCancellationRequested();
        return OnExecuteAsync(unit, context, cancellationToken);
    }

    /// <summary>Overrides custom unit validation; the default adds no rules and performs no writes.</summary>
    /// <param name="unit">The typed unit.</param>
    /// <param name="context">The validation-pass context.</param>
    /// <param name="reportDiagnosticAsync">The diagnostic sink; await every emission and forward its token.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous validation operation.</returns>
    protected virtual Task OnValidateAsync(TUnit unit, FileProcessingContext context, Func<FileDiagnostic, CancellationToken, Task> reportDiagnosticAsync, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Overrides business execution; the default throws rather than silently pretending to execute.</summary>
    /// <param name="unit">The typed unit.</param>
    /// <param name="context">The execution-pass context.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The business operation's Result.</returns>
    protected virtual Task<Result> OnExecuteAsync(TUnit unit, FileProcessingContext context, CancellationToken cancellationToken) => throw new NotSupportedException("Execution requires a handler overriding OnExecuteAsync.");
}
