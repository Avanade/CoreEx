namespace CoreEx.Entities;

public partial struct CompositeKey
{
    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(string? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts an <see cref="short"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(short identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Nullable{T}"/> <see cref="short"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(short? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts an <see cref="int"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(int identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Nullable{T}"/> <see cref="int"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(int? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="long"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(long identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Nullable{T}"/> <see cref="long"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(long? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(Guid identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Nullable{T}"/> <see cref="Guid"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(Guid? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="char"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(char identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Nullable{T}"/> <see cref="char"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(char? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="bool"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(bool identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Nullable{T}"/> <see cref="bool"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(bool? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="DateTime"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(DateTime identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Nullable{T}"/> <see cref="DateTime"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(DateTime? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="DateTimeOffset"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(DateTimeOffset identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Nullable{T}"/> <see cref="DateTimeOffset"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(DateTimeOffset? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts an <see cref="ushort"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(ushort identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Nullable{T}"/>  <see cref="ushort"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(ushort? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts an <see cref="uint"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(uint identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Nullable{T}"/>  <see cref="uint"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(uint? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="ulong"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(ulong identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Nullable{T}"/> <see cref="ulong"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(ulong? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="DateOnly"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(DateOnly identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Nullable{T}"/> <see cref="DateOnly"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(DateOnly? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="TimeOnly"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(TimeOnly identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="Nullable{T}"/> <see cref="TimeOnly"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(TimeOnly? identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts a <see cref="byte"/> <see cref="Array"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    public static implicit operator CompositeKey(byte[] identifier) => Create(identifier);

    /// <summary>
    /// Implicitly converts an <see cref="object"/> <see cref="Array"/> to a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="key">The key.</param>
    public static implicit operator CompositeKey(object?[]? key) => key is null ? new() : new(key);
}