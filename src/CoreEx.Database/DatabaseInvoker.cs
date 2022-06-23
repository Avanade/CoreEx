// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
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
    public class DatabaseInvoker : InvokerBase<IDatabase, object?>
    {
        /// <inheritdoc/>
        protected override Task<TResult> OnInvokeAsync<TResult>(IDatabase database, Func<CancellationToken, Task<TResult>> func, object? param, CancellationToken cancellationToken)
        {
            try
            {
                return base.OnInvokeAsync(database, func, param, cancellationToken);
            }
            catch (DbException dbex)
            {
                database.HandleDbException(dbex);
                throw;
            }
        }
    }
}