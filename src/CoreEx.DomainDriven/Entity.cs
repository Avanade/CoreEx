namespace CoreEx.DomainDriven;

/// <summary>
/// Provides the typed <see href="https://en.wikipedia.org/wiki/Domain-driven_design">domain-driven</see> entity functionality.
/// </summary>
/// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
/// <typeparam name="TSelf">The <see cref="Entity{TId, TSelf}"/> <see cref="Type"/> itself.</typeparam>
/// <remarks>As stated by the <see href="https://en.wikipedia.org/wiki/Domain-driven_design">domain-driven design</see> literature, an entity is identified by its identity, not its attributes. Therefore, the underlying
/// <see cref="Equals(CoreEx.DomainDriven.Entity{TId, TSelf}?)"/> only considers the <see cref="Id"/> value.
/// <para>It is expected that the implementor will adhere to the principles of domain-driven design and only expose read-only properties, and enable modification through methods, ensuring the invariant nature of the entity.</para></remarks>
/// <param name="id">The identifier.</param>
public abstract class Entity<TId, TSelf>(TId id) : EntityBase, IEntity, IReadOnlyIdentifier<TId>, IEquatable<Entity<TId, TSelf>> where TSelf : Entity<TId, TSelf>
{
    /// <inheritdoc/>
    public TId Id { get; } = id;

    /// <inheritdoc/>
    [JsonIgnore]
    object? IIdentifierCore.Id => Id;

    /// <inheritdoc/>
    [JsonIgnore]
    Type IIdentifierCore.IdType => typeof(TId);

    /// <inheritdoc/>
    [JsonIgnore]
    public override CompositeKey EntityKey => CompositeKey.Create(Id);

    /// <summary>
    /// Sets (overrides) the <see cref="PersistenceState"/>.
    /// </summary>
    /// <param name="state">The new <see cref="DomainDriven.PersistenceState"/>.</param>
    /// <remarks>This method does not check <see cref="EntityBase.IsReadOnly"/> by design as this is considered independent to.</remarks>
    protected new TSelf SetPersistenceState(PersistenceState state)
    {
        base.SetPersistenceState(state);
        return (TSelf)this;
    }

    /// <summary>
    /// Sets (overrides) the <see cref="PersistenceState"/> to <see cref="PersistenceState.New"/>.
    /// </summary>
    /// <remarks>This method does not check <see cref="EntityBase.IsReadOnly"/> by design as this is considered independent to.</remarks>
    protected TSelf AsNew() => SetPersistenceState(PersistenceState.New);

    /// <summary>
    /// Sets (overrides) the <see cref="PersistenceState"/> to <see cref="PersistenceState.NotModified"/>.
    /// </summary>
    /// <remarks>This method does not check <see cref="EntityBase.IsReadOnly"/> by design as this is considered independent to.</remarks>
    protected TSelf AsNotModified() => SetPersistenceState(PersistenceState.NotModified);

    /// <summary>
    /// Makes the entity read-only.
    /// </summary>
    /// <remarks>See <see cref="EntityBase.IsReadOnly"/>.</remarks>
    protected new TSelf MakeReadOnly()
    {
        base.MakeReadOnly();
        return (TSelf)this;
    }

    /// <summary>
    /// Sets (overrides) the <see cref="ChangeLog"/>.
    /// </summary>
    /// <param name="changeLog">The <see cref="Entities.ChangeLog"/>.</param>
    /// <remarks>Bypasses <see cref="EntityBase.IsReadOnly"/> checking and will not result in an <see cref="EntityBase.PersistenceState"/> change by design; intended to enable setting during hydration from a data source.</remarks>
    protected new TSelf SetChangeLog(ChangeLog? changeLog)
    {
        base.SetChangeLog(changeLog);
        return (TSelf)this;
    }

    /// <summary>
    /// Sets (overrides) the <see cref="ETag"/>.
    /// </summary>
    /// <param name="eTag">The entity tag.</param>
    /// <remarks>Bypasses <see cref="EntityBase.IsReadOnly"/> checking and will not result in an <see cref="EntityBase.PersistenceState"/> change by design; intended to enable setting during hydration from a data source.</remarks>
    protected new TSelf SetETag(string? eTag)
    {
        base.SetETag(eTag);
        return (TSelf)this;
    }

    /// <inheritdoc/>
    /// <remarks>Uses <see cref="Entities.CompositeKeyComparer.Default"/> for equality comparison leveraging the <see cref="Id"/> value (see underlying <see cref="IEntityKey.EntityKey"/>). As stated by the
    /// <see href="https://en.wikipedia.org/wiki/Domain-driven_design">domain-driven design</see> literature, an entity is identified by its identity, not its attributes.</remarks>
    public bool Equals(Entity<TId, TSelf>? other) => ReferenceEquals(this, other) || Entities.CompositeKeyComparer.Default.Equals(this, other);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Entities.CompositeKeyComparer.Default.Equals(this, obj);

    /// <inheritdoc/>
    public override int GetHashCode() => Entities.CompositeKeyComparer.Default.GetHashCode(this);

    /// <summary>
    /// Determines whether two <see cref="Entity{TId, TSelf}"/> instances are equal.
    /// </summary>
    /// <param name="a">The first entity to compare.</param>
    /// <param name="b">The second entity to compare.</param>
    /// <returns><see langword="true"/> where <see cref="Equals(Entity{TId, TSelf}?)"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Entity<TId, TSelf>? a, Entity<TId, TSelf>? b) => object.Equals(a, null) ? object.Equals(b, null) : a.Equals(b);

    /// <summary>
    /// Determines whether two <see cref="Entity{TId, TSelf}"/> instances are not equal.
    /// </summary>
    /// <param name="a">The first entity to compare.</param>
    /// <param name="b">The second entity to compare.</param>
    /// <returns><see langword="true"/> where not <see cref="Equals(Entity{TId, TSelf}?)"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Entity<TId, TSelf>? a, Entity<TId, TSelf>? b) => !(a == b);
}