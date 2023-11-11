// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CoreEx.Dataverse.Mapping
{
    /// <summary>
    /// Gets the <see cref="IPropertyColumnMapper"/> mappings.
    /// </summary>
    public interface IDataverseMapperMappings
    {
        /// <summary>
        /// Gets the <see cref="IPropertyColumnMapper"/> mappings.
        /// </summary>
        IEnumerable<IPropertyColumnMapper> Mappings { get; }

        /// <summary>
        /// Gets the <see cref="IPropertyColumnMapper"/> from the <see cref="Mappings"/> for the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The <see cref="IPropertyColumnMapper"/> where found.</returns>
        /// <exception cref="ArgumentException">Thrown when the property does not exist.</exception>
        IPropertyColumnMapper this[string propertyName] { get; }

        /// <summary>
        /// Attempts to get the <see cref="IPropertyColumnMapper"/> for the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="propertyName">The source property name.</param>
        /// <param name="propertyColumnMapper">The corresponding <see cref="IPropertyColumnMapper"/> item where found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        bool TryGetProperty(string propertyName, [NotNullWhen(true)] out IPropertyColumnMapper? propertyColumnMapper);

        /// <summary>
        /// Gets the <see cref="IPropertyColumnMapper.ColumnName"/>.
        /// </summary>
        /// <param name="propertyName">The source property name.</param>
        /// <returns>The <see cref="IPropertyColumnMapper.ColumnName"/> where found.</returns>
        /// <exception cref="ArgumentException">Thrown when the property does not exist.</exception>
        string GetColumnName(string propertyName);
    }
}