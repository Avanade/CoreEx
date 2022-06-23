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
    /// <typeparam name="TSource">The <see cref="IDatabaseMapper.SourceType"/>.</typeparam>
    public interface IDatabaseMapper<TSource> : IDatabaseMapper
    {
        /// <inheritdoc/>
        Type IDatabaseMapper.SourceType => typeof(TSource);

        /// <inheritdoc/>
        object? IDatabaseMapper.MapFromDb(DatabaseRecord record, OperationTypes operationType) => MapFromDb(record, operationType)!;

        /// <inheritdoc/>
        void IDatabaseMapper.MapToDb(object? value, DatabaseParameterCollection parameters, OperationTypes operationType) => MapToDb((TSource?)value, parameters, operationType);

        /// <summary>
        /// Maps from a <paramref name="record"/> creating a corresponding instance of <typeparamref name="TSource"/>.
        /// </summary>
        /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The corresponding instance of <typeparamref name="TSource"/>.</returns>
        new TSource? MapFromDb(DatabaseRecord record, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="parameters"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The <see cref="DatabaseParameterCollection"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapToDb(TSource? value, DatabaseParameterCollection parameters, OperationTypes operationType = OperationTypes.Unspecified);

        /// <inheritdoc/>
        void IDatabaseMapper.MapPrimaryKeyParameters(DatabaseParameterCollection parameters, OperationTypes operationType, object? value) => MapPrimaryKeyParameters(parameters, operationType, (TSource?)value);

        /// <summary>
        /// Maps the primary key from the <paramref name="value"/> and adds to the <paramref name="parameters"/>.
        /// </summary>
        /// <param name="parameters">The <see cref="DatabaseParameterCollection"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> that indicates that a <see cref="OperationTypes.Create"/> is being performed; therefore, any key properties that are auto-generated will have a parameter
        /// direction of <see cref="ParameterDirection.Output"/> versus <see cref="ParameterDirection.Input"/>.</param>
        /// <param name="value">The value.</param>
        void MapPrimaryKeyParameters(DatabaseParameterCollection parameters, OperationTypes operationType, TSource? value) => throw new NotSupportedException();

        /// <inheritdoc/>
        void IDatabaseMapper.MapPrimaryKeyParameters(DatabaseParameterCollection parameters, OperationTypes operationType, CompositeKey key) => throw new NotSupportedException();
    }
}