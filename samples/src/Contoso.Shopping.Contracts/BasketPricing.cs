namespace Contoso.Shopping.Contracts;

[Contract]
public partial class BasketPricing
{
    [ReadOnly(true)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public decimal SubTotal { get; set; }

    [ReadOnly(true)]
    [ReferenceData<DiscountCoupon>]
    public partial string? DiscountCouponCode { get; set; }

    [ReadOnly(true)]
    public decimal DiscountPercentage { get; set; }

    [ReadOnly(true)]
    public decimal DiscountAmount { get; set; }

    [ReadOnly(true)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public decimal Total { get; set; }
}