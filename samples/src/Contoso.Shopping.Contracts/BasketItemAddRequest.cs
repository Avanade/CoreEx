namespace Contoso.Shopping.Contracts;

public class BasketItemAddRequest
{
    public string? ProductId { get; set; }

    public decimal Quantity { get; set; }
}