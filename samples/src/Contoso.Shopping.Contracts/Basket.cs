namespace Contoso.Shopping.Contracts;

[Contract]
public partial class Basket : IIdentifier<string?>, IChangeLog, IETag
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    public string? CustomerId { get; set; }

    [ReferenceData<BasketStatus>]
    public partial string? StatusCode { get; set; }

    [ReadOnly(true)]
    public List<BasketItem>? Items { get; set; }

    public BasketPricing? Pricing { get; set; }

    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }

    [ReadOnly(true)]
    public string? ETag { get; set; }
}