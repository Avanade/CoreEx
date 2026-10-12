namespace Contoso.Shopping.Contracts;

public class BasketItemAddRequest
{
    [NonNullable]
    public string? ProductId { get; set; }

    public decimal Quantity { get; set; }
}