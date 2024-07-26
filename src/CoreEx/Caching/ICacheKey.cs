// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;

namespace CoreEx.Caching
{
    /// <summary>
    /// Provides the <see cref="CacheKey"/>.
    /// </summary>
    public interface ICacheKey : IUniqueKey
    {
        /// <summary>
        /// Gets the cache key.
        /// </summary>
        public CompositeKey CacheKey { get; }
    }
}