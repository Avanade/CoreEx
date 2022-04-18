// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections;

namespace CoreEx.Json.Merge
{
    /// <summary>
    /// The <see cref="JsonMergePatch"/> options.
    /// </summary>
    public class JsonMergePatchOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMergePatchOptions"/> class.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="Json.JsonSerializer.Default"/>.</param>
        public JsonMergePatchOptions(IJsonSerializer? jsonSerializer = null) => JsonSerializer = jsonSerializer ?? Json.JsonSerializer.Default;

        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        public IJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Gets or sets the <see cref="StringComparer"/> for matching the JSON name (defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>).
        /// </summary>
        public StringComparer NameComparer { get; set; } = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Indicates whether the dictionarys (see <see cref="IDictionary"/>) where the corresponding value is a complex class (not <see cref="IEnumerable"/>) will perform an advanced per-item merge using the configured key to match each item
        /// performing a resulting add, update or delete.
        /// </summary>
        /// <remarks>Note: this capability is unique to <i>CoreEx</i> and not part of the formal specification <see href="https://tools.ietf.org/html/rfc7396"/>.</remarks>
        public bool UseKeyMergeForDictionaries { get; set; }

        /// <summary>
        /// Indicates whether the collections where they implement <see cref="IPrimaryKeyCollection{T}"/> will perform an advanced per-item merge using the configured <see cref="IPrimaryKey.PrimaryKey"/> to match each item performing a 
        /// resulting add (to the end of existing), update or delete.
        /// </summary>
        /// <remarks><para>A <see cref="JsonMergePatchException"/> with be thrown where either source JSON or destination entity have or result in duplicates (see <see cref="IKeyedCollection.IsAnyDuplicates"/>).</para>
        /// Note: this capability is unique to <i>CoreEx</i> and not part of the formal specification <see href="https://tools.ietf.org/html/rfc7396"/>.</remarks>
        public bool UseKeyMergeForPrimaryKeyCollections { get; set; }
    }
}