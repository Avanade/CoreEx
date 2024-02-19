// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides the means to create a <see cref="CollectionRuleItem{TItemEntity}"/> instance.
    /// </summary>
    public static class CollectionRuleItem
    {
        /// <summary>
        /// Create an instance of the <see cref="CollectionRuleItem{TItem}"/> class with no <see cref="Validator"/>.
        /// </summary>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <returns>The <see cref="CollectionRuleItem{TItem}"/>.</returns>
        public static CollectionRuleItem<TItem> Create<TItem>() => new(null);

        /// <summary>
        /// Create an instance of the <see cref="CollectionRuleItem{TItem}"/> class with a corresponding <paramref name="validator"/>.
        /// </summary>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <param name="validator">The corresponding item <see cref="IValidatorEx{TItem}"/>.</param>
        /// <returns>The <see cref="CollectionRuleItem{TItem}"/>.</returns>
        public static CollectionRuleItem<TItem> Create<TItem>(IValidatorEx<TItem> validator) => new(validator.ThrowIfNull(nameof(validator)));

        /// <summary>
        /// Create an instance of the <see cref="CollectionRuleItem{TItem}"/> class leveraging the <paramref name="serviceProvider"/> to get the instance.
        /// </summary>
        /// <typeparam name="TItem">The item entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The item validator <see cref="Type"/>.</typeparam>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>; defaults to <see cref="ExecutionContext.ServiceProvider"/> where not specified.</param>
        /// <returns>The <see cref="CollectionRuleItem{TItem}"/>.</returns>
        public static CollectionRuleItem<TItem> Create<TItem, TValidator>(IServiceProvider? serviceProvider = null) where TValidator : IValidatorEx<TItem> => new(Validator.Create<TValidator>(serviceProvider));
    }
}