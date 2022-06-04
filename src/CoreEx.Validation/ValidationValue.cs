// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents a validation value.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    public class ValidationValue<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationValue{T}"/> class.
        /// </summary>
        /// <param name="entity">The parent entity value.</param>
        /// <param name="value">The value.</param>
        internal ValidationValue(object? entity, T? value)
        {
            Entity = entity;
            Value = value;
        }

        /// <summary>
        /// Gets or sets the entity value.
        /// </summary>
        public object? Entity { get; }

        /// <summary>
        /// Gets or sets the entity property value.
        /// </summary>
        public T? Value { get; }
    }
}