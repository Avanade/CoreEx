// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
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
        protected async override Task<TResult> OnInvokeAsync<TResult>(ICosmosDb cosmos, Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        {
            try
            {
                return await base.OnInvokeAsync(cosmos, func, cancellationToken).ConfigureAwait(false);
            }
            catch (CosmosException cex)
            {
                cosmos.HandleCosmosException(cex);
                throw;
            }
        }
    }
}