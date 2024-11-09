// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Caching.Memory;

namespace CoreEx.Abstractions
{
    /// <summary>
    /// Provides shareable internal capabilities.
    /// </summary>
    /// <remarks>This is intended for internal usage only; use with caution.</remarks>
    public static class Internal
    {
        private static IMemoryCache? _fallbackCache;

        /// <summary>
        /// Gets the <b>CoreEx</b> fallback <see cref="IMemoryCache"/>.
        /// </summary>
        public static IMemoryCache MemoryCache => ExecutionContext.GetService<IInternalCache>() ?? (_fallbackCache ??= new MemoryCache(new MemoryCacheOptions()));

        /// <summary>
        /// Represents a cache for internal capabilities.
        /// </summary>
        public interface IInternalCache : IMemoryCache { }
    }
}