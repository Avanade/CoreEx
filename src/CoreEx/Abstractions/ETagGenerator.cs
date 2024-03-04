// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Json;
using System;
using System.Security.Cryptography;

namespace CoreEx.Abstractions
{
    /// <summary>
    /// Provides <see cref="IETag.ETag"/> generator capabilities.
    /// </summary>
    public static class ETagGenerator
    {
        /// <summary>
        /// Generates an ETag for a value by serializing to JSON and performing an <see cref="SHA256"/> hash.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="value">The value.</param>
        /// <returns>The generated ETag.</returns>
        public static string? Generate<T>(IJsonSerializer jsonSerializer, T? value)
        {
            jsonSerializer.ThrowIfNull(nameof(jsonSerializer));
            if (value == null)
                return null;

            // Serialize to JSON and then hash.
            byte[] hash;
            var bd = jsonSerializer.SerializeToBinaryData(value);
#if NETSTANDARD2_1
            using var sha256 = SHA256.Create();
            hash = sha256.ComputeHash(bd.ToArray());
#else
            hash = SHA256.HashData(bd);
#endif
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Generates a hash of the parts using <see cref="SHA256"/>.
        /// </summary>
        /// <param name="parts">The parts to hash.</param>
        /// <returns>The hashed value.</returns>
        public static string? GenerateHash(params string[] parts)
        {
            if (parts == null || parts.Length == 0)
                return null;

            byte[] hash;
#if NETSTANDARD2_1
            var input = parts.Length == 1 ? parts[0] : string.Concat(parts);
            using var sha256 = SHA256.Create();
            hash = sha256.ComputeHash(new BinaryData(input).ToArray());
#else
            if (parts.Length == 1)
                hash = SHA256.HashData(new BinaryData(parts[0]));
            else
            {
                using var ih = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                foreach (var part in parts)
                {
                    ih.AppendData(new BinaryData(part));
                }

                hash = ih.GetCurrentHash();
            }

#endif
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Formats a <paramref name="value"/> as an <see cref="IETag.ETag"/> by bookending with the requisite double quotes character; for example '<c>abc</c>' would be formatted as '<c>"abc"</c>'.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>The formatted <see cref="IETag.ETag"/>.</returns>
        public static string? FormatETag(string? value)
        { 
            if (value is null)
                return null;

            if (value.StartsWith('\"') && value.EndsWith('\"'))
                return value;

            if (value.StartsWith("W/\"") && value.EndsWith('\"'))
                return value[2..];
            
            return $"\"{value}\"";
        }

        /// <summary>
        /// Parses an <see cref="IETag.ETag"/> by removing any weak prefix ('<c>W/</c>') double quotes character bookends; for example '<c>"abc"</c>' would be formatted as '<c>abc</c>'.
        /// </summary>
        /// <param name="etag">The <see cref="IETag.ETag"/> to unformat.</param>
        /// <returns>The unformatted value.</returns>
        public static string? ParseETag(string? etag)
        {
            if (string.IsNullOrEmpty(etag))
                return null;

            if (etag.StartsWith('\"') && etag.EndsWith('\"'))
                return etag[1..^1];

            if (etag.StartsWith("W/\"") && etag.EndsWith('\"'))
                return etag[2..^1];

            return etag;
        }
    }
}