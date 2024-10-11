// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Represents an extended <see cref="ContentResult"/> that enables customization of the <see cref="HttpResponse"/>.
    /// </summary>
    public class ExtendedContentResult : ContentResult, IExtendedActionResult
    {
        /// <inheritdoc/>
        [JsonIgnore]
        public Func<HttpResponse, Task>? BeforeExtension { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public Func<HttpResponse, Task>? AfterExtension { get; set; }

        /// <inheritdoc/>
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (BeforeExtension != null)
                await BeforeExtension(context.HttpContext.Response).ConfigureAwait(false);

            await base.ExecuteResultAsync(context).ConfigureAwait(false);

            if (AfterExtension != null)
                await AfterExtension(context.HttpContext.Response).ConfigureAwait(false);
        }
    }
}