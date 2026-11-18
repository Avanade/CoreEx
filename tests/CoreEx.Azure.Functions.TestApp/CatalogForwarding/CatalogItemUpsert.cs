namespace CoreEx.Azure.Functions.TestApp.CatalogForwarding;

/// <summary>The version-one catalog forwarding command.</summary>
/// <param name="CommandId">Stable identity of the source revision and row.</param>
/// <param name="Sku">Supplier SKU.</param>
/// <param name="UnitPrice">Unit price, including zero.</param>
public sealed record CatalogItemUpsert(string CommandId, string Sku, [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] decimal UnitPrice)
{
    /// <summary>The receiving queue.</summary>
    public const string Queue = "catalog-item-upserts";

    /// <summary>The versioned CloudEvent type and command ID namespace.</summary>
    public const string CloudEventType = "northstar.catalog.item.upsert.v1";

    /// <summary>Creates the command with the externally agreed CloudEvent identity and routing.</summary>
    public EventData CreateEvent()
        => EventData.CreateCommandWith("catalog", this, "upsert")
            .WithSource(new Uri("urn:northstar:supplier:catalog"))
            .WithTitle(CloudEventType)
            .WithPartitionKey(Sku)
            .WithId(CommandId);
}
