namespace CoreEx.Cosmos;

public partial class CosmosDbMappedContainer<TValue, TModel, TBiDirectionMapper>
{
    /// <summary>
    /// Upserts the <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the upserted value.</returns>
    /// <remarks>An upsert operation will attempt to update the model if it exists, and then create a new model if it does not (i.e. the update results in a <see cref="NotFoundException"/>). Note: It is <i>not</i> a single atomic operation.</remarks>
    public Task<DataResult<TValue>> UpsertAsync(TValue value, CancellationToken cancellationToken = default) => UpsertAsync(Container.Args, value, cancellationToken);

    /// <summary>
    /// Upserts the <paramref name="value"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the upserted value.</returns>
    /// <remarks>An upsert operation will attempt to update the model if it exists, and then create a new model if it does not (i.e. the update results in a <see cref="NotFoundException"/>). Note: It is <i>not</i> a single atomic operation.</remarks>
    public async Task<DataResult<TValue>> UpsertAsync(CosmosDbArgs args, TValue value, CancellationToken cancellationToken = default)
    {
        var r = await Container.UpsertAsync(args, Mapper.To.Map(value), cancellationToken).ConfigureAwait(false);
        return new DataResult<TValue>(Mapper.From.Map(r.Value), r.WasMutated);
    }

    /// <summary>
    /// Upserts the <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the upserted value.</returns>
    /// <remarks>An upsert operation will attempt to update the model if it exists, and then create a new model if it does not (i.e. the update results in a <see cref="NotFoundException"/>). Note: It is <i>not</i> a single atomic operation.</remarks>
    public Task<Result<DataResult<TValue>>> UpsertWithResultAsync(TValue value, CancellationToken cancellationToken = default) => UpsertWithResultAsync(Container.Args, value, cancellationToken);

    /// <summary>
    /// Upserts the <paramref name="value"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TValue}"/> containing the upserted value.</returns>
    /// <remarks>An upsert operation will attempt to update the model if it exists, and then create a new model if it does not (i.e. the update results in a <see cref="NotFoundException"/>). Note: It is <i>not</i> a single atomic operation.</remarks>
    public async Task<Result<DataResult<TValue>>> UpsertWithResultAsync(CosmosDbArgs args, TValue value, CancellationToken cancellationToken = default)
    {
        var r = await Container.UpsertWithResultAsync(args, Mapper.To.Map(value), cancellationToken).ConfigureAwait(false);
        return r.ThenAs(dr => new DataResult<TValue>(Mapper.From.Map(dr.Value)!, dr.WasMutated));
    }
}
