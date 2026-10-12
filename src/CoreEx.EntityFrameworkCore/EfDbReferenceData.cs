namespace CoreEx.EntityFrameworkCore;

/// <summary>
/// Provides standardized <see cref="RefData.Abstractions.IReferenceData"/> operations for Entity Framework Core.
/// </summary>
/// <remarks>This encapsulates common CRUD operations enabling consistent handling of reference data.</remarks>
public static class EfDbReferenceData
{
    /// <summary>
    /// Creates the specified <paramref name="value"/> in the database.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TMapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/> <see cref="Type"/>.</typeparam>
    /// <param name="ef">The EF database model.</param>
    /// <param name="value">The value to create.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="DataResult{T}"/> containing the created value.</returns>
    public static async Task<Result<DataResult<TRef>>> CreateAsync<TId, TRef, TModel, TMapper>(EfDbModel<TModel> ef, TRef value, TMapper mapper, CancellationToken cancellationToken = default)
        where TRef : class, IReferenceData<TId>
        where TModel : ReferenceDataModelBase<TId>
        where TMapper : class, IBiDirectionMapper<TRef, TModel>
    {
        var model = mapper.To.Map(value);
        var pr = CheckPropertySupport<TModel, TId>(ef, model);
        if (pr.IsFailure)
            return pr;

        model.Id = await Runtime.GenerateIdentifierAsync<TId, TRef>().ConfigureAwait(false);
        model.IsActive = false;

        var result = await ef.CreateWithResultAsync(model, cancellationToken).ConfigureAwait(false);
        return result.ThenAs(dr => new DataResult<TRef>(mapper.From.Map(dr.Value)!, dr.WasMutated));
    }

    /// <summary>
    /// Updates the specified <paramref name="value"/> in the database.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TMapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/> <see cref="Type"/>.</typeparam>
    /// <param name="ef">The EF database model.</param>
    /// <param name="id">The identifier of the value to update.</param>
    /// <param name="value">The value to update.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="DataResult{T}"/> containing the updated value.</returns>
    public static async Task<Result<DataResult<TRef>>> UpdateAsync<TId, TRef, TModel, TMapper>(EfDbModel<TModel> ef, TId id, TRef value, TMapper mapper, CancellationToken cancellationToken = default)
        where TRef : class, IReferenceData<TId>
        where TModel : ReferenceDataModelBase<TId>
        where TMapper : class, IBiDirectionMapper<TRef, TModel>
    {
        var existing = await ef.GetWithResultAsync(CompositeKey.Create(id), cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
            return existing.AsResult();

        var model = mapper.To.Map(value);
        var pr = CheckPropertySupport<TModel, TId>(ef, model);
        if (pr.IsFailure)
            return pr;

        model.Id = existing.Value.Id;
        model.Code = existing.Value.Code;           // Code is immutable and cannot be updated.
        model.IsActive = existing.Value.IsActive;   // IsActive can only be updated via specific methods.

        var result = await ef.UpdateWithResultAsync(model, cancellationToken).ConfigureAwait(false);
        return result.ThenAs(dr => new DataResult<TRef>(mapper.From.Map(dr.Value)!, dr.WasMutated));
    }

    /// <summary>
    /// Checks whether the specified <paramref name="model"/> has values where the underlying property is not supported.
    /// </summary>
    private static Result CheckPropertySupport<TModel, TId>(EfDbModel<TModel> ef, TModel model) where TModel : ReferenceDataModelBase<TId>
        => CheckWhetherPropertyIsSupported(ef, nameof(ReferenceDataModelBase<>.Description), model.Description)
            .Then(() => CheckWhetherPropertyIsSupported(ef, nameof(ReferenceDataModelBase<>.StartsOn), model.StartsOn))
            .Then(() => CheckWhetherPropertyIsSupported(ef, nameof(ReferenceDataModelBase<>.EndsOn), model.EndsOn));

    /// <summary>
    /// Checks whether the specified <paramref name="propertyName"/> is supported by the <typeparamref name="TModel"/>.
    /// </summary>
    private static Result CheckWhetherPropertyIsSupported<TModel, T>(EfDbModel<TModel> ef, string propertyName, T value) where TModel : class
    {
        if (value is null)
            return Result.Success;

        var entityType = ef.EfDb.DbContext.Model.FindEntityType(typeof(TModel));
        var property = entityType?.FindProperty(propertyName);

        if (property is null)
            return Result.Fail($"{propertyName.ToSentenceCase()} is not currently supported and as such cannot be set.", c => c.WithErrorCode("not-supported"));

        return Result.Success;
    }

    /// <summary>
    /// Activates the specified <paramref name="id"/> in the database.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TMapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/> <see cref="Type"/>.</typeparam>
    /// <param name="ef">The EF database model.</param>
    /// <param name="id">The identifier of the value to activate.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="DataResult{T}"/> containing the activated value.</returns>
    public static async Task<Result<DataResult<TRef>>> ActivateAsync<TId, TRef, TModel, TMapper>(EfDbModel<TModel> ef, TId id, TMapper mapper, CancellationToken cancellationToken = default)
        where TRef : class, IReferenceData<TId>
        where TModel : ReferenceDataModelBase<TId>
        where TMapper : class, IBiDirectionMapper<TRef, TModel>
    {
        var existing = await ef.GetWithResultAsync(CompositeKey.Create(id), cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
            return existing.AsResult();

        if (existing.Value.IsActive)
            return new DataResult<TRef>(mapper.From.Map(existing.Value)!, false);

        existing.Value.IsActive = true;
        var result = await ef.UpdateWithResultAsync(existing.Value, cancellationToken).ConfigureAwait(false);
        return result.ThenAs(dr => new DataResult<TRef>(mapper.From.Map(dr.Value)!, dr.WasMutated));
    }

    /// <summary>
    /// Deactivates the specified <paramref name="id"/> in the database.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TMapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/> <see cref="Type"/>.</typeparam>
    /// <param name="ef">The EF database model.</param>
    /// <param name="id">The identifier of the value to deactivate.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="DataResult{T}"/> containing the deactivated value.</returns>
    public static async Task<Result<DataResult<TRef>>> DeactivateAsync<TId, TRef, TModel, TMapper>(EfDbModel<TModel> ef, TId id, TMapper mapper, CancellationToken cancellationToken = default)
        where TRef : class, IReferenceData<TId>
        where TModel : ReferenceDataModelBase<TId>
        where TMapper : class, IBiDirectionMapper<TRef, TModel>
    {
        var existing = await ef.GetWithResultAsync(CompositeKey.Create(id), cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
            return existing.AsResult();

        if (!existing.Value.IsActive)
            return new DataResult<TRef>(mapper.From.Map(existing.Value)!, false);

        existing.Value.IsActive = false;
        var result = await ef.UpdateWithResultAsync(existing.Value, cancellationToken).ConfigureAwait(false);
        return result.ThenAs(dr => new DataResult<TRef>(mapper.From.Map(dr.Value)!, dr.WasMutated));
    }

    /// <summary>
    /// Deletes the specified <paramref name="id"/> from the database.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="ef">The EF database model.</param>
    /// <param name="id">The identifier of the value to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="DataResult"/></returns>
    /// <remarks>A delete is considered idempotent and as such no <see cref="NotFoundException"/> will be thrown. The returning <see cref="DataResult.WasMutated"/> is informational only.</remarks>
    public static async Task<Result<DataResult>> DeleteAsync<TId, TRef, TModel>(EfDbModel<TModel> ef, TId id, CancellationToken cancellationToken = default)
        where TRef : class, IReferenceData<TId>
        where TModel : ReferenceDataModelBase<TId>
    {
        var existing = await ef.GetWithResultAsync(CompositeKey.Create(id), cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
            return existing.IsNotFoundError ? DataResult.False : existing.AsResult();

        if (existing.Value.IsActive)
            return Result<DataResult>.Fail("An active reference data value cannot be deleted.", c => c.WithErrorCode("cannot-delete-active"));

        return await ef.DeleteWithResultAsync(CompositeKey.Create(id), cancellationToken).ConfigureAwait(false);
    }
}
