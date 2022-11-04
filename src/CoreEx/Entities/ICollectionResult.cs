// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Http;
using System;
using System.Collections;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the <see cref="Paging"/> and <see cref="Items"/> for a collection result.
    /// </summary>
    /// <remarks>Generally an <see cref="ICollectionResult"/> is not intended for serialized <see cref="HttpResponse"/>; the underlying <see cref="Items"/> is serialized with the <see cref="Paging"/> returned as <see cref="HttpResponse.Headers"/>.</remarks>
    public interface ICollectionResult
    {
        /// <summary>
        /// Gets the underlying item <see cref="Type"/>.
        /// </summary>
        Type ItemType { get; }

        /// <summary>
        /// Gets the <see cref="Items"/> <see cref="Type"/>.
        /// </summary>
        Type CollectionType { get;  }

        /// <summary>
        /// Gets or sets the <see cref="PagingResult"/>.
        /// </summary>
        PagingResult? Paging { get; set; }

        /// <summary>
        /// Gets the underlying <see cref="ICollection"/>.
        /// </summary>
        ICollection Items { get; set; }
    }
}