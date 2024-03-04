// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using System;

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
        /// Maps the <paramref name="key"/> and adds to the <paramref name="parameters"/>.
        /// </summary>
        /// <param name="key">The primary <see cref="CompositeKey"/>.</param>
        /// <param name="parameters">The <see cref="DatabaseParameterCollection"/>.</param>
        /// <remarks>This is used to map the only the key parameters; for example a <b>Get</b> or <b>Delete</b> operation.</remarks>
        void MapKeyToDb(CompositeKey key, DatabaseParameterCollection parameters);
    }
}