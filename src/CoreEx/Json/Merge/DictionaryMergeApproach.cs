// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections;
using System.Collections.Generic;

namespace CoreEx.Json.Merge
{
    /// <summary>
    /// Defines the approach for a dictionary (see <see cref="IDictionary"/>) merge; being either <see cref="Merge"/> or <see cref="Replace"/>.
    /// </summary>
    /// <remarks>The formal specification <see href="https://tools.ietf.org/html/rfc7396"/> does not explictly state how to treat a dictionary; however, the <see cref="Merge"/> approach appears to most closely align as the dictionary
    /// is represented as JSON properties (the <see cref="KeyValuePair{TKey, TValue}.Key"/> being the property name and the <see cref="KeyValuePair{TKey, TValue}.Value"/> being the corresponding value) versus an 
    /// <see cref="System.Text.Json.JsonValueKind.Array"/>.</remarks>
    public enum DictionaryMergeApproach
    {
        /// <summary>
        /// Indicates that the dictionary (see <see cref="IDictionary"/>) merge will act similar to a property merge, in that each key is a property (which is how they present within JSON). Therefore, to remove the item the value must be set
        /// to <see cref="System.Text.Json.JsonValueKind.Null"/> to explicitly remove. Where the merging key already exists then a standard value merge will result; otherwise, where the key does not exist, a resulting add will result.
        /// </summary>
        Merge,

        /// <summary>
        /// Indicates that the dictionary (see <see cref="IDictionary"/>) merge will be treated the same as any <see cref="System.Text.Json.JsonValueKind.Array"/> where the result is a replacement (overwrie) operation.
        /// </summary>
        Replace
    }
}