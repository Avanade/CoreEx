// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the typed <typeparamref name="TItem"/> <see cref="Collection"/>.
    /// </summary>
    /// <typeparam name="TItem">The The underlying item <see cref="Type"/>.</typeparam>
    /// <remarks>Generally an <see cref="ICollectionResult"/> is not intended for serialized <see cref="HttpResponse"/>; the underlying <see cref="Collection"/> is serialized with the <see cref="ICollectionResult.Paging"/> returned as <see cref="HttpResponse.Headers"/>.</remarks>
    public interface ICollectionResult<TItem> : ICollectionResult
    {
        /// <summary>
        /// Gets the underlying <see cref="ICollection{TItem}"/>.
        /// </summary>
        new ICollection<TItem> Collection { get; set; }
    }
}