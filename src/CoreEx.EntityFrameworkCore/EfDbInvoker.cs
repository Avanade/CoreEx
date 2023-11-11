// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Database;
using CoreEx.Invokers;
using CoreEx.Results;
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
        protected override TResult OnInvoke<TResult>(InvokeArgs invokeArgs, IEfDb efDb, Func<InvokeArgs, TResult> func) => throw new NotSupportedException();

        /// <inheritdoc/>
        protected async override Task<TResult> OnInvokeAsync<TResult>(InvokeArgs invokeArgs, IEfDb efdb, Func<InvokeArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        {
            try
            {
                return await base.OnInvokeAsync(invokeArgs, efdb, func, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Result? eresult = null;
                if (ex is DbException dbex)
                    eresult = efdb.Database.HandleDbException(dbex);
                else if (ex is DbUpdateConcurrencyException)
                    eresult = Result.Fail(new ConcurrencyException());
                else if (ex is DbUpdateException deux && deux.InnerException != null && deux.InnerException is DbException dbex2)
                    eresult = efdb.Database.HandleDbException(dbex2);
                else if (ex is TargetInvocationException tiex && tiex.InnerException is DbException dbex3)
                    eresult = efdb.Database.HandleDbException(dbex3);

                if (eresult.HasValue && eresult.Value.IsFailure && eresult.Value.Error is CoreEx.Abstractions.IExtendedException)
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