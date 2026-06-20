namespace CoreEx.Entities;

/// <summary>
/// Represents an immutable composite key.
/// </summary>
/// <remarks>May contain zero or more <see cref="Args"/> that represent the composite key.
/// <para><b>NOTE:</b> For performance-critical scenarios with 1-4 arguments, use the generic <see cref="Create{T}(T)"/> overloads to avoid boxing of value types.</para>
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
    private readonly object? _fastPath = null;
    private readonly byte _count = 0;

    /// <summary>
    /// Creates a new <see cref="CompositeKey"/> from the argument values.
    /// </summary>
    /// <param name="args">The argument values for the key.</param>
    /// <returns>The <see cref="CompositeKey"/>.</returns>
    public static CompositeKey Create(params IEnumerable<object?> args) => new(args);

    /// <summary>
    /// Creates a new <see cref="CompositeKey"/> from a single argument value without boxing (performance optimized).
    /// </summary>
    /// <typeparam name="T">The argument <see cref="Type"/>.</typeparam>
    /// <param name="arg">The argument value for the key.</param>
    /// <returns>The <see cref="CompositeKey"/>.</returns>
    public static CompositeKey Create<T>(T arg) => new(ValueTuple.Create(arg), 1);

    /// <summary>
    /// Creates a new <see cref="CompositeKey"/> from two argument values without boxing (performance optimized).
    /// </summary>
    /// <typeparam name="T1">The first argument <see cref="Type"/>.</typeparam>
    /// <typeparam name="T2">The second argument <see cref="Type"/>.</typeparam>
    /// <param name="arg1">The first argument value.</param>
    /// <param name="arg2">The second argument value.</param>
    /// <returns>The <see cref="CompositeKey"/>.</returns>
    public static CompositeKey Create<T1, T2>(T1 arg1, T2 arg2) => new((arg1, arg2), 2);

    /// <summary>
    /// Creates a new <see cref="CompositeKey"/> from three argument values without boxing (performance optimized).
    /// </summary>
    /// <typeparam name="T1">The first argument <see cref="Type"/>.</typeparam>
    /// <typeparam name="T2">The second argument <see cref="Type"/>.</typeparam>
    /// <typeparam name="T3">The third argument <see cref="Type"/>.</typeparam>
    /// <param name="arg1">The first argument value.</param>
    /// <param name="arg2">The second argument value.</param>
    /// <param name="arg3">The third argument value.</param>
    /// <returns>The <see cref="CompositeKey"/>.</returns>
    public static CompositeKey Create<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3) => new((arg1, arg2, arg3), 3);

    /// <summary>
    /// Creates a new <see cref="CompositeKey"/> from four argument values without boxing (performance optimized).
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
    public static CompositeKey Create<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => new((arg1, arg2, arg3, arg4), 4);

    /// <summary>
    /// Initializes a new <see cref="CompositeKey"/> from the argument values.
    /// </summary>
    /// <param name="args">The argument values for the key. Passing an explicit <see langword="null"/> creates a key with one null value; passing no arguments creates an empty key.</param>
    public CompositeKey(params IEnumerable<object?>? args) : this() => _args = args is null ? [null] : [.. args];

    /// <summary>
    /// Initializes a new <see cref="CompositeKey"/> using fast path storage (no boxing).
    /// </summary>
    private CompositeKey(object? fastPath, byte count) : this([])
    {
        _fastPath = fastPath;
        _count = count;
        _args = default;
    }

    /// <summary>
    /// Gets the argument values for the key.
    /// </summary>
    /// <remarks>The <see cref="Args"/> are immutable. When using fast path storage, this property materializes the arguments from the tuple.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ImmutableArray<object?> Args => _count == 0 ? _args : MaterializeArgs();

    /// <summary>
    /// Gets whether this key uses fast path storage (no boxing).
    /// </summary>
    internal bool IsFastPath => _count > 0;

    /// <summary>
    /// Gets the fast path storage object (tuple).
    /// </summary>
    internal object? FastPath => _fastPath;

    /// <summary>
    /// Gets the count of arguments in fast path storage.
    /// </summary>
    internal byte FastPathCount => _count;

    /// <summary>
    /// Materializes the arguments from fast path storage into an immutable array.
    /// </summary>
    private ImmutableArray<object?> MaterializeArgs()
    {
        if (_fastPath is not ITuple tuple)
            return [];

        return _count switch
        {
            1 => [tuple[0]],
            2 => [tuple[0], tuple[1]],
            3 => [tuple[0], tuple[1], tuple[2]],
            4 => [tuple[0], tuple[1], tuple[2], tuple[3]],
            _ => []
        };
    }

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
    /// Gets the string representation of the <see cref="CompositeKey"/>.
    /// </summary>
    /// <returns>The string representation.</returns>
    /// <remarks>Uses the configured <see cref="ToStringFormatter"/>.</remarks>
    public override string? ToString() => ToStringFormatter(this);

    /// <summary>
    /// Gets or sets the <see cref="ToString"/> formatter function.
    /// </summary>
    /// <remarks>The default implementation will format each argument (from the <see cref="Args"/>) to be universal, deterministic, and culture-independent.</remarks>
    public static Func<CompositeKey, string> ToStringFormatter
    {
        get;
        set => field = value.ThrowIfNull();
    } = ck => string.Join(',', ck.Args.Select(ArgumentToString));

    /// <summary>
    /// Converts an argument into a universal, deterministic, and culture-independent <see cref="string"/>.
    /// </summary>
    private static string? ArgumentToString(object? arg)
    {
        if (arg is null)
            return null;

        return arg switch
        {
            string s => s,
            Guid g => g.ToString("D"),
            DateTimeOffset dto => dto.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            float f => f.ToString("R", CultureInfo.InvariantCulture),
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            decimal m => m.ToString("G", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            byte[] bytes => Convert.ToBase64String(bytes),
            TimeSpan ts => ts.ToString("c", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("O", CultureInfo.InvariantCulture),
            char c => c.ToString(),
            _ => Convert.ToString(arg, CultureInfo.InvariantCulture),
        };
    }
}