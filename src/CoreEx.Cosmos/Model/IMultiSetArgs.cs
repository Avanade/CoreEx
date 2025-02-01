// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;
using System;

namespace CoreEx.Cosmos.Model
{
    /// <summary>
    /// Enables the <b>CosmosDb</b> multi-set arguments.
    /// </summary>
    public interface IMultiSetArgs
    {
        /// <summary>
        /// Gets the minimum number of items allowed.
        /// </summary>
        int MinItems { get; }

        /// <summary>
        /// Gets the maximum number of items allowed.
        /// </summary>
        int? MaxItems { get; }

        /// <summary>
        /// Indicates whether to stop further result set processing where the current set has resulted in a null (i.e. no items).
        /// </summary>
        bool StopOnNull { get; }

        /// <summary>
        /// Gets the model <see cref="System.Type"/>.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Adds a model <paramref name="item"/> for its respective dataset.
        /// </summary>
        /// <param name="container">The <see cref="CosmosDbContainer"/>.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="item">The model item.</param>
        Result AddItem(CosmosDbContainer container, CosmosDbArgs dbArgs, object item);

        /// <summary>
        /// Verify against contraints.
        /// </summary>
        /// <returns><c>true</c> indicates that at least one item exists to action; otherwise, <c>false</c>.</returns>
        Result<bool> Verify();

        /// <summary>
        /// Invokes the underlying action.
        /// </summary>
        void Invoke();

        /// <summary>
        /// Gets the name for the model <see cref="System.Type"/>.
        /// </summary>
        /// <param name="container">The <see cref="CosmosDbContainer"/>.</param>
        /// <returns>The model name.</returns>
        string GetModelName(CosmosDbContainer container);
    }
}