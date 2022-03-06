// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the <see cref="Paging"/> and <see cref="Collection"/> for a collection result.
    /// </summary>
    public interface IEntityCollectionResult
    {
        /// <summary>
        /// Gets the underlying item <see cref="Type"/>.
        /// </summary>
        Type ItemType { get; }

        /// <summary>
        /// Gets or sets the <see cref="PagingResult"/>.
        /// </summary>
        PagingResult? Paging { get; set; }

        /// <summary>
        /// Gets the underlying <see cref="ICollection"/>.
        /// </summary>
        ICollection? Collection { get; }
    }
}