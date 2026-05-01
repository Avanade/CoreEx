namespace CoreEx.EntityFrameworkCore;

public partial class EfDbModel<TModel>
{
    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/></returns>
    /// <remarks>A delete is considered idempotent and as such no <see cref="NotFoundException"/> will be thrown. The returning <see cref="DataResult.WasMutated"/> is informational only.</remarks>
    public Task<DataResult> DeleteAsync(CompositeKey key, CancellationToken cancellationToken = default) => DeleteAsync(Args, key, cancellationToken);

    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/></returns>
    /// <remarks>A delete is considered idempotent and as such no <see cref="NotFoundException"/> will be thrown. The returning <see cref="DataResult.WasMutated"/> is informational only.</remarks>
    public async Task<DataResult> DeleteAsync(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default) => (await DeleteWithResultInternalAsync(args, key, nameof(DeleteAsync), cancellationToken).ConfigureAwait(false)).ThrowOnError();

    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/></returns>
    /// <remarks>A delete is considered idempotent and as such no <see cref="NotFoundException"/> will be thrown. The returning <see cref="DataResult.WasMutated"/> is informational only.</remarks>
    public Task<Result<DataResult>> DeleteWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => DeleteWithResultAsync(Args, key, cancellationToken);

    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/></returns>
    /// <remarks>A delete is considered idempotent and as such no <see cref="NotFoundException"/> will be thrown. The returning <see cref="DataResult.WasMutated"/> is informational only.</remarks>
    public Task<Result<DataResult>> DeleteWithResultAsync(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default) => DeleteWithResultInternalAsync(args, key, nameof(DeleteWithResultAsync), cancellationToken);

    /// <summary>
    /// Deletes the model (internal).
    /// </summary>
    private async Task<Result<DataResult>> DeleteWithResultInternalAsync(EfDbArgs args, CompositeKey key, string memberName, CancellationToken cancellationToken = default) => await EfDb.Invoker.InvokeAsync(EfDb, args, async (_, args, cancellationToken) =>
    {
        // Check (find) to determine if the model exists.
        var model = await EfDb.DbContext.FindAsync<TModel>([.. key.Args], cancellationToken).ConfigureAwait(false);
        var cmr = CheckModel(args, model, OperationType.Delete, treatNullAsNotFound: true);

        // Where not found, return false (a delete is considered idempotent).
        if (cmr.IsNotFoundError)
            return Result.Ok(DataResult.False);

        if (cmr.IsFailure)
            return cmr.Bind();

        // Delete or logical delete as appropriate.
        try
        {
            switch (Options.LogicalDeleteSupport)
            {
                // Physical delete.
                case FeatureSupport.NotSupported:
                    EfDb.DbContext.Remove(model!);
                    break;

                // Logical delete (ambiguous exception).
                case FeatureSupport.ReadOnly:
                    throw new InvalidOperationException($"The '{nameof(Options)}.{nameof(Options.LogicalDeleteSupport)}' is set to '{nameof(FeatureSupport.ReadOnly)}' which is ambiguous for a delete operation; the model must implement '{nameof(ILogicallyDeleted)}' not '{nameof(IReadOnlyLogicallyDeleted)}'.");

                // Logical delete (update).
                case FeatureSupport.Mutable:
                    var ld = (ILogicallyDeleted)model!;
                    ld.IsDeleted = true;
                    Model.PrepareUpdate(model, EfDb.ExecutionContext);

                    EfDb.DbContext.Update(model!);
                    break;
            }

            if (args.SaveChanges)
                await EfDb.DbContext.SaveChangesAsync(true, cancellationToken).ConfigureAwait(false);

            return Result.Ok(DataResult.True);
        }
        catch (NotFoundException)
        {
            // A hopefully rare, but expected and OK behavior; swallowing is intended here.
            return Result.Ok(DataResult.False);
        }
    }, cancellationToken, memberName).ConfigureAwait(false);
}