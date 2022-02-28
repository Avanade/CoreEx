// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

namespace CoreEx
{
    /// <summary>
    /// Represents an immutable composite key.
    /// </summary>
    /// <remarks>May contain zero or more <see cref="Args"/> that represent the composite key. A subset of the the .NET <see href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types">built-in types</see>
    /// are supported: <see cref="string"/>, <see cref="char"/>, <see cref="short"/>, <see cref="int"/>, <see cref="long"/>, <see cref="ushort"/>, <see cref="uint"/>, <see cref="ulong"/>, <see cref="Guid"/>, <see cref="DateTimeOffset"/> (converted to a <see cref="DateTime"/>) and <see cref="DateTime"/>.</remarks>
    //[System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.DebuggerDisplay("Key = {ToString()}")]
    public struct CompositeKey : IEquatable<CompositeKey>
    {
        private readonly ImmutableArray<object?> _args;

        /// <summary>
        /// Represents an empty <see cref="CompositeKey"/>.
        /// </summary>
        public static readonly CompositeKey Empty;

        /// <summary>
        /// Initializes a new <see cref="CompositeKey"/> structure.
        /// </summary>
        public CompositeKey() => _args = ImmutableArray<object?>.Empty;

        /// <summary>
        /// Initializes a new <see cref="CompositeKey"/> structure with one or more values that represent the composite key.
        /// </summary>
        /// <param name="args">The argument values for the key.</param>
        public CompositeKey(params object?[] args)
        {
            if (args == null)
            {
                _args = new object?[] { null }.ToImmutableArray();
                return;
            }

            var newArgs = new object?[args.Length];
            for (int idx = 0; idx < args.Length; idx++)
            {
                newArgs[idx] = args[idx] == null ? null : args[idx] switch
                {
                    string str => str,
                    char c => c,
                    short s => s,
                    int i => i,
                    long l => l,
                    Guid g => g,
                    DateTime dt => dt,
                    DateTimeOffset dto => dto.UtcDateTime,
                    ushort us => us,
                    uint ui => ui,
                    ulong ul => ul,
                    _ => throw new ArgumentException($"{nameof(CompositeKey)} argument Type '{args[idx]!.GetType().FullName}' is not supported; must be one of the following: "
                        + "string, char, short, int, long, ushort, uint, ulong, Guid, DateTime and DateTimeOffset.")
                };
            }

            _args = newArgs.ToImmutableArray();
        }

        /// <summary>
        /// Gets the argument values for the key.
        /// </summary>
        /// <remarks>The <see cref="Args"/> are immutable.</remarks>
        public ImmutableArray<object?> Args => _args;

        /// <summary>
        /// Determines whether the current <see cref="CompositeKey"/> is equal to another <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="other">The other <see cref="CompositeKey"/>.</param>
        /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
        /// <remarks>Uses the <see cref="CompositeKeyComparer.Equals(CompositeKey, CompositeKey)"/>.</remarks>
        public bool Equals(CompositeKey other) => new CompositeKeyComparer().Equals(this, other);

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
        public override int GetHashCode() => new CompositeKeyComparer().GetHashCode(this);

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
        public override string ToString() => ToString(',');

        /// <summary>
        /// Returns the <see cref="CompositeKey"/> as a <see cref="string"/> with the <see cref="Args"/> separated by the <paramref name="separator"/>.
        /// </summary>
        /// <returns>The composite key as a <see cref="string"/>.</returns>
        /// <remarks>Each <see cref="Args"/> value is JSON-formatted to ensure consistency and portability.</remarks>
        public string ToString(char separator)
        {
            if (Args.Length == 0)
                return string.Empty;

            var index = 0;
            var sb = new StringBuilder();
            var abw = new ArrayBufferWriter<byte>();
            using var ujw = new Utf8JsonWriter(abw);

            foreach (var arg in Args)
            {
                if (index > 0)
                    sb.Append(separator);

                bool isString = JsonWrite(ujw, false, arg);
                ujw.Flush();
                if (abw.WrittenMemory.Length > 0 && !(isString && abw.WrittenMemory.Length <= 2))
                    sb.Append(new BinaryData(isString ? abw.WrittenMemory[1..^1] : abw.WrittenMemory).ToString());

                ujw.Reset();
                abw.Clear();
                index++;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns the <see cref="CompositeKey"/> as a JSON <see cref="string"/>.
        /// </summary>
        /// <returns>The composite key as a JSON <see cref="string"/>.</returns>
        public string ToJsonString()
        {
            if (Args.Length == 0)
                return "null";

            var abw = new ArrayBufferWriter<byte>();
            using var ujw = new Utf8JsonWriter(abw);
            ujw.WriteStartArray();

            foreach (var arg in Args)
            {
                ujw.WriteStartObject();
                JsonWrite(ujw, true, arg);
                ujw.WriteEndObject();
            }

            ujw.WriteEndArray();
            ujw.Flush();
            return new BinaryData(abw.WrittenMemory).ToString();
        }

        /// <summary>
        /// Writes the JSON name and argument value pair.
        /// </summary>
        private static bool JsonWrite(Utf8JsonWriter ujw, bool includeName, object? arg) => arg switch
        {
            string str => JsonWrite(ujw, includeName ? "string" : null, () => ujw.WriteStringValue(str), true),
            char c => JsonWrite(ujw, includeName ? "char" : null, () => ujw.WriteStringValue(c.ToString()), true),
            short s => JsonWrite(ujw, includeName ? "short" : null, () => ujw.WriteNumberValue(s), false),
            int i => JsonWrite(ujw, includeName ? "int" : null, () => ujw.WriteNumberValue(i), false),
            long l => JsonWrite(ujw, includeName ? "long" : null, () => ujw.WriteNumberValue(l), false),
            Guid g => JsonWrite(ujw, includeName ? "guid" : null, () => ujw.WriteStringValue(g), true),
            DateTime d => JsonWrite(ujw, includeName ? "date" : null, () => ujw.WriteStringValue(d), true),
            ushort us => JsonWrite(ujw, includeName ? "ushort" : null, () => ujw.WriteNumberValue(us), false),
            uint ui => JsonWrite(ujw, includeName ? "uint" : null, () => ujw.WriteNumberValue(ui), false),
            ulong ul => JsonWrite(ujw, includeName ? "ulong" : null, () => ujw.WriteNumberValue(ul), false),
            _ => false
        };

        /// <summary>
        /// Writes the JSON name and invokes action to write argument value.
        /// </summary>
        private static bool JsonWrite(Utf8JsonWriter ujw, string? name, Action action, bool isString)
        {
            if (name != null)
                ujw.WritePropertyName(name);

            action();
            return isString;
        }

        /// <summary>
        /// Creates a new <see cref="CompositeKey"/> from serialized <paramref name="json"/> (see <see cref="ToJsonString"/>);
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>The <see cref="CompositeKey"/>.</returns>
        public static CompositeKey Create(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "null")
                return new CompositeKey();

            using var jd = JsonDocument.Parse(json);
            var (key, error) = Deserialize(jd);
            if (key.HasValue)
                return key.Value;

            throw new ArgumentException($"The JSON document is incorrectly formatted, or contains invalid data: {error}");
        }

        /// <summary>
        /// Deserialize the document.
        /// </summary>
        private static (CompositeKey? key, string? error) Deserialize(JsonDocument jd)
        {
            if (jd.RootElement.ValueKind != JsonValueKind.Array)
                return (null, "Root element must be an array.");

            var args = new object?[jd.RootElement.GetArrayLength()];
            int i = 0;

            foreach (var jo in jd.RootElement.EnumerateArray())
            {
                if (jo.ValueKind != JsonValueKind.Object)
                    return (null, "Root element array must only contains objects.");

                foreach (var jp in jo.EnumerateObject())
                {
                    if (jp.Value.ValueKind != JsonValueKind.String && jp.Value.ValueKind != JsonValueKind.Number)
                        return (null, "Array element must be either a String or a Number.");

                    try
                    {
                        switch (jp.Name)
                        {
                            case "string": args[i] = jp.Value.GetString(); break;
                            case "char": args[i] = Convert.ToChar(jp.Value.GetString()); break;
                            case "short": args[i] = jp.Value.GetInt16(); break;
                            case "int": args[i] = jp.Value.GetInt32(); break;
                            case "long": args[i] = jp.Value.GetInt64(); break;
                            case "guid": args[i] = jp.Value.GetGuid(); break;
                            case "date": args[i] = jp.Value.GetDateTime(); break;
                            case "ushort": args[i] = jp.Value.GetUInt16(); break;
                            case "uint": args[i] = jp.Value.GetUInt32(); break;
                            case "ulong": args[i] = jp.Value.GetUInt64(); break;
                            default: return (null, $"Property '{jp.Name}' is not supported.");
                        }
                    }
                    catch (Exception ex)
                    {
                        return (null, $"Property '{jp.Name}' value is invalid: {ex.Message}");
                    }
                }

                i++;
            }

            return (new CompositeKey(args), null);
        }
    }
}