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
        /// <param name="operationType">The <see cref="CoreEx.OperationType"/>.</param>
        public WebApiParam(WebApiBase webApi, WebApiRequestOptions requestOptions, OperationType operationType = OperationType.Unspecified)
        {
            WebApi = webApi ?? throw new ArgumentNullException(nameof(webApi));
            RequestOptions = requestOptions ?? throw new ArgumentNullException(nameof(requestOptions));
            OperationType = operationType;
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
        /// Gets the <see cref="CoreEx.OperationType"/>.
        /// </summary>
        public OperationType OperationType { get; }

        /// <summary>
        /// Inspects the <paramref name="value"/> to either update the <see cref="WebApiRequestOptions.ETag"/> or <paramref name="value"/> <see cref="IETag.ETag"/> where appropriate.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The value to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="WebApiRequestOptions.ETag"/> takes precedence over the <paramref name="value"/> <see cref="IETag.ETag"/> and will override value where specified.</remarks>
        public T InspectValue<T>(T value)
        {
            if (RequestOptions.ETag != null && value != null && value is IETag etag)
                etag.ETag = RequestOptions.ETag;
            else if (RequestOptions.ETag == null && value != null && value is IETag etag2 && etag2.ETag != null)
                RequestOptions.ETag = etag2.ETag;

            return value;
        }
    }
}