// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Represents a <see cref="WebApi"/> parameter with a request <see cref="Value"/>.
    /// </summary>
    /// <remarks>This enables access to the corresponding <see cref="WebApiParam.WebApi"/>, <see cref="WebApiParam.Request"/>, <see cref="WebApiParam.RequestOptions"/>, deserialized <see cref="Value"/>, etc.</remarks>
    public class WebApiParam<T> : WebApiParam
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiParam"/> class.
        /// </summary>
        /// <param name="wap">The <see cref="WebApiParam"/> to copy from.</param>
        /// <param name="value">The deserialized request value.</param>
        public WebApiParam(WebApiParam wap, T value) : base(wap.ThrowIfNull(nameof(wap)).WebApi, wap.RequestOptions, wap.OperationType) => Value = InspectValue(value);

        /// <summary>
        /// Gets the deserialized request value.
        /// </summary>
        public T? Value { get; }
    }
}