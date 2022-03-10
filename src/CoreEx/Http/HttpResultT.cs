// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Net.Http;

namespace CoreEx.Http
{
    /// <summary>
    /// Provides the <see cref="HttpResponseMessage"/> result with a <see cref="Value"/>.
    /// </summary>
    public class HttpResult<T> : HttpResult, IHttpResult<T>
    {
        private readonly T _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResult{T}"/> class.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="content">The <see cref="HttpResponseMessage.Content"/> as a <see cref="string"/> (see <see cref="HttpContent.ReadAsStringAsync"/>).</param>
        /// <param name="value">The deserialized value where <see cref="HttpResult.IsSuccess"/>; otherwise, <c>default</c>.</param>
        internal HttpResult(HttpResponseMessage response, string? content, T value) : base(response, content) => _value = value;

        /// <inheritdoc/>
        public T Value
        {
            get
            {
                ThrowOnError();
                return _value;
            }
        }
    }
}