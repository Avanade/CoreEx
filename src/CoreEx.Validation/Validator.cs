// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Validation.Rules;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides access to the validator capabilities.
    /// </summary>
    public static class Validator
    {
        /// <summary>
        /// Create a <see cref="Validator{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <returns>A <see cref="Validator{TEntity}"/>.</returns>
        public static Validator<TEntity> Create<TEntity>() where TEntity : class => new();

        /// <summary>
        /// Create (or get) an instance of the pre-registered validator.
        /// </summary>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>; defaults to <see cref="ExecutionContext.ServiceProvider"/> where not specified.</param>
        /// <returns>The <typeparamref name="TValidator"/> instance.</returns>
        public static TValidator Create<TValidator>(IServiceProvider? serviceProvider = null) where TValidator : IValidatorEx
            => (serviceProvider == null ? ExecutionContext.GetService<TValidator>() : serviceProvider.GetService<TValidator>())
                ?? throw new InvalidOperationException($"Attempted to get service '{typeof(TValidator).FullName}' but null was returned; this would indicate that the service has not been configured correctly.");

        /// <summary>
        /// Create <typeparamref name="T"/> value validator (see <see cref="CommonValidator{T}"/>).
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="validator">An action with the <see cref="CommonValidator{T}"/> to enable further configuration.</param>
        /// <returns>The <see cref="CommonValidator{T}"/>.</returns>
        /// <remarks>This is a synonym for the <see cref="CommonValidator.Create{T}(Action{CommonValidator{T}})"/>.</remarks>
        public static CommonValidator<T> CreateFor<T>(Action<CommonValidator<T>>? validator = null) => CommonValidator.Create(validator);

        /// <summary>
        /// Create a collection-based <see cref="CommonValidator{T}"/> where the <see cref="ICollectionRuleItem"/> can be specified.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="minCount">The minimum count.</param>
        /// <param name="maxCount">The maximum count.</param>
        /// <param name="item">The item <see cref="ICollectionRuleItem"/> configuration.</param>
        /// <param name="allowNullItems">Indicates whether the underlying collection item must not be null.</param>
        /// <returns>The <see cref="CommonValidator{T}"/> for the collection.</returns>
        public static CommonValidator<TColl> CreateForCollection<TColl>(int minCount = 0, int? maxCount = null, ICollectionRuleItem? item = null, bool allowNullItems = false) where TColl : class, IEnumerable?
            => CreateFor<TColl>(v => v.Collection(minCount, maxCount, item, allowNullItems));

        /// <summary>
        /// Create a collection-based <see cref="CommonValidator{T}"/> for the specified <paramref name="itemValidator"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <param name="itemValidator">The item <see cref="IValidatorEx{T}"/>.</param>
        /// <param name="minCount">The minimum count.</param>
        /// <param name="maxCount">The maximum count.</param>
        /// <param name="allowNullItems">Indicates whether the underlying collection item must not be null.</param>
        /// <returns>The <see cref="CommonValidator{T}"/> for the collection.</returns>
        public static CommonValidator<TColl> CreateForCollection<TColl, TItem>(IValidatorEx<TItem> itemValidator, int minCount = 0, int? maxCount = null, bool allowNullItems = false) where TColl : class, IEnumerable<TItem>?
            => CreateFor<TColl>(v => v.Collection(itemValidator, minCount, maxCount, allowNullItems));

        /// <summary>
        /// Create a dictionary-based <see cref="CommonValidator{T}"/> where the <see cref="IDictionaryRuleItem"/> can be specified.
        /// </summary>
        /// <typeparam name="TDict">The dictionary <see cref="Type"/>.</typeparam>
        /// <param name="minCount">The minimum count.</param>
        /// <param name="maxCount">The maximum count.</param>
        /// <param name="item">The item <see cref="IDictionaryRuleItem"/> configuration.</param>
        /// <param name="allowNullKeys">Indicates whether the underlying dictionary key can be null.</param>
        /// <param name="allowNullValues">Indicates whether the underlying dictionary value can be null.</param>
        /// <returns>The <see cref="CommonValidator{T}"/> for the dictionary.</returns>
        public static CommonValidator<TDict> CreateForDictionary<TDict>(int minCount = 0, int? maxCount = null, IDictionaryRuleItem? item = null, bool allowNullKeys = false, bool allowNullValues = false) where TDict : class, IDictionary
            => CreateFor<TDict>(v => v.Dictionary(minCount, maxCount, item, allowNullKeys, allowNullValues));

        /// <summary>
        /// Create a dictionary-based <see cref="CommonValidator{T}"/> for the specified <paramref name="keyValidator"/> and <paramref name="valueValidator"/>.
        /// </summary>
        /// <typeparam name="TDict">The dictionary <see cref="Type"/>.</typeparam>
        /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="keyValidator">The key <see cref="IValidatorEx{T}"/>.</param>
        /// <param name="valueValidator">The value <see cref="IValidatorEx{T}"/>.</param>
        /// <param name="minCount">The minimum count.</param>
        /// <param name="maxCount">The maximum count.</param>
        /// <param name="allowNullKeys">Indicates whether the underlying dictionary key can be null.</param>
        /// <param name="allowNullValues">Indicates whether the underlying dictionary value can be null.</param>
        /// <returns>The <see cref="CommonValidator{T}"/> for the dictionary.</returns>
        public static CommonValidator<TDict> CreateForDictionary<TDict, TKey, TValue>(IValidatorEx<TKey>? keyValidator, IValidatorEx<TValue>? valueValidator, int minCount = 0, int? maxCount = null, bool allowNullKeys = false, bool allowNullValues = false) where TDict : Dictionary<TKey, TValue>? where TKey : notnull
            => CreateFor<TDict>(v => v.Dictionary(keyValidator, valueValidator, minCount, maxCount, allowNullKeys, allowNullValues));

        /// <summary>
        /// Create a dictionary-based <see cref="CommonValidator{T}"/> for the specified <paramref name="valueValidator"/>.
        /// </summary>
        /// <typeparam name="TDict">The dictionary <see cref="Type"/>.</typeparam>
        /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="valueValidator">The value <see cref="IValidatorEx{T}"/>.</param>
        /// <param name="minCount">The minimum count.</param>
        /// <param name="maxCount">The maximum count.</param>
        /// <param name="allowNullKeys">Indicates whether the underlying dictionary key can be null.</param>
        /// <param name="allowNullValues">Indicates whether the underlying dictionary value can be null.</param>
        /// <returns>The <see cref="CommonValidator{T}"/> for the dictionary.</returns>
        public static CommonValidator<TDict> CreateForDictionary<TDict, TKey, TValue>(IValidatorEx<TValue>? valueValidator, int minCount = 0, int? maxCount = null, bool allowNullKeys = false, bool allowNullValues = false) where TDict : Dictionary<TKey, TValue>? where TKey : notnull
            => CreateFor<TDict>(v => v.Dictionary((IValidatorEx<TKey>?)null, valueValidator, minCount, maxCount, allowNullKeys, allowNullValues));

        /// <summary>
        /// Creates a <c>null</c> <see cref="IValidatorEx{T}"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <returns>A <c>null</c> <see cref="IValidatorEx{T}"/>; i.e. simply <c>null</c>.</returns>
        public static IValidatorEx<T>? Null<T>() => null;
    }
}