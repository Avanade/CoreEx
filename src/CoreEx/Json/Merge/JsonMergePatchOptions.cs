// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Json.Merge
{
    /// <summary>
    /// The <see cref="JsonMergePatch"/> options.
    /// </summary>
    /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="Json.JsonSerializer.Default"/>.</param>
    public class JsonMergePatchOptions(IJsonSerializer? jsonSerializer = null)
    {
        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        public IJsonSerializer JsonSerializer { get; } = jsonSerializer ?? Json.JsonSerializer.Default;

        /// <summary>
        /// Gets or sets the <see cref="StringComparer"/> for matching the JSON name (defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>).
        /// </summary>
        public StringComparer NameComparer { get; set; } = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Gets or sets the <see cref="Merge.DictionaryMergeApproach"/>. Defaults to <see cref="DictionaryMergeApproach.Merge"/>.
        /// </summary>
        public DictionaryMergeApproach DictionaryMergeApproach { get; set; } = DictionaryMergeApproach.Merge;

        /// <summary>
        /// Gets or sets the <see cref="Merge.EntityKeyCollectionMergeApproach"/>. Defaults to <see cref="DictionaryMergeApproach.Replace"/>.
        /// </summary>
        public EntityKeyCollectionMergeApproach EntityKeyCollectionMergeApproach { get; set; } = EntityKeyCollectionMergeApproach.Replace;
    }
}