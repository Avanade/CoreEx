// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;

namespace CoreEx.Cosmos.Extended
{
    /// <summary>
    /// Enables the <b>CosmosDb</b> multi-set arguments.
    /// </summary>
    public interface IMultiSetArgs
    {
        /// <summary>
        /// Gets the <see cref="ICosmosDbContainer"/> that contains the container configuration.
        /// </summary>
        ICosmosDbContainer Container { get; }

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
        /// Adds an entity item for its respective dataset.
        /// </summary>
        /// <param name="item">The entity item.</param>
        Result AddItem(object? item);

        /// <summary>
        /// Verify against contraints.
        /// </summary>
        /// <returns><c>true</c> indicates that at least one item exists to action; otherwise, <c>false</c>.</returns>
        Result<bool> Verify();

        /// <summary>
        /// Invokes the underlying action.
        /// </summary>
        void Invoke();
    }
}