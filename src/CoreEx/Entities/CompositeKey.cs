namespace CoreEx.Entities;

/// <summary>
/// Represents an immutable composite key.
/// </summary>
/// <remarks>May contain zero or more <see cref="Args"/> that represent the composite key.
/// <para>The <see cref="CompositeKey"/> is largely intended for .NET code use only, as such there is no specific JSON serialization support enabled by design. The following code snippet demonstrates intended usage.</para>
/// <code>
/// public class SalesOrderItem
/// {
///     [JsonPropertyName("order")]
///     public string? OrderNumber { get; set; }
///     
///     [JsonPropertyName("item")]
///     public int ItemNumber { get; set; }
///     
///     [JsonIgnore()]
///     public CompositeKey SalesOrderItemKey => CompositeKey.Create(OrderNumber, ItemNumber);
/// }
/// </code>
/// </remarks>
public readonly partial struct CompositeKey() : IEquatable<CompositeKey>
{
    private readonly ImmutableArray<object?> _args = [];

    /// <summary>
    /// Creates a new <see cref="CompositeKey"/> from the argument values.
    /// </summary>
    /// <param name="args">The argument values for the key.</param>
    /// <returns>The <see cref="CompositeKey"/>.</returns>
    public static CompositeKey Create(params IEnumerable<object?> args) => new(args);

    /// <summary>
    /// Creates a new <see cref="CompositeKey"/> from a single argument value.
    /// </summary>
    /// <typeparam name="T">The argument <see cref="Type"/>.</typeparam>
    /// <param name="arg">The argument value for the key.</param>
    /// <returns>The <see cref="CompositeKey"/>.</returns>
    /// <remarks>A <see langword="null"/> <paramref name="arg"/> creates a key with one null value.</remarks>
    public static CompositeKey Create<T>(T arg) => new(ImmutableArray.Create<object?>(arg));

    /// <summary>
    /// Creates a new <see cref="CompositeKey"/> from two argument values.
    /// </summary>
    /// <typeparam name="T1">The first argument <see cref="Type"/>.</typeparam>
    /// <typeparam name="T2">The second argument <see cref="Type"/>.</typeparam>
    /// <param name="arg1">The first argument value.</param>
    /// <param name="arg2">The second argument value.</param>
    /// <returns>The <see cref="CompositeKey"/>.</returns>
    public static CompositeKey Create<T1, T2>(T1 arg1, T2 arg2) => new(ImmutableArray.Create<object?>(arg1, arg2));

    /// <summary>
    /// Creates a new <see cref="CompositeKey"/> from three argument values.
    /// </summary>
    /// <typeparam name="T1">The first argument <see cref="Type"/>.</typeparam>
    /// <typeparam name="T2">The second argument <see cref="Type"/>.</typeparam>
    /// <typeparam name="T3">The third argument <see cref="Type"/>.</typeparam>
    /// <param name="arg1">The first argument value.</param>
    /// <param name="arg2">The second argument value.</param>
    /// <param name="arg3">The third argument value.</param>
    /// <returns>The <see cref="CompositeKey"/>.</returns>
    public static CompositeKey Create<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3) => new(ImmutableArray.Create<object?>(arg1, arg2, arg3));

    /// <summary>
    /// Creates a new <see cref="CompositeKey"/> from four argument values.
    /// </summary>
    /// <typeparam name="T1">The first argument <see cref="Type"/>.</typeparam>
    /// <typeparam name="T2">The second argument <see cref="Type"/>.</typeparam>
    /// <typeparam name="T3">The third argument <see cref="Type"/>.</typeparam>
    /// <typeparam name="T4">The fourth argument <see cref="Type"/>.</typeparam>
    /// <param name="arg1">The first argument value.</param>
    /// <param name="arg2">The second argument value.</param>
    /// <param name="arg3">The third argument value.</param>
    /// <param name="arg4">The fourth argument value.</param>
    /// <returns>The <see cref="CompositeKey"/>.</returns>
    public static CompositeKey Create<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => new(ImmutableArray.Create<object?>(arg1, arg2, arg3, arg4));

    /// <summary>
    /// Initializes a new <see cref="CompositeKey"/> from the argument values.
    /// </summary>
    /// <param name="args">The argument values for the key. Passing an explicit <see langword="null"/> creates a key with one null value; passing no arguments creates an empty key.</param>
    public CompositeKey(params IEnumerable<object?>? args) : this() => _args = args is null ? [null] : [.. args];

    /// <summary>
    /// Initializes a new <see cref="CompositeKey"/> from an already immutable argument array.
    /// </summary>
    private CompositeKey(ImmutableArray<object?> args) : this() => _args = args;

    /// <summary>
    /// Gets the argument values for the key.
    /// </summary>
    /// <remarks>The <see cref="Args"/> are immutable; a <see langword="default"/> key has no arguments.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ImmutableArray<object?> Args => _args.IsDefault ? [] : _args;
    /// <summary>
    /// Determines whether the current <see cref="CompositeKey"/> is equal to another <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="other">The other <see cref="CompositeKey"/>.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Uses the <see cref="CompositeKeyComparer.Equals(CompositeKey, CompositeKey)"/>.</remarks>
    public bool Equals(CompositeKey other) => CompositeKeyComparer.Default.Equals(this, other);

    /// <summary>
    /// Determines whether the current <see cref="CompositeKey"/> is equal to another <see cref="Object"/>.
    /// </summary>
    /// <param name="obj">The other <see cref="object"/>.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is CompositeKey key && Equals(key);

    /// <summary>
    /// Returns a hash code for the <see cref="CompositeKey"/>.
    /// </summary>
    /// <returns>A hash code for the <see cref="CompositeKey"/>.</returns>
    /// <remarks>Uses the <see cref="CompositeKeyComparer.GetHashCode(CompositeKey)"/>.</remarks>
    public override int GetHashCode() => CompositeKeyComparer.Default.GetHashCode(this);

    /// <summary>
    /// Compares two <see cref="CompositeKey"/> types for equality.
    /// </summary>
    /// <param name="left">The left <see cref="CompositeKey"/>.</param>
    /// <param name="right">The right <see cref="CompositeKey"/>.</param>
    /// <returns><see langword="true"/> indicates equal; otherwise, <see langword="false"/> for not equal.</returns>
    public static bool operator ==(CompositeKey left, CompositeKey right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="CompositeKey"/> types for non-equality.
    /// </summary>
    /// <param name="left">The left <see cref="CompositeKey"/>.</param>
    /// <param name="right">The right <see cref="CompositeKey"/>.</param>
    /// <returns><see langword="true"/> indicates not equal; otherwise, <see langword="false"/> for equal.</returns>
    public static bool operator !=(CompositeKey left, CompositeKey right) => !(left == right);

    /// <summary>
    /// Gets the fixed delimiter between components. The tilde is URI-unreserved, so canonical keys can be used in URLs without percent-encoding the separator; it is also absent from the invariant formats used by the built-in scalar types.
    /// </summary>
    public const char Delimiter = '~';

    /// <summary>
    /// Gets the readable, value-based string representation of the <see cref="CompositeKey"/>.
    /// </summary>
    /// <returns>The string representation.</returns>
    /// <remarks>Produces the same text as <see cref="ToCanonicalString"/> but never throws: an empty key is an empty string, a <see langword="null"/> component is empty, and an unsupported type uses its invariant <see cref="Convert.ToString(object?, IFormatProvider?)"/> text. Use <see cref="ToCanonicalString"/> wherever the result is used as an identity.</remarks>
    public override string ToString() => Format(strict: false);

    /// <summary>
    /// Gets the readable, value-based string representation validated for use as an identity.
    /// </summary>
    /// <returns>The canonical representation.</returns>
    /// <remarks>Values are invariant-formatted without escaping and joined with <see cref="Delimiter"/>; a single-component key is its value text. The result is one-way (it is never parsed) and replay-stable: a canonical string used as a single-component key produces the same canonical string. Type is not part of identity: values with the same invariant text are equivalent. It does not change <see cref="Equals(CompositeKey)"/>. This is not a serialization or a secret. See the CoreEx.Entities README for supported types and normalization.</remarks>
    /// <exception cref="ArgumentException">The key has no components; has a null component; has a component containing a NUL character or an unpaired surrogate; or has multiple components and one contains <see cref="Delimiter"/>.</exception>
    /// <exception cref="NotSupportedException">An argument has an unsupported type.</exception>
    public string ToCanonicalString() => Format(strict: true);

    /// <summary>
    /// Formats the arguments, optionally validating them for use as an identity.
    /// </summary>
    private string Format(bool strict)
    {
        var args = Args;
        if (args.IsDefaultOrEmpty)
            return strict ? throw new ArgumentException("A canonical composite key requires at least one component.", "key") : string.Empty;

        if (!strict && args.Length == 1)
            return ArgumentToString(args[0], strict);

        var builder = new StringBuilder();
        for (var i = 0; i < args.Length; i++)
        {
            if (i > 0)
                builder.Append(Delimiter);

            var arg = args[i];
            if (strict && arg is null)
                throw new ArgumentException($"A canonical composite key component must not be null (index {i}).", "key");

            var value = ArgumentToString(arg, strict);
            if (strict)
                ValidateCanonicalComponent(value, i, args.Length > 1);

            builder.Append(value);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Rejects component text that cannot be reliably persisted or that would make a multi-component key ambiguous.
    /// </summary>
    private static void ValidateCanonicalComponent(string value, int index, bool isMultiComponent)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '\0')
                throw new ArgumentException($"A canonical composite key component must not contain a NUL character (index {index}).", "key");

            if (c == Delimiter && isMultiComponent)
                throw new ArgumentException($"A component of a multi-component canonical composite key must not contain '{Delimiter}' (index {index}).", "key");

            if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                i++;
            else if (char.IsSurrogate(c))
                throw new ArgumentException($"A canonical composite key component must not contain an unpaired surrogate (index {index}).", "key");
        }
    }

    /// <summary>
    /// Converts an argument to invariant value text; unsupported types throw when <paramref name="strict"/>.
    /// </summary>
    private static string ArgumentToString(object? arg, bool strict)
    {
        if (arg is null)
            return string.Empty;

        if (arg is Enum enumValue)
        {
            var underlying = Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumValue.GetType()), CultureInfo.InvariantCulture);
            return Convert.ToString(underlying, CultureInfo.InvariantCulture) ?? throw new InvalidOperationException("The enum value could not be formatted.");
        }

        return arg switch
        {
            string value => value,
            char value => value.ToString(),
            bool value => value ? "true" : "false",
            sbyte value => value.ToString(CultureInfo.InvariantCulture),
            byte value => value.ToString(CultureInfo.InvariantCulture),
            short value => value.ToString(CultureInfo.InvariantCulture),
            ushort value => value.ToString(CultureInfo.InvariantCulture),
            int value => value.ToString(CultureInfo.InvariantCulture),
            uint value => value.ToString(CultureInfo.InvariantCulture),
            long value => value.ToString(CultureInfo.InvariantCulture),
            ulong value => value.ToString(CultureInfo.InvariantCulture),
            nint value => value.ToString(CultureInfo.InvariantCulture),
            nuint value => value.ToString(CultureInfo.InvariantCulture),
            Int128 value => value.ToString(CultureInfo.InvariantCulture),
            UInt128 value => value.ToString(CultureInfo.InvariantCulture),
            System.Numerics.BigInteger value => value.ToString(CultureInfo.InvariantCulture),
            Half value => value == (Half)0 ? "0" : value.ToString("R", CultureInfo.InvariantCulture),
            float value => value == 0 ? "0" : value.ToString("R", CultureInfo.InvariantCulture),
            double value => value == 0 ? "0" : value.ToString("R", CultureInfo.InvariantCulture),
            decimal value => value.ToString("G29", CultureInfo.InvariantCulture),
            Guid value => value.ToString("D"),
            DateTime value => value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
            DateTimeOffset value => value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            TimeSpan value => value.ToString("c", CultureInfo.InvariantCulture),
            DateOnly value => value.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly value => value.ToString("O", CultureInfo.InvariantCulture),
            byte[] value => Convert.ToBase64String(value),
            _ when strict => throw new NotSupportedException($"The type '{arg.GetType()}' is not supported by the canonical composite-key formatter."),
            _ => Convert.ToString(arg, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }
}
