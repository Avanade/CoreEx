// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides the base validation context properties.
    /// </summary>
    public interface IValidationContextBase
    {
        /// <summary>
        /// Gets the entity value.
        /// </summary>
        object? Value { get; }

        /// <summary>
        /// Gets the <see cref="MessageItemCollection"/>.
        /// </summary>
        MessageItemCollection? Messages { get; }

        /// <summary>
        /// Indicates whether there has been a validation error.
        /// </summary>
        bool HasErrors { get; }
    }
}