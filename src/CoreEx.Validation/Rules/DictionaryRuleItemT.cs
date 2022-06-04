// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides validation configuration for an item (<see cref="KeyValuePair"/>) within a <see cref="DictionaryRule{TEntity, TProperty}"/>.
    /// </summary>
    /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    public sealed class DictionaryRuleItem<TKey, TValue> : IDictionaryRuleItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryRuleItem{TKey, TValue}"/> class with a corresponding <paramref name="valueValidator"/>.
        /// </summary>
        /// <param name="keyValidator">The corresponding key <see cref="IValidatorEx{TKey}"/>.</param>
        /// <param name="valueValidator">The corresponding value <see cref="IValidatorEx{TValue}"/>.</param>
        internal DictionaryRuleItem(IValidatorEx<TKey>? keyValidator, IValidatorEx<TValue>? valueValidator)
        {
            KeyValidator = keyValidator;
            ValueValidator = valueValidator;
        }

        /// <inheritdoc/>
        IValidatorEx? IDictionaryRuleItem.KeyValidator => KeyValidator;

        /// <summary>
        /// Gets the corresponding value <see cref="IValidatorEx{TValue}"/>.
        /// </summary>
        public IValidatorEx<TKey>? KeyValidator { get; private set; }

        /// <inheritdoc/>
        IValidatorEx? IDictionaryRuleItem.ValueValidator => ValueValidator;

        /// <summary>
        /// Gets the corresponding value <see cref="IValidatorEx{TValue}"/>.
        /// </summary>
        public IValidatorEx<TValue>? ValueValidator { get; private set; }

        /// <inheritdoc/>
        public Type ItemType => typeof(KeyValuePair<TKey, TValue>);

        /// <inheritdoc/>
        public Type KeyType => typeof(TKey);

        /// <inheritdoc/>
        public Type ValueType => typeof(TValue);
    }
}