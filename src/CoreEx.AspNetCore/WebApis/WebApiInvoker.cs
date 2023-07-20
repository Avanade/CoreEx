// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Http;
using CoreEx.Invokers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Provides pluggable invocation orchestration, logging and exception handling for <see cref="WebApiBase.RunAsync"/>.
    /// </summary>
    public class WebApiInvoker : InvokerBase<WebApiBase, WebApiParam>
    {
        private static WebApiInvoker? _default;

        /// <summary>
        /// Gets the current configured instance (see <see cref="ExecutionContext.ServiceProvider"/>).
        /// </summary>
        public static WebApiInvoker Current => CoreEx.ExecutionContext.GetService<WebApiInvoker>() ?? (_default ??= new WebApiInvoker());

        /// <summary>
        /// Indicates whether to catch and handle any <see cref="Exception"/> thrown as a result of executing the underlying logic.
        /// </summary>
        /// <remarks>Defaults to <c>true</c>.</remarks>
        public bool CatchAndHandleExceptions { get; set; } = true;

        /// <inheritdoc/>
        protected override TResult OnInvoke<TResult>(InvokeArgs invokeArgs, WebApiBase invoker, Func<TResult> func, WebApiParam? args) => throw new NotSupportedException();

        /// <inheritdoc/>
        protected async override Task<TResult> OnInvokeAsync<TResult>(InvokeArgs invokeArgs, WebApiBase owner, Func<CancellationToken, Task<TResult>> func, WebApiParam? param, CancellationToken cancellationToken)
        {
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            // Get and override the correlation-id.
            foreach (var name in owner.GetCorrelationIdNames())
            {
                if (param.Request.Headers.TryGetValue(name, out var values))
                {
                    owner.ExecutionContext.CorrelationId = values.First()!;
                    break;
                }
            }

            // Set correlation-id for the response.
            param.Request.HttpContext.Response.Headers.Add(HttpConsts.CorrelationIdHeaderName, owner.ExecutionContext.CorrelationId);

            // Start logging scope and begin work.
            using (owner.Logger.BeginScope(new Dictionary<string, object>() { { HttpConsts.CorrelationIdHeaderName, owner.ExecutionContext.CorrelationId } }))
            {
                owner.Logger.LogDebug("WebApi started.");

                try
                {
                    var ar = await func(cancellationToken).ConfigureAwait(false);
                    owner.Logger.LogDebug("WebApi stopped; completed.");
                    return ar;
                }
                catch (Exception ex) when (CatchAndHandleExceptions)
                {
                    return (TResult) await WebApiBase.CreateActionResultFromExceptionAsync(owner, param.Request.HttpContext, ex, owner.Settings, owner.Logger, owner.OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!CatchAndHandleExceptions)
                {
                    owner.Logger.LogDebug("WebApi stopped; {Error} [{Type}]", ex.Message, ex.GetType().Name);
                    throw;
                }
            }
        }
    }
}