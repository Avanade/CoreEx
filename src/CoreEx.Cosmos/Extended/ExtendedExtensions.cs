// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System.Collections.Generic;
using System;

namespace CoreEx.Cosmos.Extended
{
    /// <summary>
    /// Provides extended extension methods.
    /// </summary>
    public static class ExtendedExtensions
    {
        /// <summary>
        /// Creates a <see cref="MultiSetSingleArgs{T, TModel}"/> for the <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbValueContainer{T, TModel}"/>.</param>
        /// <param name="result">The action that will be invoked with the result of the set.</param>
        /// <param name="isMandatory">Indicates whether the value is mandatory; defaults to <c>true</c>.</param>
        /// <param name="stopOnNull">Indicates whether to stop further result set processing where the current set has resulted in a null (i.e. no items).</param>
        /// <returns>The <see cref="MultiSetSingleArgs{T, TModel}"/>.</returns>
        /// <remarks>Used by <see cref="CosmosDb.SelectMultiSetWithResultAsync(Microsoft.Azure.Cosmos.PartitionKey, string?, IEnumerable{IMultiSetArgs}, System.Threading.CancellationToken)"/>.</remarks>
        public static MultiSetSingleArgs<T, TModel> CreateMultiSetSingleArgs<T, TModel>(this CosmosDbValueContainer<T, TModel> container, Action<T> result, bool isMandatory = true, bool stopOnNull = false) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => new(container, result, isMandatory, stopOnNull);

        /// <summary>
        /// Creates a <see cref="MultiSetCollArgs{T, TModel}"/> for the <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbValueContainer{T, TModel}"/>.</param>
        /// <param name="result">The action that will be invoked with the result of the set.</param>
        /// <param name="minItems">The minimum number of items allowed.</param>
        /// <param name="maxItems">The maximum numner of items allowed.</param>
        /// <param name="stopOnNull">Indicates whether to stop further result set processing where the current set has resulted in a null (i.e. no items).</param>
        /// <returns>The <see cref="MultiSetCollArgs{T, TModel}"/>.</returns>
        /// <remarks>Used by <see cref="CosmosDb.SelectMultiSetWithResultAsync(Microsoft.Azure.Cosmos.PartitionKey, string?, IEnumerable{IMultiSetArgs}, System.Threading.CancellationToken)"/>.</remarks>
        public static MultiSetCollArgs<T, TModel> CreateMultiSetCollArgs<T, TModel>(this CosmosDbValueContainer<T, TModel> container, Action<IEnumerable<T>> result, int minItems = 0, int? maxItems = null, bool stopOnNull = false) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => new(container, result, minItems, maxItems, stopOnNull);
    }
}