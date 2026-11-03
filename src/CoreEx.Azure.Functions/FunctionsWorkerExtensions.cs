namespace CoreEx.Azure.Functions;

/// <summary>
/// Provides CoreEx integration with the isolated Functions worker.
/// </summary>
public static class FunctionsWorkerExtensions
{
    /// <summary>
    /// Adds CoreEx execution-context initialization to the worker invocation pipeline.
    /// </summary>
    /// <typeparam name="TBuilder">The worker builder type.</typeparam>
    /// <param name="builder">The worker builder.</param>
    /// <param name="configure">The optional context configuration.</param>
    /// <returns>The builder for fluent configuration.</returns>
    /// <remarks>Register <c>AddExecutionContext()</c> separately, preserving any custom context factory. For HTTP integration, call <c>ConfigureFunctionsWebApplication()</c> before this method.</remarks>
    public static TBuilder UseCoreExExecutionContext<TBuilder>(this TBuilder builder, Func<FunctionContext, ExecutionContext, CancellationToken, Task>? configure = null) where TBuilder : IFunctionsWorkerApplicationBuilder
    {
        builder.ThrowIfNull();
        builder.UseMiddleware<FunctionsExecutionContextMiddleware>();
        builder.Services.Replace(ServiceDescriptor.Singleton(new FunctionsExecutionContextMiddleware(configure)));
        return builder;
    }
}
