// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Hosting.Work;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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
        private DateTimeTransform? _dateTimeTransform;
        private StringTransform? _stringTransform;
        private StringTrim? _stringTrim;
        private StringCase? _stringCase;

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
            if (Configuration is not null && Configuration.GetSection(key)?.Value != null)
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
        /// Gets the value using the specified <paramref name="key"/> excluding any prefix (key is inferred where not specified using <see cref="CallerMemberNameAttribute"/>).
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key excluding any prefix (key is inferred where not specified using <see cref="CallerMemberNameAttribute"/>).</param>
        /// <param name="defaultValue">The default fallback value used where no non-default value is found.</param>
        /// <returns>The corresponding setting value.</returns>
        /// <remarks>This is considered a standard setting and will be checked within the <c>CoreEx:</c> nested strructure first to enable clear separation.</remarks>
        internal T GetCoreExValue<T>([CallerMemberName] string key = "", T defaultValue = default!) => TryGetValue<T>($"CoreEx:{key.ThrowIfNullOrEmpty(nameof(key))}", out var value) ? value : GetValue(key, defaultValue);

        /// <summary>
        /// Indicates whether to the include the underlying <see cref="Exception"/> content in the externally returned result.
        /// </summary>
        /// <remarks>Defaults to <c>false</c>.</remarks>
        public bool IncludeExceptionInResult => GetCoreExValue(nameof(IncludeExceptionInResult), false);

        /// <summary>
        /// Gets the default maximum event publish collection size.
        /// </summary>
        /// <remarks>Defaults to <c>100</c>.</remarks>
        public int MaxPublishCollSize => GetCoreExValue(nameof(MaxPublishCollSize), 100);

        /// <summary> 
        /// Gets the <see cref="DeploymentInfo"/> from the environment variables. 
        /// </summary>
        public DeploymentInfo Deployment { get; }

        /// <summary>
        /// Indicates whether to include any extra Health Check data that might be considered sensitive.
        /// </summary>
        /// <remarks>Defaults to <c>false</c>.</remarks>
        public bool IncludeSensitiveHealthCheckData => GetCoreExValue(nameof(IncludeSensitiveHealthCheckData), false);

        /// <summary>
        /// Gets the <see cref="Entities.PagingArgs.DefaultTake"/>; i.e. page size.
        /// </summary>
        /// <remarks>Defaults to <c>100</c>.</remarks>
        public long PagingDefaultTake => GetCoreExValue<long>(nameof(PagingDefaultTake), 100);

        /// <summary>
        /// Gets the <see cref="Entities.PagingArgs.MaxTake"/>; i.e. absolute maximum page size.
        /// </summary>
        /// <remarks>Defaults to <c>1000</c>.</remarks>
        public long PagingMaxTake => GetCoreExValue<long>(nameof(PagingMaxTake), 1000);

        /// <summary>
        /// Gets the default <see cref="RefData.ReferenceDataOrchestrator"/> <see cref="ICacheEntry.AbsoluteExpirationRelativeToNow"/>.
        /// </summary>
        /// <remarks>Defaults to <c>2</c> hours.</remarks>
        public TimeSpan? RefDataCacheAbsoluteExpirationRelativeToNow => GetCoreExValue($"RefDataCache:{nameof(ICacheEntry.AbsoluteExpirationRelativeToNow)}", TimeSpan.FromHours(2));

        /// <summary>
        /// Gets the default <see cref="RefData.ReferenceDataOrchestrator"/> <see cref="ICacheEntry.SlidingExpiration"/>. 
        /// </summary>
        /// <remarks>Defaults to <c>30</c> minutes.</remarks>
        public TimeSpan? RefDataCacheSlidingExpiration => GetCoreExValue($"RefDataCache:{nameof(ICacheEntry.SlidingExpiration)}", TimeSpan.FromMinutes(30));

        /// <summary>
        /// Indicates whether the validation (<c>CoreEx.Validation</c>) should use JSON names.
        /// </summary>
        /// <remarks>Defaults to <c>true</c>.
        /// <para>The value is cached on first use and is then considered immutable as a change in behaviour may have unintended consequences.</para></remarks>
        public bool ValidationUseJsonNames => _validationUseJsonNames ??= GetCoreExValue(nameof(ValidationUseJsonNames), true);

        /// <summary>
        /// Gets the <see cref="WorkStateOrchestrator"/> <see cref="WorkStateOrchestrator.ExpiryTimeSpan"/>.
        /// </summary>
        /// <remarks>Defaults to <c>1</c> hour.</remarks>
        public TimeSpan WorkerExpiryTimeSpan => GetCoreExValue(nameof(WorkerExpiryTimeSpan), TimeSpan.FromHours(1));

        /// <summary>
        /// Gets the <see cref="Cleaner"/> <see cref="Entities.DateTimeTransform"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="Cleaner.DefaultDateTimeTransform"/>.
        /// <para>The value is cached on first use and is then considered immutable as a change in behaviour may have unintended consequences.</para></remarks>
        public DateTimeTransform DateTimeTransform => _dateTimeTransform ??= GetCoreExValue($"{nameof(Cleaner)}:{nameof(DateTimeTransform)}", Cleaner.DefaultDateTimeTransform);

        /// <summary>
        /// Gets the <see cref="Cleaner"/> <see cref="Entities.StringTransform"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="Cleaner.DefaultStringTransform"/>.
        /// <para>The value is cached on first use and is then considered immutable as a change in behaviour may have unintended consequences.</para></remarks>
        public StringTransform StringTransform => _stringTransform ??= GetCoreExValue($"{nameof(Cleaner)}:{nameof(StringTransform)}", Cleaner.DefaultStringTransform);

        /// <summary>
        /// Gets the <see cref="Cleaner"/> <see cref="Entities.StringCase"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="Cleaner.DefaultStringCase"/>.
        /// <para>The value is cached on first use and is then considered immutable as a change in behaviour may have unintended consequences.</para></remarks>
        public StringCase StringCase => _stringCase ??= GetCoreExValue($"{nameof(Cleaner)}:{nameof(StringCase)}", Cleaner.DefaultStringCase);

        /// <summary>
        /// Gets the <see cref="Cleaner"/> <see cref="Entities.StringTrim"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="Cleaner.DefaultStringTrim"/>.
        /// <para>The value is cached on first use and is then considered immutable as a change in behaviour may have unintended consequences.</para></remarks>
        public StringTrim StringTrim => _stringTrim ??= GetCoreExValue($"{nameof(Cleaner)}:{nameof(StringTrim)}", Cleaner.DefaultStringTrim);
    }
}