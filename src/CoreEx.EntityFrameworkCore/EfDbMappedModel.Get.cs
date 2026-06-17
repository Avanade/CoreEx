namespace CoreEx.EntityFrameworkCore;

public partial class EfDbMappedModel<TValue, TModel, TBiDirectionMapper>
{
    /// <summary>
    /// Gets the value for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value where found; otherwise, <see langword="null"/>.</returns>
    public Task<TValue?> GetAsync(CompositeKey key, CancellationToken cancellationToken = default) => GetAsync(Model.Args, key, cancellationToken);

    /// <summary>
    /// Gets the value for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value where found; otherwise, <see langword="null"/>.</returns>
    public async Task<TValue?> GetAsync(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default)
    {
        var m = await Model.GetAsync(args, key, cancellationToken).ConfigureAwait(false);
        return Mapper.From.Map(m);
    }

    /// <summary>
    /// Gets the value for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value.</returns>
    public Task<Result<TValue>> GetWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => GetWithResultAsync(Model.Args, key, cancellationToken);

    /// <summary>
    /// Gets the value for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value.</returns>
    public async Task<Result<TValue>> GetWithResultAsync(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default)
    {
        var r = await Model.GetWithResultAsync(args, key, cancellationToken).ConfigureAwait(false);
        return r.IsSuccess ? Mapper.From.Map(r.Value) : r.Bind();
    }
}