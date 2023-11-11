// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Mapping.Converters;
using CoreEx.Mapping;
using System;
using Microsoft.Xrm.Sdk;

namespace CoreEx.Dataverse.Mapping
{
    /// <summary>
    /// Enables bi-directional property and <i>Dataverse</i> column mapping.
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
        /// Indicates whether the property forms part of the primary key. 
        /// </summary>
        bool IsPrimaryKey { get; }

        /// <summary>
        /// Indicates whether the primary key value maps to the underlying <see cref="Entity.Id"/> versus <see cref="Entity.KeyAttributes"/>.
        /// </summary>
        bool IsPrimaryKeyUseEntityIdentifier { get; }

        /// <summary>
        /// Sets the primary key (<see cref="IsPrimaryKey"/>).
        /// </summary>
        /// <param name="useEntityIdentifier">Indicates whether the primary key value maps to the underlying <see cref="Entity.Id"/> versus <see cref="Entity.KeyAttributes"/>.</param>
        void SetPrimaryKey(bool useEntityIdentifier = true);

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
        /// Gets the <see cref="IDataverseMapper"/> to map complex types.
        /// </summary>
        IDataverseMapper? Mapper { get; }

        /// <summary>
        /// Set the <see cref="IDataverseMapper"/> to map complex types.
        /// </summary>
        /// <param name="mapper">The <see cref="IDataverseMapper"/>.</param>
        /// <remarks>The <see cref="Mapper"/> and <see cref="Converter"/> are mutually exclusive.</remarks>
        void SetMapper(IDataverseMapper mapper);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="entity"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="entity">The <see cref="Entity"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapToDataverse(object value, Entity entity, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps from a <paramref name="entity"/> updating the <paramref name="value"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapFromDataverse(Entity entity, object value, OperationTypes operationType = OperationTypes.Unspecified);
    }
}