// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Configuration;
using CoreEx.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CoreEx.WebApis
{
    /// <summary>
    /// Provides a <b>CoreEx</b> oriented <see cref="Exception"/> handling middleware that is <see cref="IExtendedException"/> and <see cref="IExceptionResult"/> result aware.
    /// </summary>
    public class WebApiExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SettingsBase _settings;
        private readonly ILogger _logger;

        /// <summary>
        /// Indicates whether to include the unhandled <see cref="Exception"/> details in the response.
        /// </summary>
        public bool IncludeUnhandledExceptionInResponse { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiExceptionHandlerMiddleware"/>.
        /// </summary>
        /// <param name="next">The next <see cref="RequestDelegate"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public WebApiExceptionHandlerMiddleware(RequestDelegate next, SettingsBase settings, ILogger<WebApiExceptionHandlerMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the <see cref="WebApiExceptionHandlerMiddleware"/>.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                IActionResult ar;
                if (ex is IExtendedException eex)
                {
                    if (eex.ShouldBeLogged)
                        _logger.LogError(ex, "{Error}", ex.Message);

                    context.Response.Headers.Add(HttpConsts.ErrorTypeHeaderName, eex.ErrorType);
                    context.Response.Headers.Add(HttpConsts.ErrorCodeHeaderName, eex.ErrorCode.ToString());
                    ar = eex.ToResult();
                }
                else if (ex is IExceptionResult rex)
                {
                    _logger.LogCritical(ex, "WebApi encountered an Unhandled Exception: {Error}", ex.Message);
                    ar = rex.ToResult();
                }
                else
                {
                    _logger.LogCritical(ex, "WebApi encountered an Unhandled Exception: {Error}", ex.Message);
                    ar = ex.ToUnexpectedResult(_settings.IncludeExceptionInResult);
                }

                var ac = new ActionContext(context, new RouteData(), new ActionDescriptor());
                await ar.ExecuteResultAsync(ac).ConfigureAwait(false);
            }
        }
    }
}