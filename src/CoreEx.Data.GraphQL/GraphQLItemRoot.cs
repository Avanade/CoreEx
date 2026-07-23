namespace CoreEx.Data.GraphQL;

/// <summary>
/// Represents a registered GraphQL-lite single-item root field (e.g. <c>product(id: "...")</c>).
/// </summary>
/// <remarks>Bound to an existing single-item <c>GetAsync</c>-shaped delegate; see <see cref="GraphQLLiteOptions.AddGet{TItem}(string, Func{IReadOnlyDictionary{string, object}, CancellationToken, Task{TItem}})"/>.</remarks>
public sealed class GraphQLItemRoot
{
    private readonly Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<object?>> _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLItemRoot"/> class.
    /// </summary>
    /// <param name="name">The GraphQL root field name.</param>
    /// <param name="itemType">The underlying item <see cref="Type"/>.</param>
    /// <param name="resolver">The underlying single-item resolver delegate.</param>
    internal GraphQLItemRoot(string name, Type itemType, Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<object?>> resolver)
    {
        Name = name.ThrowIfNull();
        ItemType = itemType.ThrowIfNull();
        _resolver = resolver.ThrowIfNull();
    }

    /// <summary>
    /// Gets the GraphQL root field name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the underlying item <see cref="Type"/>.
    /// </summary>
    public Type ItemType { get; }

    /// <summary>
    /// Invokes the underlying single-item resolver.
    /// </summary>
    /// <param name="arguments">The resolved GraphQL field arguments.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting item (or <see langword="null"/> where not found).</returns>
    public Task<object?> InvokeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken) => _resolver(arguments, cancellationToken);
}
