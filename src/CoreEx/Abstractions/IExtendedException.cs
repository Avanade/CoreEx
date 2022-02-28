// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Net;

namespace CoreEx.Abstractions
{
    /// <summary>
    /// Enables the extended exception capabilities.
    /// </summary>
    public interface IExtendedException : IExceptionResult
    {
        /// <summary>
        /// Gets the exception message.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the <see cref="Abstractions.ErrorType"/>.
        /// </summary>
        ErrorType ErrorType { get; }

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