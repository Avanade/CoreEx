// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using System;

namespace CoreEx.Database.Mapping
{
    /// <summary>
    /// Enables <see cref="Create{TSource}"/> or <see cref="CreateAuto{TSource}(string[])"/> of a <see cref="DatabaseMapper{TSource}"/>.
    /// </summary>
    public static class DatabaseMapper
    {
        /// <summary>
        /// Creates a <see cref="DatabaseMapper{TSource}"/> where properties are added manually (leverages reflection).
        /// </summary>
        /// <returns>A <see cref="DatabaseMapper{TSource}"/>.</returns>
        /// <remarks>Where performance is critical consider using <see cref="CreateExtended{TSource}"/>.</remarks>
        public static DatabaseMapper<TSource> Create<TSource>() where TSource : class, new() => new(false);

        /// <summary>
        /// Creates a <see cref="DatabaseMapper{TSource}"/> where properties are added automatically using reflection (assumes the property, column and parameter names share the same name).
        /// </summary>
        /// <param name="ignoreSrceProperties">An array of source property names to ignore.</param>
        /// <returns>A <see cref="DatabaseMapper{TSource}"/>.</returns>
        /// <remarks>Where performance is critical consider using <see cref="CreateExtended{TSource}"/>.</remarks>
        public static DatabaseMapper<TSource> CreateAuto<TSource>(params string[] ignoreSrceProperties) where TSource : class, new() => new(true, ignoreSrceProperties);

        /// <summary>
        /// Creates a <see cref="DatabaseMapperEx{TSource}"/> where the underlying implementation is added explicitly (extended, offers potential performance benefits).
        /// </summary>
        /// <returns>A <see cref="DatabaseMapperEx{TSource}"/>.</returns>
        public static DatabaseMapperEx<TSource> CreateExtended<TSource>(Action<DatabaseRecord, TSource, OperationTypes>? mapFromDb = null, Action<CompositeKey, DatabaseParameterCollection>? mapKeyToDb = null, Action<TSource?, DatabaseParameterCollection, OperationTypes>? mapToDb = null) where TSource : class, new()
            => new(mapFromDb, mapKeyToDb, mapToDb);
    }
}