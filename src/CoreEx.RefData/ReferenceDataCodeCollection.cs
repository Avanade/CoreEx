namespace CoreEx.RefData;

/// <summary>
/// Provides a special purpose <see cref="IReferenceData"/> collection specifically for managing a referenced list of <i>serialization identifiers</i>, being the underlying <see cref="IReferenceData.Code"/>.
/// </summary>
public class ReferenceDataCodeCollection<TRef> : IReferenceDataCodeCollection, ICollection<TRef> where TRef : IReferenceData, new()
{
    private readonly List<string?> _codes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataCodeCollection{TRef}"/> class.
    /// </summary>
    public ReferenceDataCodeCollection() => _codes = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataCodeCollection{TRef}"/> class with a reference to an external <see cref="IReferenceData.Code"/> list.
    /// </summary>
    /// <param name="codes">A reference to the external <see cref="IReferenceData.Code"/> list; it is this list that will be maintained by this collection.</param>
    public ReferenceDataCodeCollection(ref List<string?>? codes) => _codes = codes ?? [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataCodeCollection{TRef}"/> class with a list of items.
    /// </summary>
    /// <param name="items">The list of <see cref="IReferenceData"/> items.</param>
    public ReferenceDataCodeCollection(IEnumerable<TRef> items) => _codes = [.. (items ?? []).Select(x => x.Code)];

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataCodeCollection{TRef}"/> class with a <see cref="IReferenceData.Code"/> array.
    /// </summary>
    /// <param name="codes">The <see cref="IReferenceData.Code"/> array.</param>
    public ReferenceDataCodeCollection(params IEnumerable<string?> codes) => _codes = [.. codes];

    /// <inheritdoc/>
    public bool HasInvalidItems => this.Any(x => x is null || !x.IsValid);

    /// <inheritdoc/>
    public bool HasInactiveItems => this.Any(x => x is not null && x.IsValid && !x.IsActive);

    /// <inheritdoc/>
    public int Count => _codes.Count;

    /// <inheritdoc/>
    public List<string?> ToCodeList() => [.. _codes];

    /// <inheritdoc/>
    public IEnumerable<IReferenceData> ToRefDataList() => [.. this];

    /// <inheritdoc/>
    public bool IsReadOnly => ((IList)_codes).IsReadOnly;

    /// <inheritdoc/>
    public void Add(TRef item) => _codes.Add(item?.Code);

    /// <inheritdoc/>
    public void Clear() => _codes.Clear();

    /// <inheritdoc/>
    public bool Contains(TRef item) => ((IList)_codes).Contains(item);

    /// <inheritdoc/>
    public void CopyTo(TRef[] array, int arrayIndex) => ((IList)_codes).CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public IEnumerator<TRef> GetEnumerator()
    {
        foreach (string? code in _codes)
        {
            yield return ReferenceDataOrchestrator.TryGetByCode<TRef>(code, out var item) ? item : item;
        }
    }

    /// <inheritdoc/>
    public int IndexOf(TRef item) => _codes.IndexOf(item?.Code);

    /// <inheritdoc/>
    public bool Remove(TRef item) => _codes.Remove(item?.Code);

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}