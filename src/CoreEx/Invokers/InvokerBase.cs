// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace CoreEx.Invokers
{
    /// <summary>
    /// Adds capabilities (wraps) an <see cref="InvokerBase{TOwner, TParam}"/> enabling standard functionality to be added to all <b>business services tier</b> (backend) invocations using a <see cref="InvokerArgs"/> to configure the 
    /// supporting capabilities (for example, <see cref="InvokerArgs.IncludeTransactionScope">transactions</see> and <see cref="InvokerArgs.EventPublisher">event publishing</see>).
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class InvokerBase : InvokerBase<object, InvokerArgs>
    {
        /// <inheritdoc/>
        protected override TResult OnInvoke<TResult>(InvokeArgs invokeArgs, object owner, Func<TResult> func, InvokerArgs param)
        {
            InvokerArgs bia = param;
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
                var result = func();

                // Where using Railway-oriented programming, rollback the transaction where a failure has occurred.
                if (result is IResult r && r.IsFailure)
                    return result;

                // Send any published events where applicable.
                if (bia.EventPublisher != null)
                    Invoker.RunSync(() => bia.EventPublisher.SendAsync(default));

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

        /// <inheritdoc/>
        protected async override Task<TResult> OnInvokeAsync<TResult>(InvokeArgs invokeArgs, object owner, Func<CancellationToken, Task<TResult>> func, InvokerArgs param, CancellationToken cancellationToken)
        {
            InvokerArgs bia = param;
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

                // Where using Railway-oriented programming, rollback the transaction where a failure has occurred.
                if (result is IResult r && r.IsFailure)
                    return result;

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
