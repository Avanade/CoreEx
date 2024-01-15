// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Caching.Memory;
using System;

namespace CoreEx.RefData.Caching
{
    /// <summary>
    /// Enables fixed expiration <see cref="ReferenceDataOrchestrator"/> <see cref="ICacheEntry"/> configuration capabilities.
    /// </summary>
    /// <remarks>Provides a consistent fixed expiration for all cache entries.</remarks>
    /// <param name="absoluteExpirationRelativeToNow">The <see cref="ICacheEntry.AbsoluteExpirationRelativeToNow"/> value.</param>
    /// <param name="slidingExpiration">The <see cref="ICacheEntry.SlidingExpiration"/> value.</param>
    public sealed class FixedExpirationCacheEntry(TimeSpan? absoluteExpirationRelativeToNow = null, TimeSpan? slidingExpiration = null) : ICacheEntryConfig
    {
        private readonly TimeSpan? _absoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
        private readonly TimeSpan? _slidingExpiration = slidingExpiration;

        /// <summary>
        /// Provides an opportunity to the maintain the <see cref="ICacheEntry"/> data prior to the cache <i>create</i> function being invoked (as a result of <see cref="ReferenceDataOrchestrator.OnGetOrCreateAsync"/>).
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <param name="entry">The <see cref="ICacheEntry"/>.</param>
        public void CreateCacheEntry(Type type, ICacheEntry entry)
        {
            entry.AbsoluteExpirationRelativeToNow = _absoluteExpirationRelativeToNow;
            entry.SlidingExpiration = _slidingExpiration;
        }
    }
}