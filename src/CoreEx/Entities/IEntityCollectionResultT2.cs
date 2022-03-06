// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the typed <typeparamref name="TItem"/> collection with a <see cref="Result"/>.
    /// </summary>
    /// <typeparam name="TColl">The result collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The underlying item <see cref="Type"/>.</typeparam>
    public interface IEntityCollectionResult<TColl, TItem> : IEntityCollectionResult<TItem>
    {
        /// <summary>
        /// Gets the result.
        /// </summary>
        TColl Result { get; set; }
    }
}