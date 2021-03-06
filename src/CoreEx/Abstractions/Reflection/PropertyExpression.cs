// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Provides access to the common property expression capabilities.
    /// </summary>
    public static class PropertyExpression
    {
        private static IMemoryCache? _fallbackCache;

        /// <summary>
        /// The <see cref="Regex"/> pattern for splitting strings into words.
        /// </summary>
        public const string WordSplitPattern = "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))";

        private readonly static Lazy<Regex> _regex = new(() => new Regex(WordSplitPattern, RegexOptions.CultureInvariant | RegexOptions.Compiled));

        /// <summary>
        /// Gets the <see cref="IMemoryCache"/>.
        /// </summary>
        internal static IMemoryCache Cache => ExecutionContext.GetService<IMemoryCache>() ?? (_fallbackCache ??= new MemoryCache(new MemoryCacheOptions()));

        /// <summary>
        /// Validates, creates and compiles the property expression; whilst also determinig the property friendly <see cref="PropertyExpression{TEntity, TProperty}.Text"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the entity property.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="JsonSerializer.Default"/> where not specified.</param>
        /// <returns>A <see cref="PropertyExpression{TEntity, TProperty}"/> which contains (in order) the compiled <see cref="System.Func{TEntity, TProperty}"/>, member name and resulting property text.</returns>
        /// <remarks>Caching is used to improve performance; subsequent calls will return the corresponding cached value.</remarks>
        public static PropertyExpression<TEntity, TProperty> Create<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, IJsonSerializer? jsonSerializer = null)
            => PropertyExpression<TEntity, TProperty>.CreateInternal(propertyExpression ?? throw new ArgumentNullException(nameof(propertyExpression)), DetermineJsonSerializer(jsonSerializer));

        /// <summary>
        /// Gets the <see cref="PropertyExpression{TEntity, TProperty}"/> from the cache.
        /// </summary>
        /// <param name="entityType">The entity <see cref="Type"/>.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="JsonSerializer.Default"/> where not specified.</param>
        /// <returns>The <see cref="IPropertyExpression"/> where found; otherwise, <c>null</c>.</returns>
        public static IPropertyExpression? Get(Type entityType, string propertyName, IJsonSerializer? jsonSerializer = null)
            => (IPropertyExpression)Cache.Get((entityType, propertyName, DetermineJsonSerializer(jsonSerializer).GetType())) ?? null;

        /// <summary>
        /// Determine the <see cref="IJsonSerializer"/> by firstly using the <see cref="ExecutionContext.ServiceProvider"/> to find, then falling back to the <see cref="JsonSerializer.Default"/>.
        /// </summary>
        /// <returns>The <see cref="IJsonSerializer"/>.</returns>
        /// <remarks>This does scream <i>Service Locator</i>, which is considered an anti-pattern by some, but this avoids the added complexity of passing the <see cref="IJsonSerializer"/> where most implementations will default to the
        /// <see cref="CoreEx.Json.JsonSerializer"/> implementation - this just avoids unnecessary awkwardness for sake of purity. Finally, this class is intended for largely internal use only.</remarks>
        private static IJsonSerializer DetermineJsonSerializer(IJsonSerializer? jsonSerializer) => jsonSerializer ?? ExecutionContext.GetService<IJsonSerializer>() ?? JsonSerializer.Default;

        /// <summary>
        /// Converts a <see cref="string"/> into sentence case.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>The <see cref="string"/> as sentence case.</returns>
        /// <remarks>For example a value of 'VarNameDB' would return 'Var Name DB'.</remarks>
        public static string ToSentenceCase(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var s = _regex.Value.Replace(text, "$1 "); // Split the string into words.
            var sc = char.ToUpper(s[0], CultureInfo.InvariantCulture) + s[1..]; // Make sure the first character is always upper case.
            return SentenceCaseSubstitutions.TryGetValue(sc, out var sub) ? sub : sc;
        }

        /// <summary>
        /// Converts a <see cref="string"/> into sentence case.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>The <see cref="string"/> as sentence case.</returns>
        /// <remarks>For example a value of 'VarNameDB' would return 'Var Name DB'.</remarks>
        public static string? ConvertToSentenceCase(string? text) => string.IsNullOrEmpty(text) ? text : text.ToSentenceCase();

        /// <summary>
        /// Gets the sentence case substitutions <see cref="Dictionary{TKey, TValue}"/> where the key is the converted sentence case and the value the corresponding substitution text.
        /// </summary>
        /// <remarks>Defaults with the following entry: key '<c>Id</c>', value '<c>Identifier</c>'.</remarks>
        public static Dictionary<string, string> SentenceCaseSubstitutions { get; } = new() { { "Id", "Identifier" } };
    }
}