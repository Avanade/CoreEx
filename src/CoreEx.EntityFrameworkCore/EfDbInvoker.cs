// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Database;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Reflection;

namespace CoreEx.EntityFrameworkCore
{
    /// <summary>
    /// Provides the standard <see cref="IEfDb"/> invoker functionality.
    /// </summary>
    /// <remarks>Catches any unhandled <see cref="DbUpdateConcurrencyException"/> or <see cref="DbException"/> and invokes <see cref="Database{TConnection}.HandleDbException(DbException)"/> to handle before bubbling up.</remarks>
    public class EfDbInvoker : InvokerBase<IEfDb>
    {
        /// <inheritdoc/>
        protected override Task<TResult> OnInvokeAsync<TResult>(IEfDb efdb, Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        {
            try
            {
                return base.OnInvokeAsync(efdb, func, cancellationToken);
            }
            catch (DbException dbex)
            {
                efdb.Database.HandleDbException(dbex);
                throw;
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException();
            }
            catch (DbUpdateException deux)
            {
                if (deux.InnerException != null && deux.InnerException is DbException dbex)
                    efdb.Database.HandleDbException(dbex);

                throw;
            }
            catch (TargetInvocationException tiex)
            {
                if (tiex?.InnerException is DbException dbex)
                    efdb.Database.HandleDbException(dbex);

                throw;
            }
        }
    }
}