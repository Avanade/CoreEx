﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Defines the core capabilities for the special-purpose <b>CosmosDb</b> object that houses an underlying model-<see cref="Value"/>.
    /// </summary>
    public interface ICosmosDbValue : IIdentifier<string>, ICosmosDbType
    {
        /// <summary>
        /// Gets the model value.
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Prepares the object before sending to Cosmos.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="type">The model <see cref="Type"/> name override.</param>
        void PrepareBefore(CosmosDbArgs dbArgs, string? type);

        /// <summary>
        /// Prepares the object after getting from Cosmos.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        void PrepareAfter(CosmosDbArgs dbArgs);
    }
}