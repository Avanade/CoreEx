// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Http;
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
                await OnBeforeAsync(owner, param, cancellationToken).ConfigureAwait(false);
                var ar = await func(cancellationToken).ConfigureAwait(false);
                ar = await OnAfterSuccessAsync(owner, param, ar, cancellationToken).ConfigureAwait(false);
                owner.Logger.LogDebug("WebApi stopped; completed successfully.");
                return ar;
            }
            catch (Exception ex) when (CatchAndHandleExceptions)
            {
                owner.Logger.LogDebug("WebApi stopped; {Type}: {Error}", ex.GetType().Name, ex.Message);

                if (ex is IExtendedException eex)
                {
                    if (eex.ShouldBeLogged)
                        owner.Logger.LogError(ex, "{Error}", ex.Message);

                    param.Request.HttpContext.Response.Headers.Add(HttpConsts.ErrorTypeHeaderName, eex.ErrorType);
                    param.Request.HttpContext.Response.Headers.Add(HttpConsts.ErrorCodeHeaderName, eex.ErrorCode.ToString());

                    return await OnAfterExceptionAsync(owner, param, ex, (TResult)eex.ToResult(), cancellationToken).ConfigureAwait(false);
                }

                if (ex is IExceptionResult rex)
                {
                    owner.Logger.LogCritical(ex, "WebApi encountered an Unhandled Exception: {Error}", ex.Message);
                    return await OnAfterExceptionAsync(owner, param, ex, (TResult)rex.ToResult(), cancellationToken).ConfigureAwait(false);
                }

                var ar = await owner.OnUnhandledExceptionAsync(ex, cancellationToken).ConfigureAwait(false);
                if (ar != null)
                    return await OnAfterExceptionAsync(owner, param, ex, (TResult)ar, cancellationToken).ConfigureAwait(false);

                owner.Logger.LogCritical(ex, "WebApi encountered an Unhandled Exception: {Error}", ex.Message);
                return await OnAfterExceptionAsync(owner, param, ex, (TResult)ex.ToUnexpectedResult(owner.Settings.IncludeExceptionInResult), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!CatchAndHandleExceptions)
            {
                owner.Logger.LogDebug("WebApi stopped; {Type}: {Error}", ex.GetType().Name, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Provides an opportunity to perform additional processing before the underlying function logic is invoked.
        /// </summary>
        /// <param name="owner">The <see cref="WebApiBase"/> owner/caller</param>
        /// <param name="param">The corresponding <see cref="WebApiParam"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        protected virtual Task OnBeforeAsync(WebApiBase owner, WebApiParam param, CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <summary>
        /// Provides an opportunity to perform additional processing after the underlying function logic has been invoked successfully.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="owner">The <see cref="WebApiBase"/> owner/caller</param>
        /// <param name="param">The corresponding <see cref="WebApiParam"/>.</param>
        /// <param name="result">The result from the function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        protected virtual Task<TResult> OnAfterSuccessAsync<TResult>(WebApiBase owner, WebApiParam param, TResult result, CancellationToken cancellationToken = default) => Task.FromResult(result);

        /// <summary>
        /// Provides an opportunity to perform additional processing after the underlying function logic has thrown an <paramref name="exception"/>.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="owner">The <see cref="WebApiBase"/> owner/caller</param>
        /// <param name="param">The corresponding <see cref="WebApiParam"/>.</param>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="result">The result from the function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        /// <remarks>Only invoked when <see cref="CatchAndHandleExceptions"/> is set to <c>true</c>. The exception will have already been converted to the corresponding result where applicable.</remarks>
        protected virtual Task<TResult> OnAfterExceptionAsync<TResult>(WebApiBase owner, WebApiParam param, Exception exception, TResult result, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }
}