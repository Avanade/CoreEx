namespace CoreEx.IO.Test.Unit.Files.OrdersJsonl;

/// <summary>Implements integration-specific validation and execution in a fresh business-unit scope.</summary>
/// <remarks>This create-only starter delegates business execution, transaction boundaries, and durable deduplication to the application.</remarks>
[ScopedService]
public partial class OrdersJsonlHandler : FileUnitHandler<OrdersJsonlOrder>
{
    private readonly IOrderImportApplication _application;

    /// <summary>Initializes the application dependency in the fresh unit scope.</summary>
    public OrdersJsonlHandler(IOrderImportApplication application) => _application = application.ThrowIfNull();

    /// <inheritdoc/>
    protected override Task<Result> OnExecuteAsync(FileUnitArgs<OrdersJsonlOrder> args, CancellationToken cancellationToken)
        => _application.ImportAsync(args.Unit, args.Context.Identity, args.Info, cancellationToken);
}
