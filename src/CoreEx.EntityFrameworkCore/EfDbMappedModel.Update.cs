namespace CoreEx.EntityFrameworkCore;

public partial class EfDbMappedModel<TValue, TModel, TBiDirectionMapper>
{
    /// <summary>
    /// Updates the <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the updated value.</returns>
    public Task<DataResult<TValue>> UpdateAsync(TValue value, CancellationToken cancellationToken = default) => UpdateAsync(Model.Args, value, cancellationToken);

    /// <summary>
    /// Updates the <paramref name="value"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the updated value.</returns>
    public async Task<DataResult<TValue>> UpdateAsync(EfDbArgs args, TValue value, CancellationToken cancellationToken = default)
    {
        var r = await Model.UpdateAsync(args, Mapper.To.Map(value), cancellationToken).ConfigureAwait(false);
        return new DataResult<TValue>(Mapper.From.Map(r.Value), r.WasMutated);
    }

    /// <summary>
    /// Updates the <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the updated value.</returns>
    public Task<Result<DataResult<TValue>>> UpdateWithResultAsync(TValue value, CancellationToken cancellationToken = default) => UpdateWithResultAsync(Model.Args, value, cancellationToken);

    /// <summary>
    /// Updates the <paramref name="value"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the updated value.</returns>
    public async Task<Result<DataResult<TValue>>> UpdateWithResultAsync(EfDbArgs args, TValue value, CancellationToken cancellationToken = default)
    {
        var r = await Model.UpdateWithResultAsync(args, Mapper.To.Map(value), cancellationToken).ConfigureAwait(false);
        return r.ThenAs(dr => new DataResult<TValue>(Mapper.From.Map(dr.Value)!, dr.WasMutated));
    }
}