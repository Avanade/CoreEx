// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the typed <typeparamref name="TItem"/> <see cref="Items"/>.
    /// </summary>
    /// <typeparam name="TColl">The result collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The underlying item <see cref="Type"/>.</typeparam>
    /// <remarks>Generally an <see cref="ICollectionResult"/> is not intended for serialized <see cref="HttpResponse"/>; the underlying <see cref="Items"/> is serialized with the <see cref="ICollectionResult.Paging"/> returned as <see cref="HttpResponse.Headers"/>.</remarks>
    public interface ICollectionResult<TColl, TItem> : ICollectionResult<TItem> where TColl : ICollection<TItem>, new()
    {
        /// <inheritdoc/>
        Type ICollectionResult.ItemType => typeof(TItem);

        /// <inheritdoc/>
        Type ICollectionResult.CollectionType => typeof(TColl);

        /// <summary>
        /// Gets or sets the underlying collection.
        /// </summary>
        new TColl Items { get; set; }

        /// <summary>
        /// Gets the underlying <see cref="ICollection"/>.
        /// </summary>
        ICollection ICollectionResult.Items { get => (ICollection)Items; set => Items = value == null ? new TColl() : (TColl)value; }

        /// <summary>
        /// Gets the underlying <see cref="ICollection{TEntity}"/>.
        /// </summary>
        ICollection<TItem> ICollectionResult<TItem>.Items { get => Items; set => Items = value == null ? new TColl() : (TColl)value; }
    }
}