// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Mapping;
using CoreEx.Mapping.Converters;
using System;
using System.Data;
using System.Data.Common;

namespace CoreEx.Database.Mapping
{
    /// <summary>
    /// Enables bi-directional property and database column mapping.
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
        /// Gets the destination database column name.
        /// </summary>
        string ColumnName { get; }

        /// <summary>
        /// Gets the destination <see cref="DbParameter.ParameterName"/>.
        /// </summary>
        string ParameterName { get; }

        /// <summary>
        /// Gets the <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property (default to <see cref="OperationTypes.Any"/>).
        /// </summary>
        OperationTypes OperationTypes { get; }

        /// <summary>
        /// Indicates whether the property forms part of the primary key. 
        /// </summary>
        bool IsPrimaryKey { get; }

        /// <summary>
        /// Indicates whether the primary key value is generated on create. 
        /// </summary>
        bool IsPrimaryKeyGeneratedOnCreate { get; }

        /// <summary>
        /// Sets the primary key (<see cref="IsPrimaryKey"/> and <see cref="IsPrimaryKeyGeneratedOnCreate"/>).
        /// </summary>
        /// <param name="generatedOnCreate">Indicates whether the column value is generated on create (defaults to <c>true</c>).</param>
        void SetPrimaryKey(bool generatedOnCreate = true);

        /// <summary>
        /// Gets the <see cref="System.Data.DbType"/>.
        /// </summary>
        DbType? DbType { get; }

        /// <summary>
        /// Sets the <see cref="DbType"/>.
        /// </summary>
        /// <param name="dbType">The <see cref="System.Data.DbType"/></param>
        void SetDbType(DbType dbType);

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
        /// Gets the <see cref="IDatabaseMapper"/> to map complex types.
        /// </summary>
        IDatabaseMapper? Mapper { get; }

        /// <summary>
        /// Set the <see cref="IDatabaseMapper"/> to map complex types.
        /// </summary>
        /// <param name="mapper">The <see cref="IDatabaseMapper"/>.</param>
        /// <remarks>The <see cref="Mapper"/> and <see cref="Converter"/> are mutually exclusive.</remarks>
        void SetMapper(IDatabaseMapper mapper);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="parameters"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The <see cref="DatabaseParameterCollection"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapToDb(object value, DatabaseParameterCollection parameters, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps from a <paramref name="record"/> updating the <paramref name="value"/>.
        /// </summary>
        /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapFromDb(DatabaseRecord record, object value, OperationTypes operationType = OperationTypes.Unspecified);
    }
}