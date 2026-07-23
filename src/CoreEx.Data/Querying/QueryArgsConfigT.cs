namespace CoreEx.Data.Querying;

/// <summary>
/// Provides the ODATA-esque dynamic LINQ queries <see cref="QueryArgs"/> execution configuration with a lazy-instantiated <see cref="Default"/> instance.
/// </summary>
/// <typeparam name="TSelf">The <see cref="QueryArgsConfig{TSelf}"/> <see cref="Type"/>.</typeparam>
/// <remarks>This class provides a lazy-instantiated default instance accessible via the <see cref="Default"/> property to defer its creation until it is first accessed.</remarks>
public class QueryArgsConfig<TSelf> : QueryArgsConfig where TSelf : QueryArgsConfig<TSelf>, new()
{
    private static readonly Lazy<TSelf> _default = new(() => new TSelf());

    /// <summary>
    /// Gets the default <see cref="QueryArgsConfig{TSelf}"/> instance.
    /// </summary>
    public static TSelf Default => _default.Value;
}
