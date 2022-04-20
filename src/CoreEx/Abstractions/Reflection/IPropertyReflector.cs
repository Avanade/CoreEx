// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Enables a reflector for a given entity (class) property.
    /// </summary>
    public interface IPropertyReflector
    {
        /// <summary>
        /// Gets the property name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the JSON property name.
        /// </summary>
        string? JsonName { get; }

        /// <summary>
        /// Gets the <see cref="EntityReflectorArgs"/>.
        /// </summary>
        EntityReflectorArgs Args { get; }

        /// <summary>
        /// Gets the <see cref="Dictionary{TKey, TValue}"/> for storing additional data.
        /// </summary>
        Dictionary<string, object?> Data { get; }

        /// <summary>
        /// Gets the corresponding <see cref="IPropertyExpression"/>.
        /// </summary>
        IPropertyExpression PropertyExpression { get; }

        /// <summary>
        /// Gets the corresponding <see cref="PropertyInfo"/>;
        /// </summary>
        PropertyInfo PropertyInfo { get; }

        /// <summary>
        /// Indicates that the property is a class with properties (and is not a <see cref="string"/>).
        /// </summary>
        bool IsClass { get; }

        /// <summary>
        /// Indicates that the property is <see cref="IEnumerable"/> (and is not a <see cref="string"/>).
        /// </summary>
        bool IsEnumerable { get; }

        /// <summary>
        /// Gets the parent entity <see cref="System.Type"/>.
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        /// Gets the property <see cref="System.Type"/>.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets the property <see cref="TypeReflectorTypeCode"/>.
        /// </summary>
        TypeReflectorTypeCode TypeCode { get; }

        /// <summary>
        /// Gets the <see cref="IEntityReflector"/> for the property where <see cref="IsClass"/>; otherwise, <c>null</c>.
        /// </summary>
        /// <returns>The corresponding <see cref="IEntityReflector"/>.</returns>
        IEntityReflector? GetEntityReflector();

        /// <summary>
        /// Compares two values for equality.
        /// </summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns><c>true</c> indicates that they are equal; otherwise, <c>false</c>.</returns>
        bool Compare(object? x, object? y);
    }
}