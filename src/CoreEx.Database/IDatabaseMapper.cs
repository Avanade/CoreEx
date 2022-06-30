// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using System;
using System.Data;

namespace CoreEx.Database
{
    /// <summary>
    /// Defines a database mapper.
    /// </summary>
    public interface IDatabaseMapper
    {
        /// <summary>
        /// Gets the source <see cref="Type"/> being mapped from/to the database.
        /// </summary>
        Type SourceType { get; }

        /// <summary>
        /// Maps from a <paramref name="record"/> creating a corresponding instance of the <see cref="SourceType"/>.
        /// </summary>
        /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The corresponding instance of the <see cref="SourceType"/>.</returns>
        object? MapFromDb(DatabaseRecord record, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="parameters"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The <see cref="DatabaseParameterCollection"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapToDb(object? value, DatabaseParameterCollection parameters, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps the primary key from the <paramref name="value"/> and adds to the <paramref name="parameters"/>.
        /// </summary>
        /// <param name="parameters">The <see cref="DatabaseParameterCollection"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> that indicates that a <see cref="OperationTypes.Create"/> is being performed; therefore, any key properties that are auto-generated will have a parameter
        /// direction of <see cref="ParameterDirection.Output"/> versus <see cref="ParameterDirection.Input"/>.</param>
        /// <param name="value">The value.</param>
        void MapPrimaryKeyParameters(DatabaseParameterCollection parameters, OperationTypes operationType, object? value);

        /// <summary>
        /// Maps the primary key for the listed <paramref name="key"/> and adds to the <paramref name="parameters"/>.
        /// </summary>
        /// <param name="parameters">The <see cref="DatabaseParameterCollection"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> that indicates that a <see cref="OperationTypes.Create"/> is being performed; therefore, any key properties that are auto-generated will have a parameter
        /// direction of <see cref="ParameterDirection.Output"/> versus <see cref="ParameterDirection.Input"/>.</param>
        /// <param name="key">The primary <see cref="CompositeKey"/>.</param>
        void MapPrimaryKeyParameters(DatabaseParameterCollection parameters, OperationTypes operationType, CompositeKey key);
    }
}