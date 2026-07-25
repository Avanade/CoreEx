namespace CoreEx.Data.GraphQL;

/// <summary>
/// Provides <see cref="IServiceCollection"/> extension methods to register the GraphQL-lite <see cref="IGraphQLEngine"/>.
/// </summary>
public static class GraphQLServiceCollectionExtensions
{
    /// <summary>
    /// Adds a singleton <see cref="GraphQLEngine"/> as the GraphQL-lite <see cref="IGraphQLEngine"/> and its registered query roots to the <paramref name="services"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">The action to configure the <see cref="GraphQLLiteOptions"/> (register roots via <see cref="GraphQLLiteOptions.AddQuery{TItem}(string, QueryArgsConfig, Func{QueryArgs?, PagingArgs?, CancellationToken, Task{IItemsResult{TItem}}})"/> and
    /// <see cref="GraphQLLiteOptions.AddGet{TItem}(string, Func{IReadOnlyDictionary{string, object}, CancellationToken, Task{TItem}})"/>), given the root <see cref="IServiceProvider"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <see cref="IGraphQLEngine"/> is registered as a singleton; where a root resolver needs a scoped dependency (e.g. a repository or application service), resolve it per-invocation
    /// (e.g. via <c>CoreEx.ExecutionContext.GetRequiredService&lt;T&gt;()</c>, which reads from the ambient <c>ExecutionContext</c>'s scoped service provider) rather than capturing an instance resolved
    /// from the root <paramref name="services"/> provider.</remarks>
    public static IServiceCollection AddCoreExGraphQLLite(this IServiceCollection services, Action<GraphQLLiteOptions, IServiceProvider> configure)
    {
        services.ThrowIfNull();
        configure.ThrowIfNull();

        services.AddSingleton<IGraphQLEngine>(sp =>
        {
            var options = new GraphQLLiteOptions();
            configure(options, sp);
            return new GraphQLEngine(options);
        });

        return services;
    }
}
