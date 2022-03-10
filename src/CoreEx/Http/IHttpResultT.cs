// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Net.Http;

namespace CoreEx.Http
{
    /// <summary>
    /// Enables the <see cref="HttpResponseMessage"/> result with a <see cref="Value"/>.
    /// </summary>
    public interface IHttpResult<T>
    {
        /// <summary>
        /// Gets the response value.
        /// </summary>
        /// <remarks>Performs a <see cref="IHttpResult.ThrowOnError"/> before returning the resuluting deserialized value.</remarks>
        T Value { get; }
    }
}