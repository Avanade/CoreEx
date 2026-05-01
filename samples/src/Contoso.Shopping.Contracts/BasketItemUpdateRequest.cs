namespace Contoso.Shopping.Contracts;

public class BasketItemUpdateRequest : IETag
{
    public decimal Quantity { get; set; }

    public string? ETag { get; set; }
}