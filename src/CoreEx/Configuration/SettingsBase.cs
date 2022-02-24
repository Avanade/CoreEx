﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace CoreEx.Configuration
{
    /// <summary>
    /// Provides the base <see cref="IConfiguration"/>-backed settings; see <see cref="GetValue{T}(string, T)"/> to understand capabilities.
    /// </summary>
    public abstract class SettingsBase
    {
        private readonly IConfiguration _configuration;
        private readonly List<string> _prefixes = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsBase"/> class.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
        /// <param name="prefixes">The key prefixes to use in order of precedence, first through to last. At least one prefix must be specified.</param>
        public SettingsBase(IConfiguration configuration, params string[] prefixes)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            if ((prefixes ?? throw new ArgumentNullException(nameof(prefixes))).Length == 0)
                throw new ArgumentException("At least one prefix must be specified.", nameof(prefixes));

            foreach (var prefix in prefixes)
            {
                if (string.IsNullOrEmpty(prefix))
                    throw new ArgumentException("Prefixes cannot be null or empty.", nameof(prefixes));

                _prefixes.Add(prefix.EndsWith('/') ? prefix : string.Concat(prefix, '/'));
            }
        }

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
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            foreach (var prefix in _prefixes)
            {
                var fullKey = string.Concat(prefix, key);
                if (_configuration.GetSection(fullKey)?.Value != null)
                    return _configuration.GetValue<T>(fullKey);
            }
            
            return _configuration.GetValue(key, defaultValue);
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
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            foreach (var prefix in _prefixes)
            {
                var fullKey = string.Concat(prefix, key);
                if (_configuration.GetSection(fullKey)?.Value != null)
                    return _configuration.GetValue<T>(fullKey);
            }

            if (_configuration.GetSection(key)?.Value == null)
                throw new ArgumentException($"Configuration key '{key}' has not been configured and the value is required.", nameof(key));

            return _configuration.GetValue<T>(key);
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
    }
}