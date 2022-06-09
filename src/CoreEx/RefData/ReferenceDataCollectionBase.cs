// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.RefData
{
    /// <summary>
    /// Represents a base <see cref="IReferenceDataCollection{TId, TRef}"/> collection with <typeparamref name="TSelf"/> to enable the <see cref="Create(IEnumerable{TRef}?)"/>.
    /// </summary>
    /// <typeparam name="TId">The <see cref="IIdentifier.Id"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSelf">The <see cref="ReferenceDataCollectionBase{TId, TRef, TSelf}"/> itself.</typeparam>
    public abstract class ReferenceDataCollectionBase<TId, TRef, TSelf> : ReferenceDataCollection<TId, TRef> where TId : IComparable<TId>, IEquatable<TId> where TRef : class, IReferenceData<TId> where TSelf : ReferenceDataCollectionBase<TId, TRef, TSelf>, new()
    {
        /// <summary>
        /// Creates an instance of <typeparamref name="TSelf"/> and adds from the <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source items.</param>
        /// <returns>An instance of <typeparamref name="TSelf"/>.</returns>
        public static TSelf Create(IEnumerable<TRef>? source = null)
        {
            var coll = new TSelf();
            if (source != null)
                coll.AddRange(source);

            return coll;
        }

        /// <summary>
        /// Creates an instance of <typeparamref name="TSelf"/> and adds from the <paramref name="source"/> asynchronously.
        /// </summary>
        /// <param name="source">The source items.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>An instance of <typeparamref name="TSelf"/>.</returns>
        public static Task<TSelf> CreateAsync(IQueryable<TRef> source, CancellationToken cancellationToken = default) 
            => CreateAsync(source is IAsyncEnumerable<TRef> ae ? ae : throw new ArgumentException("The source must implement IAsyncEnumerable<TRef>.", nameof(source)), cancellationToken);

        /// <summary>
        /// Creates an instance of <typeparamref name="TSelf"/> and adds from the <paramref name="source"/> asynchronously.
        /// </summary>
        /// <param name="source">The source items.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>An instance of <typeparamref name="TSelf"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Future proofing.")]
        public static async Task<TSelf> CreateAsync(IAsyncEnumerable<TRef>? source = null, CancellationToken cancellationToken = default)
        {
            var coll = new TSelf();

            if (source != null)
            {
                await foreach (TRef i in source.ConfigureAwait(false))
                {
                    coll.Add(i);
                }
            }

            return coll;
        }
    }
}