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
    public TId GetIdentifier<TId>(string name = "id") where TId : notnull
    {
        if (!Arguments.TryGetValue(name, out var id) || id is null)
            throw new ArgumentException($"'{name}' argument is required.", nameof(name));

        if (id is not TId idValue)
            throw new ArgumentException($"'{name}' argument must be of type {typeof(TId).Name}.", nameof(name));

        if (idValue is string { Length: 0 })
            throw new ArgumentException($"'{name}' argument must be a non-empty string.", nameof(name));

        return idValue;
    }
}
