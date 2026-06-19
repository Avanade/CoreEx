namespace CoreEx.Data;

/// <summary>
/// Enables the <see cref="Paging"/> and <see cref="Items"/> for a collection result.
/// </summary>
/// <remarks>Generally an <see cref="IItemsResult"/> is not intended for serialized <see cref="HttpResponseMessage"/>; the underlying <see cref="Items"/> is serialized with the <see cref="Paging"/> returned as <see cref="HttpResponseMessage.Headers"/>.</remarks>
public interface IItemsResult
{
    /// <summary>
    /// Gets the underlying item <see cref="Type"/>.
    /// </summary>
    Type ItemType { get; }

    /// <summary>
    /// Gets the underlying <see cref="IEnumerable"/>.
    /// </summary>
    IEnumerable? Items { get; }

    /// <summary>
    /// Gets the <see cref="PagingResult"/>.
    /// </summary>
    PagingResult? Paging { get; }

    /// <summary>
    /// Indicates whether there are one or more <see cref="Items"/>.
    /// </summary>
    bool ItemsHasAny();

    /// <summary>
    /// Gets the count of the <see cref="Items"/>.
    /// </summary>
    /// <returns>The <see cref="Items"/> count.</returns>
    int GetItemsCount();
}