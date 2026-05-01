using Microsoft.Extensions.Logging;

namespace CoreEx.EntityFrameworkCore;

/// <summary>
/// Provides the standard <see cref="IEfDb"/> invoker functionality.
/// </summary>
/// <remarks>Catches any unhandled <see cref="DbException"/> and invokes <see cref="IDatabase.HandleDbException(DbException)"/> to handle (where <see cref="DatabaseArgsBase.TransformException"/>) is <see langword="true"/>
/// before bubbling up. Also, catches and handles the <see cref="DbUpdateConcurrencyException"/> and <see cref="DbUpdateException"/> where applicable.</remarks>
[InvokerName("CoreEx.EntityFrameworkCore.EfDb")]
public class EfDbInvoker : InvokerBase<IEfDb, EfDbArgs>
{
    private static EfDbInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="EfDbInvoker"/> instance.
    /// </summary>
    public static EfDbInvoker Default => ExecutionContext.GetService<EfDbInvoker>() ?? (_default ??= new EfDbInvoker());

    /// <inheritdoc/>
    public override bool IsTracingDisabled => true;

    /// <inheritdoc/>
    protected override async Task<TResult> OnInvokeAsync<TResult>(InvokerTracer tracer, IEfDb ef, EfDbArgs efArgs, Func<InvokerTracer, EfDbArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
    {
        try
        {
            // Ensure any ambient transaction is used.
            if (ef.Database.IsInTransaction && ef.DbContext.Database.CurrentTransaction is null)
                await ef.DbContext.Database.UseTransactionAsync(ef.Database.CurrentTransaction, cancellationToken).ConfigureAwait(false);

            // Invoke the EF operation.
            return await base.OnInvokeAsync(tracer, ef, efArgs, func, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (efArgs.TransformException)
        {
            var hex = HandleDbException(ef, ex);
            if (hex is not null)
            {
                if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                    tracer.Logger.LogDebug(ex, "Database exception converted to '{ExceptionType}': {Message} [DatabaseId: {DatabaseId}]", hex.GetType().Name, hex.Message, ef.Database.DatabaseId);

                // Where the result is an IResult (ROP) and the exception is considered an error then return as an IResult _failure_.
                if (ExtendedException.TryConvertExceptionToResult<TResult>(hex, out var res))
                    return res;

                throw hex;
            }

            throw;
        }
    }

    /// <summary>
    /// Handle the various exceptions scenarios.
    /// </summary>
    private static Exception? HandleDbException(IEfDb ef, Exception ex)
    {
        if (ex is DbException dbex)
            return ef.Database.HandleDbException(dbex);
        else if (ex is DbUpdateConcurrencyException duex)
            return new ConcurrencyException(null, duex);
        else if (ex is DbUpdateException deux && deux.InnerException is not null && deux.InnerException is DbException dbex2)
            return ef.Database.HandleDbException(dbex2);
        else if (ex is TargetInvocationException tiex && tiex.InnerException is DbException dbex3)
            return ef.Database.HandleDbException(dbex3);
        else
            return null;
    }
}