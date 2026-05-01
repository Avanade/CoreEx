namespace CoreEx.EntityFrameworkCore;

public partial class EfDbModel<TModel>
{
    /// <summary>
    /// Gets the model for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The model where found; otherwise, <see langword="null"/>.</returns>
    public Task<TModel?> GetAsync(CompositeKey key, CancellationToken cancellationToken = default) => GetAsync(Args, key, cancellationToken);

    /// <summary>
    /// Gets the model for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The model where found; otherwise, <see langword="null"/>.</returns>
    public async Task<TModel?> GetAsync(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default)
        => (await GetWithResultInternalAsync(args, key, nameof(GetAsync), false, cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Gets the model for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The model.</returns>
    public Task<Result<TModel>> GetWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => GetWithResultAsync(Args, key, cancellationToken);

    /// <summary>
    /// Gets the model for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The model.</returns>
    public async Task<Result<TModel>> GetWithResultAsync(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default)
        => (await GetWithResultInternalAsync(args, key, nameof(GetWithResultAsync), true, cancellationToken).ConfigureAwait(false)).ThenAs(v => v!);

    /// <summary>
    /// Gets the model (internal).
    /// </summary>
    private async Task<Result<TModel?>> GetWithResultInternalAsync(EfDbArgs args, CompositeKey key, string memberName, bool treatNullAsNotFound, CancellationToken cancellationToken) => await EfDb.Invoker.InvokeAsync(EfDb, args.ThrowIfNull(), async (_, args, cancellationToken) =>
    {
        var model = await EfDb.DbContext.FindAsync<TModel>([.. key.Args], cancellationToken).ConfigureAwait(false);
        if (args.ClearChangeTrackerAfterGet)
            EfDb.DbContext.ChangeTracker.Clear();

        return CheckModel(args, model, OperationType.Get, treatNullAsNotFound);
    }, cancellationToken, memberName).ConfigureAwait(false);
}