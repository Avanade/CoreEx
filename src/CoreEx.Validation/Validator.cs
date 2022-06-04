// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Validation.Rules;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides access to the validator capabilities.
    /// </summary>
    public static class Validator
    {
        /// <summary>
        /// Creates a <see cref="Validator{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <returns>A <see cref="Validator{TEntity}"/>.</returns>
        public static Validator<TEntity> Create<TEntity>() where TEntity : class => new();

        /// <summary>
        /// Creates (or gets) an instance of the validator.
        /// </summary>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>; defaults to <see cref="ExecutionContext.ServiceProvider"/> where not specified.</param>
        /// <returns>The <typeparamref name="TValidator"/> instance.</returns>
        public static TValidator Create<TValidator>(IServiceProvider? serviceProvider = null) where TValidator : IValidatorEx
            => (serviceProvider == null ? ExecutionContext.GetService<TValidator>() : serviceProvider.GetService<TValidator>())
                ?? throw new InvalidOperationException($"Attempted to get service '{typeof(TValidator).FullName}' but null was returned; this would indicate that the service has not been configured correctly.");

        /// <summary>
        /// Creates a <see cref="CollectionValidator{TColl, TItem}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <param name="minCount">The minimum count.</param>
        /// <param name="maxCount">The maximum count.</param>
        /// <param name="item">The item <see cref="ICollectionRuleItem"/> configuration.</param>
        /// <param name="allowNullItems">Indicates whether the underlying collection item must not be null.</param>
        /// <returns>The <see cref="CollectionValidator{TColl, TItem}"/>.</returns>
        public static CollectionValidator<TColl, TItem> CreateCollection<TColl, TItem>(int minCount = 0, int? maxCount = null, ICollectionRuleItem? item = null, bool allowNullItems = false) where TColl : class, IEnumerable<TItem> => 
            new() { MinCount = minCount, MaxCount = maxCount, Item = item, AllowNullItems = allowNullItems };

        /// <summary>
        /// Creates a <see cref="DictionaryValidator{TDict, TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TDict">The dictionary <see cref="Type"/>.</typeparam>
        /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="minCount">The minimum count.</param>
        /// <param name="maxCount">The maximum count.</param>
        /// <param name="item">The item <see cref="ICollectionRuleItem"/> configuration.</param>
        /// <param name="allowNullKeys">Indicates whether the underlying dictionary key can be null.</param>
        /// <param name="allowNullValues">Indicates whether the underlying dictionary value can be null.</param>
        /// <returns>The <see cref="CollectionValidator{TColl, TItem}"/>.</returns>
        public static DictionaryValidator<TDict, TKey, TValue> CreateDictionary<TDict, TKey, TValue>(int minCount = 0, int? maxCount = null, IDictionaryRuleItem? item = null, bool allowNullKeys = false, bool allowNullValues = false) where TDict : class, IDictionary<TKey, TValue> =>
            new() { MinCount = minCount, MaxCount = maxCount, Item = item, AllowNullKeys = allowNullKeys, AllowNullValues = allowNullValues };

        /// <summary>
        /// Creates a <see cref="CommonValidator{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="validator">An action with the <see cref="CommonValidator{T}"/>.</param>
        /// <returns>The <see cref="CommonValidator{T}"/>.</returns>
        public static CommonValidator<T> CreateCommon<T>(Action<CommonValidator<T>> validator) => CommonValidator.Create<T>(validator);
    }
}