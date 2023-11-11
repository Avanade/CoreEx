// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Invokers;
using CoreEx.Results;
using System;
using System.Threading;
using System.Threading.Tasks;
using Soc = Simple.OData.Client;

namespace CoreEx.OData
{
    /// <summary>
    /// Provides the standard <see cref="IOData"/> invoker functionality.
    /// </summary>
    public class ODataInvoker : InvokerBase<object, IOData> 
    {
        /// <inheritdoc/>
        protected override TResult OnInvoke<TResult>(InvokeArgs invokeArgs, object invoker, Func<InvokeArgs, TResult> func, IOData? args)
        {
            try
            {
                return base.OnInvoke(invokeArgs, invoker, func, args);
            }
            catch (Soc.WebRequestException odex)
            {
                var eresult = args!.HandleODataException(odex);
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

        /// <inheritdoc/>
        protected async override Task<TResult> OnInvokeAsync<TResult>(InvokeArgs invokeArgs, object invoker, Func<InvokeArgs, CancellationToken, Task<TResult>> func, IOData? args, CancellationToken cancellationToken)
        {
            try
            {
                return await base.OnInvokeAsync(invokeArgs, invoker, func, args, cancellationToken).ConfigureAwait(false);
            }
            catch (Soc.WebRequestException odex)
            {
                var eresult = args!.HandleODataException(odex);
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