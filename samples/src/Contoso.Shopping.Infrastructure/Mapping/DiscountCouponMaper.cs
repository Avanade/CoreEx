namespace Contoso.Shopping.Infrastructure.Mapping;

internal class DiscountCouponMapper : BiDirectionMapper<Contracts.DiscountCoupon, Persistence.DiscountCoupon, DiscountCouponMapper>
{
    protected override Persistence.DiscountCoupon OnMap(Contracts.DiscountCoupon source) => throw new NotImplementedException();

    protected override Contracts.DiscountCoupon OnMap(Persistence.DiscountCoupon source) => new()
    {
        Id = source.Id!,
        Code = source.Code,
        Text = source.Text,
        SortOrder = source.SortOrder,
        DiscountPercentage = source.DiscountPercentage,
        IsInactive = !source.IsActive,
        StartsOn = source.StartsOn,
        EndsOn = source.EndsOn
    };
}