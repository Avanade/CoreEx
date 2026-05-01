namespace CoreEx.Mapping;

/// <summary>
/// Enables the bi-directional mapping between a <typeparamref name="TSource"/> value and a <typeparamref name="TDestination"/> value.
/// </summary>
/// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
/// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
/// <remarks>The <see cref="To"/> is a left-to-right <see cref="IMapper"/>, with the <see cref="From"/> being a right-to-left <see cref="IMapper"/>.</remarks>
public interface IBiDirectionMapper<TSource, TDestination> where TSource : class where TDestination : class
{
    /// <summary>
    /// Gets the <typeparamref name="TSource"/> to <typeparamref name="TDestination"/> mapper (left-to-right).
    /// </summary>
    IMapper<TSource, TDestination> To { get; }

    /// <summary>
    /// Gets the <typeparamref name="TDestination"/> to <typeparamref name="TSource"/> mapper (right-to-left).
    /// </summary>
    IMapper<TDestination, TSource> From { get; }
}