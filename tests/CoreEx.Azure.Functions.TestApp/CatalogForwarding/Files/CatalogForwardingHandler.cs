namespace CoreEx.Azure.Functions.TestApp.CatalogForwarding;

/// <summary>Forwards one validated row using a publisher from the row's scope.</summary>
[ScopedService]
public partial class CatalogForwardingHandler([FromKeyedServices(ServiceBusPublisher.DefaultServiceKey)] IEventPublisher publisher) : FileUnitHandler<CatalogForwardingItem>
{
    /// <inheritdoc/>
    protected override async Task<Result> OnExecuteAsync(FileUnitArgs<CatalogForwardingItem> args, CancellationToken cancellationToken)
    {
        var identity = args.Context.Identity;
        var id = IdentifierGenerator.FromParts(CatalogItemUpsert.CloudEventType, identity.SourceId, identity.SourceRevision, args.Info.Ordinal.ToString(CultureInfo.InvariantCulture));

        publisher.Add(CatalogItemUpsert.Queue, new CatalogItemUpsert(id, args.Unit.Sku ?? throw new InvalidOperationException("A validated SKU is required."),
            args.Unit.UnitPrice ?? throw new InvalidOperationException("A validated unit price is required.")).CreateEvent());
        await publisher.PublishAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success;
    }
}
