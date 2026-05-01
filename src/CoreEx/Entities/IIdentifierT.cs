namespace CoreEx.Entities;

/// <summary>
/// Enables a mutable identifier (<see cref="Id"/>) capability.
/// </summary>
/// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
/// <remarks>The <typeparamref name="TId"/> is intended for primitive types; e.g. <see cref="string"/>, <see cref="int"/>, <see cref="long"/> and <see cref="Guid"/>.
/// <para>See also the immutable <see cref="IReadOnlyIdentifier{TId}"/>.</para></remarks>
public interface IIdentifier<TId> : IIdentifier, IReadOnlyIdentifier<TId>
{
    /// <inheritdoc/>
    TId IReadOnlyIdentifier<TId>.Id => Id;

    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    new TId Id { get; set; }

    /// <inheritdoc/>
    object? IIdentifierCore.Id => Id;

    /// <inheritdoc/>
    object? IIdentifier.Id { get => Id; set => Id = (TId)value!; }

    /// <inheritdoc/>
    [JsonIgnore]
    Type IIdentifierCore.IdType => typeof(TId);

    /// <inheritdoc/>
    [JsonIgnore]
    bool IIdentifierCore.IsIdReadOnly => false;

    /// <inheritdoc/>
    void IIdentifierCore.SetIdentifier(object? id) => Id = (TId)id!;

    /// <summary>
    /// Sets (overrides) the identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    public void SetIdentifier(TId id) => Id = id;
}