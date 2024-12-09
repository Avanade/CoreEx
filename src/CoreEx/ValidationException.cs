// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace CoreEx
{
    /// <summary>
    /// Represents a <b>Validation</b> exception.
    /// </summary>
    /// <remarks>The <see cref="Exception.Message"/> defaults to: <i>A data validation error occurred.</i></remarks>
    public class ValidationException : Exception, IExtendedException
    {
        private const string _message = "A data validation error occurred.";

        /// <summary>
        /// Get or sets the <see cref="ShouldBeLogged"/> value.
        /// </summary>
        public static bool ShouldExceptionBeLogged { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        public ValidationException() : this(null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ValidationException(string? message) : base(message ?? new LText(typeof(ValidationException).FullName, _message)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public ValidationException(string? message, Exception innerException) : base(message ?? new LText(typeof(ValidationException).FullName, _message), innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> with a <see cref="MessageItem"/> list and optional <paramref name="message"/>.
        /// </summary>
        /// <param name="messages">The <see cref="MessageItem"/> list.</param>
        /// <param name="message">The error message.</param>
        public ValidationException(IEnumerable<MessageItem> messages, string? message = null) : base(CreateMessage(messages, message ?? new LText(typeof(ValidationException).FullName, _message)))
        {
            if (messages != null)
                Messages = new(messages.Where(x => x.Type == MessageType.Error));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> with a single <see cref="MessageItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="MessageItem"/>.</param>
        /// <param name="message">The error message.</param>
        public ValidationException(MessageItem item, string? message = null) : this([item], message) { }

        /// <summary>
        /// Gets the underlying messages.
        /// </summary>
        public MessageItemCollection? Messages { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.ValidationError"/> value as a <see cref="string"/>.</returns>
        public string ErrorType => Abstractions.ErrorType.ValidationError.ToString();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.ValidationError"/> value as a <see cref="string"/>.</returns>
        public int ErrorCode => (int)Abstractions.ErrorType.ValidationError;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="HttpStatusCode.BadRequest"/> value.</returns>
        public HttpStatusCode StatusCode => HttpStatusCode.BadRequest;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><c>false</c>; is not considered transient.</returns>
        public bool IsTransient => false;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ShouldExceptionBeLogged"/> value.</returns>
        public bool ShouldBeLogged => ShouldExceptionBeLogged;

        /// <summary>
        /// Creates the exception message.
        /// </summary>
        private static string CreateMessage(IEnumerable<MessageItem> mic, string message)
        {
            if (mic == null)
                return message;

            var sb = new StringBuilder(message);
            foreach (var mi in mic.Where(x => x.Type == MessageType.Error))
            {
                sb.Append($" [{mi.Property}: {mi.Text}]");
            }

            return sb.ToString();
        }
    }
}