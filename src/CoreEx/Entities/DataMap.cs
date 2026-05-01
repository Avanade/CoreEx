namespace CoreEx.Entities;

/// <summary>
/// Provides a simple extension of <see cref="Dictionary{TKey, TValue}"/> for use as a data map with <see cref="string"/>-based keys and <typeparamref name="TValue"/> values.
/// </summary>
/// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
/// <remarks>Additionally, the <see cref="string"/>-based key when serialized to JSON will <i>not</i> be converted to camelCase as is often the default for JSON serialization. This is to ensure that the key
/// is preserved as-is when serialized and deserialized, which is important for scenarios where the key may be case-sensitive or where the original casing needs to be maintained.
/// <para>See <see cref="JsonDataMapConverterFactory"/>.</para>
/// <para>Note that the <see cref="System.Collections.Specialized.StringDictionary"/> documentation indicates that it should be considered obsolete; hence not used, and <see cref="DataMap{TValue}"/> is provided as a modern alternative.</para></remarks>
public class DataMap<TValue> : Dictionary<string, TValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataMap{TValue}"/> class.
    /// </summary>
    public DataMap() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMap{TValue}"/> class with the specified dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary whose elements are copied to the new <see cref="DataMap{TValue}"/>.</param>
    public DataMap(IDictionary<string, TValue> dictionary) : base(dictionary) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMap{TValue}"/> class that uses the specified string comparer for key comparisons.
    /// </summary>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use for comparing keys.</param>
    public DataMap(IEqualityComparer<string>? comparer) : base(comparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMap{TValue}"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The number of elements that the <see cref="DataMap{TValue}"/> can initially contain.</param>
    public DataMap(int capacity) : base(capacity) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMap{TValue}"/> class with the specified dictionary and string comparer for key comparisons.
    /// </summary>
    /// <param name="dictionary">The dictionary whose elements are copied to the new <see cref="DataMap{TValue}"/>.</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use for comparing keys.</param>
    public DataMap(IDictionary<string, TValue> dictionary, IEqualityComparer<string>? comparer) : base(dictionary, comparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMap{TValue}"/> class with the specified initial capacity and key comparer.
    /// </summary>
    /// <param name="capacity">The number of elements that the <see cref="DataMap{TValue}"/> can initially contain.</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use for comparing keys.</param>
    public DataMap(int capacity, IEqualityComparer<string>? comparer) : base(capacity, comparer) { }
}