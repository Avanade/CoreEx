// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Represents an extended <see cref="StatusCodeResult"/> that enables customization of the <see cref="HttpResponse"/>.
    /// </summary>
    public class ExtendedStatusCodeResult : StatusCodeResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedStatusCodeResult"/> class.
        /// </summary>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
        public ExtendedStatusCodeResult(HttpStatusCode statusCode) : base((int)statusCode) { }

        /// <summary>
        /// Gets or sets the <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders.Location"/> <see cref="Uri"/>.
        /// </summary>
        public Uri? Location { get; set; }

        /// <summary>
        /// Gets or sets the function to perform the extended <see cref="HttpResponse"/> customization.
        /// </summary>
        [JsonIgnore]
        public Func<HttpResponse, Task>? BeforeExtension { get; set; }

        /// <summary>
        /// Gets or sets the function to perform the extended <see cref="HttpResponse"/> customization.
        /// </summary>
        [JsonIgnore]
        public Func<HttpResponse, Task>? AfterExtension { get; set; }

        /// <inheritdoc/>
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (Location != null)
                context.HttpContext.Response.GetTypedHeaders().Location = Location;

            if (BeforeExtension != null)
                await BeforeExtension(context.HttpContext.Response).ConfigureAwait(false);

            await base.ExecuteResultAsync(context).ConfigureAwait(false);

            if (AfterExtension != null)
                await AfterExtension(context.HttpContext.Response).ConfigureAwait(false);
        }
    }
}