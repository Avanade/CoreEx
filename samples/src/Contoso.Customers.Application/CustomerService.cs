namespace Contoso.Customers.Application;

[ScopedService<ICustomerService>]
public class CustomerService(IUnitOfWork unitOfWork, ICustomerRepository repository) : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();
    private readonly ICustomerRepository _repository = repository.ThrowIfNull();

    public Task<Customer?> GetAsync(string id, CancellationToken ct = default) => _repository.GetAsync(id, ct);

    public async Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default)
    {
        customer.ThrowIfNull();

        await CustomerValidator.Default.ValidateAndThrowAsync(customer, ct).ConfigureAwait(false);

        customer.Id = Runtime.NewId();
        customer.HasShopped = false;

        var created = await _unitOfWork.TransactionAsync(async tct =>
        {
            var dr = await _repository.CreateAsync(customer, tct).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Created)));
        }, ct).ConfigureAwait(false);

        // The ETag is not final until the unit-of-work's deferred batch has actually executed - see CoreEx.Cosmos.CosmosDbUnitOfWork.SynchronizeETag.
        _unitOfWork.SynchronizeETag(created);
        return created;
    }

    public async Task<Customer> UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        customer.ThrowIfNull();
        customer.Id.ThrowIfNullOrEmpty();

        await CustomerValidator.Default.ValidateAndThrowAsync(customer, ct).ConfigureAwait(false);

        var current = await _repository.GetAsync(customer.Id, ct).ConfigureAwait(false);
        NotFoundException.ThrowIfDefault(current);

        // HasShopped is read-only from the caller's perspective - only MarkAsShoppedAsync (below) can ever set it, so always preserve the current value here.
        customer.HasShopped = current.HasShopped;

        var updated = await _unitOfWork.TransactionAsync(async tct =>
        {
            var dr = await _repository.UpdateAsync(customer, tct).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Updated)));
        }, ct).ConfigureAwait(false);

        // The ETag is not final until the unit-of-work's deferred batch has actually executed - see CoreEx.Cosmos.CosmosDbUnitOfWork.SynchronizeETag.
        _unitOfWork.SynchronizeETag(updated);
        return updated;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var customer = await _repository.GetAsync(id, ct).ConfigureAwait(false);
        if (customer is null)
            return;

        if (customer.HasShopped)
            throw new BusinessException("A customer that has already shopped cannot be deleted.");

        await _unitOfWork.TransactionAsync(async tct =>
        {
            var dr = await _repository.DeleteAsync(id, tct).ConfigureAwait(false);
            dr.WhereMutated(() => _unitOfWork.Events.Add(EventData.CreateEvent<Customer>(EventAction.Deleted).WithKey(id)));
        }, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>Idempotent - a no-op where the customer is unknown or already flagged. Deliberately skips <see cref="CustomerValidator"/> (this only ever flips one internal, system-owned flag; it is not
    /// a caller-supplied payload that needs re-validating) - the same reasoning as an activate/deactivate-style state toggle.</remarks>
    public async Task MarkAsShoppedAsync(string id, CancellationToken ct = default)
    {
        var customer = await _repository.GetAsync(id, ct).ConfigureAwait(false);
        if (customer is null || customer.HasShopped)
            return;

        customer.HasShopped = true;

        await _unitOfWork.TransactionAsync(async tct =>
        {
            var dr = await _repository.UpdateAsync(customer, tct).ConfigureAwait(false);
            dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Updated)));
        }, ct).ConfigureAwait(false);
    }
}
