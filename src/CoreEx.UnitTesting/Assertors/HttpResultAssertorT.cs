// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Http;
using System;
using UnitTestEx.Abstractions;

namespace UnitTestEx.Assertors
{
    /// <summary>
    /// Represents the <see cref="HttpResult{TValue}"/> test assert helper with a specified result <typeparamref name="TValue"/> <see cref="Type"/>.
    /// </summary>
    /// 
    public class HttpResultAssertor<TValue> : HttpResponseMessageAssertor<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResultAssertor"/> class.
        /// </summary>
        /// <param name="owner">The owning <see cref="TesterBase"/>.</param>
        /// <param name="result">The <see cref="HttpResult"/>.</param>
        public HttpResultAssertor(TesterBase owner, HttpResult<TValue> result) : base(owner, result.ThrowIfNull(nameof(result)).Response) => Result = result;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResultAssertor"/> class.
        /// </summary>
        /// <param name="owner">The owning <see cref="TesterBase"/>.</param>
        /// <param name="value">The value already deserialized.</param>
        /// <param name="result"></param>
        public HttpResultAssertor(TesterBase owner, TValue value, HttpResult<TValue> result) : base(owner, value, result.ThrowIfNull(nameof(result)).Response) => Result = result;

        /// <summary>
        /// Gets the <see cref="HttpResult{TValue}"/>.
        /// </summary>
        public HttpResult<TValue> Result { get; private set; }
    }
}