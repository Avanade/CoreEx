namespace Contoso.Orders.Application;

[ScopedService]
public class ReferenceDataService(IReferenceDataRepository repository) : IReferenceDataProvider
{
    private readonly IReferenceDataRepository _repository = repository.ThrowIfNull();

    public IEnumerable<(Type, Type)> Types =>
    [
        (typeof(OrderStatus), typeof(OrderStatusCollection)),
    ];

    public async Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => type switch
    {
        _ when type == typeof(OrderStatus) => await _repository.GetAllOrderStatusesAsync().ConfigureAwait(false),
        _ => throw new InvalidOperationException($"Type {type.FullName} is not a known {nameof(IReferenceData)}.")
    };
}