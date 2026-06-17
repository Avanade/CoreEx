namespace Contoso.Shopping.Domain;

public sealed class BasketItem : Entity<string, BasketItem>
{
    public static BasketItem CreateNew(string productId, string sku, string text, ItemPricing pricing) => new BasketItem(Runtime.NewId())
    {
        ProductId = productId,
        Sku = sku,
        Text = text,
        Pricing = pricing
    }.AsNew();

    public static BasketItem CreateFrom(string id, string productId, string sku, string text, ItemPricing pricing, string? etag) => new BasketItem(id)
    {
        ProductId = productId,
        Sku = sku,
        Text = text,
        Pricing = pricing,
        ETag = etag
    }.AsNotModified();

    private BasketItem(string id) : base(id) { }

    public string ProductId { get; private set => field = value.ThrowIfNullOrEmpty(); } = null!;

    public string Sku { get; private set => field = value.ThrowIfNullOrEmpty(); } = null!;

    public string Text { get; private set => field = value.ThrowIfNullOrEmpty(); } = null!;

    public ItemPricing Pricing { get; private set => field = value.ThrowIfNull().EnsureIsValid(); } = null!;

    internal void OverrideQuantity(decimal quantity) => Modify(() => Pricing = Pricing with { Quantity = quantity });

    internal void IncreaseQuantity(decimal quantity) => OverrideQuantity(Pricing.Quantity + quantity);

    internal void Delete() => Remove();

    internal BasketItem Clone(PersistenceState state) => CreateFrom(Id, ProductId, Sku, Text, Pricing, ETag).SetPersistenceState(state);
}