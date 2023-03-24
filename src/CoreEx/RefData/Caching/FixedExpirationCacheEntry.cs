// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Caching.Memory;
using System;

namespace CoreEx.RefData.Caching
{
    /// <summary>
    /// Enables fixed expiration <see cref="ReferenceDataOrchestrator"/> <see cref="ICacheEntry"/> configuration capabilities.
    /// </summary>
    /// <remarks>Provides a consistent fixed expiration for all cache entries.</remarks>
    public sealed class FixedExpirationCacheEntry : ICacheEntryConfig
    {
        private readonly TimeSpan? _absoluteExpirationRelativeToNow;
        private readonly TimeSpan? _slidingExpiration;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedExpirationCacheEntry"/> using only the specified <paramref name="absoluteExpirationRelativeToNow"/> and <paramref name="slidingExpiration"/>.
        /// </summary>
        /// <param name="absoluteExpirationRelativeToNow">The <see cref="ICacheEntry.AbsoluteExpirationRelativeToNow"/> value.</param>
        /// <param name="slidingExpiration">The <see cref="ICacheEntry.SlidingExpiration"/> value.</param>
        public FixedExpirationCacheEntry(TimeSpan? absoluteExpirationRelativeToNow = null, TimeSpan? slidingExpiration = null)
        {
            _absoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
            _slidingExpiration = slidingExpiration;
        }

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