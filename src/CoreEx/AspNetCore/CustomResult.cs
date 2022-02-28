// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore
{
    /// <summary>
    /// Represents an <see cref="IActionResult"/> that enables customization of the <see cref="HttpResponse"/>.
    /// </summary>
    public class CustomResult : ActionResult, IStatusCodeActionResult
    {
        private readonly IStatusCodeActionResult _result;
        private readonly Func<HttpResponse, Task>? _action;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomResult"/> class.
        /// </summary>
        /// <param name="result">The parent <see cref="IActionResult"/>.</param>
        /// <param name="action">The function to perform the <see cref="HttpResponse"/> customization.</param>
        public CustomResult(IStatusCodeActionResult result, Func<HttpResponse, Task>? action)
        {
            _result = result ?? throw new ArgumentNullException(nameof(result));
            _action = action;
        }

        /// <inheritdoc/>
        public int? StatusCode => _result.StatusCode;

        /// <inheritdoc/>
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            await _result.ExecuteResultAsync(context).ConfigureAwait(false);
            if (_action != null)
                await _action(context.HttpContext.Response).ConfigureAwait(false);
        }
    }
}