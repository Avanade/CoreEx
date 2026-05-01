namespace CoreEx.Schemas;

/// <summary>
/// Provides schema metadata utility.
/// </summary>
public static class Schema
{
    /// <summary>
    /// Gets the default version being '<c>1.0</c>'.
    /// </summary>
    public static readonly Version DefaultVersion = new(1, 0);

    /// <summary>
    /// Gets the <see cref="DefaultVersion"/> as a formatted <see cref="string"/>.
    /// </summary>
    public static readonly string DefaultVersionString = DefaultVersion.ToString();

    /// <summary>
    /// Tries to get the configured <see cref="SchemaAttribute"/> for the specified <typeparamref name="TEntity"/> (defaults where not found).
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="metadata">The configured <see cref="SchemaAttribute"/> metadata where found; otherwise, a defaulted instance.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetMetadata<TEntity>(out SchemaAttribute metadata) => TryGetMetadata(typeof(TEntity), out metadata);

    /// <summary>
    /// Tries to get the configured <see cref="SchemaAttribute"/> metadata for the specified <paramref name="type"/> (defaults where not found).
    /// </summary>
    /// <param name="type">The entity <see cref="Type"/>.</param>
    /// <param name="metadata">The configured <see cref="SchemaAttribute"/> where found; otherwise, a defaulted instance.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetMetadata(Type type, out SchemaAttribute metadata) => SchemaAttribute.TryGetCustomAttribute(type, out metadata);
}