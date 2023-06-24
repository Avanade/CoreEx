// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the typed <typeparamref name="TItem"/> <see cref="Items"/>.
    /// </summary>
    /// <typeparam name="TItem">The The underlying item <see cref="Type"/>.</typeparam>
    /// <remarks>Generally an <see cref="ICollectionResult"/> is not intended for serialized <see cref="HttpResponseMessage"/>; the underlying <see cref="Items"/> is serialized with the <see cref="ICollectionResult.Paging"/> returned as <see cref="HttpResponseMessage.Headers"/>.</remarks>
    public interface ICollectionResult<TItem> : ICollectionResult
    {
        /// <summary>
        /// Gets the underlying <see cref="ICollection{TItem}"/>.
        /// </summary>
        new ICollection<TItem> Items { get; set; }
    }
}