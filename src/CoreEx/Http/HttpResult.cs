// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Future proofing.")]
        public static async Task<HttpResult> CreateAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
            => new HttpResult(response ?? throw new ArgumentNullException(nameof(response)), response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false));

        /// <summary>
        /// Creates a new <see cref="HttpResult{T}"/> with a <see cref="HttpResult{T}.Value"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/> for deserializing the <see cref="HttpResult{T}.Value"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Future proofing.")]
        public static async Task<HttpResult<T>> CreateAsync<T>(HttpResponseMessage response, IJsonSerializer? jsonSerializer = default, CancellationToken cancellationToken = default)
        {
            var content = (response ?? throw new ArgumentNullException(nameof(response))).Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(content))
                return new HttpResult<T>(response, content, default(T)!);

            if (typeof(T) == typeof(string) && StringComparer.OrdinalIgnoreCase.Compare(response.Content.Headers?.ContentType?.MediaType, MediaTypeNames.Text.Plain) == 0)
            {
                try
                {
                    return new HttpResult<T>(response, content, (T)Convert.ChangeType(content, typeof(T), CultureInfo.CurrentCulture));
                }
                catch (Exception ex)
                {
                    return new HttpResult<T>(response, content, new InvalidOperationException($"Unable to convert the content '{content}' [{MediaTypeNames.Text.Plain}] to Type {typeof(T).Name}.", ex));
                }
            }

            try
            {
                var value = (jsonSerializer ?? ExecutionContext.GetService<IJsonSerializer>() ?? JsonSerializer.Default).Deserialize<T>(content);
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
                return new HttpResult<T>(response, content, new InvalidOperationException($"Unable to deserialize the JSON content '{content}' [{response.Content.Headers?.ContentType?.MediaType ?? "not specified"}] to Type {typeof(T).FullName}.", ex));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResult"/> class.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="content">The <see cref="HttpResponseMessage.Content"/> as a <see cref="string"/> (see <see cref="HttpContent.ReadAsStringAsync()"/>).</param>
        public HttpResult(HttpResponseMessage response, string? content) : base(response, content) { }

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