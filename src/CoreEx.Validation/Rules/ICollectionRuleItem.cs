// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Enables the validation configuration for an item within a <see cref="CollectionRule{TEntity, TProperty}"/>.
    /// </summary>
    public interface ICollectionRuleItem
    {
        /// <summary>
        /// Gets the corresponding item <see cref="IValidatorEx"/>.
        /// </summary>
        IValidatorEx? ItemValidator { get; }

        /// <summary>
        /// Gets the item <see cref="Type"/>.
        /// </summary>
        Type ItemType { get; }

        /// <summary>
        /// Performs the duplicate validation check.
        /// </summary>
        /// <param name="context">The <see cref="IPropertyContext"/>.</param>
        /// <param name="items">The items to duplicate check.</param>
        void DuplicateValidation(IPropertyContext context, IEnumerable? items);
    }
}