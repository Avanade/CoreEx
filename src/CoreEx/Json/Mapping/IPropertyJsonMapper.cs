// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Mapping.Converters;
using CoreEx.Mapping;
using System;
using System.Text.Json.Nodes;

namespace CoreEx.Json.Mapping
{
    /// <summary>
    /// Enables bi-directional property and <see cref="JsonObject"/> property mapping.
    /// </summary>
    public interface IPropertyJsonMapper
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
        /// Gets the destination <see cref="JsonObject"/> property name.
        /// </summary>
        string JsonName { get; }

        /// <summary>
        /// Gets the <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property (default to <see cref="OperationTypes.Any"/>).
        /// </summary>
        OperationTypes OperationTypes { get; }

        /// <summary>
        /// Indicates whether the property forms part of the primary key. 
        /// </summary>
        bool IsPrimaryKey { get; }

        /// <summary>
        /// Sets the primary key (<see cref="IsPrimaryKey"/>).
        /// </summary>
        void SetPrimaryKey();

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
        /// Gets the <see cref="IJsonObjectMapper"/> to map complex types.
        /// </summary>
        IJsonObjectMapper? Mapper { get; }

        /// <summary>
        /// Set the <see cref="IJsonObjectMapper"/> to map complex types.
        /// </summary>
        /// <param name="mapper">The <see cref="IJsonObjectMapper"/>.</param>
        /// <remarks>The <see cref="Mapper"/> and <see cref="Converter"/> are mutually exclusive.</remarks>
        void SetMapper(IJsonObjectMapper mapper);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="json"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="json">The <see cref="JsonObject"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapToJson(object value, JsonObject json, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps from a <paramref name="json"/> updating the <paramref name="value"/>.
        /// </summary>
        /// <param name="json">The <see cref="JsonObject"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapFromJson(JsonObject json, object value, OperationTypes operationType = OperationTypes.Unspecified);
    }
}