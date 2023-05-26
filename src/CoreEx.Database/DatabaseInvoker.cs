// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Invokers;
using CoreEx.Results;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Database
{
    /// <summary>
    /// Provides the standard <see cref="Database{TConnection}"/> invoker functionality.
    /// </summary>
    /// <remarks>Catches any unhandled <see cref="DbException"/> and invokes <see cref="Database{TConnection}.OnDbException(DbException)"/> to handle before bubbling up.</remarks>
    public class DatabaseInvoker : InvokerBase<IDatabase>
    {
        /// <inheritdoc/>
        protected override TResult OnInvoke<TResult>(IDatabase database, Func<TResult> func) => throw new NotSupportedException();

        /// <inheritdoc/>
        protected override async Task<TResult> OnInvokeAsync<TResult>(IDatabase database, Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        {
            try
            {
                return await base.OnInvokeAsync(database, func, cancellationToken).ConfigureAwait(false);
            }
            catch (DbException dbex)
            {
                var eresult = database.HandleDbException(dbex);
                if (eresult.HasValue && eresult.Value.IsFailure)
                {
                    var dresult = default(TResult);
                    if (dresult is IResult dir)
                        return (TResult)dir.ToFailure(eresult.Value.Error);
                    else
                        eresult.Value.ThrowOnError();
                }

                throw;
            }
        }
    }
}