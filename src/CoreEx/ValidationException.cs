// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace CoreEx
{
    /// <summary>
    /// Represents a validation exception.
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        public ValidationException() : this(null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ValidationException(string? message) : base(message ?? "A data validation error occurred.") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public ValidationException(string? message, Exception innerException) : base(message ?? "A data validation error occurred.", innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class using the specified <paramref name="modelStateDictionary"/> and <paramref name="message"/>.
        /// </summary>
        /// <param name="modelStateDictionary">The <see cref="ModelStateDictionary"/> that contains the validation errors.</param>
        /// <param name="message">The error message.</param>
        public ValidationException(ModelStateDictionary modelStateDictionary, string? message = null) : this(message)
            => ModelStateDictionary = modelStateDictionary ?? throw new ArgumentNullException(nameof(modelStateDictionary));

        /// <summary>
        /// Gets the <see cref="Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary"/>.
        /// </summary>
        public ModelStateDictionary? ModelStateDictionary { get; }
    }
}