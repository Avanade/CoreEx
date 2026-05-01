namespace CoreEx.RefData;

/// <summary>
/// Enables <see cref="GetById(TId?)"/> functionality for an <see cref="IReferenceData{TId}"/> collection with a typed <see cref="IIdentifier{T}.Id"/>.
/// </summary>
public interface IReferenceDataCollection<TId, TRef> : IReferenceDataCollection, ICollection<TRef> where TRef : class, IReferenceData<TId>
{
    /// <inheritdoc/>
    [JsonIgnore]
    Type IReferenceDataCollection.ItemType => typeof(TRef);

    /// <inheritdoc/>
    void IReferenceDataCollection.Add(IReferenceData item) => Add((TRef)item);

    /// <inheritdoc/>
    void IReferenceDataCollection.AddRange(IEnumerable<IReferenceData> collection) => AddRange((IEnumerable<TRef>)collection);

    /// <inheritdoc/>
    bool IReferenceDataCollection.ContainsId(object? id) => ContainsId((TId)(id ?? default(TId)!));

    /// <inheritdoc/>
    bool IReferenceDataCollection.TryGetById(object? id, [NotNullWhen(true)] out IReferenceData? item)
    {
        if (TryGetById((TId)(id ?? default(TId)!), out TRef? item2))
        {
            item = item2;
            return true;
        }

        item = null;
        return false;
    }

    /// <inheritdoc/>
    bool IReferenceDataCollection.TryGetByCode(string code, [NotNullWhen(true)] out IReferenceData? item)
    {
        if (TryGetByCode(code, out TRef? item2))
        {
            item = item2;
            return true;
        }

        item = null;
        return false;
    }

    /// <inheritdoc/>
    IReferenceData? IReferenceDataCollection.GetById(object? id) => GetById(id);

    /// <inheritdoc/>
    IReferenceData? IReferenceDataCollection.GetByCode(string code) => GetByCode(code);

    /// <inheritdoc/>
    IReferenceData? IReferenceDataCollection.GetByMapping<T>(string name, T value) => GetByMapping(name, value);

    /// <summary>
    /// Adds the <paramref name="collection"/> to the <see cref="IReferenceDataCollection{TId, TRef}"/>.
    /// </summary>
    /// <param name="collection">The collection containing the items to add.</param>
    public void AddRange(IEnumerable<TRef> collection);

    /// <summary>
    /// Determines whether the specified <see cref="IReferenceData.Id"/> exists within the collection.
    /// </summary>
    /// <param name="id">The <see cref="IReferenceData.Id"/>.</param>
    /// <returns><see langword="true"/> if it exists; otherwise, <see langword="false"/>.</returns>
    bool ContainsId(TId id);

    /// <summary>
    /// Attempts to get the <paramref name="item"/> with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The <see cref="IReferenceData.Id"/>.</param>
    /// <param name="item">The corresponding <typeparamref name="TRef"/> item where found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    bool TryGetById(TId id, [NotNullWhen(true)] out TRef? item);

    /// <summary>
    /// Attempts to get the <paramref name="item"/> with the specified <paramref name="code"/>.
    /// </summary>
    /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
    /// <param name="item">The corresponding <typeparamref name="TRef"/> item where found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    bool TryGetByCode(string code, [NotNullWhen(true)] out TRef? item);

    /// <summary>
    /// Gets the <typeparamref name="TRef"/> for the specified <see cref="IReferenceData.Id"/>.
    /// </summary>
    /// <param name="id">The specified reference data <see cref="IReferenceData.Id"/>.</param>
    /// <returns>The <typeparamref name="TRef"/> where found; otherwise, <see langword="null"/>.</returns>
    TRef? GetById(TId id);

    /// <summary>
    /// Gets the <typeparamref name="TRef"/> for the specified <see cref="IReferenceData.Code"/>.
    /// </summary>
    /// <param name="code">The specified <see cref="IReferenceData.Code"/>.</param>
    /// <returns>The <typeparamref name="TRef"/> where found; otherwise, <see langword="null"/>.</returns>
    new TRef? GetByCode(string code);

    /// <summary>
    /// Attempts to get the <paramref name="item"/> with the specified <see cref="IReferenceData.TryGetMapping"/> value.
    /// </summary>
    /// <typeparam name="T">The mapping value <see cref="Type"/>.</typeparam>
    /// <param name="name">The mapping name.</param>
    /// <param name="value">The mapping value.</param>
    /// <param name="item">The corresponding <see cref="IReferenceData"/> item where found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    bool TryGetByMapping<T>(string name, T value, [NotNullWhen(true)] out TRef? item) where T : IComparable<T>, IEquatable<T>;

    /// <summary>
    /// Gets the <see cref="IReferenceData"/> for the specified <see cref="IReferenceData.TryGetMapping"/> value.
    /// </summary>
    /// <typeparam name="T">The mapping value <see cref="Type"/>.</typeparam>
    /// <param name="name">The mapping name.</param>
    /// <param name="value">The mapping value.</param>
    /// <returns>The <see cref="IReferenceData"/> where found; otherwise, <see langword="null"/>.</returns>
    new TRef? GetByMapping<T>(string name, T value) where T : IComparable<T>, IEquatable<T>;
}