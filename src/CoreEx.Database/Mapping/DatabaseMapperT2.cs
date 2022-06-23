// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Database.Mapping
{
    /// <summary>
    /// Provides <see cref="DatabaseMapper{TSource}"/> with a singleton <see cref="Default"/>.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TMapper">The mapper <see cref="Type"/>.</typeparam>
    public abstract class DatabaseMapper<TSource, TMapper> : DatabaseMapper<TSource> where TSource : class, new() where TMapper : DatabaseMapper<TSource, TMapper>, new()
    {
        private static readonly TMapper _default = new();

        /// <summary>
        /// Gets the current instance of the mapper.
        /// </summary>
        public static TMapper Default => _default ?? throw new InvalidOperationException("An instance of this Mapper cannot be referenced as it is still being constructed; beware that you may have a circular reference within the constructor.");
    }
}