// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.RefData;
using CoreEx.AspNetCore.WebApis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides <see cref="IApplicationBuilder"/> extensions.
    /// </summary>
    public static class AspNetCoreApplicationBuilderExtensions
    {
        /// <summary>
        /// Registers the action to <paramref name="executionContextUpdate"/> the <see cref="ExecutionContext"/> for a request. 
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="executionContextUpdate">An optional function to update the <see cref="ExecutionContext"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseExecutionContext(this IApplicationBuilder builder, Func<HttpContext, ExecutionContext, Task>? executionContextUpdate = null)
            => builder.UseMiddleware<WebApiExecutionContextMiddleware>(executionContextUpdate ?? WebApiExecutionContextMiddleware.DefaultExecutionContextUpdate);

        /// <summary>
        /// Adds the <see cref="WebApiExceptionHandlerMiddleware"/> to the <see cref="IApplicationBuilder"/> request execution pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseWebApiExceptionHandler(this IApplicationBuilder builder) => builder.UseMiddleware<WebApiExceptionHandlerMiddleware>();

        /// <summary>
        /// Registers the <see cref="ReferenceDataOrchestrator"/> instance (using <see cref="ReferenceDataOrchestrator.SetCurrent(ReferenceDataOrchestrator)"/>) so that it can be universally referenced.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseReferenceDataOrchestrator(this IApplicationBuilder builder)
        {
            ReferenceDataOrchestrator.SetCurrent(builder.ApplicationServices.GetRequiredService<ReferenceDataOrchestrator>());
            return builder;
        }
    }
}