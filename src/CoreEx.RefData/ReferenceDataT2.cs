namespace CoreEx.RefData;

/// <summary>
/// Represents a <see cref="IReferenceData{TId}"/> implementation where the <see cref="IReferenceData.Id"/> <see cref="Type"/> is specified with the <typeparamref name="TId"/>.
/// </summary>
/// <typeparam name="TId">The <see cref="IIdentifier{TId}"/> <see cref="Type"/>.</typeparam>
/// <typeparam name="TSelf">The reference data <see cref="Type"/> itself.</typeparam>
/// <remarks>The <see cref="ReferenceData{TSelf}"/> should be used where the <see cref="IReferenceData.Id"/> <see cref="Type"/> is to be a <see cref="string"/> as this is already optimized for this.</remarks>
[DebuggerDisplay("Id = {Id}, Code = {Code}, Text = {_text}, IsActive = {IsActive}")]
public abstract partial class ReferenceData<TId, TSelf> : ReferenceDataCore<TId>, IComparable<TSelf>
    where TSelf : ReferenceData<TId, TSelf>, IReferenceData<TId>, new()
{
    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> where <see cref="ReferenceDataCore{TId}.IsInactive"/>; otherwise, continues.
    /// </summary>
    /// <returns>The instance itself to support fluent-style method-chaining.</returns>
    /// <remarks>This does verify whether the reference data is invalid also.</remarks>
    public TSelf ThrowIfInactive()
    {
        ThrowIfInvalid();
        if (IsInactive)
            throw new InvalidOperationException("The reference data must not be in an inactive state.");

        return (TSelf)this;
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> where not <see cref="IReferenceData.IsValid"/>; otherwise, continues.
    /// </summary>
    /// <returns>The instance itself to support fluent-style method-chaining.</returns>
    /// <remarks>This does not verify whether the reference data is inactive.</remarks>
    public TSelf ThrowIfInvalid()
    {
        if (!IsValid)
            throw new InvalidOperationException("The reference data must not be in an invalid state.");

        return (TSelf)this;
    }

    /// <summary>
    /// Tries to get the <typeparamref name="TSelf"/> <see cref="IReferenceData"/> item for the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The <see cref="IReferenceData{TId}"/> <see cref="IReferenceData.Id"/>.</param>
    /// <param name="item">The <typeparamref name="TSelf"/> instance.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Where the item (<see cref="IReferenceData"/>) is not found it will be created and <see cref="IReferenceData.SetInvalid"/> will be invoked.
    /// <para>This leverages the <see cref="ReferenceDataOrchestrator.TryGetById{TRef, TId}(TId, out TRef)"/> internally to perform.</para></remarks>
    public static bool TryGetById(TId id, out TSelf item) => ReferenceDataOrchestrator.TryGetById(id.ThrowIfNull(), out item);

    /// <summary>
    /// Tries to get the <typeparamref name="TSelf"/> <see cref="IReferenceData"/> item for the specified <paramref name="code"/>.
    /// </summary>
    /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
    /// <param name="item">The <typeparamref name="TSelf"/> instance.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Where the item (<see cref="IReferenceData"/>) is not found it will be created and <see cref="IReferenceData.SetInvalid"/> will be invoked.
    /// <para>This leverages the <see cref="ReferenceDataOrchestrator.TryGetByCode{TRef}(string?, out TRef)"/> internally to perform.</para></remarks>
    public static bool TryGetByCode(string? code, out TSelf item) => ReferenceDataOrchestrator.TryGetByCode(code, out item);

    /// <inheritdoc/>
    /// <remarks>Comparison is based on the <see cref="IReferenceData.Code"/>.</remarks>
    public int CompareTo(TSelf? other) => other is null ? 1 : Code?.CompareTo(other.Code) ?? 0;

    /// <summary>
    /// An explicit cast operator that converts an <paramref name="id"/> to a <typeparamref name="TSelf"/> <see cref="ReferenceData{TId, TSelf}"/> instance.
    /// </summary>
    /// <param name="id">The <see cref="IReferenceData{TId}"/> <see cref="IReferenceData.Id"/>.</param>
    public static explicit operator ReferenceData<TId, TSelf>(TId id) => TryGetById(id, out var item) ? item : item;

    /// <summary>
    /// An explicit cast operator that converts a <paramref name="code"/> to a <typeparamref name="TSelf"/> <see cref="ReferenceData{TId, TSelf}"/> instance.
    /// </summary>
    /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
    [return: NotNullIfNotNull(nameof(code))]
    public static explicit operator ReferenceData<TId, TSelf>?(string? code) => code is null ? null : TryGetByCode(code, out var item) ? item : item;

    /// <summary>
    /// An implicit cast operator that converts a <see cref="ReferenceData{TId, TSelf}"/> to its identifier type <typeparamref name="TId"/>.
    /// </summary>
    /// <param name="item">The <typeparamref name="TSelf"/> instance.</param>
    public static implicit operator TId(ReferenceData<TId, TSelf>? item) => item is null ? default! : item.Id!;

    /// <summary>
    /// An implicit cast operator that converts a <see cref="ReferenceData{TId, TSelf}"/> to its <see cref="IReferenceData.Code"/> as a <see cref="string"/>.
    /// </summary>
    /// <param name="item">The <typeparamref name="TSelf"/> instance.</param>
    public static implicit operator string(ReferenceData<TId, TSelf>? item) => item?.Code!;
}