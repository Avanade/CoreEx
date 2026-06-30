namespace Contoso.Products.Application;

[ScopedService<IProductService>]
public class ProductService(IUnitOfWork unitOfWork, IProductRepository repository) : IProductService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();
    private readonly IProductRepository _repository = repository.ThrowIfNull();

    public Task<Product?> GetAsync(string id, CancellationToken ct = default) => _repository.GetAsync(id, ct);

    public async Task<Product> CreateAsync(Product product, CancellationToken ct = default)
    {
        product.ThrowIfNull();

        await ProductValidator.Default.ValidateAndThrowAsync(product, ct).ConfigureAwait(false);

        product.Id = Runtime.NewId();
        product.CategoryCode = product.SubCategory!.CategoryCode;
        product.IsInactive = true;

        return await _unitOfWork.TransactionAsync(async tct =>
        {
            var dr = await _repository.CreateAsync(product, tct).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Created)));
        }, ct).ConfigureAwait(false);
    }

    public async Task<Product> UpdateAsync(Product product, CancellationToken ct = default)
    {
        product.ThrowIfNull();
        product.Id.ThrowIfNullOrEmpty();

        await ProductValidator.Default.ValidateAndThrowAsync(product, ct).ConfigureAwait(false);

        var current = await _repository.GetAsync(product.Id, ct).ConfigureAwait(false);
        NotFoundException.ThrowIfDefault(current);

        product.CategoryCode = product.SubCategory!.CategoryCode;
        product.IsNonStocked = current.IsNonStocked;
        product.IsInactive = current.IsInactive;

        return await _unitOfWork.TransactionAsync(async tct =>
        {
            var dr = await _repository.UpdateAsync(product, tct).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Updated)));
        }, ct).ConfigureAwait(false);
    }

    public async Task<Product> ActivateAsync(string id, CancellationToken ct = default)
    {
        var product = await _repository.GetAsync(id, ct).ConfigureAwait(false);
        NotFoundException.ThrowIfDefault(product);

        if (!product.IsInactive)
            return product;

        return await _unitOfWork.TransactionAsync(async tct =>
        {
            product.IsInactive = false;

            var dr = await _repository.UpdateAsync(product, tct).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Activated)));
        }, ct).ConfigureAwait(false);
    }

    public async Task<Product> DeactivateAsync(string id, CancellationToken ct = default)
    {
        var product = await _repository.GetAsync(id, ct).ConfigureAwait(false);
        NotFoundException.ThrowIfDefault(product);

        if (product.IsInactive)
            return product;

        return await _unitOfWork.TransactionAsync(async tct =>
        {
            product.IsInactive = true;

            var dr = await _repository.UpdateAsync(product, tct).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Deactivated)));
        }, ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var product = await _repository.GetAsync(id, ct).ConfigureAwait(false);
        if (product is null)
            return;

        if (!product.IsInactive)
            throw new BusinessException("A product must first be deactivated before it can be deleted.");

        await _unitOfWork.TransactionAsync(async tct =>
        {
            var dr = await _repository.DeleteAsync(id, tct).ConfigureAwait(false);
            dr.WhereMutated(() => _unitOfWork.Events.Add(EventData.CreateEvent<Product>(EventAction.Deleted).WithKey(id)));
        }, ct).ConfigureAwait(false);
    }
}