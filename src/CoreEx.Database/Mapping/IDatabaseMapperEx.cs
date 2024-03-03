// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;

namespace CoreEx.Database.Mapping
{
    /// <summary>
    /// Defines a database extended/explicit mapper.
    /// </summary>
    public interface IDatabaseMapperEx : IDatabaseMapper
    {
        /// <summary>
        /// Maps from a <paramref name="record"/> into the <paramref name="value"/>
        /// </summary>
        /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
        /// <param name="value">The value to map into.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapFromDb(DatabaseRecord record, object value, OperationTypes operationType = OperationTypes.Unspecified);
    }
}