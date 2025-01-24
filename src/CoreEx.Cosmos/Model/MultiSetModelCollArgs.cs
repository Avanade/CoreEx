// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreEx.Cosmos.Model
{
    /// <summary>
    /// Provides the <b>CosmosDb</b> <see cref="IMultiSetModelArgs"/> when expecting a collection of items.
    /// </summary>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    public class MultiSetModelCollArgs<TModel> : IMultiSetModelArgs where TModel : class, ICosmosDbType, IEntityKey, new()
    {
        private List<TModel>? _items;
        private readonly Action<IEnumerable<TModel>> _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiSetModelCollArgs{TModel}"/> class.
        /// </summary>
        /// <param name="result">The action that will be invoked with the result of the set.</param>
        /// <param name="minItems">The minimum number of items allowed.</param>
        /// <param name="maxItems">The maximum numner of items allowed.</param>
        /// <param name="stopOnNull">Indicates whether to stop further result set processing where the current set has resulted in a null (i.e. no items).</param>
        public MultiSetModelCollArgs(Action<IEnumerable<TModel>> result, int minItems = 0, int? maxItems = null, bool stopOnNull = false)
        {
            _result = result.ThrowIfNull(nameof(result));
            if (maxItems.HasValue && minItems <= maxItems.Value)
                throw new ArgumentException("Max Items is less than Min Items.", nameof(maxItems));

            MinItems = minItems;
            MaxItems = maxItems;
            StopOnNull = stopOnNull;
        }

        /// <inheritdoc/>
        public int MinItems { get; }

        /// <inheritdoc/>
        public int? MaxItems { get; }

        /// <inheritdoc/>
        public bool StopOnNull { get; set; }

        /// <inheritdoc/>
        Type IMultiSetArgs.Type => typeof(TModel);

        /// <inheritdoc/>
        Result IMultiSetArgs.AddItem(CosmosDbContainer container, CosmosDbArgs dbArgs, object item)
            => MultiSetModelSingleArgs<TModel>.Validate(container, dbArgs, (TModel)item)
                .WhenAs(m => m is not null, m =>
                {
                    _items ??= [];
                    _items.Add(m);
                    return !MaxItems.HasValue || _items.Count <= MaxItems.Value
                        ? Result.Success
                        : Result.Fail(new InvalidOperationException($"MultiSetCollArgs has returned more items ({_items.Count}) than expected ({MaxItems.Value})."));
                });

        /// <inheritdoc/>
        Result<bool> IMultiSetArgs.Verify()
        {
            var count = _items?.Count ?? 0;
            if (count < MinItems)
                return Result.Fail(new InvalidOperationException($"MultiSetCollArgs has returned less items ({count}) than expected ({MinItems})."));

            return count > 0;
        }

        /// <inheritdoc/>
        void IMultiSetArgs.Invoke()
        {
            if (_items is not null)
                _result(_items.AsEnumerable());
        }

        /// <inheritdoc/>
        string IMultiSetArgs.GetModelName(CosmosDbContainer container) => container.Model.GetModelName<TModel>();
    }
}