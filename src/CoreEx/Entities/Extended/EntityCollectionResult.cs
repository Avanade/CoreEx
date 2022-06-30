// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Represents an <see cref="EntityBaseCollection{TEntity, TSelf}"/> class with a <see cref="PagingResult"/> and underlying <see cref="Collection"/>.
    /// </summary>
    /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TEntity">The entity item <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSelf">The entity <see cref="Type"/> itself.</typeparam>
    /// <remarks>Generally an <see cref="EntityCollectionResult{TColl, TEntity, TSelf}"/> is not intended for serialized <see cref="HttpResponse"/>; the underlying <see cref="Collection"/> is serialized with the <see cref="Paging"/> returned as <see cref="HttpResponse.Headers"/>.</remarks>
    [System.Diagnostics.DebuggerStepThrough]
    public class EntityCollectionResult<TColl, TEntity, TSelf> : EntityBase<TSelf>, ICollectionResult<TColl, TEntity>, IPagingResult, ICopyFrom
        where TColl : EntityBaseCollection<TEntity, TColl>, new()
        where TEntity : EntityBase<TEntity>, new()
        where TSelf : EntityCollectionResult<TColl, TEntity, TSelf>, new()
    {
        private PagingResult? _paging;
        private TColl? _collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCollectionResult{TColl, TEntity, TSelf}"/> class.
        /// </summary>
        public EntityCollectionResult() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCollectionResult{TColl, TEntity, TSelf}"/> class.
        /// </summary>
        /// <param name="paging">Defaults the <see cref="Paging"/> to the requesting <see cref="PagingArgs"/>.</param>
        public EntityCollectionResult(PagingArgs? paging)
        {
            if (paging != null)
                _paging = Paging is PagingResult pr ? pr : new PagingResult(paging);
        }

        /// <summary>
        /// Gets or sets the underlying collection.
        /// </summary>
        public TColl Collection { get => _collection ??= new TColl(); set => SetValue(ref _collection, value); }

        /// <summary>
        /// Gets or sets the <see cref="PagingResult"/>.
        /// </summary>
        /// <remarks>Where this value is <c>null</c> it indicates that the paging was unable to be determined.</remarks>
        public PagingResult? Paging { get => _paging; set => SetValue(ref _paging, value); }

        /// <summary>
        /// Gets the item <see cref="Type"/>.
        /// </summary>
        Type ICollectionResult.ItemType => typeof(TEntity);

        /// <inheritdoc/>
        protected override IEnumerable<IPropertyValue> GetPropertyValues()
        {
            yield return CreateProperty(Paging, v => Paging = v);
            yield return CreateProperty(Collection, v => Collection = v!);
        }
    }
}