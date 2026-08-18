namespace CoreEx.Cosmos;

public partial class CosmosDbMappedContainer<TValue, TModel, TBiDirectionMapper>
{
    /// <summary>
    /// Creates the <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the created value.</returns>
    public Task<DataResult<TValue>> CreateAsync(TValue value, CancellationToken cancellationToken = default) => CreateAsync(Container.Args, value, cancellationToken);

    /// <summary>
    /// Creates the <paramref name="value"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the created value.</returns>
    public async Task<DataResult<TValue>> CreateAsync(CosmosDbArgs args, TValue value, CancellationToken cancellationToken = default)
    {
        var r = await Container.CreateAsync(args, Mapper.To.Map(value), cancellationToken).ConfigureAwait(false);
        return new DataResult<TValue>(Mapper.From.Map(r.Value), r.WasMutated);
    }

    /// <summary>
    /// Creates the <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the created value.</returns>
    public Task<Result<DataResult<TValue>>> CreateWithResultAsync(TValue value, CancellationToken cancellationToken = default) => CreateWithResultAsync(Container.Args, value, cancellationToken);

    /// <summary>
    /// Creates the <paramref name="value"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the created value.</returns>
    public async Task<Result<DataResult<TValue>>> CreateWithResultAsync(CosmosDbArgs args, TValue value, CancellationToken cancellationToken = default)
    {
        var r = await Container.CreateWithResultAsync(args, Mapper.To.Map(value), cancellationToken).ConfigureAwait(false);
        return r.ThenAs(dr => new DataResult<TValue>(Mapper.From.Map(dr.Value)!, dr.WasMutated));
    }
}
