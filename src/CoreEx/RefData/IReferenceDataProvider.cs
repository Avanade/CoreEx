// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides a means to manage and group one or more <see cref="IReferenceData"/> entities for use by the centralised <see cref="ReferenceDataManager"/>.
    /// </summary>
    public interface IReferenceDataProvider
    {
        /// <summary>
        /// Gets all the underlying <see cref="IReferenceData"/> <see cref="Type">types</see> provided.
        /// </summary>
        /// <returns>The <see cref="IReferenceData"/> <see cref="Type">types</see> provided.</returns>
        Type[] Types { get; }

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/>.</returns>
        IReferenceDataCollection this[Type type] { get; }

        /// <summary>
        /// Prefetches all of the named <see cref="IReferenceData"/> objects. 
        /// </summary>
        /// <param name="names">The list of <see cref="IReferenceData"/> names.</param>
        /// <remarks>Note for implementers: this should only fetch where not already cached or expired. This is provided to improve performance for consuming applications to reduce the overhead of making multiple individual invocations,
        /// i.e. reduces chattiness across a potentially high-latency connection.</remarks>
        public Task PrefetchAsync(params string[] names) => throw new NotSupportedException();
    }
}