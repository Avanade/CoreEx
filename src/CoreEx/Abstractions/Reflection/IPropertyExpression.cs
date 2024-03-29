﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using System.Linq.Expressions;
using System.Reflection;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Enables the property <see cref="Expression"/> capabilities.
    /// </summary>
    public interface IPropertyExpression
    {
        /// <summary>
        /// Gets the corresponding <see cref="System.Reflection.PropertyInfo"/>.
        /// </summary>
        PropertyInfo PropertyInfo { get; }

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
        /// Indicates that the property is a class with properties (and is not a <see cref="string"/>).
        /// </summary>
        bool IsClass { get; }

        /// <summary>
        /// Gets the property text.
        /// </summary>
        LText Text { get; }

        /// <summary>
        /// Gets the default value.
        /// </summary>
        /// <returns>The default value.</returns>
        object? GetDefault();

        /// <summary>
        /// Gets the property value for the given entity.
        /// </summary>
        /// <param name="entity">The entity value.</param>
        /// <returns>The corresponding property value.</returns>
        object? GetValue(object? entity);

        /// <summary>
        /// Sets the property value for the given entity.
        /// </summary>
        /// <param name="entity">The entity value.</param>
        /// <param name="value">The corresponding property value.</param>
        void SetValue(object entity, object? value);
    }
}