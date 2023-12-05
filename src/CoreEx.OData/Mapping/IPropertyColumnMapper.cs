// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Mapping.Converters;
using CoreEx.Mapping;
using System;

namespace CoreEx.OData.Mapping
{
    /// <summary>
    /// Enables bi-directional property and <see cref="ODataItem"/> column mapping.
    /// </summary>
    public interface IPropertyColumnMapper
    {
        /// <summary>
        /// Gets the <see cref="IPropertyExpression"/>.
        /// </summary>
        IPropertyExpression PropertyExpression { get; }

        /// <summary>
        /// Gets the source property name.
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        /// Gets the source property <see cref="Type"/>.
        /// </summary>
        Type PropertyType { get; }

        /// <summary>
        /// Indicates whether the underlying source property is a complex type.
        /// </summary>
        bool IsSrcePropertyComplex { get; }

        /// <summary>
        /// Gets the destination <i>Dataverse</i> column name.
        /// </summary>
        string ColumnName { get; }

        /// <summary>
        /// Gets the <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property (default to <see cref="OperationTypes.Any"/>).
        /// </summary>
        OperationTypes OperationTypes { get; }

        /// <summary>
        /// Gets the <see cref="IConverter"/> (used where a specific source and destination type conversion is required).
        /// </summary>
        IConverter? Converter { get; }

        /// <summary>
        /// Sets the <see cref="Converter"/>.
        /// </summary>
        /// <param name="converter">The <see cref="IConverter"/>.</param>
        /// <remarks>The <see cref="Converter"/> and <see cref="Mapper"/> are mutually exclusive.</remarks>
        void SetConverter(IConverter converter);

        /// <summary>
        /// Gets the <see cref="IMapper"/> to map complex types.
        /// </summary>
        IODataMapper? Mapper { get; }

        /// <summary>
        /// Set the <see cref="IODataMapper"/> to map complex types.
        /// </summary>
        /// <param name="mapper">The <see cref="IODataMapper"/>.</param>
        /// <remarks>The <see cref="Mapper"/> and <see cref="Converter"/> are mutually exclusive.</remarks>
        void SetMapper(IODataMapper mapper);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="entity"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="entity">The <see cref="ODataItem"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapToOData(object value, ODataItem entity, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps from a <paramref name="entity"/> updating the <paramref name="value"/>.
        /// </summary>
        /// <param name="entity">The <see cref="ODataItem"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapFromOData(ODataItem entity, object value, OperationTypes operationType = OperationTypes.Unspecified);
    }
}