namespace Contoso.Shopping.Domain;

public sealed class Basket : Aggregate<string, Basket>
{
    private List<BasketItem> _items = [];

    public static Basket CreateNew(string customerId) => new Basket(Runtime.NewId())
    {
        CustomerId = customerId,
        Status = BasketStatus.Empty
    }.AsNew();

    public static Basket CreateFrom(string id, string customerId, BasketStatus status, DiscountCoupon? discountCoupon, IEnumerable<BasketItem>? items, ChangeLog? changeLog, string? etag) => new Basket(id)
    {
        CustomerId = customerId,
        Status = status,
        DiscountCoupon = discountCoupon,
        _items = items is null ? [] : [.. items.Select(i => i.Clone(PersistenceState.NotModified))],
        ChangeLog = changeLog,
        ETag = etag
    }.AsNotModified();

    private Basket(string id) : base(id) { }

    public string CustomerId { get; private set => field = value.ThrowIfNullOrEmpty(); } = null!;

    public BasketStatus Status { get; private set => field = value.ThrowIfNull().ThrowIfInactive(); } = null!;

    public DiscountCoupon? DiscountCoupon { get; private set => field = value?.ThrowIfInvalid(); }

    public IReadOnlyList<BasketItem> Items => _items;

    public decimal SubTotal => _items.Where(i => i.PersistenceState != PersistenceState.Removed).Sum(i => i.Pricing.Total);

    public decimal DiscountPercentage => DiscountCoupon?.DiscountPercentage ?? 0;

    public decimal DiscountAmount => DiscountCoupon is null ? 0 : Math.Round(SubTotal * (DiscountCoupon.DiscountPercentage / 100), 2);

    public decimal Total => SubTotal - DiscountAmount;

    public bool HasChanges => PersistenceState.IsNewOrModified || _items.Any(i => i.PersistenceState.IsNewOrModified);

    /// <inheritdoc/>
    /// <remarks>Check that it can be mutated; otherwise, error/fail.</remarks>
    protected override Result OnCheckCanMutate() => Status.CanBeMutated
        ? Result.Success
        : Result.BusinessError($"Basket has a status of '{Status}' and as such cannot be modified.", c => c.WithKey(Id).WithErrorCode("invalid-status"));

    /// <inheritdoc/>
    /// <remarks>On mutation then re-determine status.</remarks>
    protected override void OnMutate()
    {
        // Automatically update the status based on the items in the basket (where it can be mutated).
        if (Status.CanBeMutated)
            Status = _items.Any(i => i.PersistenceState.IsNotRemoved) ? BasketStatus.Active : BasketStatus.Empty;
    }

    /// <summary>
    /// Applies the discount coupon to the basket (where not already applied).
    /// </summary>
    public Result ApplyDiscount(DiscountCoupon discountCoupon)
    {
        discountCoupon.ThrowIfNull().ThrowIfInactive();
        if (discountCoupon != DiscountCoupon)
            Modify(() => DiscountCoupon = discountCoupon);

        return Result.Success;
    }

    /// <summary>
    /// Adds new or merges into an existing item in the basket.
    /// </summary>
    public Result ItemAdd(BasketItem item) => Modify(() =>
    {
        item.ThrowIfNull();
        if (_items.FirstOrDefault(i => i.ProductId == item.ProductId && i.PersistenceState.IsNotRemoved) is BasketItem existing)
            existing.IncreaseQuantity(item.Pricing.Quantity);
        else
            _items.Add(item.Clone(PersistenceState.New));

        return Result.Success;
    });

    /// <summary>
    /// Updates the quantity of an existing item in the basket.
    /// </summary>
    public Result ItemUpdate(string basketItemId, decimal quantity, string? etag)
    {
        var item = _items.FirstOrDefault(i => i.Id == basketItemId.ThrowIfNullOrEmpty() && i.PersistenceState.IsNotRemoved);
        if (item is null)
            return Result.NotFoundError();

        if (quantity != item.Pricing.Quantity)
            Modify(() =>
            {
                item.OverrideQuantity(quantity);
                item.SetETag(etag);
            });

        return Result.Success;
    }

    /// <summary>
    /// Deletes the item from the basket (marking as removed).
    /// </summary>
    public Result ItemDelete(string basketItemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == basketItemId.ThrowIfNullOrEmpty() && i.PersistenceState.IsNotRemoved);
        if (item is not null)
            Modify(() => item.Delete());

        return Result.Success;
    }

    /// <summary>
    /// Performs a basket checkout.
    /// </summary>
    public Result Checkout()
    {
        if (Status == BasketStatus.Empty)
            return Result.BusinessError("An empty basket can not be checked out.", c => c.WithKey(Id).WithErrorCode("empty-basket"));

        if (_items.Sum(i => i.Pricing.Quantity) == 0)
            return Result.BusinessError("A basket must have at least one item with a quantity greater than zero to be checked out.", c => c.WithKey(Id).WithErrorCode("zero-quantity-basket"));

        if (HasChanges)
            throw new InvalidOperationException("A basket can not be checked out where changes have not been committed.");

        Modify(() =>
        {
            foreach (var item in _items.Where(i => i.Pricing.Quantity == 0))
                item.Delete();

            Status = BasketStatus.CheckedOut;
        });

        return Result.Success;
    }
}