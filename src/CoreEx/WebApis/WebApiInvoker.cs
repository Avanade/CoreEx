// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Http;
using CoreEx.Invokers;
using CoreEx.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.WebApis
{
    /// <summary>
    /// Provides pluggable invocation orchestration, logging and exception handling for <see cref="WebApiBase.RunAsync(Microsoft.AspNetCore.Http.HttpRequest, Func{WebApiParam, CancellationToken, Task{Microsoft.AspNetCore.Mvc.IActionResult}}, OperationType, CancellationToken)"/>.
    /// </summary>
    public class WebApiInvoker : InvokerBase<WebApiBase, WebApiParam>
    {
        /// <summary>
        /// Indicates whether to catch and handle any <see cref="Exception"/> thrown as a result of executing the underlying logic.
        /// </summary>
        /// <remarks>Defaults to <c>true</c>.</remarks>
        public bool CatchAndHandleExceptions { get; set; } = true;

        /// <inheritdoc/>
        protected async override Task<TResult> OnInvokeAsync<TResult>(WebApiBase owner, Func<CancellationToken, Task<TResult>> func, WebApiParam? param, CancellationToken cancellationToken)
        {
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            // Get and override the correlation-id.
            foreach (var name in owner.GetCorrelationIdNames())
            {
                if (param.Request.Headers.TryGetValue(name, out var values))
                {
                    owner.ExecutionContext.CorrelationId = values.First();
                    break;
                }
            }

            // Set correlation-id for the response.
            param.Request.HttpContext.Response.Headers.Add(HttpConsts.CorrelationIdHeaderName, owner.ExecutionContext.CorrelationId);

            // Start logging scope and begin work.
            using var scope = owner.Logger.BeginScope(new Dictionary<string, object>() { { HttpConsts.CorrelationIdHeaderName, owner.ExecutionContext.CorrelationId } });
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