﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;
using System;

namespace CoreEx.Database
{
    /// <summary>
    /// Provides a per <see cref="DatabaseRecord"/> mapper that simulates a <see cref="IDatabaseMapper{T}.MapFromDb(DatabaseRecord, OperationTypes)"/> by invoking the function passed into the constructor.
    /// </summary>
    /// <param name="func">The <see cref="DatabaseRecord"/> mapping function.</param>
    internal class DatabaseRecordMapper<T>(Func<DatabaseRecord, T> func) : IDatabaseMapper<T>
    {
        private readonly Func<DatabaseRecord, T> _func = func.ThrowIfNull(nameof(func));

        /// <inheritdoc/>
        T? IDatabaseMapper<T>.MapFromDb(DatabaseRecord record, OperationTypes operationType) => _func(record);

        /// <inheritdoc/>
        /// <remarks>This method will result in a <see cref="NotSupportedException"/>.</remarks>
        void IDatabaseMapper<T>.MapToDb(T? value, DatabaseParameterCollection parameters, OperationTypes operationType) => throw new NotSupportedException();
    }
}