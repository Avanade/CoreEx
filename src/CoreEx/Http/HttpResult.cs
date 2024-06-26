﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Json;
using CoreEx.Results;
using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Provides the <see cref="HttpResponseMessage"/> result with no value.
    /// </summary>
    public class HttpResult : HttpResultBase, IToResult
    {
        /// <summary>
        /// Creates a new <see cref="HttpResult"/> with no value.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public static async Task<HttpResult> CreateAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
#if NETSTANDARD2_1
            => new HttpResult(response.ThrowIfNull(nameof(response)), 
                response.Content == null || response.Content.Headers.ContentLength == 0 ? null : new BinaryData(await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false)));
#else
            => new HttpResult(response.ThrowIfNull(nameof(response)), 
                response.Content == null || response.Content.Headers.ContentLength == 0 ? null : new BinaryData(await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false)));
#endif

        /// <summary>
        /// Creates a new <see cref="HttpResult{T}"/> with a <see cref="HttpResult{T}.Value"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/> for deserializing the <see cref="HttpResult{T}.Value"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public static async Task<HttpResult<T>> CreateAsync<T>(HttpResponseMessage response, IJsonSerializer? jsonSerializer = default, CancellationToken cancellationToken = default)
        {
#if NETSTANDARD2_1
            var content = (response.ThrowIfNull(nameof(response))).Content == null || response.Content.Headers.ContentLength == 0 
                ? null : new BinaryData(await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false));
#else
            var content = (response.ThrowIfNull(nameof(response))).Content == null || response.Content.Headers.ContentLength == 0 
                ? null : new BinaryData(await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false));
#endif

            if (!response.IsSuccessStatusCode || content == BinaryData.Empty)
                return new HttpResult<T>(response, content, default(T)!);

            if (typeof(T) == typeof(string) && StringComparer.OrdinalIgnoreCase.Compare(response.Content.Headers?.ContentType?.MediaType, MediaTypeNames.Text.Plain) == 0)
            {
                try
                {
                    return content == null 
                        ? new HttpResult<T>(response, content, default(T)!)
                        : new HttpResult<T>(response, content, (T)Convert.ChangeType(content.ToString(), typeof(T), CultureInfo.CurrentCulture));
                }
                catch (Exception ex)
                {
                    return new HttpResult<T>(response, content, new InvalidOperationException($"Unable to convert the content [{MediaTypeNames.Text.Plain}] content to Type {typeof(T).Name}.", ex));
                }
            }

            try
            {
                var value = content == null ? default! : (jsonSerializer ?? ExecutionContext.GetService<IJsonSerializer>() ?? JsonSerializer.Default).Deserialize<T>(content);
                if (value != null && value is IETag etag && etag.ETag == null && response.Headers.ETag != null)
                    etag.ETag = response.Headers.ETag.Tag;

                // Where the value is an ICollectionResult then update the Paging property from the corresponding response headers.
                if (value is ICollectionResult cr && cr != null)
                {
                    if (response.TryGetPagingResult(out var paging))
                        cr.Paging = paging;
                }

                return new HttpResult<T>(response, content, value!);
            }
            catch (Exception ex)
            {
                return new HttpResult<T>(response, content, new InvalidOperationException($"Unable to deserialize the JSON [{response.Content.Headers?.ContentType?.MediaType ?? "not specified"}] content to Type {typeof(T).FullName}.", ex));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResult"/> class.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="content">The <see cref="HttpResponseMessage.Content"/> as <see cref="BinaryData"/> (see <see cref="HttpContent.ReadAsByteArrayAsync()"/>).</param>
        internal HttpResult(HttpResponseMessage response, BinaryData? content) : base(response, content) { }

        /// <summary>
        /// Throws an exception if the request was not successful (see <see cref="HttpResultBase.IsSuccess"/>).
        /// </summary>
        /// <param name="throwKnownException">Indicates whether to check the <see cref="HttpResponseMessage.StatusCode"/> and where it matches one of the <i>known</i> <see cref="IExtendedException.StatusCode"/> values then that <see cref="IExtendedException"/> will be thrown.</param>
        /// <param name="useContentAsErrorMessage">Indicates whether to use the <see cref="HttpResponseMessage.Content"/> as the resulting exception message.</param>
        /// <returns>The <see cref="HttpResult"/> instance to support fluent-style method-chaining.</returns>
        public HttpResult ThrowOnError(bool throwKnownException = true, bool useContentAsErrorMessage = true)
        {
            if (IsSuccess)
                return this;

            if (throwKnownException)
            {
                var eex = CreateExtendedException(Response, Content, useContentAsErrorMessage);
                if (eex != null)
                    throw (Exception)eex;
            }

            Response.EnsureSuccessStatusCode();
            return this;
        }

        /// <inheritdoc/>
        public Result ToResult() => ToResult(true);

        /// <summary>
        /// Converts the <see cref="HttpResult"/> into an equivalent <see cref="Result"/>.
        /// </summary>
        /// <param name="convertToKnownException">Indicates whether to check the <see cref="HttpResponseMessage.StatusCode"/> and where it matches one of the <i>known</i> <see cref="IExtendedException.StatusCode"/> values then that <see cref="IExtendedException"/> will be used.</param>
        /// <param name="useContentAsErrorMessage">Indicates whether to use the <see cref="HttpResponseMessage.Content"/> as the resulting exception message.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public Result ToResult(bool convertToKnownException, bool useContentAsErrorMessage = true)
        {
            if (IsSuccess)
                return Result.Success;

            if (convertToKnownException)
            {
                var eex = CreateExtendedException(Response, Content, useContentAsErrorMessage);
                if (eex != null)
                    return new Result((Exception)eex);
            }

            return new Result(new HttpRequestException(Content));
        }
    }
}