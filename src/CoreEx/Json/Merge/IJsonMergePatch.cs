// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Json.Merge
{
    /// <summary>
    /// Enables a JSON Merge Patch (<c>application/merge-patch+json</c>) whereby the contents of a JSON document are merged into an existing object value as per <see href="https://tools.ietf.org/html/rfc7396"/>.
    /// </summary>
    public interface IJsonMergePatch
    {
        /// <summary>
        /// Merges the JSON <see cref="string"/> content into the <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="json">The JSON to merge.</param>
        /// <param name="value">The value to merge into.</param>
        /// <returns><c>true</c> indicates that changes were made to the <paramref name="value"/> as a result of the merge; otherwise, <c>false</c> for no changes.</returns>
        bool Merge<T>(string json, ref T? value);

        /// <summary>
        /// Merges the JSON <see cref="string"/> content into the value returned by the <paramref name="getValue"/> function.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="json">The JSON to merge.</param>
        /// <param name="getValue">The function to get the value to merge into. The function is passed in the <paramref name="json"/> deserialized value.</param>
        /// <returns><c>true</c> indicates that changes were made to the entity value as a result of the merge; otherwise, <c>false</c> for no changes. The merged value is also returned.</returns>
        /// <remarks>Provides the opportunity to validate the JSON before getting the value where this execution order is important; i.e. get operation is expensive (latency).</remarks>
        Task<(bool HasChanges, T? Value)> MergeAsync<T>(string json, Func<T?, Task<T?>> getValue);
    }
}