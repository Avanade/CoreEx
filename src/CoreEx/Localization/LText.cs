// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Localization
{
    /// <summary>
    /// Represents the <b>localization text</b> key/identifier to be used by the <see cref="TextProvider"/>.
    /// </summary>
    public struct LText : IEquatable<LText>
    {
        /// <summary>
        /// Gets the empty <see cref="LText"/>.
        /// </summary>
        /// <remarks>The <see cref="KeyAndOrText"/> and <see cref="FallbackText"/> are both <c>null</c>.</remarks>
        public static readonly LText Empty = new();

        /// <summary>
        /// Gets or sets the numeric (<see cref="long"/>) key/identifier format to convert to a standardized <see cref="string"/>.
        /// </summary>
        public static string NumericKeyFormat { get; set; } = "000000";

        /// <summary>
        /// Initializes a new instance of the <see cref="LText"/> with <c>null</c> <see cref="KeyAndOrText"/> and <see cref="FallbackText"/>.
        /// </summary>
        public LText()
        {
            KeyAndOrText = null;
            FallbackText = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LText"/> with a <paramref name="keyAndOrText"/> and optional <paramref name="fallbackText"/>.
        /// </summary>
        /// <param name="keyAndOrText">The key and/or text.</param>
        /// <param name="fallbackText">The fallback text to be used when the <paramref name="keyAndOrText"/> is not found by the <see cref="TextProvider"/>.</param>
        /// <remarks>At least one of the arguments must be specified.</remarks>
        public LText(string? keyAndOrText, string? fallbackText = null)
        {
            KeyAndOrText = keyAndOrText;
            FallbackText = fallbackText;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LText"/> with an <see cref="long"/> key and optional <paramref name="fallbackText"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="fallbackText">The fallback text to be used when not found by the <see cref="TextProvider"/>.</param>
        public LText(long key, string? fallbackText = null)
        {
            KeyAndOrText = key <= 0 ? throw new ArgumentException("Key must be a positive integer.", nameof(key)) : key.ToString(NumericKeyFormat, System.Globalization.CultureInfo.InvariantCulture);
            FallbackText = fallbackText;
        }

        /// <summary>
        /// Gets or sets the key and/or text (where the key is not found, it will used as the text; unless a <see cref="FallbackText"/> is specified.
        /// </summary>
        public string? KeyAndOrText { get; }

        /// <summary>
        /// Gets or sets the optional fallback text to be used when the <see cref="KeyAndOrText"/> is not found by the <see cref="TextProvider"/> (where not specified the <see cref="KeyAndOrText"/> becomes the fallback text).
        /// </summary>
        public string? FallbackText { get; set; }

        /// <summary>
        /// Indicates whether the <see cref="LText"/> is empty; i.e. the <see cref="KeyAndOrText"/> and <see cref="FallbackText"/> are both <c>null</c>.
        /// </summary>
        public readonly bool IsEmpty => KeyAndOrText is null && FallbackText is null;

        /// <summary>
        /// Returns the <see cref="LText"/> as a <see cref="string"/> (see <see cref="TextProvider"/> <see cref="TextProvider.Current"/> <see cref="TextProviderBase.GetText(LText)"/>).
        /// </summary>
        /// <returns>The <see cref="LText"/> string value.</returns>
        public override readonly string ToString() => this!;

        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj is LText r && Equals(r);

        /// <inheritdoc/>
        public readonly bool Equals(LText other) => KeyAndOrText == other.KeyAndOrText && FallbackText == other.FallbackText;

        /// <inheritdoc/>
        public override readonly int GetHashCode() => HashCode.Combine(KeyAndOrText, FallbackText);

        /// <summary>
        /// Indicates whether the current <see cref="LText"/> is equal to another <see cref="LText"/>.
        /// </summary>
        public static bool operator ==(LText left, LText right) => left.Equals(right);

        /// <summary>
        /// Indicates whether the current <see cref="LText"/> is not equal to another <see cref="LText"/>.
        /// </summary>
        public static bool operator !=(LText left, LText right) => !(left == right);

        /// <summary>
        /// An implicit cast from an <see cref="LText"/> to a <see cref="string"/> (see <see cref="TextProvider"/> <see cref="TextProvider.Current"/> <see cref="TextProviderBase.GetText(LText)"/>).
        /// </summary>
        /// <param name="text">The <see cref="LText"/>.</param>
        /// <returns>The corresponding text where found; otherwise, the <see cref="FallbackText"/> where specified. Where nothing found or specified then the key itself will be returned.</returns>
        public static implicit operator string(LText text) => TextProvider.Current.GetText(text)!;

        /// <summary>
        /// An implicit cast from a text <see cref="string"/> to an <see cref="LText"/> value updating the <see cref="KeyAndOrText"/>.
        /// </summary>
        /// <param name="keyAndOrText">The key and/or text.</param>
        public static implicit operator LText(string keyAndOrText) => keyAndOrText is null ? Empty : new(keyAndOrText);
    }
}