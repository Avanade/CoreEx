// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using CoreEx.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CoreEx.Configuration
{
    /// <summary>
    /// Provides the base <see cref="Configuration"/>-backed settings; see <see cref="GetValue{T}(string, T)"/> to further understand capabilities.
    /// </summary>
    public abstract class SettingsBase
    {
        private readonly ThreadLocal<bool> _isReflectionCall = new();
        private readonly List<string> _prefixes = [];
        private readonly Dictionary<string, PropertyInfo> _allProperties;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsBase"/> class.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
        /// <param name="prefixes">The key prefixes to use in order of precedence, first through to last.</param>
        public SettingsBase(IConfiguration? configuration, params string[] prefixes)
        {
            Configuration = configuration;
            Deployment = new DeploymentInfo(configuration);

            foreach (var prefix in prefixes.ThrowIfNull(nameof(prefixes)))
            {
                if (string.IsNullOrEmpty(prefix))
                    throw new ArgumentException("Prefixes cannot be null or empty.", nameof(prefixes));

                _prefixes.Add(prefix.EndsWith('/') ? prefix : string.Concat(prefix, '/'));
            }

            _allProperties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).ToDictionary(p => p.Name, p => p);
        }

        /// <summary>
        /// Gets the underlying <see cref="IConfiguration"/>.
        /// </summary>
        public IConfiguration? Configuration { get; }

        /// <summary>
        /// Gets the value using the specified <paramref name="key"/> excluding any prefix (key is inferred where not specified using <see cref="CallerMemberNameAttribute"/>).
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key excluding any prefix (key is inferred where not specified using <see cref="CallerMemberNameAttribute"/>).</param>
        /// <param name="defaultValue">The default fallback value used where no non-default value is found.</param>
        /// <returns>The corresponding setting value.</returns>
        /// <remarks>Where <paramref name="key"/> is '<c>Foo</c>' and the provided prefixes are '<c>Product</c>' and '<c>Common</c>', then the following full keys will be attempted until a non-default value is found:
        /// '<c>Product/Foo</c>', '<c>Common/Foo</c>', '<c>Foo</c>' (no prefix), then finally the <paramref name="defaultValue"/> will be returned.</remarks>
        public T GetValue<T>([CallerMemberName] string key = "", T defaultValue = default!)
        {
            key.ThrowIfNullOrEmpty(nameof(key));

            if (Configuration == null)
                return defaultValue;

            // Do not allow recursive calls to go too deep
            if (_allProperties.TryGetValue(key, out PropertyInfo? pi) && !_isReflectionCall.Value)
            {
                try
                {
                    _isReflectionCall.Value = true;
                    return pi.GetValue(this) is T value ? value : defaultValue;
                }
                finally
                {
                    _isReflectionCall.Value = false;
                }
            }

            T kv;
            foreach (var prefix in _prefixes)
            {
                if (TryGetValue(string.Concat(prefix, key), out kv))
                    return kv;
            }

            return TryGetValue(key, out kv) ? kv : defaultValue;
        }

        /// <summary>
        /// Try get the value with key and alternate format alternatives.
        /// </summary>
        private bool TryGetValue<T>(string key, out T value)
        {
            // Try the key as specified.
            if (Configuration!.GetSection(key)?.Value != null)
            {
                value = Configuration.GetValue<T>(key)!;
                return true;
            }

            // Colon is read as double underscore by Configuration.
            var alternateKey = key.Replace(":", "__");
            if (alternateKey != key && Configuration.GetSection(alternateKey)?.Value != null)
            {
                value = Configuration.GetValue<T>(alternateKey)!;
                return true;
            }

            // Double underscore is read as ":" by Configuration.
            alternateKey = key.Replace("__", ":");
            if (alternateKey != key && Configuration.GetSection(alternateKey)?.Value != null)
            {
                value = Configuration.GetValue<T>(alternateKey)!;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Gets the value using the specified <paramref name="key"/> excluding any prefix (key is inferred where not specified using <see cref="CallerMemberNameAttribute"/>) and throws an <see cref="ArgumentException"/> where no corresponding
        /// value has been configured.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key excluding any prefix (key is inferred where not specified using <see cref="CallerMemberNameAttribute"/>).</param>
        /// <returns>The corresponding setting value.</returns>
        /// <remarks>Where <paramref name="key"/> is '<c>Foo</c>' and the provided prefixes are '<c>Product</c>' and '<c>Common</c>', then the following full keys will be attempted until a non-default value is found:
        /// '<c>Product/Foo</c>', '<c>Common/Foo</c>', '<c>Foo</c>' (no prefix), then finally an <see cref="ArgumentException"/> will be thrown.</remarks>
        /// <exception cref="ArgumentException">Thrown where the <paramref name="key"/> has not been configured.</exception>
        public T GetRequiredValue<T>([CallerMemberName] string key = "")
        {
            key.ThrowIfNullOrEmpty(nameof(key));
            if (Configuration == null)
                throw new InvalidOperationException("An IConfiguration instance is required where GetRequiredValue is used.");

            T kv;
            foreach (var prefix in _prefixes)
            {
                if (TryGetValue(string.Concat(prefix, key), out kv))
                    return kv;
            }

            return TryGetValue(key, out kv) ? kv : throw new ArgumentException($"Configuration key '{key}' has not been configured and the value is required.", nameof(key));
        }

        /// <summary>
        /// Indicates whether the <see cref="TypedHttpClientBase{TSelf}"/> logs the request and response <see cref="HttpContent"/>. It is recommended that this is <b>only</b> used for development/debugging purposes.
        /// </summary>
        public bool HttpLogContent => GetValue(nameof(HttpLogContent), false);

        /// <summary>
        /// Gets the default <see cref="TypedHttpClientBase{TSelf}"/> retry count. Defaults to <c>3</c>.
        /// </summary>
        public int HttpRetryCount => GetValue(nameof(HttpRetryCount), 3);

        /// <summary>
        /// Gets the default <see cref="TypedHttpClientBase{TSelf}"/> retry delay in seconds. Defaults to <c>1.8</c>.
        /// </summary>
        public double HttpRetrySeconds => GetValue(nameof(HttpRetrySeconds), 1.8d);

        /// <summary>
        /// Indicates whether to the include the underlying <see cref="Exception"/> content in the externally returned result. Defaults to <c>false</c>.
        /// </summary>
        public bool IncludeExceptionInResult => GetValue(nameof(IncludeExceptionInResult), false);

        /// <summary>
        /// Gets the default maximum event publish collection size. Defaults to <c>100</c>.
        /// </summary>
        public int MaxPublishCollSize => GetValue(nameof(MaxPublishCollSize), 100);

        /// <summary> 
        /// Gets the <see cref="DeploymentInfo"/> from the environment variables. 
        /// </summary>
        public DeploymentInfo Deployment { get; }

        /// <summary>
        /// Gets the default <see cref="TypedHttpClientBase{TSelf}"/> timeout. Defaults to <c>90</c> seconds.
        /// </summary>
        public int HttpTimeoutSeconds => GetValue(defaultValue: 90);

        /// <summary>
        /// Gets the default <see cref="TypedHttpClientBase{TSelf}"/> maximum retry delay. Defaults to <c>2</c> minutes.
        /// </summary>
        public TimeSpan MaxRetryDelay => TimeSpan.FromSeconds(GetValue(defaultValue: 120));

        /// <summary>
        /// Gets the <see cref="Entities.PagingArgs.DefaultTake"/>; i.e. page size.
        /// </summary>
        public long PagingDefaultTake => GetValue<long>(nameof(PagingDefaultTake), 100);

        /// <summary>
        /// Gets the <see cref="Entities.PagingArgs.MaxTake"/>; i.e. absolute maximum page size.
        /// </summary>
        public long PagingMaxTake => GetValue<long>(nameof(PagingMaxTake), 1000);

        /// <summary>
        /// Gets the default <see cref="RefData.ReferenceDataOrchestrator"/> <see cref="ICacheEntry.AbsoluteExpirationRelativeToNow"/>. Defaults to <c>2</c> hours.
        /// </summary>
        public TimeSpan? RefDataCacheAbsoluteExpirationRelativeToNow => GetValue($"RefDataCache__{nameof(ICacheEntry.AbsoluteExpirationRelativeToNow)}", TimeSpan.FromHours(2));

        /// <summary>
        /// Gets the default <see cref="RefData.ReferenceDataOrchestrator"/> <see cref="ICacheEntry.SlidingExpiration"/>. Defaults to <c>30</c> minutes.
        /// </summary>
        public TimeSpan? RefDataCacheSlidingExpiration => GetValue($"RefDataCache__{nameof(ICacheEntry.SlidingExpiration)}", TimeSpan.FromMinutes(30));
    }
}