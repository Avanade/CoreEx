namespace Contoso.Shopping.Contracts;

[Contract]
public partial class BasketItem : IIdentifier<string?>, IETag
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    [ReadOnly(true)]
    public string? ProductId { get; set; }

    [ReadOnly(true)]
    public string? Sku { get; set; }

    [ReadOnly(true)]
    public string? Text { get; set; }

    public decimal? Quantity { get; set; }

    [ReadOnly(true)]
    [ReferenceData<UnitOfMeasure>]
    public partial string? UnitOfMeasureCode { get; set; }

    [ReadOnly(true)]
    public decimal? UnitPrice { get; set; }

    [ReadOnly(true)]
    public decimal? Total { get; set; }

    [ReadOnly(true)]
    public string? ETag { get; set; }
}