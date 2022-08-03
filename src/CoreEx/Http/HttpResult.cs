// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Provides the <see cref="HttpResponseMessage"/> result with no value.
    /// </summary>
    public class HttpResult
    {
        private readonly Lazy<string?> _errorType;
        private readonly Lazy<int?> _errorCode;
        private readonly Lazy<MessageItemCollection?> _messages;

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
        public static async Task<HttpResult<T>> CreateAsync<T>(HttpResponseMessage response, IJsonSerializer jsonSerializer, CancellationToken cancellationToken = default)
        {
            var content = (response ?? throw new ArgumentNullException(nameof(response))).Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(content))
                return new HttpResult<T>(response, content, default!);

            if (typeof(T) == typeof(string) && StringComparer.OrdinalIgnoreCase.Compare(response.Content.Headers?.ContentType?.MediaType, MediaTypeNames.Text.Plain) == 0)
            {
                try
                {
                    return new HttpResult<T>(response, content, (T)Convert.ChangeType(content, typeof(T), CultureInfo.CurrentCulture));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Unable to convert the content '{content}' [{MediaTypeNames.Text.Plain}] to Type {typeof(T).Name}", ex);
                }
            }

            try
            {
                var value = (jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer))).Deserialize<T>(content);
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
                throw new InvalidOperationException($"Unable to deserialize the JSON content '{content}' [{response.Content.Headers?.ContentType?.MediaType ?? "not specified"}] to Type {typeof(T).FullName}", ex);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResult"/> class.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="content">The <see cref="HttpResponseMessage.Content"/> as a <see cref="string"/> (see <see cref="HttpContent.ReadAsStringAsync()"/>).</param>
        protected HttpResult(HttpResponseMessage response, string? content)
        {
            Response = response ?? throw new ArgumentNullException(nameof(response));
            Content = content;

            _errorType = new Lazy<string?>(() =>
            {
                if (Response.TryGetHeaderValue(HttpConsts.ErrorTypeHeaderName, out var et))
                    return et;
                else
                    return null;
            });

            _errorCode = new Lazy<int?>(() =>
            {
                if (Response.TryGetHeaderValue(HttpConsts.ErrorCodeHeaderName, out var ec) && int.TryParse(ec, out var code))
                    return code;
                else
                    return null;
            });

            _messages = new Lazy<MessageItemCollection?>(() =>
            {
                if (!Response.TryGetHeaderValue(HttpConsts.MessagesHeaderName, out var mic) || string.IsNullOrEmpty(mic))
                    return null;

                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<MessageItemCollection>(mic);
                }
                catch
                {
                    return null;  // Swallow any deserialization errors.
                }
            });
        }

        /// <summary>
        /// Gets the <see cref="HttpResponseMessage"/>.
        /// </summary>
        public HttpResponseMessage Response { get; }

        /// <summary>
        /// Gets the <see cref="HttpResponseMessage.Content"/> as a <see cref="string"/> (see <see cref="HttpContent.ReadAsStringAsync()"/>).
        /// </summary>
        public string? Content { get; }

        /// <summary>
        /// Gets the underlying <see cref="HttpRequestMessage"/>.
        /// </summary>
        public HttpRequestMessage? Request => Response.RequestMessage;

        /// <summary>
        /// Gets the <see cref="HttpStatusCode"/>.
        /// </summary>
        public HttpStatusCode StatusCode => Response.StatusCode;

        /// <summary>
        /// Indicates whether the request was successful.
        /// </summary>
        public bool IsSuccess => Response.IsSuccessStatusCode;

        /// <summary>
        /// Gets the <see cref="MessageItemCollection"/>.
        /// </summary>
        public MessageItemCollection? Messages => _messages.Value;

        /// <summary>
        /// Gets the error type using the <see cref="HttpConsts.ErrorTypeHeaderName"/>.
        /// </summary>
        public string? ErrorType => _errorType.Value;

        /// <summary>
        /// Gets the error code using the <see cref="HttpConsts.ErrorCodeHeaderName"/>
        /// </summary>
        public int? ErrorCode => _errorCode.Value;

        /// <summary>
        /// Throws an exception if the request was not successful (see <see cref="IsSuccess"/>).
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

        /// <summary>
        /// Creates an <see cref="IExtendedException"/> from the <see cref="HttpResponseMessage"/> based on the <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="content">The <see cref="HttpResponseMessage.Content"/> as a <see cref="string"/> (see <see cref="HttpContent.ReadAsStringAsync()"/>).</param>
        /// <param name="useContentAsErrorMessage">Indicates whether to use the <see cref="HttpResponseMessage.Content"/> as the resulting exception message.</param>
        /// <returns>The corresponding <see cref="IExtendedException"/> where applicable; otherwise, <c>null</c>.</returns>
        internal static IExtendedException? CreateExtendedException(HttpResponseMessage response, string? content, bool useContentAsErrorMessage = true)
        {
            if (response == null || response.IsSuccessStatusCode)
                return null;

            if (!(response.TryGetHeaderValue(HttpConsts.ErrorTypeHeaderName, out var et) && Enum.TryParse(et, out ErrorType errorType)))
                errorType = Abstractions.ErrorType.UnhandledError;

            var message = useContentAsErrorMessage ? content : null;

            switch (response.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    if (errorType == Abstractions.ErrorType.BusinessError)
                        return new BusinessException(message);
                    else
                    {
                        var mic = CreateMessageItems(content);
                        if (mic == null)
                            return new ValidationException(message);
                        else
                            return new ValidationException(mic);
                    }

                case HttpStatusCode.Forbidden: return new AuthenticationException(message);
                case HttpStatusCode.Unauthorized: return new AuthorizationException(message);
                case HttpStatusCode.PreconditionFailed: return new ConcurrencyException(message);
                case HttpStatusCode.Conflict: return errorType == Abstractions.ErrorType.DuplicateError ? new DuplicateException(message) : new ConflictException(message);
                case HttpStatusCode.NotFound: return new NotFoundException(message);
                case HttpStatusCode.ServiceUnavailable: return new TransientException(message);
                default: return null;
            }
        }

        /// <summary>
        /// Create <see cref="MessageItemCollection"/> from <paramref name="content"/>.
        /// </summary>
        /// <param name="content">The <see cref="HttpResponseMessage.Content"/> as a <see cref="string"/> (see <see cref="HttpContent.ReadAsStringAsync()"/>).</param>
        /// <returns>The <see cref="MessageItemCollection"/> where successfully deserialized; otherwise, <c>null</c>.</returns>
        internal static MessageItemCollection? CreateMessageItems(string? content)
        {
            MessageItemCollection? mic = null;

            if (content != null)
            {
                try
                {
                    var errors = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string[]>>(content);
                    if (errors != null)
                    {
                        foreach (var kvp in errors.Where(x => !string.IsNullOrEmpty(x.Key)))
                        {
                            foreach (var error in kvp.Value.Where(x => !string.IsNullOrEmpty(x)))
                            {
                                (mic ??= new MessageItemCollection()).AddPropertyError(kvp.Key, error);
                            }
                        }
                    }
                }
                catch { } // Swallow any deserialization errors.
            }

            return mic;
        }
    }
}