// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Results;
using System;
using System.Net;
using System.Net.Http;

namespace CoreEx.Http
{
    /// <summary>
    /// Provides the <see cref="HttpResponseMessage"/> result with a <see cref="Value"/>.
    /// </summary>
    public class HttpResult<T> : HttpResultBase, IToResult<T>
    {
        private readonly T _value;
        private readonly Exception? _internalException = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResult{T}"/> class.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="content">The <see cref="HttpResponseMessage.Content"/> as <see cref="BinaryData"/> (see <see cref="HttpContent.ReadAsByteArrayAsync()"/>).</param>
        /// <param name="value">The deserialized value where <see cref="HttpResultBase.IsSuccess"/>; otherwise, <c>default</c>.</param>
        internal HttpResult(HttpResponseMessage response, BinaryData? content, T value) : base(response, content) => _value = value;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResult{T}"/> class that has an exception.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="content">The <see cref="HttpResponseMessage.Content"/> as <see cref="BinaryData"/> (see <see cref="HttpContent.ReadAsByteArrayAsync()"/>).</param>
        /// <param name="internalException">The internal <see cref="Exception"/>.</param>
        internal HttpResult(HttpResponseMessage response, BinaryData? content, Exception? internalException) : this(response, content, default(T)!) => _internalException = internalException;

        /// <summary>
        /// Gets the response value.
        /// </summary>
        /// <remarks>Performs a <see cref="HttpResult.ThrowOnError"/> before returning the resulting deserialized value.</remarks>
        public T Value
        {
            get
            {
                ThrowOnError();
                return _value;
            }
        }

        /// <summary>
        /// Gets the internal exception where the request/response handling was not successful; i.e. JSON deserialization error.
        /// </summary>
        public Exception? Exception => _internalException;

        /// <inheritdoc/>
        public override bool IsSuccess => _internalException is null && base.IsSuccess;

        /// <inheritdoc/>
        public override HttpStatusCode StatusCode => _internalException is not null ? HttpStatusCode.InternalServerError : base.StatusCode;

        /// <summary>
        /// Throws an exception if the request was not successful (see <see cref="HttpResultBase.IsSuccess"/>).
        /// </summary>
        /// <param name="throwKnownException">Indicates whether to check the <see cref="HttpResponseMessage.StatusCode"/> and where it matches one of the <i>known</i> <see cref="IExtendedException.StatusCode"/> values then that <see cref="IExtendedException"/> will be thrown.</param>
        /// <param name="useContentAsErrorMessage">Indicates whether to use the <see cref="HttpResponseMessage.Content"/> as the resulting exception message.</param>
        /// <returns>The <see cref="HttpResult"/> instance to support fluent-style method-chaining.</returns>
        public HttpResult<T> ThrowOnError(bool throwKnownException = true, bool useContentAsErrorMessage = true)
        {
            if (IsSuccess)
                return this;

            if (_internalException is not null)
                throw _internalException;

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
        public Result<T> ToResult() => ToResult(true);

        /// <summary>
        /// Converts the <see cref="HttpResult"/> into an equivalent <see cref="Result"/>.
        /// </summary>
        /// <param name="convertToKnownException">Indicates whether to check the <see cref="HttpResponseMessage.StatusCode"/> and where it matches one of the <i>known</i> <see cref="IExtendedException.StatusCode"/> values then that <see cref="IExtendedException"/> will be used.</param>
        /// <param name="useContentAsErrorMessage">Indicates whether to use the <see cref="HttpResponseMessage.Content"/> as the resulting exception message.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public Result<T> ToResult(bool convertToKnownException, bool useContentAsErrorMessage = true)
        {
            if (_internalException is not null)
                return new Result<T>(_internalException);

            if (IsSuccess)
                return Result.Ok(Value);

            if (convertToKnownException)
            {
                var eex = CreateExtendedException(Response, Content, useContentAsErrorMessage);
                if (eex != null)
                    return new Result<T>((Exception)eex);
            }

            return new Result<T>(new HttpRequestException(Content));
        }
    }
}