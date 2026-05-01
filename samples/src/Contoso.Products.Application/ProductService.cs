namespace Contoso.Products.Application;

[ScopedService<IProductService>]
public class ProductService(IUnitOfWork unitOfWork, IProductRepository repository) : IProductService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();
    private readonly IProductRepository _repository = repository.ThrowIfNull();

    public Task<Product?> GetAsync(string id) => _repository.GetAsync(id);

    public async Task<Product> CreateAsync(Product product)
    {
        product.ThrowIfNull();

        await ProductValidator.Default.ValidateAndThrowAsync(product);

        product.Id = Runtime.NewId();
        product.CategoryCode = product.SubCategory!.CategoryCode;
        product.IsInactive = true;

        return await _unitOfWork.ExecuteAsync(async () =>
        {
            var dr = await _repository.CreateAsync(product).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Created)));
        }).ConfigureAwait(false);
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        product.ThrowIfNull();
        product.Id.ThrowIfNullOrEmpty();

        await ProductValidator.Default.ValidateAndThrowAsync(product);

        var current = await _repository.GetAsync(product.Id).ConfigureAwait(false);
        NotFoundException.ThrowIfDefault(current);

        product.CategoryCode = product.SubCategory!.CategoryCode;
        product.IsNonStocked = current.IsNonStocked;
        product.IsInactive = current.IsInactive;

        return await _unitOfWork.ExecuteAsync(async () =>
        {
            var dr = await _repository.UpdateAsync(product).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Updated)));
        }).ConfigureAwait(false);
    }

    public async Task<Product> ActivateAsync(string id)
    {
        var product = await _repository.GetAsync(id).ConfigureAwait(false);
        NotFoundException.ThrowIfDefault(product);

        if (product.IsInactive)
            return product;

        return await _unitOfWork.ExecuteAsync(async () =>
        {
            product.IsInactive = false;

            var dr = await _repository.UpdateAsync(product).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Activated)));
        }).ConfigureAwait(false);
    }

    public async Task<Product> DeactivateAsync(string id)
    {
        var product = await _repository.GetAsync(id).ConfigureAwait(false);
        NotFoundException.ThrowIfDefault(product);

        if (!product.IsInactive)
            return product;

        return await _unitOfWork.ExecuteAsync(async () =>
        {
            product.IsInactive = true;

            var dr = await _repository.UpdateAsync(product).ConfigureAwait(false);
            return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Deactivated)));
        }).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id)
    {
        var product = await _repository.GetAsync(id).ConfigureAwait(false);
        if (product is null)
            return;

        if (!product.IsInactive)
            throw new BusinessException("A product must first be deactivated before it can be deleted.");

        await _unitOfWork.ExecuteAsync(async () =>
        {
            var dr = await _repository.DeleteAsync(id).ConfigureAwait(false);
            dr.WhereMutated(() => _unitOfWork.Events.Add(EventData.CreateEventWith<Product>(default, EventAction.Deleted).WithKey(id)));
        }).ConfigureAwait(false);
    }
}