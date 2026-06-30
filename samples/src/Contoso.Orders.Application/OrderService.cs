namespace Contoso.Orders.Application;

[ScopedService<IOrderService>]
public class OrderService(IUnitOfWork unitOfWork, IOrderRepository repository) : IOrderService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();
    private readonly IOrderRepository _repository = repository.ThrowIfNull();

    public Task<Order?> GetAsync(string id, CancellationToken ct = default) => _repository.GetAsync(id, ct);

    public async Task<Order> CreateAsync(Order order, CancellationToken ct = default)
    {
        order.ThrowIfNull();

        await OrderValidator.Default.ValidateAndThrowAsync(order, ct).ConfigureAwait(false);

        await IdentifierGenerator.Current.AssignIdentifierAsync(order).ConfigureAwait(false);
        order.StatusCode ??= "P";

        return await _unitOfWork.TransactionAsync(async tct =>
        {
            var dr = await _repository.CreateAsync(order, tct).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Created)));
        }, ct).ConfigureAwait(false);
    }

    public async Task<Order> UpdateAsync(Order order, CancellationToken ct = default)
    {
        order.ThrowIfNull();
        order.Id.ThrowIfNullOrEmpty();

        await OrderValidator.Default.ValidateAndThrowAsync(order, ct).ConfigureAwait(false);

        var current = await _repository.GetAsync(order.Id, ct).ConfigureAwait(false);
        NotFoundException.ThrowIfDefault(current);

        return await _unitOfWork.TransactionAsync(async tct =>
        {
            var dr = await _repository.UpdateAsync(order, tct).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Updated)));
        }, ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var order = await _repository.GetAsync(id, ct).ConfigureAwait(false);
        if (order is null)
            return;

        await _unitOfWork.TransactionAsync(async tct =>
        {
            var dr = await _repository.DeleteAsync(id, tct).ConfigureAwait(false);
            dr.WhereMutated(() => _unitOfWork.Events.Add(EventData.CreateEvent<Order>(EventAction.Deleted).WithKey(id)));
        }, ct).ConfigureAwait(false);
    }
}