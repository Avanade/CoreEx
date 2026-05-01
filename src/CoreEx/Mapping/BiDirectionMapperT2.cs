namespace CoreEx.Mapping;

/// <summary>
/// Provides a bi-direction mapper between <typeparamref name="TSource"/> and <typeparamref name="TDestination"/>.
/// </summary>
/// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
/// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
/// <remarks>The <see cref="To"/> is a left-to-right <see cref="IMapper"/>, with the <see cref="From"/> being a right-to-left <see cref="IMapper"/>.</remarks>
public abstract class BiDirectionMapper<TSource, TDestination> : IBiDirectionMapper<TSource, TDestination> where TSource : class where TDestination : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BiDirectionMapper{TSource, TDestination}"/> class.
    /// </summary>
    public BiDirectionMapper()
    {
        if (typeof(TSource) == typeof(TDestination))
            throw new InvalidOperationException($"{nameof(BiDirectionMapper<,>)} source and destination types cannot be the same.");

        To = new SourceToDestinationMapper(OnMap);
        From = new DestinationToSourceMapper(OnMap);
    }

    /// <inheritdoc/>
    public IMapper<TSource, TDestination> To { get; }

    /// <inheritdoc/>
    public IMapper<TDestination, TSource> From { get; }

    /// <summary>
    /// Maps the <paramref name="source"/> (<typeparamref name="TSource"/>) value to a new destination (<typeparamref name="TDestination"/>) value.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <returns>The destination value.</returns>
    /// <remarks>This represents the left-to-right mapping direction.</remarks>
    protected abstract TDestination OnMap(TSource source);

    /// <summary>
    /// Maps the <paramref name="source"/> (<typeparamref name="TDestination"/>) value to a new destination (<typeparamref name="TSource"/>) value.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <returns>The destination value.</returns>
    /// <remarks>This represents the right-to-left mapping direction.</remarks>
    protected abstract TSource OnMap(TDestination source);

    /// <summary>
    /// Provides the underlying <typeparamref name="TSource"/> to <typeparamref name="TSource"/> mapping.
    /// </summary>
    public sealed class SourceToDestinationMapper(Func<TSource, TDestination> map) : Mapper<TSource, TDestination>
    {
        private readonly Func<TSource, TDestination> _map = map;

        /// <inheritdoc/>
        protected override TDestination OnMap(TSource source) => _map(source);
    }

    /// <summary>
    /// Provides the underlying <typeparamref name="TDestination"/> to <typeparamref name="TSource"/> mapping.
    /// </summary>
    public sealed class DestinationToSourceMapper(Func<TDestination, TSource> map) : Mapper<TDestination, TSource>
    {
        private readonly Func<TDestination, TSource> _map = map;

        /// <inheritdoc/>
        protected override TSource OnMap(TDestination source) => _map(source);
    }
}