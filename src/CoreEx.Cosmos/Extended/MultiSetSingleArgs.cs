// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
using System;
using System.Collections.Generic;

namespace CoreEx.Cosmos.Extended
{
    /// <summary>
    /// Provides the <b>CosmosDb</b> multi-set arguments when expecting a single item only.
    /// </summary>
    /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <param name="container">The <see cref="ICosmosDbContainer{T, TModel}"/> that contains the <typeparamref name="T"/> and <typeparamref name="TModel"/> container configuration.</param>
    /// <param name="result">The action that will be invoked with the result of the set.</param>
    /// <param name="isMandatory">Indicates whether the value is mandatory; defaults to <c>true</c>.</param>
    /// <param name="stopOnNull">Indicates whether to stop further result set processing where the current set has resulted in a null (i.e. no items).</param>
    public class MultiSetSingleArgs<T, TModel>(ICosmosDbContainer<T, TModel> container, Action<T> result, bool isMandatory = true, bool stopOnNull = false) : IMultiSetArgs where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
    {
        private List<T>? _items;
        private readonly Action<T> _result = result.ThrowIfNull(nameof(result));

        /// <inheritdoc/>
        ICosmosDbContainer IMultiSetArgs.Container => Container;

        /// <summary>
        /// Gets the <see cref="ICosmosDbContainer{T, TModel}"/> that contains the <typeparamref name="T"/> and <typeparamref name="TModel"/> container configuration.
        /// </summary>
        public ICosmosDbContainer<T, TModel> Container { get; } = container.ThrowIfNull(nameof(container));

        /// <summary>
        /// Indicates whether the value is mandatory; i.e. a corresponding record must be read.
        /// </summary>
        public bool IsMandatory { get; set; } = isMandatory;

        /// <inheritdoc/>
        public int MinItems => IsMandatory ? 1 : 0;

        /// <inheritdoc/>
        public int? MaxItems => 1;

        /// <inheritdoc/>
        public bool StopOnNull { get; set; } = stopOnNull;

        /// <inheritdoc/>
        Result IMultiSetArgs.AddItem(object? item)
        {
            if (item is null)
                return Result.Success;

            _items ??= [];
            _items.Add((T)item);
            return !MaxItems.HasValue || _items.Count <= MaxItems.Value
                ? Result.Success
                : Result.Fail(new InvalidOperationException($"MultiSetSingleArgs has returned more items ({_items.Count}) than expected ({MaxItems.Value})."));
        }

        /// <inheritdoc/>
        Result<bool> IMultiSetArgs.Verify()
        {
            var count = _items?.Count ?? 0;
            if (count < MinItems)
                return Result.Fail(new InvalidOperationException($"MultiSetSingleArgs has returned less items ({count}) than expected ({MinItems})."));

            return count > 0;
        }

        /// <inheritdoc/>
        void IMultiSetArgs.Invoke()
        {
            if (_items is not null && _items.Count == 1)
                _result(_items[0]);
        }
    }
}