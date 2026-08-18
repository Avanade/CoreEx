namespace CoreEx.Cosmos;

/// <summary>
/// Provides the standard <see cref="ICosmosDb"/> invoker functionality.
/// </summary>
/// <remarks>Catches any unhandled <see cref="CosmosException"/> and invokes <see cref="ICosmosDb.HandleCosmosException(CosmosException)"/> to handle before bubbling up.</remarks>
[InvokerName("CoreEx.Cosmos.CosmosDb")]
public class CosmosDbInvoker : InvokerBase<ICosmosDb, CosmosDbArgs>
{
    private static CosmosDbInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="CosmosDbInvoker"/> instance.
    /// </summary>
    public static CosmosDbInvoker Default => ExecutionContext.GetService<CosmosDbInvoker>() ?? (_default ??= new CosmosDbInvoker());

    /// <inheritdoc/>
    public override bool IsTracingDisabled => true;

    /// <inheritdoc/>
    protected override async Task<TResult> OnInvokeAsync<TResult>(InvokerTracer tracer, ICosmosDb cosmosDb, CosmosDbArgs args, Func<InvokerTracer, CosmosDbArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
    {
        try
        {
            return await base.OnInvokeAsync(tracer, cosmosDb, args, func, cancellationToken).ConfigureAwait(false);
        }
        catch (CosmosException cex)
        {
            var hex = cosmosDb.HandleCosmosException(cex);
            if (hex is not null)
            {
                if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                    tracer.Logger.LogDebug(cex, "Cosmos exception converted to '{ExceptionType}': {Message}", hex.GetType().Name, hex.Message);

                // Where the result is an IResult (ROP) and the exception is considered an error then return as an IResult _failure_.
                if (ExtendedException.TryConvertExceptionToResult<TResult>(hex, out var res))
                    return res;

                throw hex;
            }

            throw;
        }
    }
}
