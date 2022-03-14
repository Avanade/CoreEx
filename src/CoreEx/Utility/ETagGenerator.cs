// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Json;
using System;
using System.Globalization;
using System.Text;

namespace CoreEx.Utility
{
    /// <summary>
    /// Provides <see cref="IETag.ETag"/> generator capabilities.
    /// </summary>
    public static class ETagGenerator
    {
        /// <summary>
        /// Represents the divider character where ETag value is made up of multiple parts.
        /// </summary>
        public const char DividerCharacter = '|';

        /// <summary>
        /// Generates an ETag for a value by serializing to JSON and performing an <see cref="System.Security.Cryptography.SHA256"/> hash.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="parts">Optional extra part(s) to append to the JSON to include in the underlying hash computation.</param>
        /// <returns>The generated ETag.</returns>
        public static string? Generate<T>(IJsonSerializer jsonSerializer, T? value, params string[] parts) where T : class
        {
            if (jsonSerializer == null)
                throw new ArgumentNullException(nameof(jsonSerializer));

            if (value == null)
                return null;

            // Where value is IFormattable/IComparable use ToString; otherwise, JSON serialize.
            var txt = ConvertToString(jsonSerializer, value);

            if (parts.Length > 0)
            {
                var sb = new StringBuilder(txt);
                foreach (var ex in parts)
                {
                    sb.Append(DividerCharacter);
                    sb.Append(ex);
                }

                txt = sb.ToString();
            }

            return $"\"{GenerateHash(txt)}\"";
        }

        /// <summary>
        /// Generates a hash of the string using <see cref="System.Security.Cryptography.SHA256"/>.
        /// </summary>
        /// <param name="text">The text value to hash.</param>
        /// <returns>The hashed value.</returns>
        public static string GenerateHash(string text)
        {
            var buf = Encoding.UTF8.GetBytes(text ?? throw new ArgumentNullException(nameof(text)));
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = new BinaryData(sha256.ComputeHash(buf));
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Converts the <paramref name="value"/> to a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>A <see cref="string"/> representation of the value.</returns>
        private static string ConvertToString(IJsonSerializer jsonSerializer, object? value)
        {
            if (value == null)
                return string.Empty;

            if (value is string str)
                return str;

            if (value is DateTime dte)
                return dte.ToString("o");

            return (value is IFormattable ic)
                ? ic.ToString(null, CultureInfo.InvariantCulture)
                : ((value is IComparable) ? value.ToString() : jsonSerializer.Serialize(value)) ?? string.Empty;
        }
    }
}