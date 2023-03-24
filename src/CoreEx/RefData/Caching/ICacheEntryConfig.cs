// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Caching.Memory;
using System;

namespace CoreEx.RefData.Caching
{
    /// <summary>
    /// Provides the <see cref="ReferenceDataOrchestrator"/> <see cref="ICacheEntry"/> configuration capabilities.
    /// </summary>
    public interface ICacheEntryConfig
    {
        /// <summary>
        /// Gets the cache key to be used (defaults to <paramref name="type"/>).
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The cache key.</returns>
        /// <remarks>To support the likes of multi-tenancy caching then the resulting cache key should be overridden to include the both the <see cref="ExecutionContext.TenantId"/> and <paramref name="type"/>.</remarks>
        public object GetCacheKey(Type type) => type;

        /// <summary>
        /// Provides an opportunity to the maintain the <see cref="ICacheEntry"/> data prior to the cache <i>create</i> function being invoked (as a result of <see cref="ReferenceDataOrchestrator.OnGetOrCreateAsync"/>).
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <param name="entry">The <see cref="ICacheEntry"/>.</param>
        void CreateCacheEntry(Type type, ICacheEntry entry);
    }
}