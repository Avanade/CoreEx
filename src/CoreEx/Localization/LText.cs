namespace CoreEx.Localization;

/// <summary>
/// Represents a localization agnostic text.
/// </summary>
public readonly struct LText : IEquatable<LText>
{
    /// <summary>
    /// Gets the empty <see cref="LText"/>.
    /// </summary>
    /// <remarks>The <see cref="KeyAndOrText"/> and <see cref="FallbackText"/> are both <see langword="null"/>.</remarks>
    public static readonly LText Empty = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LText"/> with a <paramref name="keyAndOrText"/>.
    /// </summary>
    /// <param name="keyAndOrText">The key and/or text.</param>
    public LText(string? keyAndOrText) => KeyAndOrText = keyAndOrText;

    /// <summary>
    /// Initializes a new instance of the <see cref="LText"/> with a <paramref name="keyAndOrText"/> and optional <paramref name="fallbackText"/>.
    /// </summary>
    /// <param name="keyAndOrText">The key and/or text.</param>
    /// <param name="fallbackText">The fallback text to be used when the <paramref name="keyAndOrText"/> is not found by the <see cref="TextProvider"/>.</param>
    /// <param name="args">The object array that contains zero or more objects to format.</param>
    /// <remarks>Where the <paramref name="fallbackText"/> is explicitly set to <see langword="null"/> this will set <see langword="null"/> as the explicit fallback versus using the <paramref name="keyAndOrText"/> (see <see cref="WasFallBackTextSetToNull"/>).</remarks>
    public LText(string? keyAndOrText, string? fallbackText, params IEnumerable<object?> args)
    {
        KeyAndOrText = keyAndOrText;
        FallbackText = fallbackText;
        if (fallbackText is null)
            WasFallBackTextSetToNull = true;

        Args = [.. args];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LText"/> copying the existing <see cref="LText"/>.
    /// </summary>
    /// <param name="ltext">The existing <see cref="LText"/>.</param>
    /// <param name="args">The object array that contains zero or more objects to format.</param>
    private LText(LText ltext, params IEnumerable<object?>? args)
    {
        KeyAndOrText = ltext.KeyAndOrText;
        FallbackText = ltext.FallbackText;
        WasFallBackTextSetToNull = ltext.WasFallBackTextSetToNull;
        Args = args is null ? null : [ ..args];
    }

    /// <summary>
    /// Gets the key and/or text (where the key is not found, it will used as the text; unless a <see cref="FallbackText"/> is specified.
    /// </summary>
    public string? KeyAndOrText { get; }

    /// <summary>
    /// Gets the optional fallback text to be used when the <see cref="KeyAndOrText"/> is not found; where not specified the <see cref="KeyAndOrText"/> becomes the fallback text.
    /// </summary>
    public string? FallbackText { get; }

    /// <summary>
    /// Indicates whether the <see cref="FallbackText"/> was explicitly set to <see langword="null"/> (i.e. the fallback is not the <see cref="KeyAndOrText"/>).
    /// </summary>
    public bool WasFallBackTextSetToNull { get; } = false;

    /// <summary>
    /// Gets the object array that contains zero or more objects to format.
    /// </summary>
    /// <remarks>Also consider using <see cref="WithArgs(IEnumerable{object?})"/> to extend.</remarks>
    public object?[]? Args { get; }

    /// <summary>
    /// Indicates whether the <see cref="LText"/> has <see cref="Args"/>.
    /// </summary>
    public bool HasArgs => Args is not null && Args.Length > 0;

    /// <summary>
    /// Indicates whether the <see cref="LText"/> is empty; i.e. the <see cref="KeyAndOrText"/> and <see cref="FallbackText"/> are both <see langword="null"/>.
    /// </summary>
    public readonly bool IsEmpty => KeyAndOrText is null && FallbackText is null;

    /// <summary>
    /// Creates a new <see cref="LText"/> extending (adds to) the <see cref="Args"/> with the specified <paramref name="args"/>.
    /// </summary>
    /// <param name="args">The object array that contains zero or more objects to format.</param>
    /// <returns>The new <see cref="LText"/>.</returns>
    public LText WithArgs(params IEnumerable<object?> args)
    {
        if (args is null || !args.Any())
            return this;

        return new LText(this, Args is null ? args : [.. Args, .. args]);
    }

    /// <summary>
    /// Ensures that the <see cref="LText"/> has no <see cref="Args"/>; otherwise, will throw an <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <returns>The <see cref="LText"/> to support fluent-style method-chaining.</returns>
    public readonly LText EnsureNoArgs()
    {
        if (Args is not null && Args.Length > 0)
            throw new InvalidOperationException($"The {nameof(LText)} must have no {nameof(Args)} to be used in this capacity.");

        return this;
    }

    /// <summary>
    /// Ensures that the <see cref="LText"/> has no <see cref="Args"/> <i>when</i> <paramref name="args"/> have been specified; otherwise, will throw an <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <param name="args">The object array that contains zero or more objects to format.</param>
    /// <returns>The <see cref="LText"/> to support fluent-style method-chaining.</returns>
    public readonly LText EnsureNoArgsWhen(params IEnumerable<object?> args)
    {
        if (args is null || !args.Any())
            return this;

        return EnsureNoArgs();
    }

    /// <summary>
    /// Returns the <see cref="LText"/> as a <see cref="string"/> (see <see cref="TextProvider"/> <see cref="TextProvider.Current"/> <see cref="TextProviderBase.GetText(LText)"/>).
    /// </summary>
    /// <returns>The <see cref="LText"/> string value.</returns>
    public override readonly string? ToString() => (string?)this;

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) => obj is LText r && Equals(r);

    /// <inheritdoc/>
    public readonly bool Equals(LText other) => KeyAndOrText == other.KeyAndOrText && FallbackText == other.FallbackText && WasFallBackTextSetToNull == other.WasFallBackTextSetToNull && ArrayEquals(Args, other.Args);

    /// <summary>
    /// Compare the two arrays for equality.
    /// </summary>
    private static bool ArrayEquals(object?[]? a, object?[]? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        if (a.Length != b.Length)
            return false;

        return Enumerable.SequenceEqual(a, b);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(KeyAndOrText);
        hashCode.Add(FallbackText);
        hashCode.Add(WasFallBackTextSetToNull);
        if (Args is not null)
        {
            foreach (var arg in Args)
                hashCode.Add(arg);
        }

        return hashCode.ToHashCode();
    }

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
    public static implicit operator string?(LText text) => TextProvider.Current.GetText(text)!;

    /// <summary>
    /// An implicit cast from an <see cref="LText"/> to a <see cref="string"/> (see <see cref="TextProvider"/> <see cref="TextProvider.Current"/> <see cref="TextProviderBase.GetText(LText)"/>).
    /// </summary>
    /// <param name="text">The <see cref="LText"/>.</param>
    /// <returns>The corresponding text where found; otherwise, the <see cref="FallbackText"/> where specified. Where nothing found or specified then the key itself will be returned.</returns>
    public static implicit operator string?(LText? text) => text is null ? null : TextProvider.Current.GetText(text.Value);

    /// <summary>
    /// An implicit cast from a text <see cref="string"/> to an <see cref="LText"/> value updating the <see cref="KeyAndOrText"/>.
    /// </summary>
    /// <param name="keyAndOrText">The key and/or text.</param>
    public static implicit operator LText(string? keyAndOrText) => keyAndOrText is null ? Empty : new(keyAndOrText);

    /// <summary>
    /// An implicit cast from a text <see cref="string"/> to an <see cref="LText"/> value updating the <see cref="KeyAndOrText"/>.
    /// </summary>
    /// <param name="keyAndOrText">The key and/or text.</param>
    public static implicit operator LText?(string? keyAndOrText) => keyAndOrText is null ? null : new(keyAndOrText);
}