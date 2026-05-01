namespace CoreEx.EntityFrameworkCore;

public partial class EfDbMappedModel<TValue, TModel, TBiDirectionMapper>
{
    /// <summary>
    /// Creates the <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the created value.</returns>
    public Task<DataResult<TValue>> CreateAsync(TValue value, CancellationToken cancellationToken = default) => CreateAsync(Model.Args, value, cancellationToken);

    /// <summary>
    /// Creates the <paramref name="value"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the created value.</returns>
    public async Task<DataResult<TValue>> CreateAsync(EfDbArgs args, TValue value, CancellationToken cancellationToken = default)
    {
        var r = await Model.CreateAsync(args, Mapper.To.Map(value), cancellationToken).ConfigureAwait(false);
        return new DataResult<TValue>(Mapper.From.Map(r.Value), r.WasMutated);
    }

    /// <summary>
    /// Creates the <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the created value.</returns>
    public Task<Result<DataResult<TValue>>> CreateWithResultAsync(TValue value, CancellationToken cancellationToken = default) => CreateWithResultAsync(Model.Args, value, cancellationToken);

    /// <summary>
    /// Creates the <paramref name="value"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the created value.</returns>
    public async Task<Result<DataResult<TValue>>> CreateWithResultAsync(EfDbArgs args, TValue value, CancellationToken cancellationToken = default)
    {
        var r = await Model.CreateWithResultAsync(args, Mapper.To.Map(value), cancellationToken).ConfigureAwait(false);
        return r.ThenAs(dr => new DataResult<TValue>(Mapper.From.Map(dr.Value)!, dr.WasMutated));
    }
}