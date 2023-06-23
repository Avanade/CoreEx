// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents a basic <see cref="ICollectionResult{TEntity}"/> class with a <see cref="PagingResult"/> and underlying <see cref="Items"/>.
    /// </summary>
    /// <typeparam name="TColl">The result collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The underlying entity <see cref="Type"/>.</typeparam>
    /// <remarks>Generally an <see cref="CollectionResult{TColl, TItem}"/> is not intended to be (de)serialized. For an <see cref="HttpResponseMessage"/> the underlying <see cref="Items"/> is (de)serialized only, with the <see cref="Paging"/> 
    /// included within the corresponding <see cref="HttpResponseMessage.Headers"/>. The <see cref="Json.IJsonSerializer"/> implementations have specific functionality included to (de)serialize the <see cref="Items"/> only, dropping the
    /// <see cref="Paging"/>; see <see cref="Text.Json.CollectionResultConverterFactory"/> as an example.</remarks>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class CollectionResult<TColl, TItem> : ICollectionResult<TColl, TItem>, IPagingResult
        where TColl : List<TItem>, new()
        where TItem : class
    {
        private TColl? _collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionResult{TColl, TEntity}"/> class.
        /// </summary>
        public CollectionResult() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionResult{TColl, TEntity}"/> class with <paramref name="paging"/>.
        /// </summary>
        /// <param name="paging">Defaults the <see cref="Paging"/> to the requesting <see cref="PagingArgs"/>.</param>
        protected CollectionResult(PagingArgs? paging)
        {
            if (paging != null)
                Paging = new PagingResult(paging);
        }

        /// <inheritdoc/>
        public TColl Items
        {
            get => _collection ??= new TColl();
            set => _collection = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <inheritdoc/>
        public PagingResult? Paging { get; set; }
    }
}