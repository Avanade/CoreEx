// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Net;

namespace CoreEx.Abstractions
{
    /// <summary>
    /// Enables the extended exception capabilities.
    /// </summary>
    public interface IExtendedException
    {
        /// <summary>
        /// Gets the exception message.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the error type/reason.
        /// </summary>
        /// <remarks>See <see cref="Abstractions.ErrorType"/> for standard values.</remarks>
        string ErrorType { get; }

        /// <summary>
        /// Gets the error code.
        /// </summary>
        /// <remarks>See <see cref="Abstractions.ErrorType"/> for standard values.</remarks>
        int ErrorCode { get; }

        /// <summary>
        /// Gets the corresponding <see cref="HttpStatusCode"/>.
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Indicates whether exception is transient; i.e. is a candidate for a retry.
        /// </summary>
        bool IsTransient { get; }

        /// <summary>
        /// Indicates whether the <see cref="IExtendedException"/> should be logged.
        /// </summary>
        bool ShouldBeLogged { get; }
    }
}