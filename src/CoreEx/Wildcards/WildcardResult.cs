// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Text.RegularExpressions;

namespace CoreEx.Wildcards
{
    /// <summary>
    /// Represents the <see cref="Wildcard"/> <see cref="Wildcard.Parse"/> result.
    /// </summary>
    public class WildcardResult
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="WildcardResult"/> class.
        /// </summary>
        /// <param name="wildcard">The originating <see cref="Wildcards.Wildcard"/> configuration.</param>
        internal WildcardResult(Wildcard wildcard) => Wildcard = wildcard;

        /// <summary>
        /// Gets the originating <see cref="Wildcards.Wildcard"/> configuration.
        /// </summary>
        internal Wildcard Wildcard { get; private set; }

        /// <summary>
        /// Gets the resulting <see cref="WildcardSelection"/>.
        /// </summary>
        public WildcardSelection Selection { get; internal set; }

        /// <summary>
        /// Gets the updated wildcard text.
        /// </summary>
        public string? Text { get; internal set; }

        /// <summary>
        /// Indicates whether the <see cref="Text"/> contains one or more non-<see cref="Wildcard.Supported"/> errors.
        /// </summary>
        public bool HasError => !Wildcard.Validate(Selection);

        /// <summary>
        /// Gets the <see cref="Text"/> with all the wildcard characters removed.
        /// </summary>
        public string? GetTextWithoutWildcards()
        {
            var s = Text;
            if (Selection.HasFlag(WildcardSelection.MultiWildcard))
                s = s!.Replace(new string(Wildcard.MultiWildcard, 1), string.Empty, StringComparison.InvariantCulture);

            if (Selection.HasFlag(WildcardSelection.SingleWildcard))
                s = s!.Replace(new string(Wildcard.SingleWildcard, 1), string.Empty, StringComparison.InvariantCulture);

            return s;
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> where result <see cref="HasError"/> is <c>true</c>.
        /// </summary>
        /// <returns>The current instance to enable method-chaining.</returns>
        public WildcardResult ThrowOnError() => HasError ? throw new InvalidOperationException("Wildcard selection text is not supported.") : this;

        /// <summary>
        /// Creates the corresponding <see cref="Regex"/> for the wildcard text.
        /// </summary>
        /// <param name="ignoreCase">Indicates whether the regular expression should ignore case (default) or not.</param>
        /// <returns></returns>
        public Regex CreateRegex(bool ignoreCase = true)
        {
            ThrowOnError();

            if (Selection.HasFlag(WildcardSelection.Single))
                return CreateRegex(Selection.HasFlag(WildcardSelection.MultiWildcard) ? "^.*$" : "^.$", ignoreCase);

            var p = Regex.Escape(Text);
            if (Selection.HasFlag(WildcardSelection.MultiWildcard))
                p = p.Replace("\\*", ".*", StringComparison.InvariantCulture);

            if (Selection.HasFlag(WildcardSelection.SingleWildcard))
                p = p.Replace("\\?", ".", StringComparison.InvariantCulture);

            return CreateRegex($"^{p}$", ignoreCase);
        }

        /// <summary>
        /// Create the <see cref="Regex"/>.
        /// </summary>
        private static Regex CreateRegex(string pattern, bool ignoreCase) => ignoreCase ? new Regex(pattern, RegexOptions.IgnoreCase) : new Regex(pattern);

        /// <summary>
        /// Gets the corresponding <b>regular expression</b> pattern for the wildcard text.
        /// </summary>
        /// <returns>The corresponding <see cref="Regex"/> pattern.</returns>
        /// <exception cref="InvalidOperationException">Throws an <see cref="InvalidOperationException"/> where result <see cref="HasError"/> is <c>true</c>.</exception>
        public string GetRegexPattern()
        {
            ThrowOnError();

            if (Selection.HasFlag(WildcardSelection.Single))
                return Selection.HasFlag(WildcardSelection.MultiWildcard) ? "^.*$" : "^.$";

            var p = Regex.Escape(Text);
            if (Selection.HasFlag(WildcardSelection.MultiWildcard))
                p = p.Replace("\\*", ".*", StringComparison.InvariantCulture);

            if (Selection.HasFlag(WildcardSelection.SingleWildcard))
                p = p.Replace("\\?", ".", StringComparison.InvariantCulture);

            return $"^{p}$";
        }
    }
}