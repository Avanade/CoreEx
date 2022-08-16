// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using System.Net.Http;

namespace CoreEx.Http
{
    /// <summary>
    /// Provides the <see cref="HttpResponseMessage"/> result with a <see cref="Value"/>.
    /// </summary>
    public class HttpResult<T> : HttpResult
    {
        private readonly T _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResult{T}"/> class.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="content">The <see cref="HttpResponseMessage.Content"/> as a <see cref="string"/> (see <see cref="HttpContent.ReadAsStringAsync()"/>).</param>
        /// <param name="value">The deserialized value where <see cref="HttpResult.IsSuccess"/>; otherwise, <c>default</c>.</param>
        internal HttpResult(HttpResponseMessage response, string? content, T value) : base(response, content) => _value = value;

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
        /// Throws an exception if the request was not successful (see <see cref="HttpResult.IsSuccess"/>).
        /// </summary>
        /// <param name="throwKnownException">Indicates whether to check the <see cref="HttpResponseMessage.StatusCode"/> and where it matches one of the <i>known</i> <see cref="IExtendedException.StatusCode"/> values then that <see cref="IExtendedException"/> will be thrown.</param>
        /// <param name="useContentAsErrorMessage">Indicates whether to use the <see cref="HttpResponseMessage.Content"/> as the resulting exception message.</param>
        /// <returns>The <see cref="HttpResult"/> instance to support fluent-style method-chaining.</returns>
        public new HttpResult<T> ThrowOnError(bool throwKnownException = true, bool useContentAsErrorMessage = true)
        {
            base.ThrowOnError(throwKnownException, useContentAsErrorMessage);
            return this;
        }
    }
}