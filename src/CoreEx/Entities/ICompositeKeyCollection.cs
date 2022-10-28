// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections;

namespace CoreEx.Entities
{
    /// <summary>
    /// Enables the <see cref="CompositeKey"/> collection capabilities.
    /// </summary>
    public interface ICompositeKeyCollection: ICollection, IList
    {
        /// <summary>
        /// Indicates whether an item with the specified <paramref name="key"/> exists.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <returns><c>true</c> where the item exists; otherwise, <c>false</c>.</returns>
        bool ContainsKey(CompositeKey key);

        /// <summary>
        /// Gets the first item with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <returns>The item where found; otherwise, <c>null</c>.</returns>
        object? GetByKey(CompositeKey key);

        /// <summary>
        /// Removes all items with the specified primary <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        void RemoveByKey(CompositeKey key);

        /// <summary>
        /// Indicates whether there are any duplicate items in the collection.
        /// </summary>
        /// <returns><c>true</c> where there are one or more duplicates; otherwise, <c>false</c> where all items are unique.</returns>
        bool IsAnyDuplicates();
    }
}