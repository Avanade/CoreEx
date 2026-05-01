namespace Contoso.Orders.Infrastructure.Repositories;

public class OrdersOutboxPublisher(SqlServerDatabase database, IDestinationProvider? destinationProvider = null, IEventFormatter? formatter = null, ILogger<OrdersOutboxPublisher>? logger = null)
    : SqlServerOutboxPublisher(database, destinationProvider, formatter, logger)
{
    public override SqlStatement Statement { get; set; } = SqlStatement.StoredProcedure("[Orders].[spOutboxEnqueue]");
}