namespace CoreEx.IO;

/// <summary>Defines scoped custom validation and execution for a flat row or assembled business root group.</summary>
/// <typeparam name="TUnit">The typed business execution unit, not necessarily a physical record.</typeparam>
/// <remarks>Resolve a fresh handler in each applicable unit scope. Store file-wide accumulators in the explicit context, not handler fields. The coordinator owns diagnostics, lifecycle, and whole-file acceptance.</remarks>
public abstract class FileUnitHandler<TUnit> where TUnit : class
{
    /// <summary>Invokes custom unit validation without creating exceptions for content errors.</summary>
    /// <param name="unit">The mapped flat row or structurally accepted root group.</param>
    /// <param name="info">The unit's provenance.</param>
    /// <param name="context">The validation-pass context.</param>
    /// <param name="reportDiagnosticAsync">The awaited diagnostic sink; the coordinator applies locations, counts, and continuation policy.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous validation operation; operational faults propagate.</returns>
    internal Task ValidateAsync(TUnit unit, FileUnitInfo info, FileProcessingContext context, Func<FileDiagnostic, CancellationToken, Task> reportDiagnosticAsync, CancellationToken cancellationToken = default)
    {
        unit.ThrowIfNull();
        info.ThrowIfNull();
        context.ThrowIfNull();
        reportDiagnosticAsync.ThrowIfNull();
        cancellationToken.ThrowIfCancellationRequested();
        return OnValidateAsync(new(unit, info, context, reportDiagnosticAsync), cancellationToken);
    }

    /// <summary>Invokes business execution after unit validation in single-pass mode, or after whole-file acceptance on replay.</summary>
    /// <param name="unit">The validated flat row or assembled root group, reconstructed on replay.</param>
    /// <param name="info">The unit's provenance.</param>
    /// <param name="context">The pass-local context; single-pass execution shares validation state, while replay uses fresh state.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The application's Result, preserved without throw/catch conversion. Failure stops subsequent units without rollback.</returns>
    internal Task<Result> ExecuteAsync(TUnit unit, FileUnitInfo info, FileProcessingContext context, CancellationToken cancellationToken = default)
    {
        unit.ThrowIfNull();
        info.ThrowIfNull();
        context.ThrowIfNull();
        cancellationToken.ThrowIfCancellationRequested();
        return OnExecuteAsync(new(unit, info, context), cancellationToken);
    }

    /// <summary>Overrides custom unit validation; the default adds no rules and performs no writes.</summary>
    /// <param name="args">The unit, provenance, validation-pass context, and awaited diagnostic sink.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous validation operation.</returns>
    protected virtual Task OnValidateAsync(FileUnitValidationArgs<TUnit> args, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Overrides business execution; the default throws rather than silently pretending to execute.</summary>
    /// <param name="args">The unit, provenance, and pass-local context.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The business operation's Result.</returns>
    protected virtual Task<Result> OnExecuteAsync(FileUnitArgs<TUnit> args, CancellationToken cancellationToken) => throw new NotSupportedException("Execution requires a handler overriding OnExecuteAsync.");
}
