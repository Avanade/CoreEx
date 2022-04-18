// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Represents the core <see cref="EntityBase"/> collection capabilities.
    /// </summary>
    public interface IEntityBaseCollection : ICollection, ICleanUp, IInitial
    {
        /// <summary>
        /// Adds the items of the specified collection to the end of the current collection.
        /// </summary>
        /// <param name="collection">The collection containing the items to add.</param>
        void AddRange(IEnumerable collection);
    }
}