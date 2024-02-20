// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace CoreEx.Text
{
    /// <summary>
    /// Provides common sentence case capabilities.
    /// </summary>
    public static partial class SentenceCase
    {
        /// <summary>
        /// The <see cref="Regex"/> pattern for splitting strings into a sentence of words.
        /// </summary>
        public const string WordSplitPattern = "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))";

        /// <summary>
        /// Gets the compiled <see cref="Regex"/> for splitting strings into a sentence of words (see <see cref="WordSplitPattern"/>).
        /// </summary>
#if NET7_0_OR_GREATER
        public static Regex WordSplitRegex { get; } = _wordSplitRegex();

        /// <summary>
        /// Provides the generated <see cref="Regex"/> for splitting strings into a sentence of words.
        /// </summary>
        [GeneratedRegex(WordSplitPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant)]
        private static partial Regex _wordSplitRegex();
#else
        public static Regex WordSplitRegex { get; } = new Regex(WordSplitPattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);
#endif

        /// <summary>
        /// Performs a sentence case word split on the specified <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The text to sentence case word split.</param>
        /// <returns></returns>
        public static string[] WordSplit(string text) => string.IsNullOrEmpty(text) ? [] : WordSplitRegex.Split(text);

        /// <summary>
        /// Converts a <see cref="string"/> into sentence case.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>The <see cref="string"/> as sentence case.</returns>
        /// <remarks>For example a value of '<c>VarNameDB</c>' would return '<c>Var Name DB</c>'.
        /// <para>Uses the <see cref="SentenceCaseConverter"/> function to perform the conversion.</para></remarks>
        [return: NotNullIfNotNull(nameof(text))]
        public static string? ToSentenceCase(string? text) => SentenceCaseConverter == null ? text : SentenceCaseConverter(text);

        /// <summary>
        /// Gets or sets the underlying logic to perform the sentence case conversion.
        /// </summary>
        /// <remarks>Defaults to the internal <see cref="SentenceCaseConversion(string?)"/> logic.</remarks>
        public static Func<string?, string?>? SentenceCaseConverter { get; set; } = SentenceCaseConversion;

        /// <summary>
        /// Performs the out-of-the-box sentence case conversion.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <remarks>Defaults to the following: Initial word splitting is performed using the <see cref="WordSplitPattern"/> <see cref="Regex"/>. First letter is always capitalized, initial full text is tested (and replaced where matched) 
        /// against <see cref="Substitutions"/>, then each word is tested (and replaced where matched) against <see cref="Substitutions"/>. Finally, the last word in the initial text is tested against
        /// <see cref="LastWordRemovals"/> and where matched the final word will be removed.</remarks>
        private static string? SentenceCaseConversion(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Make sure the first character is always upper case.
            if (char.IsLower(text[0]))
                text = char.ToUpper(text[0], CultureInfo.InvariantCulture) + text[1..];

            // Check if there is a one-to-one substitution.
            if (Substitutions.TryGetValue(text, out var scs))
                return scs;

            // Determine whether last word should be removed, then go through each word and substitute.
            var s = WordSplitRegex.Replace(text, "$1 "); // Split the string into words.
            var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var removeLastWord = LastWordRemovals.Contains(parts.Last());

            for (int i = 0; i < parts.Length; i++)
            {
                if (Substitutions.TryGetValue(text, out var iscs))
                    parts[i] = iscs;
            }

            // Rejoin the words back into the final sentence.
            return string.Join(" ", parts, 0, parts.Length - (removeLastWord ? 1 : 0));
        }

        /// <summary>
        /// Gets or sets the sentence case substitutions <see cref="Dictionary{TKey, TValue}"/> where the key is the originating (input) text and the value the corresponding substitution sentence case text.
        /// </summary>
        /// <remarks>Defaults with the following entry: key '<c>Id</c>' and value '<c>Identifier</c>'.
        /// <para>This subtitution applies to all words in the text with the exception of the last where it matches the <see cref="LastWordRemovals"/>.</para></remarks>
        public static Dictionary<string, string> Substitutions { get; set; } = new() { { "Id", "Identifier" } };

        /// <summary>
        /// Gets or sets the sentence case last word removal list; i.e. where there is more than one word, and there is a match, the word will be removed.
        /// </summary>
        /// <remarks>Defaults with the following entry: '<c>Id</c>'.
        /// <para>For example a value of '<c>EmployeeId</c>' would return just '<c>Employee</c>'.</para></remarks>
        public static List<string> LastWordRemovals { get; set; } = ["Id"];
    }
}