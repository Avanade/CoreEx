namespace Contoso.Products.Infrastructure.Repositories;

public class ProductsOutboxPublisher(SqlServerDatabase database, IDestinationProvider? destinationProvider = null, IEventFormatter? formatter = null, ILogger<ProductsOutboxPublisher>? logger = null)
    : SqlServerOutboxPublisher(database, destinationProvider, formatter, logger)
{
    public override SqlStatement Statement { get; set; } = SqlStatement.StoredProcedure("[Products].[spOutboxEnqueue]");
}