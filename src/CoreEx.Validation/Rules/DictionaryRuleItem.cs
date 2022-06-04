// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides the means to create a <see cref="DictionaryRuleItem{TKey, TValue}"/> instance.
    /// </summary>
    public static class DictionaryRuleItem
    {
        /// <summary>
        /// Create an instance of the <see cref="DictionaryRuleItem{TKey, TValue}"/> class.
        /// </summary>
        /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The corresponding value <see cref="IValidatorEx{TValue}"/>.</param>
        /// <param name="value">The corresponding value <see cref="IValidatorEx{TValue}"/>.</param>
        /// <returns>The <see cref="DictionaryRuleItem{TKey, TValue}"/>.</returns>
        public static DictionaryRuleItem<TKey, TValue> Create<TKey, TValue>(IValidatorEx<TKey>? key = null, IValidatorEx<TValue>? value = null) => new(key, value);
    }
}