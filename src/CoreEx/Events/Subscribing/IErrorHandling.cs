// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Provides the standard <see cref="ErrorHandling"/> options based on the <see cref="Type"/> of <see cref="Exception"/> encountered.
    /// </summary>
    /// <remarks>The <see cref="EventSubscriberInvoker"/> is largely responsible for the handling at runtime.</remarks>
    public interface IErrorHandling
    {
        /// <summary>
        /// Gets the <see cref="ErrorHandling"/> for when an unhandled <see cref="Exception"/> (none of the others) is encountered.
        /// </summary>
        ErrorHandling UnhandledHandling { get; }

        /// <summary>
        /// Gets the <see cref="ErrorHandling"/> for when a <see cref="AuthenticationException"/> or <see cref="AuthorizationException"/> is encountered.
        /// </summary>
        ErrorHandling SecurityHandling { get; }

        /// <summary>
        /// Gets the <see cref="ErrorHandling"/> for when a <see cref="TransientException"/> is encountered.
        /// </summary>
        ErrorHandling TransientHandling { get; }

        /// <summary>
        /// Gets the <see cref="ErrorHandling"/> for when a <see cref="NotFoundException"/> is encountered.
        /// </summary>
        ErrorHandling NotFoundHandling { get; }

        /// <summary>
        /// Gets the <see cref="ErrorHandling"/> for when a <see cref="ConcurrencyException"/> is encountered.
        /// </summary>
        ErrorHandling ConcurrencyHandling { get; }

        /// <summary>
        /// Gets the <see cref="ErrorHandling"/> for when a <see cref="ValidationException"/>, <see cref="BusinessException"/>, <see cref="DuplicateException"/> or <see cref="ConflictException"/> is encountered.
        /// </summary>
        ErrorHandling InvalidDataHandling { get; }
    }
}