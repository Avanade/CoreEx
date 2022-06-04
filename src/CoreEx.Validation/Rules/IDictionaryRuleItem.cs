// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Enables the validation configuration for an item (<see cref="KeyValuePair"/>) within a <see cref="DictionaryRule{TEntity, TProperty}"/>.
    /// </summary>
    public interface IDictionaryRuleItem
    {
        /// <summary>
        /// Gets the corresponding key <see cref="IValidatorEx"/>.
        /// </summary>
        IValidatorEx? KeyValidator { get; }

        /// <summary>
        /// Gets the corresponding value <see cref="IValidatorEx"/>.
        /// </summary>
        IValidatorEx? ValueValidator { get; }

        /// <summary>
        /// Gets the item <see cref="Type"/>.
        /// </summary>
        Type ItemType { get; }

        /// <summary>
        /// Gets the key <see cref="Type"/>.
        /// </summary>
        Type KeyType { get; }

        /// <summary>
        /// Gets the value <see cref="Type"/>.
        /// </summary>
        Type ValueType { get; }
    }
}