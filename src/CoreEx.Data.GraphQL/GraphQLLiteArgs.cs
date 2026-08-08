namespace CoreEx.Data.GraphQL;

/// <summary>
/// Provides the GraphQL query <see cref="Arguments"/>.
/// </summary>
public sealed class GraphQLLiteArgs(IReadOnlyDictionary<string, object?> arguments)
{
    /// <summary>
    /// Gets the GraphQL query arguments.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Arguments { get; } = arguments.ThrowIfNull();

    /// <summary>
    /// Gets the named identifier argument value (default name is "id") and converts to the specified type.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
    /// <param name="name">The name of the argument (default is "id").</param>
    /// <returns>The identifier value.</returns>
    /// <exception cref="ArgumentException">Thrown where the named argument is missing, an empty string, or not convertible to <typeparamref name="TId"/> - mapped by
    /// <c>GraphQLEngine.MapException</c> to an <c>ARGUMENT_ERROR</c> GraphQL error (mirroring the REST convention for a bad/missing request input) rather than surfacing as an
    /// opaque, logged <c>EXECUTION_ERROR</c>.</exception>
    /// <remarks>A literal argument value (e.g. <c>{ person(id: 2) { ... } }</c>) and a variable-supplied one (e.g. <c>variables: { "id": 2 }</c>) can resolve to different boxed
    /// CLR types for the identical logical value (<see cref="int"/> vs. <see cref="long"/> respectively - see <see cref="GraphQLValueConverter"/>), and a <see cref="Guid"/>
    /// identifier always arrives as a <see cref="string"/> regardless (there is no native GraphQL <see cref="Guid"/> scalar) - so this always attempts an exact-type match
    /// first, falling back to <typeparamref name="TId"/>'s own <see cref="IParsable{TSelf}.Parse(string, IFormatProvider?)"/> to normalize either case.</remarks>
    public TId GetIdentifier<TId>(string name = "id") where TId : notnull, IParsable<TId>
    {
        if (!Arguments.TryGetValue(name, out var id) || id is null)
            throw new ArgumentException($"'{name}' argument is required.", nameof(name));

        if (id is string { Length: 0 })
            throw new ArgumentException($"'{name}' argument must be a non-empty string.", nameof(name));

        if (id is TId idValue)
            return idValue;

        try
        {
            return TId.Parse(id.ToString()!, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new ArgumentException($"'{name}' argument must be of type {typeof(TId).Name}.", nameof(name));
        }
    }
}
