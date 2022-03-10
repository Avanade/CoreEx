// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the typed <typeparamref name="TItem"/> <see cref="Collection"/>.
    /// </summary>
    /// <typeparam name="TItem">The The underlying item <see cref="Type"/>.</typeparam>
    public interface ICollectionResult<TItem> : ICollectionResult
    {
        /// <summary>
        /// Gets the underlying <see cref="ICollection{TItem}"/>.
        /// </summary>
        new ICollection<TItem>? Collection { get; }
    }
}