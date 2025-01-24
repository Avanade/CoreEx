﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
using System;
using System.Collections.Generic;

namespace CoreEx.Cosmos.Model
{
    /// <summary>
    /// Provides the <b>CosmosDb</b> <see cref="IMultiSetModelArgs"/> when expecting a single item only.
    /// </summary>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <param name="result">The action that will be invoked with the result of the set.</param>
    /// <param name="isMandatory">Indicates whether the value is mandatory; defaults to <c>true</c>.</param>
    /// <param name="stopOnNull">Indicates whether to stop further result set processing where the current set has resulted in a null (i.e. no items).</param>
    public class MultiSetModelSingleArgs<TModel>(Action<TModel> result, bool isMandatory = true, bool stopOnNull = false) : IMultiSetModelArgs where TModel : class, ICosmosDbType, IEntityKey, new()
    {
        private List<TModel>? _items;
        private readonly Action<TModel> _result = result.ThrowIfNull(nameof(result));

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
        Type IMultiSetArgs.Type => typeof(TModel);

        /// <inheritdoc/>
        Result IMultiSetArgs.AddItem(CosmosDbContainer container, CosmosDbArgs dbArgs, object item)
            => Validate(container, dbArgs, (TModel)item)
                .WhenAs(v => v is not null, v =>
                {
                    _items ??= [];
                    _items.Add(v);
                    return !MaxItems.HasValue || _items.Count <= MaxItems.Value
                        ? Result.Success
                        : Result.Fail(new InvalidOperationException($"MultiSetSingleArgs has returned more items ({_items.Count}) than expected ({MaxItems.Value})."));
                });

        /// <summary>
        /// Validate and map the <paramref name="model"/> to the <typeparamref name="TModel"/>.
        /// </summary>
        /// <param name="container">The <see cref="CosmosDbContainer"/>.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The <typeparamref name="TModel"/> value.</param>
        /// <returns>The validated and converted value.</returns>
        internal static Result<TModel> Validate(CosmosDbContainer container, CosmosDbArgs dbArgs, TModel model)
        {
            if (!container.Model.IsModelValid(model, dbArgs, true))
                return Result.Success;

            return model;
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

        /// <inheritdoc/>
        string IMultiSetArgs.GetModelName(CosmosDbContainer container) => container.Model.GetModelName<TModel>();
    }
}