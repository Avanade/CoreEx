namespace CoreEx.RefData.Abstractions;

/// <summary>
/// Represents the core <see cref="IReferenceDataCollection{TId, TRef}"/> implementation.
/// </summary>
/// <typeparam name="TId">The <see cref="IReferenceData.Id"/> <see cref="Type"/>.</typeparam>
/// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
/// <remarks>This class leverages dictionaries internally to manage the items and as such there is no implied order when using the likes of the <see cref="GetEnumerator"/>; use 
/// <see cref="GetItems(CoreEx.RefData.ReferenceDataSortOrder?, bool?, bool?)"/> to achieve desired ordering where applicable. 
/// <para>The <see cref="IReferenceData"/> <see cref="IReferenceData.Id"/> and <see cref="IReferenceData.Code"/> must be both unique.</para></remarks>
public abstract class ReferenceDataCollectionCore<TId, TRef> : IReferenceDataCollection<TId, TRef>, ICollection<TRef> where TRef : class, IReferenceData<TId>
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly ConcurrentDictionary<object, TRef> _rdcId = new();
    private readonly ConcurrentDictionary<string, TRef> _rdcCode;
    private Dictionary<(string, object?), TRef>? _mappingsDict;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataCollection{TItem, TId}"/> class.
    /// </summary>
    /// <param name="sortOrder">The default <see cref="ReferenceDataSortOrder"/> for the collection. Defaults to <see cref="ReferenceDataSortOrder.SortOrder"/>.</param>
    /// <param name="codeComparer">The <see cref="StringComparer"/> for <see cref="IReferenceData.Code"/> comparisons. Defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>.</param>
    internal ReferenceDataCollectionCore(ReferenceDataSortOrder sortOrder = ReferenceDataSortOrder.SortOrder, StringComparer? codeComparer = null)
    {
        SortOrder = sortOrder;
        _rdcCode = new ConcurrentDictionary<string, TRef>(codeComparer ?? StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the default <see cref="ReferenceDataSortOrder"/> used by <see cref="GetItems"/>.
    /// </summary>
    [JsonIgnore]
    public ReferenceDataSortOrder SortOrder { get; }

    /// <summary>
    /// Gets the item for the specified <see cref="IReferenceData.Code"/>.
    /// </summary>
    /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
    /// <returns>The item where found; otherwise, <see langword="null"/>.</returns>
    public TRef? this[string code] => _rdcCode[code];

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_lock)
        {
            _rdcId.Clear();
            _rdcCode.Clear();
            _mappingsDict?.Clear();
        }
    }

    /// <inheritdoc/>
    /// <remarks>The underlying <see cref="IReferenceData.Mappings"/> are included during add; if they are maintained (see <see cref="IReferenceData.SetMapping{T}(string, T)"/>) after these will not be included.</remarks>
    public void Add(TRef item)
    {
        item.ThrowIfNull();
        item.Id.ThrowIfNull();
        item.Code.ThrowIfNullOrEmpty();

        lock (_lock)
        {
            if (_rdcId.Values.Contains(item))
                throw new ArgumentException($"Item already exists within the collection.", nameof(item));

            if (_rdcId.ContainsKey(item.Id))
                throw new ArgumentException($"Item with Id '{item.Id}' already exists within the collection.", nameof(item));

            if (_rdcCode.ContainsKey(item.Code))
                throw new ArgumentException($"Item with Code '{item.Code!}' already exists within the collection.", nameof(item));

            if (item.HasMappings)
            {
                _mappingsDict ??= [];

                // Make sure there are no duplicates.
                foreach (var map in item.Mappings!)
                {
                    if (_mappingsDict.ContainsKey((map.Key, map.Value)))
                        throw new ArgumentException($"Item with Mapping Key '{map.Key}' and Value '{map.Value}' already exists within the collection.");
                }

                // Now add 'em in.
                foreach (var map in item.Mappings)
                {
                    _mappingsDict.Add((map.Key, map.Value), item);
                }
            }

            // Add to the underlying dictionaries.
            _rdcId.TryAdd(item.Id, item);
            _rdcCode.TryAdd(item.Code, item);
        }
    }

    /// <summary>
    /// Adds items from the <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The source <see cref="IEnumerable{T}"/>.</param>
    public void AddRange(IEnumerable<TRef> source)
    {
        if (source is null)
            return;

        foreach (var item in source)
        {
            Add(item);
        }
    }

    /// <summary>
    /// Adds items from the <paramref name="source"/> asynchronously.
    /// </summary>
    /// <param name="source">The source <see cref="IQueryable{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns></returns>
    public async Task AddRangeAsync(IQueryable<TRef> source, CancellationToken cancellationToken = default)
    {
        if (source is IAsyncEnumerable<TRef> ae)
            await AddRangeAsync(ae, cancellationToken).ConfigureAwait(false);

        AddRange(source);
    }

    /// <summary>
    /// Adds items from the <paramref name="source"/> asynchronously.
    /// </summary>
    /// <param name="source">The source <see cref="IAsyncEnumerable{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task AddRangeAsync(IAsyncEnumerable<TRef>? source, CancellationToken cancellationToken = default)
    {
        if (source is not null)
        {
            await foreach (TRef item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                Add(item);
            }
        }
    }

    /// <inheritdoc/>
    public bool ContainsId(TId id) => _rdcId.ContainsKey(id.ThrowIfNull());

    /// <inheritdoc/>
    public bool TryGetById(TId id, [NotNullWhen(true)] out TRef? item)
    {
        if (id is not null)
            return _rdcId.TryGetValue(id, out item);

        item = default;
        return false;
    }

    /// <inheritdoc/>
    public TRef? GetById(TId id) => id is null ? default : _rdcId[id];

    /// <inheritdoc/>
    public bool ContainsCode(string code) => _rdcCode.ContainsKey(code);

    /// <inheritdoc/>
    public bool TryGetByCode(string code, [NotNullWhen(true)] out TRef? item)
    {
        if (code is not null)
            return _rdcCode.TryGetValue(code, out item);

        item = default;
        return false;
    }

    /// <inheritdoc/>
    public TRef? GetByCode(string code) => code is null ? default : _rdcCode[code];

    /// <inheritdoc/>
    public bool ContainsMapping<T>(string name, T value) where T : IComparable<T>, IEquatable<T> => _mappingsDict is not null && _mappingsDict.ContainsKey((name, value));

    /// <inheritdoc/>
    bool IReferenceDataCollection.TryGetByMapping<T>(string name, T value, [NotNullWhen(true)] out IReferenceData? item)
    {
        var r = TryGetByMapping<T>(name, value, out TRef? itemx);
        item = itemx;
        return r;
    }

    /// <inheritdoc/>
    public bool TryGetByMapping<T>(string name, T value, [NotNullWhen(true)] out TRef? item) where T : IComparable<T>, IEquatable<T>
    {
        if (_mappingsDict is not null)
            return _mappingsDict.TryGetValue((name, value), out item);

        item = default;
        return false;
    }

    /// <inheritdoc/>
    public TRef? GetByMapping<T>(string name, T value) where T : IComparable<T>, IEquatable<T> => TryGetByMapping(name, value, out var item) ? item : default;

    /// <inheritdoc/>
    [JsonIgnore]
    IEnumerable<IReferenceData> IReferenceDataCollection.AllItems => AllList;

    /// <inheritdoc/>
    [JsonIgnore]
    IEnumerable<IReferenceData> IReferenceDataCollection.ActiveItems => ActiveList;

    /// <summary>
    /// Gets a list of all items (excluding where <i>not</i> <see cref="IsItemValid(TRef)"/>) sorted by the <see cref="SortOrder"/> value.
    /// </summary>
    /// <value>An <see cref="IList{T}"/> containing the selected items.</value>
    /// <remarks>This is provided as a property to more easily support binding; it encapsulates the following method invocation: <c><see cref="GetItems"/>(SortOrder, null, true);</c></remarks>
    [JsonIgnore]
    public IList<TRef> AllList => GetItems(SortOrder, null, true);

    /// <summary>
    /// Gets a list of all active (<see cref="IsItemActive(TRef)"/> and <see cref="IsItemValid(TRef)"/>) items sorted by the <see cref="SortOrder"/> value.
    /// </summary>
    /// <value>An <see cref="IList{TItem}"/> containing the selected items.</value>
    /// <remarks>This is provided as a property to more easily support binding; it encapsulates the following method invocation: <c><see cref="GetItems"/>(SortOrder, true, null);</c></remarks>
    [JsonIgnore]
    public IList<TRef> ActiveList => GetItems(SortOrder, true, null);

    /// <summary>
    /// Gets a list of <typeparamref name="TRef"/> items from the collection using the specified criteria.
    /// </summary>
    /// <param name="sortOrder">Defines the <see cref="ReferenceDataSortOrder"/>; <see langword="null"/> indicates to use the defined <see cref="SortOrder"/>.</param>
    /// <param name="isActive">Indicates whether the list should include values with the same <see cref="IReferenceData.IsInactive"/> value; otherwise, <see langword="null"/> indicates all.</param>
    /// <param name="isValid">Indicates whether the list should include values with the same <see cref="IsItemValid"/> value; otherwise, <see langword="null"/> indicates all.</param>
    /// <remarks>This is leveraged by <see cref="AllList"/> and <see cref="ActiveList"/>. Where both the <paramref name="isActive"/> and <paramref name="isValid"/> are provided they are treated like a logical <i>AND</i>.</remarks>
    public List<TRef> GetItems(ReferenceDataSortOrder? sortOrder = null, bool? isActive = null, bool? isValid = true)
    {
        if (_rdcId.IsEmpty)
            return [];

        var list = from rd in _rdcId.Values select rd;
        if (isActive is not null)
            list = list.Where(item => isActive.Value ? IsItemActive(item) : !IsItemActive(item));

        if (isValid is not null)
            list = list.Where(item => isValid.Value ? IsItemValid(item) : !IsItemValid(item));

        list = (sortOrder ?? SortOrder) switch
        {
            ReferenceDataSortOrder.Id => list.OrderBy(x => x.Id),
            ReferenceDataSortOrder.Code => list.OrderBy(x => x.Code),
            ReferenceDataSortOrder.Text => list.OrderBy(x => x.Text).ThenBy(x => x.Code),
            _ => list.OrderBy(x => x.SortOrder).ThenBy(x => x.Text).ThenBy(x => x.Code)
        };

        return [.. list];
    }

    /// <summary>
    /// Determines whether the <paramref name="item"/> is considered active and therefore accessible from within the collection.
    /// </summary>
    /// <param name="item">The item to validate.</param>
    /// <returns><see langword="true"/> indicates active; otherwise, <see langword="false"/>.</returns>
    /// <remarks>By default checks <see cref="IReferenceData.IsInactive"/>.</remarks>
    protected virtual bool IsItemActive(TRef item) => !item.IsInactive;

    /// <summary>
    /// Determines whether the <paramref name="item"/> is considered valid and therefore accessible from within the collection.
    /// </summary>
    /// <param name="item">The item to validate.</param>
    /// <returns><see langword="true"/> indicates valid; otherwise, <see langword="false"/>.</returns>
    /// <remarks>By default checks <see cref="IReferenceData.IsValid"/>.</remarks>
    protected virtual bool IsItemValid(TRef item) => item.IsValid;

    #region ICollection

    /// <inheritdoc/>
    [JsonIgnore]
    public ICollection<TId> Keys => throw new NotSupportedException();

    /// <inheritdoc/>
    [JsonIgnore]
    public ICollection<TRef> Values => throw new NotSupportedException();

    /// <inheritdoc/>
    [JsonIgnore]
    public int Count => _rdcId.Count;

    /// <inheritdoc/>
    [JsonIgnore]
    bool ICollection<TRef>.IsReadOnly => false;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IsSynchronized => false;

    /// <inheritdoc/>
    [JsonIgnore]
    public object SyncRoot => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool Contains(TRef item) => _rdcId.Values.Contains(item);

    /// <inheritdoc/>
    void ICollection<TRef>.CopyTo(TRef[] array, int arrayIndex) => throw new NotSupportedException();

    /// <inheritdoc/>
    bool ICollection<TRef>.Remove(TRef item) => throw new NotSupportedException();

    /// <inheritdoc/>
    /// <remarks>Only items that are <see cref="IsItemValid(TRef)"/> are enumerated. There is no implied sort order; use <see cref="GetItems(ReferenceDataSortOrder?, bool?, bool?)"/> for sorted lists.</remarks>
    public IEnumerator<TRef> GetEnumerator()
    {
        foreach (TRef item in _rdcId.Values)
        {
            if (IsItemValid(item))
                yield return item;
        }
    }

    /// <inheritdoc/>
    /// <remarks>Only items that are <see cref="IsItemValid(TRef)"/> are enumerated. There is no implied sort order; use <see cref="GetItems(ReferenceDataSortOrder?, bool?, bool?)"/> for sorted lists.</remarks>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public void CopyTo(Array array, int index) => throw new NotSupportedException();

    #endregion
}