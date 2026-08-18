namespace CoreEx.Cosmos;

/// <summary>
/// Provides the extended <see cref="ICosmosDb"/>-based <i>mapped</i> value to/from model functionality.
/// </summary>
/// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
/// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
/// <typeparam name="TBiDirectionMapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/> <see cref="Type"/>.</typeparam>
/// <remarks><i>Note</i>: the <see cref="CosmosDbMappedContainer{TValue, TModel, TBiDirectionMapper}"/> does not provide a <c>Query</c> method equivalent to <see cref="CosmosDbContainer{TModel}.Query(Func{IQueryable{TModel}, IQueryable{TModel}}?, CosmosDbArgs?)"/>
/// by design. This is because queries are tightly-coupled to the model; use <see cref="CosmosDbContainer{TModel}.Query(Func{IQueryable{TModel}, IQueryable{TModel}}?, CosmosDbArgs?)"/> directly plus
/// <see cref="CosmosDbQuery{TModel}.ToMappedItemsResultAsync{T}(Func{TModel, T}, bool, CancellationToken)"/> where applicable.</remarks>
public partial class CosmosDbMappedContainer<TValue, TModel, TBiDirectionMapper> where TValue : class where TModel : class, IEntityKey, new() where TBiDirectionMapper : IBiDirectionMapper<TValue, TModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDbMappedContainer{TValue, TModel, TBiDirectionMapper}"/> class.
    /// </summary>
    /// <param name="container">The <see cref="CosmosDbContainer{TModel}"/>.</param>
    /// <param name="mapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/>.</param>
    internal CosmosDbMappedContainer(CosmosDbContainer<TModel> container, TBiDirectionMapper mapper)
    {
        Container = container.ThrowIfNull();
        Mapper = mapper.ThrowIfNull();
    }

    /// <summary>
    /// Gets the underlying <see cref="CosmosDbContainer{TModel}"/>.
    /// </summary>
    public CosmosDbContainer<TModel> Container { get; }

    /// <summary>
    /// Gets the <see cref="IBiDirectionMapper{TSource, TDestination}"/>.
    /// </summary>
    public TBiDirectionMapper Mapper { get; }
}
