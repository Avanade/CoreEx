// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.AspNetCore.Http;
using CoreEx.Entities;
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
        /// <summary>
        /// Gets or sets the <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders"/> <see cref="CoreEx.Http.HttpConsts.MessagesHeaderName"/> <see cref="MessageItemCollection"/>.
        /// </summary>
        /// <remarks>Defaults to the <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.Messages"/>.
        /// <para><i>Note:</i> These are only written to the headers where the <see cref="ContentResult.StatusCode"/> is considered successful; i.e. is in the 200-299 range.</para></remarks>
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

            if (BeforeExtension != null)
                await BeforeExtension(context.HttpContext.Response).ConfigureAwait(false);

            await base.ExecuteResultAsync(context).ConfigureAwait(false);

            if (AfterExtension != null)
                await AfterExtension(context.HttpContext.Response).ConfigureAwait(false);
        }
    }
}