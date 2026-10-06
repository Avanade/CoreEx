namespace CoreEx.Cosmos;

/// <summary>
/// Provides standardized <see cref="RefData.Abstractions.IReferenceData"/> operations for Cosmos DB.
/// </summary>
/// <remarks>This encapsulates common CRUD operations enabling consistent handling of reference data; the Cosmos DB equivalent of <c>EfDbReferenceData</c>.
/// <para>The reference data is assumed to be stored within a container (typically shared by all reference data types, differentiated by <see cref="ITypeDiscriminator"/>) using the default (no explicit)
/// partition key, as the identifier alone is used to get, update and delete the underlying document. Where an ambient <see cref="CosmosDbUnitOfWork"/> transaction is active the mutations are enlisted
/// and only executed when the unit-of-work completes; in which case the resulting <see cref="IETag.ETag"/> is not final until then (see <see cref="IUnitOfWork.SynchronizeETag{T}(CompositeKey, T)"/>).</para></remarks>
public static class CosmosDbReferenceData
{
    /// <summary>
    /// Creates the specified <paramref name="value"/> in the container.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TMapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/> <see cref="Type"/>.</typeparam>
    /// <param name="container">The <see cref="CosmosDbContainer{TModel}"/>.</param>
    /// <param name="value">The value to create.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="DataResult{T}"/> containing the created value.</returns>
    /// <remarks>The identifier is generated using <see cref="Runtime.GenerateIdentifierAsync{TId, TFor}"/> and stored as a <see cref="string"/>. A newly created value is always inactive. A duplicate <c>Code</c> (for the same type discriminator) results in a <see cref="DuplicateException"/>, but only where the container has a unique key policy covering <c>/typeDiscriminator</c> and <c>/code</c>; the container must also use <see cref="CosmosDbContainerOptions.WithReferenceDataOutboxEvent"/> (see <see cref="CosmosDbOptions.Container(string, Action{CosmosDbContainerOptions}?)"/>) so that the co-located outbox events do not themselves violate that unique key.</remarks>
    public static async Task<Result<DataResult<TRef>>> CreateAsync<TId, TRef, TModel, TMapper>(CosmosDbContainer<TModel> container, TRef value, TMapper mapper, CancellationToken cancellationToken = default)
        where TRef : class, IReferenceData<TId>
        where TModel : CosmosDbReferenceDataModelBase, IEntityKey, new()
        where TMapper : class, IBiDirectionMapper<TRef, TModel>
    {
        container.ThrowIfNull();
        var model = mapper.To.Map(value)!;

        model.Id = (await Runtime.GenerateIdentifierAsync<TId, TRef>().ConfigureAwait(false))?.ToString() ?? throw new InvalidOperationException("A generated identifier must not be null.");
        model.IsActive = false;

        var result = await container.CreateWithResultAsync(model, cancellationToken).ConfigureAwait(false);
        return result.ThenAs(dr => new DataResult<TRef>(mapper.From.Map(dr.Value)!, dr.WasMutated));
    }

    /// <summary>
    /// Updates the specified <paramref name="value"/> in the container.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TMapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/> <see cref="Type"/>.</typeparam>
    /// <param name="container">The <see cref="CosmosDbContainer{TModel}"/>.</param>
    /// <param name="id">The identifier of the value to update.</param>
    /// <param name="value">The value to update.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="DataResult{T}"/> containing the updated value.</returns>
    /// <remarks>The <see cref="CosmosDbReferenceDataModelBase.Code"/> and active state are retained from the existing value. The optimistic concurrency check is performed using the <see cref="IETag.ETag"/> of the <paramref name="value"/>.</remarks>
    public static async Task<Result<DataResult<TRef>>> UpdateAsync<TId, TRef, TModel, TMapper>(CosmosDbContainer<TModel> container, TId id, TRef value, TMapper mapper, CancellationToken cancellationToken = default)
        where TRef : class, IReferenceData<TId>
        where TModel : CosmosDbReferenceDataModelBase, IEntityKey, new()
        where TMapper : class, IBiDirectionMapper<TRef, TModel>
    {
        container.ThrowIfNull();
        var existing = await container.GetWithResultAsync(CompositeKey.Create(id), cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
            return existing.AsResult();

        var model = mapper.To.Map(value)!;
        model.Id = existing.Value.Id;
        model.Code = existing.Value.Code;             // Code is immutable and cannot be updated.
        model.IsActive = existing.Value.IsActive;     // IsActive can only be updated via specific methods.

        var result = await container.UpdateWithResultAsync(model, cancellationToken).ConfigureAwait(false);
        return result.ThenAs(dr => new DataResult<TRef>(mapper.From.Map(dr.Value)!, dr.WasMutated));
    }

    /// <summary>
    /// Activates the specified <paramref name="id"/> in the container.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TMapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/> <see cref="Type"/>.</typeparam>
    /// <param name="container">The <see cref="CosmosDbContainer{TModel}"/>.</param>
    /// <param name="id">The identifier of the value to activate.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="DataResult{T}"/> containing the activated value.</returns>
    public static Task<Result<DataResult<TRef>>> ActivateAsync<TId, TRef, TModel, TMapper>(CosmosDbContainer<TModel> container, TId id, TMapper mapper, CancellationToken cancellationToken = default)
        where TRef : class, IReferenceData<TId>
        where TModel : CosmosDbReferenceDataModelBase, IEntityKey, new()
        where TMapper : class, IBiDirectionMapper<TRef, TModel>
        => SetActiveAsync<TId, TRef, TModel, TMapper>(container, id, true, mapper, cancellationToken);

    /// <summary>
    /// Deactivates the specified <paramref name="id"/> in the container.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TMapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/> <see cref="Type"/>.</typeparam>
    /// <param name="container">The <see cref="CosmosDbContainer{TModel}"/>.</param>
    /// <param name="id">The identifier of the value to deactivate.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="DataResult{T}"/> containing the deactivated value.</returns>
    public static Task<Result<DataResult<TRef>>> DeactivateAsync<TId, TRef, TModel, TMapper>(CosmosDbContainer<TModel> container, TId id, TMapper mapper, CancellationToken cancellationToken = default)
        where TRef : class, IReferenceData<TId>
        where TModel : CosmosDbReferenceDataModelBase, IEntityKey, new()
        where TMapper : class, IBiDirectionMapper<TRef, TModel>
        => SetActiveAsync<TId, TRef, TModel, TMapper>(container, id, false, mapper, cancellationToken);

    /// <summary>
    /// Sets the active state of the specified <paramref name="id"/>; no update is performed where already in the requested state.
    /// </summary>
    private static async Task<Result<DataResult<TRef>>> SetActiveAsync<TId, TRef, TModel, TMapper>(CosmosDbContainer<TModel> container, TId id, bool isActive, TMapper mapper, CancellationToken cancellationToken)
        where TRef : class, IReferenceData<TId>
        where TModel : CosmosDbReferenceDataModelBase, IEntityKey, new()
        where TMapper : class, IBiDirectionMapper<TRef, TModel>
    {
        container.ThrowIfNull();
        var existing = await container.GetWithResultAsync(CompositeKey.Create(id), cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
            return existing.AsResult();

        if (existing.Value.IsActive == isActive)
            return new DataResult<TRef>(mapper.From.Map(existing.Value)!, false);

        existing.Value.IsActive = isActive;
        var result = await container.UpdateWithResultAsync(existing.Value, cancellationToken).ConfigureAwait(false);
        return result.ThenAs(dr => new DataResult<TRef>(mapper.From.Map(dr.Value)!, dr.WasMutated));
    }

    /// <summary>
    /// Deletes the specified <paramref name="id"/> from the container.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="container">The <see cref="CosmosDbContainer{TModel}"/>.</param>
    /// <param name="id">The identifier of the value to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="DataResult"/></returns>
    /// <remarks>A delete is considered idempotent and as such no <see cref="NotFoundException"/> will be thrown. The returning <see cref="DataResult.WasMutated"/> is informational only.</remarks>
    public static async Task<Result<DataResult>> DeleteAsync<TId, TRef, TModel>(CosmosDbContainer<TModel> container, TId id, CancellationToken cancellationToken = default)
        where TRef : class, IReferenceData<TId>
        where TModel : CosmosDbReferenceDataModelBase, IEntityKey, new()
    {
        container.ThrowIfNull();
        var existing = await container.GetWithResultAsync(CompositeKey.Create(id), cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
            return existing.IsNotFoundError ? DataResult.False : existing.AsResult();

        if (existing.Value.IsActive)
            return Result<DataResult>.Fail("An active reference data value cannot be deleted.", c => c.WithErrorCode("cannot-delete-active"));

        return await container.DeleteWithResultAsync(CompositeKey.Create(id), cancellationToken).ConfigureAwait(false);
    }
}
