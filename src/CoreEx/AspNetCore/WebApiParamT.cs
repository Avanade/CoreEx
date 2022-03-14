// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.AspNetCore
{
    /// <summary>
    /// Represents a <see cref="WebApi"/> parameter with a request <see cref="Value"/>.
    /// </summary>
    public class WebApiParam<T> : WebApiParam
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiParam"/> class.
        /// </summary>
        /// <param name="wap">The <see cref="WebApiParam"/> to copy from.</param>
        /// <param name="value">The deserialized request value.</param>
        public WebApiParam(WebApiParam wap, T value) : base((wap ?? throw new ArgumentNullException(nameof(wap))).WebApi, wap.Request, wap.RequestOptions) => Value = value;

        /// <summary>
        /// Gets the deserialized request value.
        /// </summary>
        public T? Value { get; }
    }
}