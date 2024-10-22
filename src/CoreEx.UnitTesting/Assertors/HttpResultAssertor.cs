// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Http;
using UnitTestEx.Abstractions;

namespace UnitTestEx.Assertors
{
    /// <summary>
    /// Represents the <see cref="HttpResult"/> test assert helper.
    /// </summary>
    /// <param name="owner">The owning <see cref="TesterBase"/>.</param>
    /// <param name="result">The <see cref="HttpResult"/>.</param>
    public class HttpResultAssertor(TesterBase owner, HttpResult result) : HttpResponseMessageAssertor(owner, result.ThrowIfNull(nameof(result)).Response)
    {
        /// <summary>
        /// Gets the <see cref="HttpResult"/>.
        /// </summary>
        public HttpResult Result { get; private set; } = result;
    }
}