namespace CoreEx.IO;

/// <summary>Provides a business unit, its provenance, and pass-local context to execution and observation hooks.</summary>
/// <typeparam name="TUnit">The typed business unit.</typeparam>
public sealed class FileUnitArgs<TUnit> where TUnit : class
{
    /// <summary>Initializes framework-owned unit arguments.</summary>
    internal FileUnitArgs(TUnit unit, FileUnitInfo info, FileProcessingContext context)
        => (Unit, Info, Context) = (unit.ThrowIfNull(), info.ThrowIfNull(), context.ThrowIfNull());

    /// <summary>Gets the mapped or reconstructed business unit.</summary>
    public TUnit Unit { get; }

    /// <summary>Gets the unit's source provenance.</summary>
    public FileUnitInfo Info { get; }

    /// <summary>Gets the pass-local file context.</summary>
    public FileProcessingContext Context { get; }

    /// <summary>Creates typed arguments for dispatching the same unit to a record-specific hook.</summary>
    /// <typeparam name="TRecord">The concrete record type.</typeparam>
    /// <returns>Arguments preserving the same unit, provenance, and context.</returns>
    /// <exception cref="InvalidOperationException">The unit is not of the requested type.</exception>
    public FileUnitArgs<TRecord> As<TRecord>() where TRecord : class
        => Unit is TRecord record ? new(record, Info, Context) : throw new InvalidOperationException("The business unit does not match the requested record type.");
}
