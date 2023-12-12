// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CoreEx.OData.Mapping
{
    /// <summary>
    /// Defines an <see cref="ODataItem"/> mapper.
    /// </summary>
    public interface IODataMapper
    {
        /// <summary>
        /// Gets the source <see cref="Type"/> being mapped from/to the <see cref="ODataItem"/>.
        /// </summary>
        Type SourceType { get; }

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

        /// <summary>
        /// Maps from an <paramref name="entity"/> creating a corresponding instance of the <see cref="SourceType"/>.
        /// </summary>
        /// <param name="entity">The <see cref="ODataItem"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The corresponding instance of the <see cref="SourceType"/>.</returns>
        object? MapFromOData(ODataItem entity, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="entity"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="entity">The <see cref="ODataItem"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapToOData(object? value, ODataItem entity, OperationTypes operationType = OperationTypes.Unspecified);
    }
}