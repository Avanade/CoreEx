namespace CoreEx.RefData.Abstractions;

/// <summary>
/// Enables <see cref="GetById(object)"/> and <see cref="GetByCode(string)"/> functionality for an <see cref="IReferenceData"/> collection.
/// </summary>
public interface IReferenceDataCollection : ICollection
{
    /// <summary>
    /// Gets the underlying item <see cref="Type"/>.
    /// </summary>
    [JsonIgnore]
    Type ItemType { get; }

    /// <summary>
    /// Adds the <see cref="IReferenceData"/> to the <see cref="IReferenceDataCollection"/>.
    /// </summary>
    /// <param name="item">The <see cref="IReferenceData"/>.</param>
    void Add(IReferenceData item);

    /// <summary>
    /// Adds the <paramref name="collection"/> of items to the <see cref="IReferenceDataCollection"/>.
    /// </summary>
    /// <param name="collection">The collection containing the items to add.</param>
    void AddRange(IEnumerable<IReferenceData> collection);

    /// <summary>
    /// Determines whether the specified <see cref="IReferenceData.Id"/> exists within the collection.
    /// </summary>
    /// <param name="id">The <see cref="IReferenceData.Id"/>.</param>
    /// <returns><see langword="true"/> if it exists; otherwise, <see langword="false"/>.</returns>
    bool ContainsId(object? id); 

    /// <summary>
    /// Determines whether the specified <see cref="IReferenceData.Code"/> exists within the collection.
    /// </summary>
    /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
    /// <returns><see langword="true"/> if it exists; otherwise, <see langword="false"/>.</returns>
    bool ContainsCode(string code);

    /// <summary>
    /// Attempts to get the <paramref name="item"/> with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The <see cref="IReferenceData.Id"/>.</param>
    /// <param name="item">The corresponding <see cref="IReferenceData"/> item where found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    bool TryGetById(object? id, [NotNullWhen(true)] out IReferenceData? item);

    /// <summary>
    /// Attempts to get the <paramref name="item"/> with the specified <paramref name="code"/>.
    /// </summary>
    /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
    /// <param name="item">The corresponding <see cref="IReferenceData"/> item where found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    bool TryGetByCode(string code, [NotNullWhen(true)] out IReferenceData? item);

    /// <summary>
    /// Gets the <see cref="IReferenceData"/> for the specified <see cref="IReferenceData.Id"/>.
    /// </summary>
    /// <param name="id">The specified reference data <see cref="IReferenceData.Id"/>.</param>
    /// <returns>The <see cref="IReferenceData"/> where found; otherwise, <see langword="null"/>.</returns>
    IReferenceData? GetById(object? id);

    /// <summary>
    /// Gets the<see cref="IReferenceData"/> for the specified <see cref="IReferenceData.Code"/>.
    /// </summary>
    /// <param name="code">The specified <see cref="IReferenceData.Code"/>.</param>
    /// <returns>The <see cref="IReferenceData"/> where found; otherwise, <see langword="null"/>.</returns>
    IReferenceData? GetByCode(string code);

    /// <summary>
    /// Determines whether the specified <see cref="IReferenceData.TryGetMapping"/> value exists within the collection.
    /// </summary>
    /// <typeparam name="T">The mapping value <see cref="Type"/>.</typeparam>
    /// <param name="name">The mapping name.</param>
    /// <param name="value">The mapping value.</param>
    /// <returns><see langword="true"/> if it exists; otherwise, <see langword="false"/>.</returns>
    bool ContainsMapping<T>(string name, T value) where T : IComparable<T>, IEquatable<T>;

    /// <summary>
    /// Attempts to get the <paramref name="item"/> with the specified <see cref="IReferenceData.TryGetMapping"/> value.
    /// </summary>
    /// <typeparam name="T">The mapping value <see cref="Type"/>.</typeparam>
    /// <param name="name">The mapping name.</param>
    /// <param name="value">The mapping value.</param>
    /// <param name="item">The corresponding <see cref="IReferenceData"/> item where found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    bool TryGetByMapping<T>(string name, T value, [NotNullWhen(true)] out IReferenceData? item) where T : IComparable<T>, IEquatable<T>;

    /// <summary>
    /// Gets the <see cref="IReferenceData"/> for the specified <see cref="IReferenceData.TryGetMapping"/> value.
    /// </summary>
    /// <typeparam name="T">The mapping value <see cref="Type"/>.</typeparam>
    /// <param name="name">The mapping name.</param>
    /// <param name="value">The mapping value.</param>
    /// <returns>The <see cref="IReferenceData"/> where found; otherwise, <see langword="null"/>.</returns>
    IReferenceData? GetByMapping<T>(string name, T value) where T : IComparable<T>, IEquatable<T>;

    /// <summary>
    /// Gets all items (excluding invalid only) sorted by the <see cref="IReferenceData.SortOrder"/> value.
    /// </summary>
    /// <value>An <see cref="IEnumerable{T}"/> containing the selected <see cref="IReferenceData"/> items.</value>
    [JsonIgnore]
    IEnumerable<IReferenceData> AllItems { get; }

    /// <summary>
    /// Gets all active (excluding <see cref="IReferenceData.IsInactive"/>) items sorted by the <see cref="IReferenceData.SortOrder"/> value.
    /// </summary>
    /// <value>An <see cref="IEnumerable{T}"/> containing the selected <see cref="IReferenceData"/> items.</value>
    [JsonIgnore]
    IEnumerable<IReferenceData> ActiveItems { get; }
}