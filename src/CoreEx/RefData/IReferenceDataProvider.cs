// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides a means to manage and group one or more <see cref="IReferenceData"/> entities for use by the centralised <see cref="ReferenceDataOrchestrator"/>.
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
        Task<IReferenceDataCollection> GetAsync(Type type);
    }
}