namespace CoreEx.EntityFrameworkCore;

/// <summary>
/// Provides the extended <see href="https://learn.microsoft.com/en-us/ef/core/">Entity Framework Core</see> <i>mapped</i> value to/from model functionality.
/// </summary>
/// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
/// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
/// <typeparam name="TBiDirectionMapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/> <see cref="Type"/>.</typeparam>
/// <remarks><i>Note</i>: the <see cref="EfDbMappedModel{TValue, TModel, TBiDirectionMapper}"/> does not provide a <c>Query</c> method equivalent to <see cref="EfDbModel{TModel}.Query(EfDbArgs?)"/> by design. This is because queries
/// are tightly-coupled to the model and the <see cref="EfDbExtensions.ToMappedItemsResultAsync{TSource, TItem}(IQueryable{TSource}, IMapper{TSource, TItem}, PagingArgs?, bool, CancellationToken)"/> enables where applicable.</remarks>
public partial class EfDbMappedModel<TValue, TModel, TBiDirectionMapper> where TValue : class where TModel : class where TBiDirectionMapper : IBiDirectionMapper<TValue, TModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EfDbMappedModel{T, TModel, TBiDirectionMapper}"/> class.
    /// </summary>
    /// <param name="efDbModel">The <see cref="EfDbModel{TModel}"/>.</param>
    /// <param name="mapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/>.</param>
    internal EfDbMappedModel(EfDbModel<TModel> efDbModel, TBiDirectionMapper mapper)
    {
        Model = efDbModel.ThrowIfNull();
        Mapper = mapper.ThrowIfNull();
    }

    /// <summary>
    /// Gets the underlying <see cref="EfDbModel{TModel}"/>.
    /// </summary>
    public EfDbModel<TModel> Model { get; }

    /// <summary>
    /// Gets the <see cref="IBiDirectionMapper{TSource, TDestination}"/>.
    /// </summary>
    public TBiDirectionMapper Mapper { get; }
}