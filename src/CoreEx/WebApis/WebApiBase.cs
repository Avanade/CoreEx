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
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.WebApis
{
    /// <summary>
    /// Provides the base Web API execution encapsulation to <see cref="RunAsync(HttpRequest, Func{WebApiParam, CancellationToken, Task{IActionResult}}, OperationType, CancellationToken)"/> the underlying logic in a consistent manner.
    /// </summary>
    public abstract class WebApiBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApi"/> class.
        /// </summary>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="invoker">The <see cref="WebApiInvoker"/>; defaults where not specified.</param>
        protected WebApiBase(ExecutionContext executionContext, SettingsBase settings, IJsonSerializer jsonSerializer, ILogger<WebApiBase> logger, WebApiInvoker? invoker)
        {
            ExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            JsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Invoker = invoker ?? new WebApiInvoker();
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
        /// Gets the <see cref="WebApiInvoker"/>.
        /// </summary>
        public WebApiInvoker Invoker { get; }

        /// <summary>
        /// Gets or sets the list of secondary correlation identifier names.
        /// </summary>
        /// <remarks>Searches the <see cref="HttpRequest.Headers"/> for <see cref="HttpConsts.CorrelationIdHeaderName"/> or one of the other <see cref="SecondaryCorrelationIdNames"/> to determine the <see cref="ExecutionContext.CorrelationId"/> (uses first value found in sequence).</remarks>
        public IEnumerable<string> SecondaryCorrelationIdNames { get; set; } = new string[] { "x-ms-client-tracking-id" };

        /// <summary>
        /// Gets the list of correlation identifier names, being <see cref="HttpConsts.CorrelationIdHeaderName"/> and <see cref="SecondaryCorrelationIdNames"/> (inclusive).
        /// </summary>
        /// <returns>The list of correlation identifier names.</returns>
        public virtual IEnumerable<string> GetCorrelationIdNames()
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
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>This is, and must be, used by all methods that process an <see cref="HttpRequest"/> to ensure that the standardized before and after, success and error, handling occurs as required.</remarks>
        protected async Task<IActionResult> RunAsync(HttpRequest request, Func<WebApiParam, CancellationToken, Task<IActionResult>> function, OperationType operationType = OperationType.Unspecified, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            // Invoke the "actual" function via the pluggable invoker.
            ExecutionContext.OperationType = operationType;
            var wap = new WebApiParam(this, new WebApiRequestOptions(request));
            return await Invoker.InvokeAsync(this, ct => function(wap, ct), wap, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Provides an opportunity to handle an unhandled exception.
        /// </summary>
        /// <param name="ex">The unhandled <see cref="Exception"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> to return where handled; otherwise, <c>null</c> which in turn will result in <see cref="ExceptionResultExtensions.ToUnexpectedResult(Exception, bool)"/>.</returns>
        /// <remarks>Any <see cref="IExtendedException"/> exceptions will be handled per their implementation; see <see cref="IExceptionResult.ToResult"/>.</remarks>
        protected internal virtual Task<IActionResult?> OnUnhandledExceptionAsync(Exception ex, CancellationToken cancellationToken) => OnUnhandledException(ex, cancellationToken);

        /// <summary>
        /// Gets or sets the delegate that is invoked as an opportunity to handle an unhandled exception.
        /// </summary>
        /// <remarks>This is invoked by <see cref="OnUnhandledExceptionAsync(Exception, CancellationToken)"/>.</remarks>
        public Func<Exception, CancellationToken, Task<IActionResult?>> OnUnhandledException { get; set; } = (_, __) => Task.FromResult<IActionResult?>(null!);
    }
}