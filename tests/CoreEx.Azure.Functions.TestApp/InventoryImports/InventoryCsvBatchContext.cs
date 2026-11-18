namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

public partial class InventoryCsvBatchContext
{
    /// <summary>Gets reconciliation state owned by this pass, not copied to replay.</summary>
    public InventoryBatchTotals Totals { get; } = new();
}
