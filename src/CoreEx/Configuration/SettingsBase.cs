// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Hosting.Work;
using CoreEx.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace CoreEx.Configuration
{
    /// <summary>
    /// Provides the base <see cref="Configuration"/>-backed settings; see <see cref="GetValue{T}(string, T)"/> to further understand capabilities.
    /// </summary>
    public abstract class SettingsBase
    {
        private readonly List<string> _prefixes = [];
        private bool? _validationUseJsonNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsBase"/> class.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
        /// <param name="prefixes">The key prefixes to use in order of precedence, first through to last.</param>
        public SettingsBase(IConfiguration? configuration, params string[] prefixes)
        {
            Configuration = configuration;
            Deployment = new DeploymentInfo(configuration);

            foreach (var prefix in prefixes)
            {
                if (string.IsNullOrEmpty(prefix))
                    throw new ArgumentException("A prefix cannot be null or empty.", nameof(prefixes));

                _prefixes.Add(prefix.EndsWith('/') ? prefix : string.Concat(prefix, '/'));
            }
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
            // One-off replace double underscore with colon; enables support for both types.
            var ckey = key.ThrowIfNullOrEmpty(nameof(key)).Replace("__", ":");

            if (Configuration is null)
                return defaultValue;

            // Try each prefix until found.
            T kv;
            foreach (var prefix in _prefixes)
            {
                if (TryGetValue(string.Concat(prefix, ckey), out kv))
                    return kv;
            }

            // Final without prefix.
            return TryGetValue(ckey, out kv) ? kv : defaultValue;
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
            // One-off replace double underscore with colon; enables support for both types.
            var ckey = key.ThrowIfNullOrEmpty(nameof(key)).Replace("__", ":");

            if (Configuration == null)
                throw new InvalidOperationException($"An IConfiguration instance is required where {nameof(GetRequiredValue)} is used.");

            // Try each prefix until found.
            T kv;
            foreach (var prefix in _prefixes)
            {
                if (TryGetValue(string.Concat(prefix, ckey), out kv))
                    return kv;
            }

            // Final without prefix.
            return TryGetValue(ckey, out kv) ? kv : throw new ArgumentException($"Configuration key '{key}' has not been configured and the value is required.", nameof(key));
        }

        /// <summary>
        /// Indicates whether the <see cref="TypedHttpClientBase{TSelf}"/> logs the request and response <see cref="HttpContent"/>. It is recommended that this is <b>only</b> used for development/debugging purposes.
        /// </summary>
        [Obsolete("This feature will soon be deprecated; please leverage IHttpClientFactory capabilies. See https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-logging/?view=aspnetcore-8.0 on how to implement.")]
        public bool HttpLogContent => GetValue(nameof(HttpLogContent), false);

        /// <summary>
        /// Gets the default <see cref="TypedHttpClientBase{TSelf}"/> retry count. Defaults to <c>3</c>.
        /// </summary>
        [Obsolete("This feature will soon be deprecated; please leverage IHttpClientFactory capabilies. See https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests on how to implement.")]
        public int HttpRetryCount => GetValue(nameof(HttpRetryCount), 3);

        /// <summary>
        /// Gets the default <see cref="TypedHttpClientBase{TSelf}"/> retry delay in seconds. Defaults to <c>1.8</c>.
        /// </summary>
        [Obsolete("This feature will soon be deprecated; please leverage IHttpClientFactory capabilies. See https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests on how to implement.")]
        public double HttpRetrySeconds => GetValue(nameof(HttpRetrySeconds), 1.8d);

        /// <summary>
        /// Gets the default <see cref="TypedHttpClientBase{TSelf}"/> timeout. Defaults to <c>90</c> seconds.
        /// </summary>
        [Obsolete("This feature will soon be deprecated; please leverage IHttpClientFactory capabilies. See https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests on how to implement.")]
        public int HttpTimeoutSeconds => GetValue(defaultValue: 90);

        /// <summary>
        /// Gets the default <see cref="TypedHttpClientBase{TSelf}"/> maximum retry delay. Defaults to <c>2</c> minutes.
        /// </summary>
        [Obsolete("This feature will soon be deprecated; please leverage IHttpClientFactory capabilies. See https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests on how to implement.")]
        public TimeSpan HttpMaxRetryDelay => TimeSpan.FromSeconds(GetValue(defaultValue: 120));

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

        /// <summary>
        /// Gets the default validation use of JSON names. Defaults to <c>true</c>.
        /// </summary>
        public bool ValidationUseJsonNames => _validationUseJsonNames ??= GetValue(nameof(ValidationUseJsonNames), true);

        /// <summary>
        /// Gets or sets the <see cref="WorkStateOrchestrator"/> <see cref="WorkStateOrchestrator.ExpiryTimeSpan"/>. Defaults to <c>1</c> hour.
        /// </summary>
        public TimeSpan WorkerExpiryTimeSpan => GetValue(nameof(WorkerExpiryTimeSpan), TimeSpan.FromHours(1));
    }
}