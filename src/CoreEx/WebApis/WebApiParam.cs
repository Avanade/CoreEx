// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using Microsoft.AspNetCore.Http;
using System;

namespace CoreEx.WebApis
{
    /// <summary>
    /// Represents a <see cref="WebApi"/> parameter.
    /// </summary>
    public class WebApiParam
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiParam"/> class.
        /// </summary>
        /// <param name="webApi">The parent <see cref="WebApiBase"/> instance.</param>
        /// <param name="requestOptions">The <see cref="WebApiRequestOptions"/>.</param>
        public WebApiParam(WebApiBase webApi, WebApiRequestOptions requestOptions)
        {
            WebApi = webApi ?? throw new ArgumentNullException(nameof(webApi));
            RequestOptions = requestOptions ?? throw new ArgumentNullException(nameof(requestOptions));
        }

        /// <summary>
        /// Gets the parent (invoking) <see cref="WebApi"/>.
        /// </summary>
        public WebApiBase WebApi { get; }

        /// <summary>
        /// Gets or sets the <see cref="HttpRequest"/>.
        /// </summary>
        public HttpRequest Request => RequestOptions.Request;

        /// <summary>
        /// Gets or sets the <see cref="WebApiRequestOptions"/>.
        /// </summary>
        public WebApiRequestOptions RequestOptions { get; }

        /// <summary>
        /// Gets the <see cref="Entities.PagingArgs"/>.
        /// </summary>
        public PagingArgs? Paging => RequestOptions.Paging;

        /// <summary>
        /// Inspects the <paramref name="value"/> to update the <see cref="WebApiRequestOptions.ETag"/> where appropriate.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The value to support fluent-style method-chaining.</returns>
        public T InspectValue<T>(T value)
        {
            if (RequestOptions.ETag == null && value != null && value is IETag etag && etag.ETag != null)
                RequestOptions.ETag = etag.ETag;

            return value;
        }
    }
}