namespace Contoso.Shopping.Infrastructure.Repositories;

public class ShoppingOutboxPublisher(SqlServerDatabase database, IDestinationProvider? destinationProvider = null, IEventFormatter? formatter = null, ILogger<ShoppingOutboxPublisher>? logger = null)
    : SqlServerOutboxPublisher(database, destinationProvider, formatter, logger)
{
    public override SqlStatement Statement { get; set; } = SqlStatement.StoredProcedure("[Shopping].[spOutboxEnqueue]");
}