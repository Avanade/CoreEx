// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Invokers;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace CoreEx.Results.Abstractions
{
    /// <summary>
    /// Provides the try-catch capability for a <see cref="TryCatchWith{T}"/>.
    /// </summary>
    [DebuggerStepThrough]
    public class TryCatchWithInvoker<T> : InvokerBase<TryCatchWith<T>> where T : IResult
    {
        private static TryCatchWithInvoker<T>? _default;

        /// <summary>
        /// Gets the current configured instance (see <see cref="ExecutionContext.ServiceProvider"/>).
        /// </summary>
        public static TryCatchWithInvoker<T> Current => CoreEx.ExecutionContext.GetService<TryCatchWithInvoker<T>>() ?? (_default ??= new TryCatchWithInvoker<T>());

        /// <inheritdoc/>
        protected override TResult OnInvoke<TResult>(TryCatchWith<T> invoker, Func<TResult> func)
        {
            try
            {
                return base.OnInvoke(invoker, func);
            }
            catch (Exception ex)
            {
                var hex = invoker.HandleException(ex);
                if (hex is not null)
                {
                    var r = default(TResult)!;
                    if (r is IResult ir)
                        return (TResult)ir.ToFailure(hex);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        protected async override Task<TResult> OnInvokeAsync<TResult>(TryCatchWith<T> invoker, Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        {
            try
            {
                return await base.OnInvokeAsync(invoker, func, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var hex = invoker.HandleException(ex);
                if (hex is not null)
                {
                    var r = default(TResult)!;
                    if (r is IResult ir)
                        return (TResult)ir.ToFailure(hex);
                }

                throw;
            }
        }
    }
}