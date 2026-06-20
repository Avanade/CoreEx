namespace CoreEx.EntityFrameworkCore;

public partial class EfDbModel<TModel>
{
    /// <summary>
    /// Updates the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the updated model.</returns>
    public Task<DataResult<TModel>> UpdateAsync(TModel model, CancellationToken cancellationToken = default) => UpdateAsync(Args, model, cancellationToken);

    /// <summary>
    /// Updates the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the updated model.</returns>
    public async Task<DataResult<TModel>> UpdateAsync(EfDbArgs args, TModel model, CancellationToken cancellationToken = default) => (await UpdateWithResultInternalAsync(args, model, nameof(UpdateAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Updates the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the updated model.</returns>
    public Task<Result<DataResult<TModel>>> UpdateWithResultAsync(TModel model, CancellationToken cancellationToken = default) => UpdateWithResultAsync(Args, model, cancellationToken);

    /// <summary>
    /// Updates the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the updated model.</returns>
    public Task<Result<DataResult<TModel>>> UpdateWithResultAsync(EfDbArgs args, TModel model, CancellationToken cancellationToken = default) => UpdateWithResultInternalAsync(args, model, nameof(UpdateWithResultAsync), cancellationToken);

    /// <summary>
    /// Updates the model (internal).
    /// </summary>
    private async Task<Result<DataResult<TModel>>> UpdateWithResultInternalAsync(EfDbArgs args, TModel model, string memberName, CancellationToken cancellationToken)
    {
        model.ThrowIfNull();

        if (model is IReadOnlyLogicallyDeleted ld && ld.IsDeleted)
            throw new InvalidOperationException($"Cannot update a model and set to the deleted state ({nameof(ILogicallyDeleted.IsDeleted)} must be false); use the delete operation to perform.");

        args.CheckRefreshAndSaveChangesCombination();

        return await EfDb.Invoker.InvokeAsync(EfDb, args, async (_, args, cancellationToken) =>
        {
            // Determine whether the model is already being tracked by EF and handle accordingly.
            switch (EfDb.DbContext.Entry(model).State)
            {
                // Where detached, we need to find the tracked entity and copy the values across.
                case Microsoft.EntityFrameworkCore.EntityState.Detached:
                    // Check (find) to ensure that the model exists.
                    var found = await EfDb.DbContext.FindAsync<TModel>([.. Options.GetKeyFromModel(model).Args], cancellationToken).ConfigureAwait(false);
                    var dr = CheckModel(args, found, OperationType.Get, treatNullAsNotFound: true);
                    if (dr.IsFailure)
                        return dr.Bind();

                    // Guard against null; should never be null here.
                    found.ThrowIfNull();

                    // Check optimistic concurrency of etag/row-version to ensure valid.
                    if (model is IReadOnlyETag etag && !ETag.TryCompare(etag, (IReadOnlyETag)found))
                        return Result.ConcurrencyError();

                    // Apply the updates into the found (tracked) entity.
                    if (!Options.MapModelForUpdate(model, found))
                        return Result.Ok(new DataResult<TModel>(model!, false));

                    // Use the found (tracked) as the model going forward.
                    model = found;
                    break;

                // Where attached and is unchanged then exit as there is nothing to do.
                case Microsoft.EntityFrameworkCore.EntityState.Unchanged:
                    return Result.Ok(new DataResult<TModel>(model!, false));
            }

            // Prepare the model.
            var br = Options.OnBeforeCreateOrUpdate(model, OperationType.Update);
            if (br.IsFailure)
                return br;

            Model.PrepareUpdate(model, EfDb.ExecutionContext);
            var r = CheckModel(args, model, OperationType.Update);
            if (r.IsFailure)
                return r.Bind();

            // EF update.
            EfDb.DbContext.Update(model);

            if (args.SaveChanges)
                await EfDb.DbContext.SaveChangesAsync(true, cancellationToken).ConfigureAwait(false);

            // Refresh as required.
            var pr = await RefreshPostMutationAsync(args, model, memberName, cancellationToken).ConfigureAwait(false);
            return pr.ThenAs(m => new DataResult<TModel>(m, true));
        }, cancellationToken, memberName).ConfigureAwait(false);
    }
}