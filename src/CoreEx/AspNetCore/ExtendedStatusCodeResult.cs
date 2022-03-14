// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore
{
    /// <summary>
    /// Represents an extended <see cref="StatusCodeResult"/> that enables customization of the <see cref="HttpResponse"/>.
    /// </summary>
    public class ExtendedStatusCodeResult : StatusCodeResult
    {
        private readonly Func<HttpResponse, Task>? _action;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedStatusCodeResult"/> class.
        /// </summary>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
        /// <param name="action">The function to perform the extended <see cref="HttpResponse"/> customization.</param>
        public ExtendedStatusCodeResult(HttpStatusCode statusCode, Func<HttpResponse, Task>? action) : base((int)statusCode) => _action = action;

        /// <inheritdoc/>
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            await base.ExecuteResultAsync(context).ConfigureAwait(false);
            if (_action != null)
                await _action(context.HttpContext.Response).ConfigureAwait(false);
        }
    }
}