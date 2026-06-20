namespace Contoso.Shopping.Contracts;

public partial class DiscountCoupon
{
    public decimal DiscountPercentage { get; init => field = value.ThrowIfLessThanOrEqualToZero("Discount percentage cannot be less than or equal to zero.").ThrowWhen(value => value > 100, "Discount percentage cannot be greater than 100."); }
}