namespace CoreEx.EntityFrameworkCore;

public partial class EfDbMappedModel<TValue, TModel, TBiDirectionMapper>
{
    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/></returns>
    /// <remarks>A delete is considered idempotent and as such no <see cref="NotFoundException"/> will be thrown. The returning <see cref="DataResult.WasMutated"/> is informational only.</remarks>
    public Task<DataResult> DeleteAsync(CompositeKey key, CancellationToken cancellationToken = default) => DeleteAsync(Model.Args, key, cancellationToken);

    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/></returns>
    /// <remarks>A delete is considered idempotent and as such no <see cref="NotFoundException"/> will be thrown. The returning <see cref="DataResult.WasMutated"/> is informational only.</remarks>
    public Task<DataResult> DeleteAsync(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default) => Model.DeleteAsync(args, key, cancellationToken);

    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/></returns>
    /// <remarks>A delete is considered idempotent and as such no <see cref="NotFoundException"/> will be thrown. The returning <see cref="DataResult.WasMutated"/> is informational only.</remarks>
    public Task<Result<DataResult>> DeleteWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => DeleteWithResultAsync(Model.Args, key, cancellationToken);

    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/></returns>
    /// <remarks>A delete is considered idempotent and as such no <see cref="NotFoundException"/> will be thrown. The returning <see cref="DataResult.WasMutated"/> is informational only.</remarks>
    public Task<Result<DataResult>> DeleteWithResultAsync(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default) => Model.DeleteWithResultAsync(args, key, cancellationToken);
}