namespace CoreEx.Entities;

/// <summary>
/// Represents an optionally specified value, including a specified <see langword="null"/> value.
/// </summary>
/// <typeparam name="T">The wrapped value type.</typeparam>
[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct Optional<T> : IOptionalValue<T>, IEquatable<Optional<T>>, IWrappedValueCleaner<Optional<T>>
{
    private readonly T? _value;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    private Optional(T? value)
    {
        _value = value;
        IsSpecified = true;
    }

    /// <summary>
    /// Gets whether a value was specified.
    /// </summary>
    public bool IsSpecified { get; }

    /// <summary>
    /// Gets the specified value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is unspecified.</exception>
    public T Value => IsSpecified ? _value! : throw new InvalidOperationException("The Optional value is unspecified.");

    /// <summary>
    /// Creates an <see cref="Optional{T}"/> containing the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value, which may be <see langword="null"/>.</param>
    /// <returns>The specified optional value.</returns>
    public static Optional<T> Specified(T? value) => new(value);

    /// <summary>
    /// Implicitly converts a value to a specified <see cref="Optional{T}"/>.
    /// </summary>
    /// <param name="value">The value, which may be <see langword="null"/>.</param>
    public static implicit operator Optional<T>(T? value) => Specified(value);

    /// <inheritdoc/>
    public bool Equals(Optional<T> other) => IsSpecified == other.IsSpecified && (!IsSpecified || EqualityComparer<T?>.Default.Equals(_value, other._value));

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Optional<T> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => IsSpecified ? HashCode.Combine(true, _value) : 0;

    /// <summary>
    /// Determines whether two optional values are equal.
    /// </summary>
    public static bool operator ==(Optional<T> left, Optional<T> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two optional values are not equal.
    /// </summary>
    public static bool operator !=(Optional<T> left, Optional<T> right) => !left.Equals(right);

    /// <inheritdoc/>
    Optional<T> IWrappedValueCleaner<Optional<T>>.Clean(CleanArgs args) => IsSpecified ? Specified(RuntimeMetadata.Clean(_value, args)) : this;
}
