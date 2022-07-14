// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace CoreEx.Business
{
    /// <summary>
    /// Adds capabilities (wraps) an <see cref="InvokerBase{TOwner, TParam}"/> enabling standard functionality to be added to all <b>business tier</b> invocations using a <see cref="BusinessInvokerArgs"/> to configure the supporting capabilities.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class BusinessInvokerBase : InvokerBase<object, BusinessInvokerArgs>
    {
        /// <inheritdoc/>
        protected async override Task<TResult> OnInvokeAsync<TResult>(object owner, Func<CancellationToken, Task<TResult>> func, BusinessInvokerArgs? param, CancellationToken cancellationToken)
        {
            BusinessInvokerArgs bia = param ?? BusinessInvokerArgs.Default;
            TransactionScope? txn = null;
            var ot = CoreEx.ExecutionContext.Current.OperationType;
            if (bia.OperationType.HasValue)
                CoreEx.ExecutionContext.Current.OperationType = bia.OperationType.Value;

            try
            {
                // Initiate a transaction where requested.
                if (bia.IncludeTransactionScope)
                    txn = new TransactionScope(bia.TransactionScopeOption, TransactionScopeAsyncFlowOption.Enabled);

                // Invoke the underlying logic.
                var result = await func(cancellationToken).ConfigureAwait(false);

                // Send any published events where applicable.
                if (bia.EventPublisher != null)
                    await bia.EventPublisher.SendAsync(cancellationToken).ConfigureAwait(false);

                // Complete the transaction where requested to orchestrate one.
                txn?.Complete();
                return result;
            }
            catch (Exception ex)
            {
                bia.ExceptionHandler?.Invoke(ex);
                throw;
            }
            finally
            {
                txn?.Dispose();
                CoreEx.ExecutionContext.Current.OperationType = ot;
            }
        }
    }
}
