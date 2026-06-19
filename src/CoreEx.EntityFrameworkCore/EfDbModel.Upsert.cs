namespace CoreEx.EntityFrameworkCore;

public partial class EfDbModel<TModel>
{
    /// <summary>
    /// Upserts the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the upserted model.</returns>
    /// <remarks>An upsert operation will attempt to update the model if it exists, and then create a new model if it does not (i.e. the create results in a <see cref="NotFoundException"/>). Note: It is <i>not</i> a single atomic operation.</remarks>
    public Task<DataResult<TModel>> UpsertAsync(TModel model, CancellationToken cancellationToken = default) => UpsertAsync(Args, model, cancellationToken);

    /// <summary>
    /// Upserts the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the upserted model.</returns>
    /// <remarks>An upsert operation will attempt to update the model if it exists, and then create a new model if it does not (i.e. the create results in a <see cref="NotFoundException"/>). Note: It is <i>not</i> a single atomic operation.</remarks>
    public async Task<DataResult<TModel>> UpsertAsync(EfDbArgs args, TModel model, CancellationToken cancellationToken = default) => (await UpsertWithResultInternalAsync(args, model, nameof(UpsertAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Upserts the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the upserted model.</returns>
    /// <remarks>An upsert operation will attempt to update the model if it exists, and then create a new model if it does not (i.e. the create results in a <see cref="NotFoundException"/>). Note: It is <i>not</i> a single atomic operation.</remarks>
    public Task<Result<DataResult<TModel>>> UpsertWithResultAsync(TModel model, CancellationToken cancellationToken = default) => UpsertWithResultAsync(Args, model, cancellationToken);

    /// <summary>
    /// Upserts the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the upserted model.</returns>
    /// <remarks>An upsert operation will attempt to update the model if it exists, and then create a new model if it does not (i.e. the create results in a <see cref="NotFoundException"/>). Note: It is <i>not</i> a single atomic operation.</remarks>
    public Task<Result<DataResult<TModel>>> UpsertWithResultAsync(EfDbArgs args, TModel model, CancellationToken cancellationToken = default) => UpsertWithResultInternalAsync(args, model, nameof(UpsertWithResultAsync), cancellationToken);

    /// <summary>
    /// Upserts the model (internal).
    /// </summary>
    private async Task<Result<DataResult<TModel>>> UpsertWithResultInternalAsync(EfDbArgs args, TModel model, string memberName, CancellationToken cancellationToken)
    {
        model.ThrowIfNull();

        return await EfDb.Invoker.InvokeAsync(EfDb, args, (_, args, cancellationToken) =>
        {
            return Result.GoAsync(() => UpdateWithResultAsync(args, model, cancellationToken))
                .OnFailureAsync(r => r.IsNotFoundError ? CreateWithResultAsync(args, model, cancellationToken) : r.AsTask());
        }, cancellationToken, memberName).ConfigureAwait(false);
    }
}