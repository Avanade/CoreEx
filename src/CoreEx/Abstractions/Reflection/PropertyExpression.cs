// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Provides access to the common property expression capabilities.
    /// </summary>
    public static partial class PropertyExpression
    {
        private static IMemoryCache? _fallbackCache;

        /// <summary>
        /// The <see cref="Regex"/> pattern for splitting strings into a sentence of words.
        /// </summary>
        public const string SentenceCaseWordSplitPattern = "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))";

#if NET7_0_OR_GREATER
        private readonly static Lazy<Regex> _regex = new(SentenceRegex);
#else
        private readonly static Lazy<Regex> _regex = new(() => new Regex(SentenceCaseWordSplitPattern, RegexOptions.CultureInvariant | RegexOptions.Compiled));
#endif

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
            => PropertyExpression<TEntity, TProperty>.CreateInternal(propertyExpression.ThrowIfNull(nameof(propertyExpression)), DetermineJsonSerializer(jsonSerializer));

        /// <summary>
        /// Gets the <see cref="PropertyExpression{TEntity, TProperty}"/> from the cache.
        /// </summary>
        /// <param name="entityType">The entity <see cref="Type"/>.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="JsonSerializer.Default"/> where not specified.</param>
        /// <returns>The <see cref="IPropertyExpression"/> where found; otherwise, <c>null</c>.</returns>
        public static IPropertyExpression? Get(Type entityType, string propertyName, IJsonSerializer? jsonSerializer = null)
            => (IPropertyExpression?)Cache.Get((entityType, propertyName, DetermineJsonSerializer(jsonSerializer).GetType())) ?? null;

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
        /// <remarks>For example a value of '<c>VarNameDB</c>' would return '<c>Var Name DB</c>'.
        /// <para>Uses the <see cref="SentenceCaseConverter"/> function to perform the conversion.</para></remarks>
        public static string? ToSentenceCase(this string? text) => SentenceCaseConverter == null ? text : SentenceCaseConverter(text);

        /// <summary>
        /// Converts a <see cref="string"/> into sentence case.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>The <see cref="string"/> as sentence case.</returns>
        /// <remarks>For example a value of '<c>VarNameDB</c>' would return '<c>Var Name DB</c>'.
        /// <para>Uses the <see cref="SentenceCaseConverter"/> function to perform the conversion.</para></remarks>
        public static string? ConvertToSentenceCase(string? text) => string.IsNullOrEmpty(text) ? text : text.ToSentenceCase();

        /// <summary>
        /// Gets or sets the underlying logic to perform the sentence case conversion.
        /// </summary>
        /// <remarks>Defaults to <see cref="SentenceCaseConversion(string?)"/>.</remarks>
        public static Func<string?, string?>? SentenceCaseConverter { get; set; } = SentenceCaseConversion;

        /// <summary>
        /// Performs the out-of-the-box sentence case conversion.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <remarks>Defaults to the following: Initial word splitting is performed using the <see cref="SentenceCaseWordSplitPattern"/> <see cref="Regex"/>. First letter is always capitalized, initial full text is tested (and replaced where matched) 
        /// against <see cref="SentenceCaseSubstitutions"/>, then each word is tested (and replaced where matched) against <see cref="SentenceCaseSubstitutions"/>. Finally, the last word in the initial text is tested against
        /// <see cref="SentenceCaseLastWordRemovals"/> and where matched the final word will be removed.</remarks>
        public static string? SentenceCaseConversion(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Make sure the first character is always upper case.
            if (char.IsLower(text[0]))
                text = char.ToUpper(text[0], CultureInfo.InvariantCulture) + text[1..];

            // Check if there is a one-to-one substitution.
            if (SentenceCaseSubstitutions.TryGetValue(text, out var scs))
                return scs;

            // Determine whether last word should be removed, then go through each word and substitute.
            var s = _regex.Value.Replace(text, "$1 "); // Split the string into words.
            var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var removeLastWord = SentenceCaseLastWordRemovals.Contains(parts.Last());

            for (int i = 0; i < parts.Length; i++)
            {
                if (SentenceCaseSubstitutions.TryGetValue(text, out var iscs))
                    parts[i] = iscs;
            }

            // Rejoin the words back into the final sentence.
            return string.Join(" ", parts, 0, parts.Length - (removeLastWord ? 1 : 0));
        }

        /// <summary>
        /// Gets or sets the sentence case substitutions <see cref="Dictionary{TKey, TValue}"/> where the key is the originating (input) text and the value the corresponding substitution sentence case text.
        /// </summary>
        /// <remarks>Defaults with the following entry: key '<c>Id</c>' and value '<c>Identifier</c>'.
        /// <para>This subtitution applies to all words in the text with the exception of the last where it matches the <see cref="SentenceCaseLastWordRemovals"/>.</para></remarks>
        public static Dictionary<string, string> SentenceCaseSubstitutions { get; set; } = new() { { "Id", "Identifier" } };

        /// <summary>
        /// Gets or sets the sentence case last word removal list; i.e. where there is more than one word, and there is a match, the word will be removed.
        /// </summary>
        /// <remarks>Defaults with the following entry: '<c>Id</c>'.
        /// <para>For example a value of '<c>EmployeeId</c>' would return just '<c>Employee</c>'.</para></remarks>
        public static List<string> SentenceCaseLastWordRemovals { get; set; } = ["Id"];

#if NET7_0_OR_GREATER
        /// <summary>
        /// Provides the compiled <see cref="Regex"/> for splitting strings into a sentence of words.
        /// </summary>
        [GeneratedRegex(SentenceCaseWordSplitPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant)]
        private static partial Regex SentenceRegex();
#endif
    }
}