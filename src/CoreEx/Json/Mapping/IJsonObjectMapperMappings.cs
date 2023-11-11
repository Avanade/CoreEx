// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CoreEx.Json.Mapping
{
    /// <summary>
    /// Gets the <see cref="IPropertyJsonMapper"/> mappings.
    /// </summary>
    public interface IJsonObjectMapperMappings
    {
        /// <summary>
        /// Gets the <see cref="IPropertyJsonMapper"/> mappings.
        /// </summary>
        IEnumerable<IPropertyJsonMapper> Mappings { get; }

        /// <summary>
        /// Gets the <see cref="IPropertyJsonMapper"/> from the <see cref="Mappings"/> for the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The <see cref="IPropertyJsonMapper"/> where found.</returns>
        /// <exception cref="ArgumentException">Thrown when the property does not exist.</exception>
        IPropertyJsonMapper this[string propertyName] { get; }

        /// <summary>
        /// Attempts to get the <see cref="IPropertyJsonMapper"/> for the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="propertyName">The source property name.</param>
        /// <param name="propertyColumnMapper">The corresponding <see cref="IPropertyJsonMapper"/> item where found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        bool TryGetProperty(string propertyName, [NotNullWhen(true)] out IPropertyJsonMapper? propertyColumnMapper);

        /// <summary>
        /// Gets the <see cref="IPropertyJsonMapper.JsonName"/>.
        /// </summary>
        /// <param name="propertyName">The source property name.</param>
        /// <returns>The <see cref="IPropertyJsonMapper.JsonName"/> where found.</returns>
        /// <exception cref="ArgumentException">Thrown when the property does not exist.</exception>
        string GetJsonName(string propertyName);
    }
}