// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;
using System.Collections.Generic;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Enables a reflector for a given entity (class) <see cref="Type"/>.
    /// </summary>
    public interface IEntityReflector
    {
        /// <summary>
        /// Gets the <see cref="EntityReflectorArgs"/>.
        /// </summary>
        EntityReflectorArgs Args { get; }

        /// <summary>
        /// Gets the entity <see cref="Type"/>.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> <see cref="TypeReflectorTypeCode"/>.
        /// </summary>
        TypeReflectorTypeCode TypeCode { get; }

        /// <summary>
        /// Gets the underlying item <see cref="System.Type"/> where <see cref="Type"/> implements <see cref="IEnumerable"/>. 
        /// </summary>
        Type? ItemType { get; }

        /// <summary>
        /// Gets the underlying <see cref="ItemType"/> <see cref="TypeReflectorTypeCode"/>.
        /// </summary>
        TypeReflectorTypeCode? ItemTypeCode { get; }

        /// <summary>
        /// Gets the <see cref="Dictionary{TKey, TValue}"/> for storing additional data.
        /// </summary>
        Dictionary<string, object?> Data { get; }

        /// <summary>
        /// Gets the <see cref="IPropertyReflector"/> for the specified property name.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <returns>The <see cref="IPropertyReflector"/>.</returns>
        IPropertyReflector GetProperty(string name);

        /// <summary>
        /// Gets the <see cref="IPropertyReflector"/> for the specified JSON name.
        /// </summary>
        /// <param name="jsonName">The JSON name.</param>
        /// <returns>The <see cref="IPropertyReflector"/>.</returns>
        /// <remarks>Uses the <see cref="EntityReflectorArgs.NameComparer"/> to match the JSON name.</remarks>
        IPropertyReflector? GetJsonProperty(string jsonName);

        /// <summary>
        /// Creates a new instance of the <see cref="Type"/> using the default empty constructor.
        /// </summary>
        /// <returns>A new instance of the <see cref="Type"/>.</returns>
        object CreateInstance() => Activator.CreateInstance(Type);

        /// <summary>
        /// Gets the <see cref="IEntityReflector"/> for <see cref="ItemType"/> where it is a class.
        /// </summary>
        /// <returns>The corresponding <see cref="IEntityReflector"/>.</returns>
        IEntityReflector? GetItemEntityReflector();

        /// <summary>
        /// Compares two values for equality;
        /// </summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns><c>true</c> indicates that they are equal; otherwise, <c>false</c>.</returns>
        bool Compare(object? x, object? y);
    }
}