namespace Contoso.Shopping.Application;

[ScopedService]
public class ReferenceDataService(IReferenceDataRepository repository) : IReferenceDataProvider
{
    private readonly IReferenceDataRepository _repository = repository.ThrowIfNull();

    public IEnumerable<(Type, Type)> Types =>
    [
        (typeof(BasketStatus), typeof(BasketStatusCollection)),
        (typeof(DiscountCoupon), typeof(DiscountCouponCollection)),
        (typeof(UnitOfMeasure), typeof(UnitOfMeasureCollection)),
    ];

    public async Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => type switch
    {
        _ when type == typeof(BasketStatus) => await _repository.GetAllBasketStatusesAsync().ConfigureAwait(false),
        _ when type == typeof(DiscountCoupon) => await _repository.GetAllDiscountCouponsAsync().ConfigureAwait(false),
        _ when type == typeof(UnitOfMeasure) => await _repository.GetAllUnitsOfMeasureAsync().ConfigureAwait(false),
        _ => throw new InvalidOperationException($"Type {type.FullName} is not a known {nameof(IReferenceData)}.")
    };
}