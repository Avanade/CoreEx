// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Enables a reflector for a given <see cref="Type"/>.
    /// </summary>
    public interface ITypeReflector
    {
        /// <summary>
        /// Gets the <see cref="TypeReflectorArgs"/>.
        /// </summary>
        TypeReflectorArgs Args { get; }

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
        /// Gets all the properties for the <see cref="Type"/>.
        /// </summary>
        IEnumerable<IPropertyReflector> GetProperties();

        /// <summary>
        /// Gets the <see cref="IPropertyReflector"/> for the specified property name where it exists.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="property">The <see cref="IPropertyReflector"/> where property exists; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> where the property exists; otherwise, <c>false</c>.</returns>
        bool TryGetProperty(string name, [NotNullWhen(true)] out IPropertyReflector? property);

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
        /// <remarks>Uses the <see cref="TypeReflectorArgs.NameComparer"/> to match the JSON name.</remarks>
        IPropertyReflector? GetJsonProperty(string jsonName);

        /// <summary>
        /// Creates a new instance of the <see cref="Type"/> using the default empty constructor.
        /// </summary>
        /// <returns>A new instance of the <see cref="Type"/>.</returns>
        object CreateInstance() => Activator.CreateInstance(Type)!;

        /// <summary>
        /// Gets the <see cref="ITypeReflector"/> for <see cref="ItemType"/>.
        /// </summary>
        /// <returns>The corresponding <see cref="ITypeReflector"/>.</returns>
        ITypeReflector? GetItemTypeReflector();

        /// <summary>
        /// Compares two values for equality;
        /// </summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns><c>true</c> indicates that they are equal; otherwise, <c>false</c>.</returns>
        bool Compare(object? x, object? y);
    }
}