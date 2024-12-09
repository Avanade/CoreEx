// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Caching.Memory;
using System.Runtime.CompilerServices;

[assembly: 
    InternalsVisibleTo("CoreEx.AspNetCore, PublicKey=00240000048000009400000006020000002400005253413100040000010001007dee530af6d801902d40685e9cd0a3d8991ddbf545be3ef6147c9f79bacd7464d92fbd94fee34e885c37e3dff4ea15a4f9978f1f614798e0f48e3a3d5bf15e8b2fba9c19b6966838f97444bc247bc101454946d70ac93207cf2c611956aed59c316f81f1bf8c8486f8f0b3f9adf93c2f07e06a86745f6dc4b819c2bc2f3fdad5"),
    InternalsVisibleTo("CoreEx.Azure, PublicKey=00240000048000009400000006020000002400005253413100040000010001007dee530af6d801902d40685e9cd0a3d8991ddbf545be3ef6147c9f79bacd7464d92fbd94fee34e885c37e3dff4ea15a4f9978f1f614798e0f48e3a3d5bf15e8b2fba9c19b6966838f97444bc247bc101454946d70ac93207cf2c611956aed59c316f81f1bf8c8486f8f0b3f9adf93c2f07e06a86745f6dc4b819c2bc2f3fdad5"),
    InternalsVisibleTo("CoreEx.Database.SqlServer, PublicKey=00240000048000009400000006020000002400005253413100040000010001007dee530af6d801902d40685e9cd0a3d8991ddbf545be3ef6147c9f79bacd7464d92fbd94fee34e885c37e3dff4ea15a4f9978f1f614798e0f48e3a3d5bf15e8b2fba9c19b6966838f97444bc247bc101454946d70ac93207cf2c611956aed59c316f81f1bf8c8486f8f0b3f9adf93c2f07e06a86745f6dc4b819c2bc2f3fdad5"),
    InternalsVisibleTo("CoreEx.Solace, PublicKey=00240000048000009400000006020000002400005253413100040000010001007dee530af6d801902d40685e9cd0a3d8991ddbf545be3ef6147c9f79bacd7464d92fbd94fee34e885c37e3dff4ea15a4f9978f1f614798e0f48e3a3d5bf15e8b2fba9c19b6966838f97444bc247bc101454946d70ac93207cf2c611956aed59c316f81f1bf8c8486f8f0b3f9adf93c2f07e06a86745f6dc4b819c2bc2f3fdad5"),
]

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
        internal static IMemoryCache MemoryCache => ExecutionContext.GetService<IInternalCache>() ?? (_fallbackCache ??= new MemoryCache(new MemoryCacheOptions()));

        /// <summary>
        /// Represents a cache for internal capabilities.
        /// </summary>
        public interface IInternalCache : IMemoryCache { }
    }
}