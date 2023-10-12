// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace CoreEx.Http
{
    /// <summary>
    /// Provides the <see cref="HttpResponseMessage"/> result base capabilities.
    /// </summary>
    public abstract class HttpResultBase
    {
        private readonly Lazy<string?> _errorType;
        private readonly Lazy<int?> _errorCode;
        private readonly Lazy<MessageItemCollection?> _messages;
        private string? _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResultBase"/> class.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="content">The <see cref="HttpResponseMessage.Content"/> as <see cref="BinaryData"/>.</param>
        protected HttpResultBase(HttpResponseMessage response, BinaryData? content)
        {
            Response = response ?? throw new ArgumentNullException(nameof(response));
            BinaryContent = content;

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
        /// Gets the <see cref="HttpResponseMessage.Content"/> as <see cref="BinaryData"/>.
        /// </summary>
        public BinaryData? BinaryContent { get; }

        /// <summary>
        /// Gets the <see cref="BinaryContent"/> as a <see cref="string"/>.
        /// </summary>
        public string? Content { get => WillResultInNullAsNotFound ? null : _content ??= BinaryContent?.ToString(); }

        /// <summary>
        /// Gets the underlying <see cref="HttpRequestMessage"/>.
        /// </summary>
        public HttpRequestMessage? Request => Response.RequestMessage;

        /// <summary>
        /// Gets the <see cref="HttpStatusCode"/>.
        /// </summary>
        public virtual HttpStatusCode StatusCode => WillResultInNullAsNotFound ? HttpStatusCode.NoContent : Response.StatusCode;

        /// <summary>
        /// Indicates whether the request was successful.
        /// </summary>
        public virtual bool IsSuccess => WillResultInNullAsNotFound || Response.IsSuccessStatusCode;

        /// <summary>
        /// Gets the <see cref="MessageItemCollection"/>.
        /// </summary>
        public MessageItemCollection? Messages => _messages.Value;

        /// <summary>
        /// Gets the error type using the <see cref="HttpConsts.ErrorTypeHeaderName"/>.
        /// </summary>
        public string? ErrorType => WillResultInNullAsNotFound ? null : _errorType.Value;

        /// <summary>
        /// Gets the error code using the <see cref="HttpConsts.ErrorCodeHeaderName"/>
        /// </summary>
        public int? ErrorCode => WillResultInNullAsNotFound ? null : _errorCode.Value;

        /// <summary>
        /// Indicates whether a <c>null/default</c> is to be returned where the <b>response</b> has a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>; i.e. it acts as <see cref="HttpStatusCode.NoContent"/>.
        /// </summary>
        /// <remarks>When set to <c>true</c> and the corresponding <see cref="Response"/> has a <see cref="StatusCode"/> is <see cref="HttpStatusCode.NotFound"/>, then <see cref="IsSuccess"/> will return <c>true</c> and <see cref="Content"/> will return <c>null</c>.</remarks>
        public bool NullOnNotFoundResponse { get; set; }

        /// <summary>
        /// Indicates whether the <see cref="HttpResult"/> will result in a <c>null</c> response where the <b>response</b> has a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>
        /// </summary>
        /// <remarks>See <see cref="NullOnNotFoundResponse"/>.</remarks>
        public bool WillResultInNullAsNotFound => NullOnNotFoundResponse && Response.StatusCode == HttpStatusCode.NotFound;

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
                        return new BusinessException(message, new HttpRequestException(content));
                    else
                    {
                        var mic = CreateMessageItems(content);
                        if (mic == null)
                            return new ValidationException(message, new HttpRequestException(content));
                        else
                            return new ValidationException(mic);
                    }

                case HttpStatusCode.Forbidden: return new AuthenticationException(message, new HttpRequestException(content));
                case HttpStatusCode.Unauthorized: return new AuthorizationException(message, new HttpRequestException(content));
                case HttpStatusCode.PreconditionFailed: return new ConcurrencyException(message, new HttpRequestException(content));
                case HttpStatusCode.NotFound: return new NotFoundException(message, new HttpRequestException(content));
                case HttpStatusCode.ServiceUnavailable: return new TransientException(message, new HttpRequestException(content));

                case HttpStatusCode.Conflict:
                    return errorType switch
                    {
                        Abstractions.ErrorType.DuplicateError => new DuplicateException(message, new HttpRequestException(content)),
                        Abstractions.ErrorType.DataConsistencyError => new DataConsistencyException(message, new HttpRequestException(content)),
                        _ => new ConflictException(message, new HttpRequestException(content)),
                    };

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