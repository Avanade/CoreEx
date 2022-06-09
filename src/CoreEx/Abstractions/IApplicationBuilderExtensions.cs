// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.WebApis;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides <see cref="IApplicationBuilder"/> extensions.
    /// </summary>
    public static class IApplicationBuilderExtensions
    {
        /// <summary>
        /// Registers the action to <paramref name="executionContextUpdate"/> the <see cref="ExecutionContext"/> for a request. 
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="executionContextUpdate">An optional function to update the <see cref="ExecutionContext"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseExecutionContext(this IApplicationBuilder builder, Func<HttpContext, ExecutionContext, Task>? executionContextUpdate = null)
            => builder.UseMiddleware<WebApiExecutionContextMiddleware>(executionContextUpdate ?? WebApiExecutionContextMiddleware.DefaultExecutionContextUpdate);
    }
}