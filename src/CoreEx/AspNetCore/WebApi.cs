// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Configuration;
using CoreEx.Http;
using CoreEx.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore
{
    /// <summary>
    /// Provides the Web API execution encapsulation to run the underlying logic in a consistent manner.
    /// </summary>
    public class WebApi
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApi"/> class.
        /// </summary>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public WebApi(ExecutionContext executionContext, SettingsBase settings, IJsonSerializer jsonSerializer, ILogger<WebApi> logger)
        {
            ExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            JsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the <see cref="CoreEx.ExecutionContext"/>.
        /// </summary>
        public ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        public SettingsBase Settings { get; }

        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        public IJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Gets or sets the list of secondary correlation identifier names.
        /// </summary>
        protected virtual IEnumerable<string> SecondaryCorrelationIdNames { get; set; } = new string[] { "x-ms-client-tracking-id" };

        /// <summary>
        /// Gets the list of correlation identifier names, being <see cref="HttpConsts.CorrelationIdHeaderName"/> and <see cref="SecondaryCorrelationIdNames"/> (inclusive).
        /// </summary>
        /// <returns>The list of correlation identifier names.</returns>
        protected virtual IEnumerable<string> GetCorrelationIdNames()
        {
            var list = new List<string>(new string[] { HttpConsts.CorrelationIdHeaderName });
            list.AddRange(SecondaryCorrelationIdNames);
            return list;
        }

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> returning a corresponding <see cref="IActionResult"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        protected async Task<IActionResult> RunAsync(HttpRequest request, Func<WebApiParam, Task<IActionResult>> function, OperationType operationType)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            ExecutionContext.OperationType = operationType;
            var wap = new WebApiParam(this, request, request.GetRequestOptions(true));

            foreach (var name in GetCorrelationIdNames())
            {
                if (request.Headers.TryGetValue(name, out var values))
                {
                    ExecutionContext.Current.CorrelationId = values.First();
                    break;
                }
            }

            request.HttpContext.Response.Headers.Add(HttpConsts.CorrelationIdHeaderName, ExecutionContext.CorrelationId);

            var scope = Logger.BeginScope(new Dictionary<string, object>() { { HttpConsts.CorrelationIdHeaderName, ExecutionContext.CorrelationId } });

            try
            {
                return await function(wap).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is IExtendedException eex)
                {
                    if (eex.ShouldBeLogged)
                        Logger.LogError(ex, "{Error}", ex.Message);

                    return eex.ToResult();
                }

                Logger.LogCritical(ex, "Executor encountered an Unhandled Exception: {Error}", ex.Message);
                return (ex is IExceptionResult rex) ? rex.ToResult() : ex.ToUnexpectedResult(Settings.IncludeExceptionInResult);
            }
            finally
            {
                scope.Dispose();
            }
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Get"/> operation.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The primary status code where successful.</param>
        /// <param name="alternateStatusCode">The alternate status code where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public async Task<IActionResult> GetAsync<TResult>(HttpRequest request, Func<WebApi, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NotFound, OperationType operationType = OperationType.Read)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Method != HttpMethods.Get)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Get}' to use {nameof(GetAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async wap =>
            {
                var result = await function(this).ConfigureAwait(false);
                return ValueContentResult.CreateResult(result, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: true, nullReplacement: null, location: null);
            }, operationType).ConfigureAwait(false);
        }
    }
}