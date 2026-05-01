namespace CoreEx.Database.Abstractions;

/// <summary>
/// Provides the standard <see cref="IDatabase"/> invoker functionality.
/// </summary>
/// <remarks>Catches any unhandled <see cref="DbException"/> and invokes <see cref="IDatabase.HandleDbException(DbException)"/> to handle (where <see cref="DatabaseArgsBase.TransformException"/>) is <see langword="true"/>
/// before bubbling up.</remarks>
public abstract class DatabaseInvoker : InvokerBase<IDatabase, DatabaseArgs>
{
    /// <inheritdoc/>
    protected override async Task<TResult> OnInvokeAsync<TResult>(InvokerTracer tracer, IDatabase database, DatabaseArgs dbArgs, Func<InvokerTracer, DatabaseArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
    {
        try
        {
            return await base.OnInvokeAsync(tracer, database, dbArgs, func, cancellationToken).ConfigureAwait(false);
        }
        catch (DbException dbex) when (dbArgs.TransformException)
        {
            var hex = database.HandleDbException(dbex);
            if (hex is not null)
            {
                if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                    tracer.Logger.LogDebug(dbex, "Database exception converted to '{ExceptionType}': {Message} [DatabaseId: {DatabaseId}]", hex.GetType().Name, hex.Message, database.DatabaseId);

                // Where the result is an IResult (ROP) and the exception is considered an error then return as an IResult _failure_.
                if (ExtendedException.TryConvertExceptionToResult<TResult>(hex, out var res))
                    return res;

                throw hex;
            }

            throw;
        }
    }
}