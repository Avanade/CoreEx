// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Invokers;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace CoreEx.Solace.PubSub
{
    /// <summary>
    /// Provides the standard <see cref="PubSubSender"/> invoker functionality.
    /// </summary>
    /// <remarks>Suppresses the <see cref="TransactionScope"/> as Azure Service Bus does not support distributed transactions and this may be invoked in the context of an already enlisted transaction.</remarks>
    public class PubSubSenderInvoker : InvokerBase<PubSubSender>
    {
        /// <inheritdoc/>
        protected override TResult OnInvoke<TResult>(InvokeArgs invokeArgs, PubSubSender invoker, Func<InvokeArgs, TResult> func)
        {
            TransactionScope? txn = null;
            try
            {
                txn = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
                return base.OnInvoke(invokeArgs, invoker, func);
            }
            finally
            {
                txn?.Dispose();
            }
        }

        /// <inheritdoc/>
        protected async override Task<TResult> OnInvokeAsync<TResult>(InvokeArgs invokeArgs, PubSubSender invoker, Func<InvokeArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        {
            TransactionScope? txn = null;
            try
            {
                txn = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
                return await base.OnInvokeAsync(invokeArgs, invoker, func, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                txn?.Dispose();
            }
        }
    }
}