namespace CoreEx.Data.GraphQL;

/// <summary>
/// Provides the <see cref="GraphQLEngine"/> invoker.
/// </summary>
[InvokerName("CoreEx.Data.GraphQL.GraphQLEngine")]
public class GraphQLEngineInvoker : InvokerBase<GraphQLEngine>
{
    private static GraphQLEngineInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="GraphQLEngineInvoker"/> instance.
    /// </summary>
    public static GraphQLEngineInvoker Default => ExecutionContext.GetService<GraphQLEngineInvoker>() ?? (_default ??= new GraphQLEngineInvoker());
}
