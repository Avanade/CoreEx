namespace CoreEx.Data;

/// <summary>
/// Enables the typed <typeparamref name="TItem"/> <see cref="Items"/> (for a <see cref="IItemsResult"/>).
/// </summary>
/// <typeparam name="TItem">The underlying item <see cref="Type"/>.</typeparam>
/// <remarks>Generally an <see cref="IItemsResult"/> is not intended to be serialized and returned as <see cref="HttpResponseMessage"/> content; 
/// the underlying <see cref="Items"/> should be serialized with the <see cref="IItemsResult.Paging"/> returned as <see cref="HttpResponseMessage.Headers"/>.</remarks>
public interface IItemsResult<TItem> : IItemsResult
{
    /// <inheritdoc/>
    Type IItemsResult.ItemType => typeof(TItem);

    /// <inheritdoc/>
    IEnumerable? IItemsResult.Items => Items;

    /// <summary>
    /// Gets or sets the underlying collection.
    /// </summary>
    new IEnumerable<TItem>? Items { get; }

    /// <inheritdoc/>
    bool IItemsResult.ItemsHasAny() => Items?.Any() ?? false;

    /// <inheritdoc/>
    int IItemsResult.GetItemsCount() => Items is null ? 0 : Items.Count();
}