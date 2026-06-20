namespace CoreEx.EntityFrameworkCore;

/// <summary>
/// Provides the extended <see href="https://learn.microsoft.com/en-us/ef/core/">Entity Framework Core</see> model functionality.
/// </summary>
/// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
public sealed partial class EfDbModel<TModel> where TModel : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EfDbModel{TModel}"/> class.
    /// </summary>
    /// <param name="efDb">The owning <see cref="IEfDb"/></param>
    /// <param name="options">The <see cref="EfDbModelOptions{TModel}"/>.</param>
    internal EfDbModel(IEfDb efDb, EfDbModelOptions<TModel> options)
    {
        EfDb = efDb.ThrowIfNull();
        Options = options.ThrowIfNull();
    }

    /// <summary>
    /// Gets the <see cref="IEfDb"/>.
    /// </summary>
    public IEfDb EfDb { get; }

    /// <summary>
    /// Gets the <see cref="EfDbModelOptions{TModel}"/>.
    /// </summary>
    public EfDbModelOptions<TModel> Options { get; }

    /// <summary>
    /// Gets the default <see cref="EfDbArgs"/>.
    /// </summary>
    /// <remarks>Uses the <see cref="EfDbModelOptions{TModel}.Args"/> where specified; otherwise, the <see cref="EfDbOptions.Args"/>.</remarks>
    public EfDbArgs Args => Options.Args ?? EfDb.Options.Args;

    /// <summary>
    /// Checks (ensures) that the <paramref name="model"/> is valid.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="operationType">The <see cref="OperationType"/>.</param>
    /// <param name="treatNullAsNotFound">Indicates whether to treat a <see langword="null"/> model as a not found error.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    [return: NotNullIfNotNull(nameof(model))]
    public Result<TModel?> CheckModel(EfDbArgs args, TModel? model, OperationType operationType, bool treatNullAsNotFound = false)
    {
        if (model is null)
            return treatNullAsNotFound ? Result.NotFoundError() : Result.Ok<TModel?>(null);

        // Check valid tenant where multi-tenancy is being used.
        if (model is IReadOnlyTenantId tenant)
        {
            model.ThrowWhen(_ => string.IsNullOrEmpty(tenant.TenantId), $"{nameof(ITenantId.TenantId)} must be specified.");
            if (tenant.TenantId != ExecutionContext.Current.TenantId)
                return treatNullAsNotFound ? Result.NotFoundError() : Result.Ok<TModel?>(null);
        }

        // Check not logically deleted.
        if (model is IReadOnlyLogicallyDeleted ld && ld.IsDeleted)
            return treatNullAsNotFound ? Result.NotFoundError() : Result.Ok<TModel?>(null);

        // Check filters.
        return Options.CheckFilters(args, model, operationType)
            .OnFailure(fr =>
            {
                if (fr.IsNotFoundError)
                    return treatNullAsNotFound ? fr : Result.Ok<TModel?>(null);
                else
                    return fr;
            });
    }

    /// <summary>
    /// Refreshes the model post-mutation (as required).
    /// </summary>
    private async Task<Result<TModel>> RefreshPostMutationAsync(EfDbArgs args, TModel model, string memberName, CancellationToken cancellationToken)
    {
        // Refresh the model as requested.
        if (args.Refresh && Options.GetKeyFromModel(model) is CompositeKey key)
            return Result.Go((await GetWithResultInternalAsync(args, key, memberName, treatNullAsNotFound: true, cancellationToken).ConfigureAwait(false)).ThenAs(v => v!));

        // Return the current model.
        return Result.Ok(model);
    }

    /// <summary>
    /// Creates a <see cref="EfDbMappedModel{T, TModel, TBiDirectionMapper}"/> that provides mapped <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> operations (Create, Read, Update and Delete).
    /// </summary>
    /// <typeparam name="T">The mapped <see cref="Type"/>.</typeparam>
    /// <typeparam name="TBiDirectionMapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/> <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The <see cref="BiDirectionMapper{TSource, TDestination}"/>.</param>
    /// <returns>The <see cref="EfDbMappedModel{T, TModel, TBiDirectionMapper}"/>.</returns>
    public EfDbMappedModel<T, TModel, TBiDirectionMapper> ToMappedModel<T, TBiDirectionMapper>(TBiDirectionMapper mapper) where T : class  where TBiDirectionMapper : IBiDirectionMapper<T, TModel> => new(this, mapper);
}
