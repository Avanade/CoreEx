namespace CoreEx.Entities;

/// <summary>
/// Enables a read-only identifier (<see cref="Id"/>) capability.
/// </summary>
/// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
/// <remarks>The <typeparamref name="TId"/> is intended for primitive types; e.g. <see cref="string"/>, <see cref="int"/>, <see cref="long"/> and <see cref="Guid"/>.
/// <para>See also the mutable <see cref="IIdentifier{TId}"/>.</para></remarks>
public interface IReadOnlyIdentifier<TId> : IReadOnlyIdentifier
{
    /// <inheritdoc/>
    object? IIdentifierCore.Id { get => Id; }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    new TId Id { get; }

    /// <inheritdoc/>
    [JsonIgnore]
    Type IIdentifierCore.IdType => typeof(TId);

    /// <inheritdoc/>
    [JsonIgnore]
    CompositeKey IEntityKey.EntityKey => CompositeKey.Create(Id);

    /// <inheritdoc/>
    [JsonIgnore]
    bool IIdentifierCore.IsIdReadOnly => true;

    /// <inheritdoc/>
    void IIdentifierCore.SetIdentifier(object? id) => throw new InvalidOperationException("Identifier is read-only.");
}