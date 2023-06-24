// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.AspNetCore.Http;
using CoreEx.Configuration;
using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.Json;
using CoreEx.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
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
            Invoker = invoker ?? WebApiInvoker.Current;
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
            var wap = new WebApiParam(this, new WebApiRequestOptions(request), operationType);
            return await Invoker.InvokeAsync(this, wap, (w, ct) => function(w, ct), wap, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Validate the <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="wap">The <see cref="WebApiParam"/>.</param>
        /// <param name="useValue">Indicates whether to use the <paramref name="value"/>; otherwise, deserialize the JSON from the <see cref="WebApiParam.Request"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The optional <see cref="IValidator{T}"/> to validate the value (only invoked where the value is not <c>null</c>).</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Exception"/> where there is an error; otherwise, <see cref="WebApiParam{T}"/> for success.</returns>
        protected internal async Task<(WebApiParam<TValue>?, Exception?)> ValidateValueAsync<TValue>(WebApiParam wap, bool useValue, TValue value, bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
        {
            if (wap == null)
                throw new ArgumentNullException(nameof(wap));

            WebApiParam<TValue> wapv;
            if (useValue)
            {
                if (valueIsRequired && value == null)
                    return (null, new ValidationException($"{HttpResultExtensions.InvalidJsonMessagePrefix} Value is mandatory."));

                if (value != null && validator != null)
                {
                    var vr = await validator.ValidateAsync(value, cancellationToken).ConfigureAwait(false);
                    return (null, vr.ToException());
                }

                wapv = new WebApiParam<TValue>(wap, value);
            }
            else
            {
                var vr = await wap.Request.ReadAsJsonValueAsync(JsonSerializer, valueIsRequired, validator, cancellationToken).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return (null, vr.ValidationException);

                wapv = new WebApiParam<TValue>(wap, vr.Value);
            }

            return (wapv, null);
        }

        /// <summary>
        /// Provides an opportunity to handle an unhandled exception.
        /// </summary>
        /// <param name="ex">The unhandled <see cref="Exception"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> to return where handled; otherwise, <c>null</c> which in turn will result in it be handled by <see cref="CreateActionResultFromExceptionAsync"/>.</returns>
        protected internal virtual Task<IActionResult?> OnUnhandledExceptionAsync(Exception ex, ILogger logger, CancellationToken cancellationToken) => UnhandledExceptionAsync(ex, logger, cancellationToken);

        /// <summary>
        /// Gets or sets the delegate that is invoked as an opportunity to handle an unhandled exception.
        /// </summary>
        /// <remarks>This is invoked by <see cref="OnUnhandledExceptionAsync(Exception, ILogger, CancellationToken)"/>.
        /// <para>This should also include any logging requirements; not performed by default as it may not be required for all exception types.</para></remarks>
        public Func<Exception, ILogger, CancellationToken, Task<IActionResult?>> UnhandledExceptionAsync { get; set; } = (_, _, _) => Task.FromResult<IActionResult?>(null!);

        /// <summary>
        /// Creates an <see cref="IActionResult"/> from an <paramref name="exception"/>.
        /// </summary>
        /// <param name="owner">The optional owning <see cref="WebApiBase"/>.</param>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="unhandledExceptionAsync">The delegate that is invoked as an opportunity to handle an unhandled exception.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IActionResult"/>.</returns>
        public static async Task<IActionResult> CreateActionResultFromExceptionAsync(WebApiBase? owner, HttpContext context, Exception exception, SettingsBase settings, ILogger logger, Func<Exception, ILogger, CancellationToken, Task<IActionResult?>>? unhandledExceptionAsync = null, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (exception is null) throw new ArgumentNullException(nameof(exception));
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            if (owner is not null && !owner.Invoker.CatchAndHandleExceptions)
                throw exception;

            logger.LogDebug("WebApi error: {Error} [{Type}]", exception.Message, exception.GetType().Name);

            IActionResult? ar = null;
            if (exception is IExtendedException eex)
            {
                if (eex.ShouldBeLogged)
                    logger.LogError(exception, "{Error}", exception.Message);

                ar = CreateActionResultFromExtendedException(eex);
            }
            else
            {
                if (unhandledExceptionAsync is not null)
                    ar = await unhandledExceptionAsync(exception, logger, cancellationToken).ConfigureAwait(false);

                if (ar is null)
                {
                    logger.LogCritical(exception, "WebApi unhandled exception: {Error}", exception.Message);
                    ar = CreateActionResultForUnexpectedResult(exception, settings.IncludeExceptionInResult);
                }
            }

            return ar;
        }

        /// <summary>
        /// Creates an <see cref="IActionResult"/> from an <paramref name="extendedException"/>.
        /// </summary>
        /// <param name="extendedException">The <see cref="IExtendedException"/>.</param>
        public static IActionResult CreateActionResultFromExtendedException(IExtendedException extendedException)
        {
            if (extendedException is ValidationException vex && vex.Messages is not null && vex.Messages.Count > 0)
            {
                var msd = new ModelStateDictionary();
                foreach (var item in vex.Messages.GetMessagesForType(MessageType.Error))
                {
                    if (item.Property is not null && item.Text is not null)
                        msd.AddModelError(item.Property, item.Text);
                }

                return new BadRequestObjectResult(msd);
            }

            return new ExtendedContentResult
            {
                Content = extendedException.Message,
                ContentType = MediaTypeNames.Text.Plain,
                StatusCode = (int)extendedException.StatusCode,
                BeforeExtension = r =>
                {
                    var th = r.GetTypedHeaders();
                    th.Set(HttpConsts.ErrorTypeHeaderName, extendedException.ErrorType);
                    th.Set(HttpConsts.ErrorCodeHeaderName, extendedException.ErrorCode.ToString());
                    if (extendedException is TransientException tex)
                        th.Set(HeaderNames.RetryAfter, tex.RetryAfterSeconds);

                    return Task.CompletedTask;
                }
            };
        }

        /// <summary>
        /// Creates an <see cref="IActionResult"/> from an unexpected <paramref name="exception"/>.
        /// </summary>
        private static IActionResult CreateActionResultForUnexpectedResult(Exception exception, bool includeExceptionInResult) => includeExceptionInResult
            ? new ContentResult { StatusCode = (int)HttpStatusCode.InternalServerError, ContentType = MediaTypeNames.Text.Plain, Content = $"An unexpected internal server error has occurred. CorrelationId={ExecutionContext.Current.CorrelationId} Exception={exception}" }
            : new ContentResult { StatusCode = (int)HttpStatusCode.InternalServerError, ContentType = MediaTypeNames.Text.Plain, Content = $"An unexpected internal server error has occurred. CorrelationId={ExecutionContext.Current.CorrelationId}" };
    }
}