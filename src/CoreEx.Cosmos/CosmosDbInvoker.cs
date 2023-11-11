// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Invokers;
using CoreEx.Results;
using Microsoft.Azure.Cosmos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides the standard <see cref="ICosmosDb"/> invoker functionality.
    /// </summary>
    /// <remarks>Catches any unhandled <see cref="CosmosException"/> and invokes <see cref="ICosmosDb.HandleCosmosException(CosmosException)"/> to handle before bubbling up.</remarks>
    public class CosmosDbInvoker : InvokerBase<ICosmosDb>
    {
        /// <inheritdoc/>
        protected override TResult OnInvoke<TResult>(InvokeArgs invokeArgs, ICosmosDb cosmos, Func<InvokeArgs, TResult> func) => throw new NotSupportedException();

        /// <inheritdoc/>
        protected async override Task<TResult> OnInvokeAsync<TResult>(InvokeArgs invokeArgs, ICosmosDb cosmos, Func<InvokeArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        {
            try
            {
                return await base.OnInvokeAsync(invokeArgs, cosmos, func, cancellationToken).ConfigureAwait(false);
            }
            catch (CosmosException cex)
            {
                var eresult = cosmos.HandleCosmosException(cex);
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