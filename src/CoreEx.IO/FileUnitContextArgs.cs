namespace CoreEx.IO;

/// <summary>Provides trusted context configuration before scoped unit services are resolved.</summary>
/// <typeparam name="TUnit">The typed business unit, not an authenticated identity.</typeparam>
public sealed class FileUnitContextArgs<TUnit> where TUnit : class
{
    /// <summary>Initializes framework-owned scope-configuration arguments.</summary>
    internal FileUnitContextArgs(TUnit unit, FileUnitInfo info, FileProcessingContext context, ExecutionContext? parent, ExecutionContext unitContext)
        => (Unit, Info, Context, Parent, UnitContext) = (unit.ThrowIfNull(), info.ThrowIfNull(), context.ThrowIfNull(), parent, unitContext.ThrowIfNull());

    /// <summary>Gets the input business unit, never an authenticated identity.</summary>
    public TUnit Unit { get; }

    /// <summary>Gets the unit's source provenance.</summary>
    public FileUnitInfo Info { get; }

    /// <summary>Gets the pass-local file context.</summary>
    public FileProcessingContext Context { get; }

    /// <summary>Gets the file's parent ambient context, when present.</summary>
    public ExecutionContext? Parent { get; }

    /// <summary>Gets the fresh registered context to configure before handler resolution.</summary>
    public ExecutionContext UnitContext { get; }
}
