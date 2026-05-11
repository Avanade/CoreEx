namespace Contoso.Orders.Application;

[ScopedService<IOrderService>]
public class OrderService(IUnitOfWork unitOfWork, IOrderRepository repository) : IOrderService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();
    private readonly IOrderRepository _repository = repository.ThrowIfNull();

    public Task<Order?> GetAsync(string id) => _repository.GetAsync(id);

    public async Task<Order> CreateAsync(Order order)
    {
        order.ThrowIfNull();

        await OrderValidator.Default.ValidateAndThrowAsync(order).ConfigureAwait(false);

        order.Id = Runtime.NewId();
        order.StatusCode ??= "P";

        return await _unitOfWork.ExecuteAsync(async () =>
        {
            var dr = await _repository.CreateAsync(order).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Created)));
        }).ConfigureAwait(false);
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        order.ThrowIfNull();
        order.Id.ThrowIfNullOrEmpty();

        await OrderValidator.Default.ValidateAndThrowAsync(order).ConfigureAwait(false);

        var current = await _repository.GetAsync(order.Id).ConfigureAwait(false);
        NotFoundException.ThrowIfDefault(current);

        return await _unitOfWork.ExecuteAsync(async () =>
        {
            var dr = await _repository.UpdateAsync(order).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Updated)));
        }).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id)
    {
        var order = await _repository.GetAsync(id).ConfigureAwait(false);
        if (order is null)
            return;

        await _unitOfWork.ExecuteAsync(async () =>
        {
            var dr = await _repository.DeleteAsync(id).ConfigureAwait(false);
            dr.WhereMutated(() => _unitOfWork.Events.Add(EventData.CreateEventWith<Order>(default, EventAction.Deleted).WithKey(id)));
        }).ConfigureAwait(false);
    }
}