// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the typed <typeparamref name="TItem"/> collection with a <see cref="Result"/>.
    /// </summary>
    /// <typeparam name="TColl">The result collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The underlying item <see cref="Type"/>.</typeparam>
    public interface ICollectionResult<TColl, TItem> : ICollectionResult<TItem> where TColl : ICollection<TItem>
    {
        /// <summary>
        /// Gets or sets the collection result.
        /// </summary>
        TColl Result { get; set; }
    }
}