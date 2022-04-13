// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using System.Linq.Expressions;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Enables the property <see cref="Expression"/> capabilities.
    /// </summary>
    public interface IPropertyExpression
    {
        /// <summary>
        /// Gets the property name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the JSON property name (where applicable).
        /// </summary>
        string? JsonName { get; }

        /// <summary>
        /// Indicates whether the property is JSON serializable.
        /// </summary>
        bool IsJsonSerializable { get; }

        /// <summary>
        /// Gets the property text.
        /// </summary>
        LText Text { get; }

        /// <summary>
        /// Gets the property value for the given entity value.
        /// </summary>
        /// <param name="entity">The entity value.</param>
        /// <returns>The corresponding property value.</returns>
        object? GetValue(object entity);
    }
}