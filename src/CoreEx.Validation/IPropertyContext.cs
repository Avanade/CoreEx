// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Localization;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables a validation context for a property.
    /// </summary>
    public interface IPropertyContext
    {
        /// <summary>
        /// Gets the <see cref="IValidationContext"/> for the parent entity.
        /// </summary>
        IValidationContext Parent { get; }

        /// <summary>
        /// Gets the property name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the JSON property name.
        /// </summary>
        string JsonName { get; }

        /// <summary>
        /// Gets the property text.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets the property value.
        /// </summary>
        object? Value { get; }

        /// <summary>
        /// Gets the fully qualified property name.
        /// </summary>
        string FullyQualifiedPropertyName { get; }

        /// <summary>
        /// Gets the fully qualified Json property name.
        /// </summary>
        string FullyQualifiedJsonPropertyName { get; }

        /// <summary>
        /// Indicates whether there has been a validation error.
        /// </summary>
        bool HasError { get; }

        /// <summary>
        /// Creates a new <see cref="MessageType.Error"/> <see cref="MessageItem"/> with the specified format and <b>adds</b> to the underlying <see cref="IValidationContext"/>.
        /// The friendly <see cref="Text"/> and <see cref="Value"/> are automatically passed as the first two arguments to the string formatter.
        /// </summary>
        /// <param name="format">The composite format string.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        MessageItem CreateErrorMessage(LText format);

        /// <summary>
        /// Creates a new <see cref="MessageType.Error"/> <see cref="MessageItem"/> with the specified format and additional values to be included in the text and <b>adds</b> to the underlying <see cref="IValidationContext"/>.
        /// The friendly <see cref="Text"/> and <see cref="Value"/> are automatically passed as the first two arguments to the string formatter.
        /// </summary>
        /// <param name="format">The composite format string.</param>
        /// <param name="values">The values that form part of the message text (<see cref="Text"/> and <see cref="Value"/> are automatically passed as the first two arguments to the string formatter).</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        MessageItem CreateErrorMessage(LText format, params object?[] values);

        /// <summary>
        /// Creates a fully qualified property name for the name.
        /// </summary>
        /// <param name="name">The property name.</param>
        string CreateFullyQualifiedPropertyName(string name);

        /// <summary>
        /// Creates a fully qualified JSON property name for the name.
        /// </summary>
        /// <param name="name">The property name.</param>
        string CreateFullyQualifiedJsonPropertyName(string name);
    }
}