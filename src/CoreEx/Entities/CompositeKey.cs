// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents an immutable composite key.
    /// </summary>
    /// <remarks>May contain zero or more <see cref="Args"/> that represent the composite key. A subset of the the .NET <see href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types">built-in types</see>
    /// are supported: <see cref="string"/>, <see cref="char"/>, <see cref="short"/>, <see cref="int"/>, <see cref="long"/>, <see cref="ushort"/>, <see cref="uint"/>, <see cref="ulong"/>, <see cref="Guid"/>, <see cref="DateTimeOffset"/> (converted to a <see cref="DateTime"/>) and <see cref="DateTime"/>.
    /// Extended support is enabled for <see cref="IReferenceData"/> types such that the <see cref="IReferenceData.Code"/> is used.
    /// <para>A <see cref="CompositeKey"/> is not generally intended to be a first-class JSON-serialized property type, although is supported (see <see cref="CoreEx.Text.Json.CompositeKeyConverterFactory"/>); but, to be used in a read-only non-serialized manner to group (encapsulate) other properties
    /// into a single value. The <see cref="CompositeKey"/> is also used within the <see cref="IEntityKey"/>, <see cref="IIdentifier"/> and <see cref="IPrimaryKey"/>.</para><para>Example as follows:
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
    /// </code></para></remarks>
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.DebuggerDisplay("Args = {ToString()}")]
    public readonly struct CompositeKey : IEquatable<CompositeKey>
    {
        private static readonly string[] _singleEmptyArray = [string.Empty];
        private readonly ImmutableArray<object?> _args;

        /// <summary>
        /// Represents an empty <see cref="CompositeKey"/>.
        /// </summary>
        public static readonly CompositeKey Empty = new();

        /// <summary>
        /// Creates a new <see cref="CompositeKey"/> from the argument values,
        /// </summary>
        /// <param name="args">The argument values for the key.</param>
        /// <returns>The <see cref="CompositeKey"/>.</returns>
        public static CompositeKey Create(params object?[] args) => new(args);

        /// <summary>
        /// Initializes a new <see cref="CompositeKey"/> structure.
        /// </summary>
#if NET8_0_OR_GREATER
        public CompositeKey() => _args = [];
#else
        public CompositeKey() => _args = ImmutableArray<object?>.Empty;
#endif

        /// <summary>
        /// Initializes a new <see cref="CompositeKey"/> structure with one or more values that represent the composite key.
        /// </summary>
        /// <param name="args">The argument values for the key.</param>
        public CompositeKey(params object?[] args)
        {
            if (args == null)
            {
#if NET8_0_OR_GREATER
                _args = [null];
#else
                _args = new object?[] { null }.ToImmutableArray();
#endif
                return;
            }

            object? temp;
            for (int idx = 0; idx < args.Length; idx++)
            {
                temp = args[idx] == null ? null : args[idx] switch
                {
                    string str => str,
                    char c => c,
                    short s => s,
                    int i => i,
                    long l => l,
                    Guid g => g,
                    DateTime dt => dt,
                    DateTimeOffset dto => dto,
                    ushort us => us,
                    uint ui => ui,
                    ulong ul => ul,
                    IReferenceData rd => rd?.Code,
                    _ => throw new ArgumentException($"{nameof(CompositeKey)} argument Type '{args[idx]!.GetType().FullName}' is not supported; must be one of the following: "
                        + "string, char, short, int, long, ushort, uint, ulong, Guid, DateTime and DateTimeOffset.")
                };
            }

#if NET8_0_OR_GREATER
            _args = [.. args];
#else
            _args = args.ToImmutableArray();
#endif
        }

        /// <summary>
        /// Gets the argument values for the key.
        /// </summary>
        /// <remarks>The <see cref="Args"/> are immutable.</remarks>
        public ImmutableArray<object?> Args => _args;

        /// <summary>
        /// Asserts the <see cref="Args"/> length and throws an <see cref="ArgumentException"/> where the length is not as expected.
        /// </summary>
        /// <param name="length">The expected length.</param>
        /// <returns>The <see cref="CompositeKey"/> to support fluent-style method-chaining.</returns>
        public CompositeKey AssertLength(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than or equal to zero.");

            if (_args.Length != length)
                throw new ArgumentException($"The number of arguments within the {nameof(CompositeKey)} must equal {length}.", nameof(length));

            return this;
        }

        /// <summary>
        /// Determines whether the current <see cref="CompositeKey"/> is equal to another <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="other">The other <see cref="CompositeKey"/>.</param>
        /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
        /// <remarks>Uses the <see cref="CompositeKeyComparer.Equals(CompositeKey, CompositeKey)"/>.</remarks>
        public bool Equals(CompositeKey other) => CompositeKeyComparer.Default.Equals(this, other);

        /// <summary>
        /// Determines whether the current <see cref="CompositeKey"/> is equal to another <see cref="Object"/>.
        /// </summary>
        /// <param name="obj">The other <see cref="object"/>.</param>
        /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
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
        /// <returns><c>true</c> indicates equal; otherwise, <c>false</c> for not equal.</returns>
        public static bool operator ==(CompositeKey left, CompositeKey right) => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="CompositeKey"/> types for non-equality.
        /// </summary>
        /// <param name="left">The left <see cref="CompositeKey"/>.</param>
        /// <param name="right">The right <see cref="CompositeKey"/>.</param>
        /// <returns><c>true</c> indicates not equal; otherwise, <c>false</c> for equal.</returns>
        public static bool operator !=(CompositeKey left, CompositeKey right) => !(left == right);

        /// <summary>
        /// Determines whether the <see cref="CompositeKey"/> is considered initial; i.e. all <see cref="Args"/> have their default value.
        /// </summary>
        /// <returns><c>true</c> indicates that the <see cref="CompositeKey"/> is initial; otherwise, <c>false</c>.</returns>
        public bool IsInitial
        {
            get
            {
                if (Args == null || Args.Length == 0)
                    return true;

                foreach (var arg in Args)
                {
                    if (arg != null && !arg.Equals(GetDefaultValue(arg.GetType())))
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets the default value for a specified <paramref name="type"/>.
        /// </summary>
        private static object? GetDefaultValue(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

        /// <summary>
        /// Returns the <see cref="CompositeKey"/> as a comma-separated <see cref="Args"/> <see cref="string"/>.
        /// </summary>
        /// <returns>The composite key as a <see cref="string"/>.</returns>
        /// <remarks>Each <see cref="Args"/> value is JSON-formatted to ensure consistency and portability.</remarks>
        public override string? ToString() => ToString(',');

        /// <summary>
        /// Returns the <see cref="CompositeKey"/> as a <see cref="string"/> with the <see cref="Args"/> separated by the <paramref name="separator"/>.
        /// </summary>
        /// <param name="separator">The seperator character.</param>
        /// <returns>The composite key as a <see cref="string"/>.</returns>
        public string? ToString(char separator)
        {
            if (Args.Length == 0 || (Args.Length == 1 && Args[0] is null))
                return null;

            if (Args.Length == 1 && Args[0] is string s)
                return s;

            var sb = new StringBuilder();
            for (int i = 0; i < Args.Length; i++)
            {
                if (i > 0)
                    sb.Append(separator);

                if (Args[i] is not null)
                    sb.Append(ConvertArgToString(Args[i]));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convert the argument to a string.
        /// </summary>
        private static string ConvertArgToString(object? arg) => arg switch
        {
            string str => str,
            char c => c.ToString(),
            Guid g => g.ToString(),
            int i => i.ToString(NumberFormatInfo.InvariantInfo),
            long l => l.ToString(NumberFormatInfo.InvariantInfo),
            short s => s.ToString(NumberFormatInfo.InvariantInfo),
            DateTime d => d.ToString("O"),
            DateTimeOffset o => o.ToString("O"),
            uint ui => ui.ToString(NumberFormatInfo.InvariantInfo),
            ulong ul => ul.ToString(NumberFormatInfo.InvariantInfo),
            ushort us => us.ToString(NumberFormatInfo.InvariantInfo),
            _ => throw new InvalidOperationException($"Type {arg!.GetType().Name} is not supported for a {nameof(ToString)}.")
        };

        /// <summary>
        /// Returns the <see cref="CompositeKey"/> as a JSON <see cref="string"/>.
        /// </summary>
        /// <returns>The composite key as a JSON <see cref="string"/>.</returns>
        /// <remarks>Uses the <see cref="Json.JsonSerializer.Default"/> internally.</remarks>
        public string ToJsonString() => Json.JsonSerializer.Default.Serialize(this);

        /// <summary>
        /// Creates a new <see cref="CompositeKey"/> from serialized <paramref name="json"/> (see <see cref="ToJsonString"/>);
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>The <see cref="CompositeKey"/>.</returns>
        /// <remarks>Uses the <see cref="Json.JsonSerializer.Default"/> internally.</remarks>
        public static CompositeKey CreateFromJson(string json) => (string.IsNullOrEmpty(json) || json == "null") ? new CompositeKey() : Json.JsonSerializer.Default.Deserialize<CompositeKey>(json);

        /// <summary>
        /// Try and create a new <see cref="CompositeKey"/> from a string-based <paramref name="key"/> (<see cref="ToString()"/>) where the key is of the <see cref="Type"/> specified.
        /// </summary>
        /// <typeparam name="T">The key <see cref="Type"/>.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="compositeKey">The resulting <see cref="CompositeKey"/></param>
        /// <returns><c>true</c> indicates that the <paramref name="compositeKey"/> was successfully created; otherwise, <c>false</c></returns>
        /// <remarks>The types specified must represent exact match of underlying <paramref name="key"/> parts.
        /// <para>There is no specific character escaping etc. performed automatically.</para></remarks>
        public static bool TryCreateFromString<T>(string? key, out CompositeKey compositeKey) => TryCreateFromString(key, [typeof(T)], out compositeKey);

        /// <summary>
        /// Creates a new <see cref="CompositeKey"/> from a string-based <paramref name="key"/> (<see cref="ToString()"/>) where each underlying part is of the <see cref="Type"/> specified.
        /// </summary>
        /// <typeparam name="T1">The key <see cref="Type"/> for the first part.</typeparam>
        /// <typeparam name="T2">The key <see cref="Type"/> for the second part.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="compositeKey">The resulting <see cref="CompositeKey"/></param>
        /// <returns><c>true</c> indicates that the <paramref name="compositeKey"/> was successfully created; otherwise, <c>false</c></returns>
        /// <remarks>The types specified must represent exact match of underlying <paramref name="key"/> parts.</remarks>
        public static bool TryCreateFromString<T1, T2>(string? key, out CompositeKey compositeKey) => TryCreateFromString(key, [typeof(T1), typeof(T2)], out compositeKey);

        /// <summary>
        /// Creates a new <see cref="CompositeKey"/> from a string-based <paramref name="key"/> (<see cref="ToString()"/>) where each underlying part is of the <see cref="Type"/> specified.
        /// </summary>
        /// <typeparam name="T1">The key <see cref="Type"/> for the first part.</typeparam>
        /// <typeparam name="T2">The key <see cref="Type"/> for the second part.</typeparam>
        /// <typeparam name="T3">The key <see cref="Type"/> for the third part.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="compositeKey">The resulting <see cref="CompositeKey"/></param>
        /// <returns><c>true</c> indicates that the <paramref name="compositeKey"/> was successfully created; otherwise, <c>false</c></returns>
        /// <remarks>The types specified must represent exact match of underlying <paramref name="key"/> parts.</remarks>
        public static bool TryCreateFromString<T1, T2, T3>(string? key, out CompositeKey compositeKey) => TryCreateFromString(key, [typeof(T1), typeof(T2), typeof(T3)], out compositeKey);

        /// <summary>
        /// Try and create a new <see cref="CompositeKey"/> from a string-based <paramref name="key"/> representation (<see cref="ToString()"/>) where each underlying part is of the <see cref="Type"/> specified.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="types">The <see cref="Type"/> array.</param>
        /// <param name="compositeKey">The resulting <see cref="CompositeKey"/></param>
        /// <returns><c>true</c> indicates that the <paramref name="compositeKey"/> was successfully created; otherwise, <c>false</c></returns>
        /// <remarks>The types specified must represent exact match of underlying <paramref name="key"/> parts.</remarks>
        public static bool TryCreateFromString(string? key, Type[] types, out CompositeKey compositeKey) => TryCreateFromString(key, ',', types, out compositeKey);

        /// <summary>
        /// Try and create a new <see cref="CompositeKey"/> from a string-based <paramref name="key"/> (<see cref="ToString()"/>) where the key is of the <see cref="Type"/> specified.
        /// </summary>
        /// <typeparam name="T">The key <see cref="Type"/>.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="separator">The seperator character.</param>
        /// <param name="compositeKey">The resulting <see cref="CompositeKey"/></param>
        /// <returns><c>true</c> indicates that the <paramref name="compositeKey"/> was successfully created; otherwise, <c>false</c></returns>
        /// <remarks>The types specified must represent exact match of underlying <paramref name="key"/> parts.</remarks>
        public static bool TryCreateFromString<T>(string? key, char separator, out CompositeKey compositeKey) => TryCreateFromString(key, separator, [typeof(T)], out compositeKey);

        /// <summary>
        /// Creates a new <see cref="CompositeKey"/> from a string-based <paramref name="key"/> (<see cref="ToString()"/>) where each underlying part is of the <see cref="Type"/> specified.
        /// </summary>
        /// <typeparam name="T1">The key <see cref="Type"/> for the first part.</typeparam>
        /// <typeparam name="T2">The key <see cref="Type"/> for the second part.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="separator">The seperator character.</param>
        /// <param name="compositeKey">The resulting <see cref="CompositeKey"/></param>
        /// <returns><c>true</c> indicates that the <paramref name="compositeKey"/> was successfully created; otherwise, <c>false</c></returns>
        /// <remarks>The types specified must represent exact match of underlying <paramref name="key"/> parts.</remarks>
        public static bool TryCreateFromString<T1, T2>(string? key, char separator, out CompositeKey compositeKey) => TryCreateFromString(key, separator, [typeof(T1), typeof(T2)], out compositeKey);

        /// <summary>
        /// Creates a new <see cref="CompositeKey"/> from a string-based <paramref name="key"/> (<see cref="ToString()"/>) where each underlying part is of the <see cref="Type"/> specified.
        /// </summary>
        /// <typeparam name="T1">The key <see cref="Type"/> for the first part.</typeparam>
        /// <typeparam name="T2">The key <see cref="Type"/> for the second part.</typeparam>
        /// <typeparam name="T3">The key <see cref="Type"/> for the third part.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="separator">The seperator character.</param>
        /// <param name="compositeKey">The resulting <see cref="CompositeKey"/></param>
        /// <returns><c>true</c> indicates that the <paramref name="compositeKey"/> was successfully created; otherwise, <c>false</c></returns>
        /// <remarks>The types specified must represent exact match of underlying <paramref name="key"/> parts.</remarks>
        public static bool TryCreateFromString<T1, T2, T3>(string? key, char separator, out CompositeKey compositeKey) => TryCreateFromString(key, separator, [typeof(T1), typeof(T2), typeof(T3)], out compositeKey);

        /// <summary>
        /// Try and create a new <see cref="CompositeKey"/> from a string-based <paramref name="key"/> representation (<see cref="ToString()"/>) where each underlying part is of the <see cref="Type"/> specified.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="separator">The seperator character.</param>
        /// <param name="types">The <see cref="Type"/> array.</param>
        /// <param name="compositeKey">The resulting <see cref="CompositeKey"/></param>
        /// <returns><c>true</c> indicates that the <paramref name="compositeKey"/> was successfully created; otherwise, <c>false</c></returns>
        /// <remarks>The types specified must represent exact match of underlying <paramref name="key"/> parts.</remarks>
        public static bool TryCreateFromString(string? key, char separator, Type[] types, out CompositeKey compositeKey)
        {
            var parts = string.IsNullOrEmpty(key) ? _singleEmptyArray : key.Split(separator, StringSplitOptions.None);
            if (parts.Length != types.Length)
            {
                compositeKey = Empty;
                return false;
            }

            var args = new object?[types.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var type = Nullable.GetUnderlyingType(types[i]);
                if (type is not null)
                {
                    if (string.IsNullOrEmpty(part))
                    {
                        args[i] = null;
                        continue;
                    }
                }
                else
                    type = types[i];

                if (!(type switch
                {
                    Type t when t == typeof(string) => TryParse(args, i, () => (true, part.Length == 0 ? null : part)),
                    Type t when t == typeof(char) => TryParse(args, i, () => part.Length == 0 ? (true, ' ') : (part.Length == 1 ? (true, part[0]) : (false, ' '))),
                    Type t when t == typeof(short) => TryParse(args, i, () => short.TryParse(part, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out short v) ? (true, v) : (false, 0)),
                    Type t when t == typeof(int) => TryParse(args, i, () => int.TryParse(part, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out int v) ? (true, v) : (false, 0)),
                    Type t when t == typeof(long) => TryParse(args, i, () => long.TryParse(part, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out long v) ? (true, v) : (false, 0)),
                    Type t when t == typeof(Guid) => TryParse(args, i, () => Guid.TryParse(part, out Guid v) ? (true, v) : (false, Guid.Empty)),
                    Type t when t == typeof(DateTime) => TryParse(args, i, () => DateTime.TryParseExact(part, "O", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.RoundtripKind, out DateTime v) ? (true, v) : (false, DateTime.MinValue)),
                    Type t when t == typeof(DateTimeOffset) => TryParse(args, i, () => DateTimeOffset.TryParseExact(part, "O", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.RoundtripKind, out DateTimeOffset v) ? (true, v) : (false, DateTimeOffset.MinValue)),
                    Type t when t == typeof(uint) => TryParse(args, i, () => uint.TryParse(part, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out uint v) ? (true, v) : (false, 0)),
                    Type t when t == typeof(ulong) => TryParse(args, i, () => ulong.TryParse(part, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out ulong v) ? (true, v) : (false, 0)),
                    Type t when t == typeof(ushort) => TryParse(args, i, () => ushort.TryParse(part, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out ushort v) ? (true, v) : (false, 0)),
                    _ => TryParse(args, i, () => (false, part))
                }))
                { 
                    compositeKey = Empty;
                    return false;
                }
            }

            compositeKey = new CompositeKey(args);
            return true;
        }

        /// <summary>
        /// Attempt parse and update the array.
        /// </summary>
        private static bool TryParse<T>(object?[] args, int index, Func<(bool, T)> parse)
        {
            (bool parsed, T value) = parse();
            args[index] = value;
            return parsed;
        }
    }
}