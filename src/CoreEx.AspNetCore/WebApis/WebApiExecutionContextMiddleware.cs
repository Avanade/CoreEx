// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Provides an <see cref="ExecutionContext"/> handling middleware that (using dependency injection) enables additional configuration where required.
    /// </summary>
    /// <remarks>A new <see cref="ExecutionContext"/> <see cref="ExecutionContext.Current"/> is instantiated through dependency injection using the <see cref="HttpContext.RequestServices"/>.</remarks>
    /// <param name="next">The next <see cref="RequestDelegate"/>.</param>
    /// <param name="executionContextUpdate">The optional function to update the <see cref="ExecutionContext"/>. Defaults to <see cref="DefaultExecutionContextUpdate(HttpContext, ExecutionContext)"/> where not specified.</param>
    public class WebApiExecutionContextMiddleware(RequestDelegate next, Func<HttpContext, ExecutionContext, Task>? executionContextUpdate = null)
    {
        private readonly RequestDelegate _next = next.ThrowIfNull(nameof(next));
        private readonly Func<HttpContext, ExecutionContext, Task> _updateFunc = executionContextUpdate ?? DefaultExecutionContextUpdate;

        /// <summary>
        /// Gets the default username where it is unable to be inferred (<see cref="System.Security.Principal.IIdentity.Name"/> from the <see cref="HttpContext"/> <see cref="HttpContext.User"/>).
        /// </summary>
        /// <remarks>Defaults to '<c>Anonymous</c>'.</remarks>
        public static string DefaultUsername { get; set; } = "Anonymous";

        /// <summary>
        /// Represents the default <see cref="ExecutionContext"/> update function. 
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="ec">The <see cref="ExecutionContext"/>.</param>
        /// <remarks>The <see cref="ExecutionContext.UserName"/> will be set to the <see cref="System.Security.Principal.IIdentity.Name"/> from the <see cref="HttpContext"/> <see cref="HttpContext.User"/>; otherwise, <see cref="DefaultUsername"/> where <c>null</c>.</remarks>
        public static Task DefaultExecutionContextUpdate(HttpContext context, ExecutionContext ec)
        {
            context.ThrowIfNull(nameof(context));
            ec.ThrowIfNull(nameof(ec));

            ec.UserName = context.User?.Identity?.Name ?? DefaultUsername;
            ec.Timestamp = context.RequestServices.GetService<ISystemTime>()?.UtcNow ?? SystemTime.Default.UtcNow;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Invokes the <see cref="WebApiExecutionContextMiddleware"/>.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            context.ThrowIfNull(nameof(context));

            var ec = context.RequestServices.GetRequiredService<ExecutionContext>();
            ec.ServiceProvider ??= context.RequestServices;

            await _updateFunc(context, ec).ConfigureAwait(false);
            await _next(context).ConfigureAwait(false);
        }
    }
}