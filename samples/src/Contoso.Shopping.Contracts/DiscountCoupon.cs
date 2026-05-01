namespace Contoso.Shopping.Contracts;

[ReferenceData]
public partial class DiscountCoupon : ReferenceData<DiscountCoupon>
{
    public decimal DiscountPercentage { get; init => field = value.ThrowIfLessThanOrEqualToZero("Discount percentage cannot be less than or equal to zero.").ThrowWhen(value => value > 100, "Discount percentage cannot be greater than 100."); }
}

public class DiscountCouponCollection() : ReferenceDataCollection<DiscountCoupon>(ReferenceDataSortOrder.Code) { }