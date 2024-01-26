// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Provides a <b>CoreEx</b> oriented <see cref="Exception"/> handling middleware that is <see cref="IExtendedException"/> result aware.
    /// </summary>
    /// <param name="next">The next <see cref="RequestDelegate"/>.</param>
    /// <param name="settings">The <see cref="SettingsBase"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public class WebApiExceptionHandlerMiddleware(RequestDelegate next, SettingsBase settings, ILogger<WebApiExceptionHandlerMiddleware> logger)
    {
        private readonly RequestDelegate _next = next.ThrowIfNull(nameof(next));
        private readonly SettingsBase _settings = settings.ThrowIfNull(nameof(settings));
        private readonly ILogger _logger = logger.ThrowIfNull(nameof(logger));

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
                var ar = await WebApiBase.CreateActionResultFromExceptionAsync(null, context, ex, _settings, _logger, null, default).ConfigureAwait(false);
                var ac = new ActionContext(context, new RouteData(), new ActionDescriptor());
                await ar.ExecuteResultAsync(ac).ConfigureAwait(false);
            }
        }
    }
}