﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

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
        protected async override Task<TResult> OnInvokeAsync<TResult>(IEfDb efdb, Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        {
            try
            {
                return await base.OnInvokeAsync(efdb, func, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Result eresult = Result.Success;
                if (ex is DbException dbex)
                    eresult = efdb.Database.HandleDbException(dbex);
                else if (ex is DbUpdateConcurrencyException)
                    eresult = Result.Fail(new ConcurrencyException());
                else if (ex is DbUpdateException deux && deux.InnerException != null && deux.InnerException is DbException dbex2)
                    eresult = efdb.Database.HandleDbException(dbex2);
                else if (ex is TargetInvocationException tiex && tiex.InnerException is DbException dbex3)
                    eresult = efdb.Database.HandleDbException(dbex3);

                if (eresult.IsFailure)
                {
                    var dresult = default(TResult);
                    if (dresult is IResult dir)
                        return (TResult)dir.ToFailure(eresult.Error);
                    else
                        eresult.ThrowOnError();
                }

                throw;
            }
        }
    }
}