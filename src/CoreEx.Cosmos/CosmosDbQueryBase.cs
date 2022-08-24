// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Generic;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Enables the common <b>CosmosDb/DocumentDb</b> query capabilities.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    public abstract class CosmosDbQueryBase<T, TModel> where T : class, new() where TModel : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbQueryBase{T, TModel}"/> class.
        /// </summary>
        protected CosmosDbQueryBase(ICosmosDbContainer container) => Container = container ?? throw new ArgumentNullException(nameof(container));

        /// <summary>
        /// Gets the <see cref="ICosmosDbContainer"/>.
        /// </summary>
        protected ICosmosDbContainer Container { get; }

        /// <summary>
        /// Gets the <see cref="CosmosDbArgs"/>.
        /// </summary>
        public CosmosDbArgs? QueryArgs => Container.DbArgs;

        /// <summary>
        /// Gets the <see cref="PagingResult"/>.
        /// </summary>
        public PagingResult? Paging { get; protected set; }

        /// <summary>
        /// Selects a single item.
        /// </summary>
        /// <returns>The single item.</returns>
        public abstract T SelectSingle();

        /// <summary>
        /// Selects a single item or default.
        /// </summary>
        /// <returns>The single item or default.</returns>
        public abstract T? SelectSingleOrDefault();

        /// <summary>
        /// Selects first item.
        /// </summary>
        /// <returns>The first item.</returns>
        public abstract T SelectFirst();

        /// <summary>
        /// Selects first item or default.
        /// </summary>
        /// <returns>The single item or default.</returns>
        public abstract T? SelectFirstOrDefault();

        /// <summary>
        /// Executes the query command creating a resultant collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <returns>A resultant collection.</returns>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public TColl SelectQuery<TColl>() where TColl : ICollection<T>, new()
        {
            var coll = new TColl();
            SelectQuery(coll);
            return coll;
        }

        /// <summary>
        /// Executes the query command creating a resultant array.
        /// </summary>
        /// <returns>A resultant array.</returns>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public T[] ToArray() => SelectQuery<List<T>>().ToArray();

        /// <summary>
        /// Executes the query adding to the passed collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="coll">The collection to add items to.</param>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public abstract void SelectQuery<TColl>(TColl coll) where TColl : ICollection<T>;
    }
}