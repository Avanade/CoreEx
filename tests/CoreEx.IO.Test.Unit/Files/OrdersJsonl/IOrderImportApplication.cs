namespace CoreEx.IO.Test.Unit.Files.OrdersJsonl;

/// <summary>Defines the application-owned order import, outside file parsing and report hooks.</summary>
/// <remarks>A production implementation commits its durable source-revision/unit receipt and business mutation in one transaction. File processing itself provides neither persistence nor exactly-once execution.</remarks>
public interface IOrderImportApplication
{
    /// <summary>Imports one accepted order using the caller-pinned source revision and stable order identity.</summary>
    Task<Result> ImportAsync(OrdersJsonlOrder order, FileProcessingIdentity identity, FileUnitInfo info, CancellationToken cancellationToken);
}
