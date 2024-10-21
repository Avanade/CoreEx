// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.AspNetCore.Http;
using CoreEx.Entities;
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
    /// <param name="statusCode">The status code value.</param>
    public class ExtendedStatusCodeResult(int statusCode) : StatusCodeResult(statusCode), IExtendedActionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedStatusCodeResult"/> class with the specified <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
        public ExtendedStatusCodeResult(HttpStatusCode statusCode) : this((int)statusCode) { }

        /// <summary>
        /// Gets or sets the <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders.Location"/> <see cref="Uri"/>.
        /// </summary>
        public Uri? Location { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders"/> <see cref="CoreEx.Http.HttpConsts.MessagesHeaderName"/> <see cref="MessageItemCollection"/>.
        /// </summary>
        /// <remarks>Defaults to the <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.Messages"/>.
        /// <para><i>Note:</i> These are only written to the headers where the <see cref="StatusCodeResult.StatusCode"/> is considered successful; i.e. is in the 200-299 range.</para></remarks>
        public MessageItemCollection? Messages { get; set; } = ExecutionContext.HasCurrent && ExecutionContext.Current.HasMessages ? ExecutionContext.Current.Messages : null;

        /// <inheritdoc/>
        [JsonIgnore]
        public Func<HttpResponse, Task>? BeforeExtension { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public Func<HttpResponse, Task>? AfterExtension { get; set; }

        /// <inheritdoc/>
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (StatusCode >= 200 || StatusCode <= 299)
                context.HttpContext.Response.Headers.AddMessages(Messages);

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