// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace CoreEx.RefData.Caching
{
    /// <summary>
    /// Enables the <see cref="SettingsBase"/>-based <see cref="ReferenceDataOrchestrator"/> <see cref="ICacheEntry"/> configuration capabilities.
    /// </summary>
    /// <remarks>See <see href="https://github.com/Avanade/CoreEx/blob/main/src/CoreEx/RefData/README.md#cache-policy-configuration">cache policy configuration</see> for more information.
    /// <para>Where the <see cref="SettingsBase"/> has not been configured, then the default behaviour sets the following: <see cref="ICacheEntry.AbsoluteExpirationRelativeToNow"/> = 2 hours, and <see cref="ICacheEntry.SlidingExpiration"/> = 30 minutes.</para></remarks>
    /// <param name="settings">The <see cref="IServiceProvider"/>.</param>
    public class SettingsBasedCacheEntry(SettingsBase? settings) : ICacheEntryConfig
    {
        /// <summary>
        /// Gets the optional <see cref="SettingsBase"/>.
        /// </summary>
        public SettingsBase? Settings { get; } = settings;

        /// <summary>
        /// Gets the cache key to be used (defaults to <paramref name="type"/>).
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The cache key.</returns>
        /// <remarks>To support the likes of multi-tenancy caching then the resulting cache key should be overridden to include the both the <see cref="ExecutionContext.TenantId"/> and <paramref name="type"/>.</remarks>
        public virtual object GetCacheKey(Type type) => type;

        /// <summary>
        /// Provides an opportunity to the maintain the <see cref="ICacheEntry"/> data prior to the cache <i>create</i> function being invoked (as a result of <see cref="ReferenceDataOrchestrator.OnGetOrCreateAsync"/>).
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <param name="entry">The <see cref="ICacheEntry"/>.</param>
        /// <remarks>The default behaviour sets the following: <see cref="ICacheEntry.AbsoluteExpirationRelativeToNow"/> = 2 hours, and <see cref="ICacheEntry.SlidingExpiration"/> = 30 minutes unless overidden by optional <see cref="Settings"/>.
        /// This should be overridden where more advanced behaviour is required.</remarks>
        public virtual void CreateCacheEntry(Type type, ICacheEntry entry)
        {
            entry.AbsoluteExpirationRelativeToNow = Settings?.GetCoreExValue($"RefDataCache:{type.Name}:{nameof(ICacheEntry.AbsoluteExpirationRelativeToNow)}", Settings.RefDataCacheAbsoluteExpirationRelativeToNow) ?? TimeSpan.FromHours(2);
            entry.SlidingExpiration = Settings?.GetCoreExValue($"RefDataCache:{type.Name}:{nameof(ICacheEntry.SlidingExpiration)}", Settings.RefDataCacheSlidingExpiration) ?? TimeSpan.FromMinutes(30);
        }
    }
}