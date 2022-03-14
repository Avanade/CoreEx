// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Http;
using Microsoft.AspNetCore.Http;
using System;

namespace CoreEx.AspNetCore
{
    /// <summary>
    /// Represents a <see cref="WebApi"/> parameter.
    /// </summary>
    public class WebApiParam
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiParam"/> class.
        /// </summary>
        /// <param name="webApi">The parent <see cref="WebApi"/> instance.</param>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="requestOptions">The <see cref="RequestOptions"/>.</param>
        public WebApiParam(WebApi webApi, HttpRequest request, HttpRequestOptions requestOptions)
        {
            WebApi = webApi ?? throw new ArgumentNullException(nameof(webApi));
            Request = request ?? throw new ArgumentNullException(nameof(request));
            RequestOptions = requestOptions ?? throw new ArgumentNullException(nameof(requestOptions));
        }

        /// <summary>
        /// Gets the parent (invoking) <see cref="WebApi"/>.
        /// </summary>
        public WebApi WebApi { get; }

        /// <summary>
        /// Gets or sets the <see cref="HttpRequest"/>.
        /// </summary>
        public HttpRequest Request { get; }

        /// <summary>
        /// Gets or sets the <see cref="HttpRequestOptions"/>.
        /// </summary>
        public HttpRequestOptions RequestOptions { get; }

        /// <summary>
        /// Gets the <see cref="Entities.PagingArgs"/>.
        /// </summary>
        public PagingArgs? Paging => RequestOptions.Paging;
    }
}