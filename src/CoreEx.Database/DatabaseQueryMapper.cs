// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;
using System;

namespace CoreEx.Database
{
    /// <summary>
    /// Enables a database query only <see cref="IDatabaseMapper{T}"/>.
    /// </summary>
    /// <typeparam name="T">The resulting <see cref="Type"/>.</typeparam>
    public abstract class DatabaseQueryMapper<T> : IDatabaseMapper<T>
    {
        /// <inheritdoc/>
        /// <remarks>This method will result in a <see cref="NotSupportedException"/>.</remarks>
        public abstract T MapFromDb(DatabaseRecord record, OperationTypes operationType = OperationTypes.Unspecified);

        /// <inheritdoc/>
        /// <remarks>This method will result in a <see cref="NotSupportedException"/>.</remarks>
        void IDatabaseMapper<T>.MapToDb(T? value, DatabaseParameterCollection parameters, OperationTypes operationType) => throw new NotSupportedException();
    }
}