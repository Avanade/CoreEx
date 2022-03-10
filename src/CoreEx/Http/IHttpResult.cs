// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using System.Net;
using System.Net.Http;

namespace CoreEx.Http
{
    /// <summary>
    /// Enables the <see cref="HttpResponseMessage"/> result with no value.
    /// </summary>
    public interface IHttpResult
    {
        /// <summary>
        /// Gets the <see cref="HttpResponseMessage"/>.
        /// </summary>
        HttpResponseMessage Response { get; }

        /// <summary>
        /// Gets the underlying <see cref="HttpRequestMessage"/>.
        /// </summary>
        HttpRequestMessage Request { get; }

        /// <summary>
        /// Gets the <see cref="HttpStatusCode"/>.
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Gets the <see cref="HttpResponseMessage.Content"/> as a <see cref="string"/> (see <see cref="HttpContent.ReadAsStringAsync"/>).
        /// </summary>
        string? Content { get; }

        /// <summary>
        /// Indicates whether the request was successful.
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// Gets the <see cref="MessageItemCollection"/>.
        /// </summary>
        MessageItemCollection? Messages { get; }

        /// <summary>
        /// Gets the error type using the <see cref="HttpConsts.ErrorTypeHeaderName"/>.
        /// </summary>
        string? ErrorType { get; }

        /// <summary>
        /// Gets the error type using the <see cref="HttpConsts.ErrorCodeHeaderName"/>
        /// </summary>
        int? ErrorCode { get; }

        /// <summary>
        /// Throws an exception if the request was not successful (see <see cref="IsSuccess"/>).
        /// </summary>
        /// <param name="throwKnownException">Indicates whether to check the <see cref="HttpResponseMessage.StatusCode"/> and where it matches one of the <i>known</i> <see cref="IExtendedException.StatusCode"/> values then that <see cref="IExtendedException"/> will be thrown.</param>
        /// <param name="useContentAsErrorMessage">Indicates whether to use the <see cref="HttpResponseMessage.Content"/> as the resulting exception message.</param>
        /// <returns>The <see cref="IHttpResult"/> instance to support fluent-style method-chaining.</returns>
        IHttpResult ThrowOnError(bool throwKnownException, bool useContentAsErrorMessage);
    }
}