namespace CoreEx.EntityFrameworkCore;

public partial class EfDbModel<TModel>
{
    /// <summary>
    /// Creates the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the created model.</returns>
    public Task<DataResult<TModel>> CreateAsync(TModel model, CancellationToken cancellationToken = default) => CreateAsync(Args, model, cancellationToken);

    /// <summary>
    /// Creates the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the created model.</returns>
    public async Task<DataResult<TModel>> CreateAsync(EfDbArgs args, TModel model, CancellationToken cancellationToken = default) => (await CreateWithResultInternalAsync(args, model, nameof(CreateAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Creates the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the created model.</returns>
    public Task<Result<DataResult<TModel>>> CreateWithResultAsync(TModel model, CancellationToken cancellationToken = default) => CreateWithResultAsync(Args, model, cancellationToken);

    /// <summary>
    /// Creates the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the created model.</returns>
    public Task<Result<DataResult<TModel>>> CreateWithResultAsync(EfDbArgs args, TModel model, CancellationToken cancellationToken = default) => CreateWithResultInternalAsync(args, model, nameof(CreateWithResultAsync), cancellationToken);

    /// <summary>
    /// Creates the model (internal).
    /// </summary>
    private async Task<Result<DataResult<TModel>>> CreateWithResultInternalAsync(EfDbArgs args, TModel model, string memberName, CancellationToken cancellationToken)
    {
        model.ThrowIfNull();

        if (model is IReadOnlyLogicallyDeleted ld && ld.IsDeleted)
            throw new InvalidOperationException($"Cannot create a model with a deleted state; {nameof(ILogicallyDeleted.IsDeleted)} must be false.");

        args.CheckRefreshAndSaveChangesCombination();

        return await EfDb.Invoker.InvokeAsync(EfDb, args, async (_, args, cancellationToken) =>
        {
            // Prepare the model.
            var br = Options.OnBeforeCreateOrUpdate(model, OperationType.Create);
            if (br.IsFailure)
                return br;

            Mapper.MapChangeLogInto(ChangeLog.Empty, model);
            Model.PrepareCreate(model, EfDb.ExecutionContext);

            // Check model is valid.
            var r = CheckModel(args, model, OperationType.Create);
            if (r.IsFailure)
                return r.Bind();

            // EF add (create).
            EfDb.DbContext.Add(model);

            if (args.SaveChanges)
                await EfDb.DbContext.SaveChangesAsync(true, cancellationToken).ConfigureAwait(false);

            // Refresh as required.
            var pr = await RefreshPostMutationAsync(args, model, memberName, cancellationToken).ConfigureAwait(false);
            return pr.ThenAs(m => new DataResult<TModel>(m, true));
        }, cancellationToken, memberName).ConfigureAwait(false);
    }
}