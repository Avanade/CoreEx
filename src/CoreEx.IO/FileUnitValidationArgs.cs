namespace CoreEx.IO;

/// <summary>Provides a business unit and an awaited diagnostic sink to scoped validation hooks.</summary>
/// <typeparam name="TUnit">The typed business unit.</typeparam>
public sealed class FileUnitValidationArgs<TUnit> where TUnit : class
{
    /// <summary>Initializes framework-owned validation arguments.</summary>
    internal FileUnitValidationArgs(TUnit unit, FileUnitInfo info, FileProcessingContext context, Func<FileDiagnostic, CancellationToken, Task> reportDiagnosticAsync)
        => (Unit, Info, Context, ReportDiagnosticAsync) = (unit.ThrowIfNull(), info.ThrowIfNull(), context.ThrowIfNull(), reportDiagnosticAsync.ThrowIfNull());

    /// <summary>Gets the mapped business unit.</summary>
    public TUnit Unit { get; }

    /// <summary>Gets the unit's source provenance.</summary>
    public FileUnitInfo Info { get; }

    /// <summary>Gets the validation-pass file context.</summary>
    public FileProcessingContext Context { get; }

    /// <summary>Gets the diagnostic sink; await every emission and forward its cancellation token.</summary>
    public Func<FileDiagnostic, CancellationToken, Task> ReportDiagnosticAsync { get; }

    /// <summary>Creates typed arguments for dispatching the same unit to a record-specific validation hook.</summary>
    /// <typeparam name="TRecord">The concrete record type.</typeparam>
    /// <returns>Arguments preserving the same unit, provenance, context, and diagnostic sink.</returns>
    /// <exception cref="InvalidOperationException">The unit is not of the requested type.</exception>
    public FileUnitValidationArgs<TRecord> As<TRecord>() where TRecord : class
        => Unit is TRecord record ? new(record, Info, Context, ReportDiagnosticAsync) : throw new InvalidOperationException("The business unit does not match the requested record type.");
}
