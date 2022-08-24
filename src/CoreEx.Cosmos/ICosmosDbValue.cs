// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Defines the core capabilities for the special-purpose <b>CosmosDb/DocumentDb</b> object that houses an underlying model-<see cref="Value"/>.
    /// </summary>
    public interface ICosmosDbValue : IIdentifier<string>
    {
        /// <summary>
        /// Gets or sets the <see cref="Type"/> name.
        /// </summary>
        string? Type { get; }

        /// <summary>
        /// Gets the model value.
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Prepares the object before sending to Cosmos.
        /// </summary>
        /// <param name="db">The <see cref="ICosmosDb"/>.</param>
        void PrepareBefore(ICosmosDb db);

        /// <summary>
        /// Prepares the object after getting from Cosmos.
        /// </summary>
        /// <param name="db">The <see cref="ICosmosDb"/>.</param>
        void PrepareAfter(ICosmosDb db);
    }
}