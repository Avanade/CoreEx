namespace CoreEx.RefData;

/// <summary>
/// Represents a <see cref="IReferenceData{TId}"/> implementation where the <see cref="IReferenceData.Id"/> <see cref="Type"/> is a <see cref="string"/>.
/// </summary>
/// <typeparam name="TSelf">The reference data <see cref="Type"/> itself.</typeparam>
/// <remarks>The underlying implicit/explicit <see cref="string"/> casting only supports <see cref="IReferenceData.Code"/> as there is no means to distinguish between it and the <see cref="IIdentifier{TId}.Id"/> where they are of type <see cref="string"/>.</remarks>
[DebuggerDisplay("Id = {Id}, Code = {Code}, Text = {_text}, IsActive = {IsActive}")]
public abstract partial class ReferenceData<TSelf> : ReferenceDataCore<string>, IComparable<TSelf>
    where TSelf : ReferenceData<TSelf>, IReferenceData<string>, new()
{
    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> where <see cref="ReferenceDataCore{TId}.IsInactive"/>; otherwise, continues.
    /// </summary>
    /// <returns>The instance itself to support fluent-style method-chaining.</returns>
    /// <remarks>This does verify whether the reference data is invalid also.</remarks>
    public TSelf ThrowIfInactive()
    {
        if (IsInactive)
            throw new InvalidOperationException("The reference data not be in an active state.");

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
    public static bool TryGetById(string id, out TSelf item) => ReferenceDataOrchestrator.TryGetById(id, out item);

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
    /// An explicit cast operator that converts a <paramref name="code"/> to a <typeparamref name="TSelf"/> <see cref="ReferenceData{TSelf}"/> instance.
    /// </summary>
    /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
    [return: NotNullIfNotNull(nameof(code))]
    public static explicit operator ReferenceData<TSelf>?(string? code) => code is null ? null : TryGetByCode(code, out var item) ? item : item;

    /// <summary>
    /// An implicit cast operator that converts a <see cref="ReferenceData{TSelf}"/> to its <see cref="IReferenceData.Code"/> as a <see cref="string"/>.
    /// </summary>
    /// <param name="item">The <typeparamref name="TSelf"/> instance.</param>
    public static implicit operator string(ReferenceData<TSelf>? item) => item?.Code!;
}