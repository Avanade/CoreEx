namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Retains only pass-local counts and quantities for EOF reconciliation.</summary>
public sealed class InventoryBatchTotals
{
    private long _count;
    private long _quantity;

    /// <summary>Adds one individually validated business unit, never a header or trailer.</summary>
    public void Add(int quantity)
    {
        _count = checked(_count + 1);
        _quantity = checked(_quantity + quantity);
    }

    /// <summary>Reports a trailer mismatch without treating invalid input as trustworthy totals.</summary>
    public Task ReconcileAsync(FileValidationArgs args, int? expectedCount, long? expectedQuantity, CancellationToken cancellationToken)
        => args.Context.Progress.ErrorCount != 0 || expectedCount == _count && expectedQuantity == _quantity
            ? Task.CompletedTask
            : args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Reconciliation, "inventory.trailer.mismatch",
                "The trailer count or quantity total does not match the validated adjustments."), cancellationToken);

    /// <summary>Projects validated child quantities into the application's single-quantity command.</summary>
    public static int Sum(IEnumerable<int?> quantities)
        => quantities.Sum(x => x ?? throw new InvalidOperationException("Validated adjustment lines require a quantity."));

    /// <summary>Rejects a group that cannot fit the application's integer quantity before replay.</summary>
    public static Task ValidateAsync<TUnit>(IEnumerable<int?> quantities, FileUnitValidationArgs<TUnit> args, CancellationToken cancellationToken) where TUnit : class
        => quantities.Sum(x => (long)(x ?? throw new InvalidOperationException("Validated adjustment lines require a quantity."))) <= int.MaxValue
            ? Task.CompletedTask
            : args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "inventory.quantity.overflow",
                "The combined adjustment quantity exceeds the supported business quantity."), cancellationToken);
}
