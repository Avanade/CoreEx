// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;

namespace CoreEx.Database.Mapping
{
    /// <summary>
    /// Defines a database extended/explicit mapper.
    /// </summary>
    /// <typeparam name="TSource">The <see cref="IDatabaseMapper.SourceType"/>.</typeparam>
    public interface IDatabaseMapperEx<TSource> : IDatabaseMapperEx, IDatabaseMapper<TSource>
    {
        /// <inheritdoc/>
        void IDatabaseMapperEx.MapFromDb(DatabaseRecord record, object value, OperationTypes operationType)
            => MapFromDb(record, (TSource)value, operationType);

        /// <summary>
        /// Maps from a <paramref name="record"/> into the <paramref name="value"/>
        /// </summary>
        /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
        /// <param name="value">The value to extend map into.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The updated <paramref name="value"/>.</returns>
        void MapFromDb(DatabaseRecord record, TSource value, OperationTypes operationType = OperationTypes.Unspecified);
    }
}