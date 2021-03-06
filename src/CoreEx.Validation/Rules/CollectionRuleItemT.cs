// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Abstractions.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CoreEx.Localization;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides validation configuration for an item within a <see cref="CollectionRule{TEntity, TProperty}"/>.
    /// </summary>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    public sealed class CollectionRuleItem<TItem> : ICollectionRuleItem
    {
        private bool _duplicateCheck = false;
        private IPropertyExpression? _propertyExpression;
        private LText? _duplicateText = null;
        private bool _ignoreWherePrimaryKeyIsInitial = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionRuleItem{TItem}"/> class with a corresponding <paramref name="validator"/>.
        /// </summary>
        /// <param name="validator">The corresponding item <see cref="IValidatorEx{TItem}"/>.</param>
        internal CollectionRuleItem(IValidatorEx<TItem>? validator) => ItemValidator = validator;

        /// <summary>
        /// Gets the corresponding item <see cref="IValidatorEx"/>.
        /// </summary>
        IValidatorEx? ICollectionRuleItem.ItemValidator => ItemValidator;

        /// <summary>
        /// Gets the corresponding item <see cref="IValidatorEx{TItemEntity}"/>.
        /// </summary>
        public IValidatorEx<TItem>? ItemValidator { get; private set; }

        /// <summary>
        /// Gets the item <see cref="Type"/>.
        /// </summary>
        public Type ItemType => typeof(TItem);

        /// <summary>
        /// Specifies that the collection is to be checked for duplicates using the item's <see cref="IPrimaryKey"/> value.
        /// </summary>
        /// <param name="duplicateText">The duplicate text <see cref="LText"/> to be passed for the error message; defaults to <see cref="ValidatorStrings.PrimaryKey"/>.</param>
        /// <returns>The <see cref="CollectionRuleItem{TItemEntity}"/> instance to support chaining/fluent.</returns>
        public CollectionRuleItem<TItem> PrimaryKeyDuplicateCheck(LText? duplicateText = null)
        {
            if (_duplicateCheck)
                throw new InvalidOperationException("A DuplicateCheck or PrimaryKeyDuplicateCheck can only be specified once.");

            if (ItemType.GetInterface(typeof(IPrimaryKey).Name) == null)
                throw new InvalidOperationException($"A CollectionRuleItem ItemType '{ItemType.Name}' must implement '{nameof(IPrimaryKey)}' to support {nameof(PrimaryKeyDuplicateCheck)}.");

            _duplicateText = string.IsNullOrEmpty(duplicateText) ? ValidatorStrings.PrimaryKey : duplicateText;
            _duplicateCheck = true;
            _ignoreWherePrimaryKeyIsInitial = false;

            return this;
        }

        /// <summary>
        /// Specifies that the collection is to be checked for duplicates using the item's <see cref="IPrimaryKey"/> value with an option to <paramref name="ignoreWherePrimaryKeyIsInitial"/>.
        /// </summary>
        /// <param name="ignoreWherePrimaryKeyIsInitial">Indicates whether to ignore the <see cref="IPrimaryKey.PrimaryKey"/> where <see cref="CompositeKey.IsInitial"/>; useful where the primary key will be generated by the underlying data source on create.</param>
        /// <param name="duplicateText">The duplicate text <see cref="LText"/> to be passed for the error message; defaults to <see cref="ValidatorStrings.PrimaryKey"/>.</param>
        /// <returns>The <see cref="CollectionRuleItem{TItemEntity}"/> instance to support chaining/fluent.</returns>
        public CollectionRuleItem<TItem> PrimaryKeyDuplicateCheck(bool ignoreWherePrimaryKeyIsInitial, LText? duplicateText = null)
        {
            PrimaryKeyDuplicateCheck(duplicateText);
            _ignoreWherePrimaryKeyIsInitial = ignoreWherePrimaryKeyIsInitial;
            return this;
        }

        /// <summary>
        /// Specifies that the collection is to be checked for duplicates using the specified item property.
        /// </summary>
        /// <typeparam name="TItemProperty">The item property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the item property that is being duplicate checked.</param>
        /// <param name="duplicateText">The duplicate text <see cref="LText"/> to be passed for the error message (default is to derive the text from the property itself where possible).</param>
        /// <returns>The <see cref="CollectionRuleItem{TItemEntity}"/> instance to support chaining/fluent.</returns>
        public CollectionRuleItem<TItem> DuplicateCheck<TItemProperty>(Expression<Func<TItem, TItemProperty>> propertyExpression, LText? duplicateText = null)
        {
            if (_duplicateCheck)
                throw new InvalidOperationException("A DuplicateCheck or PrimaryKeyDuplicateCheck can only be specified once.");

            _propertyExpression = PropertyExpression.Create(propertyExpression);
            _duplicateText = duplicateText ?? _propertyExpression.Text;
            _duplicateCheck = true;

            return this;
        }

        /// <summary>
        /// Performs the duplicate validation check.
        /// </summary>
        /// <param name="context">The <see cref="IPropertyContext"/>.</param>
        /// <param name="items">The items to duplicate check.</param>
        void ICollectionRuleItem.DuplicateValidation(IPropertyContext context, IEnumerable? items) => DuplicateValidation(context, (IEnumerable<TItem>?)items);

        /// <summary>
        /// Performs the duplicate validation check.
        /// </summary>
        /// <param name="context">The <see cref="IPropertyContext"/>.</param>
        /// <param name="items">The items to duplicate check.</param>
        private void DuplicateValidation(IPropertyContext context, IEnumerable<TItem>? items)
        {
            if (!_duplicateCheck || items == null)
                return;

            if (_propertyExpression == null)
            {
                var dict = new Dictionary<CompositeKey, object?>(CompositeKeyComparer.Default);
                foreach (var item in items.Where(x => x != null).Cast<IPrimaryKey>())
                {
                    if (_ignoreWherePrimaryKeyIsInitial && (item.PrimaryKey == null || item.PrimaryKey.IsInitial))
                        continue;

                    if (dict.ContainsKey(item.PrimaryKey))
                    {
                        if (item.PrimaryKey.Args.Length == 1)
                            context.CreateErrorMessage(ValidatorStrings.DuplicateValueFormat, _duplicateText!, item.PrimaryKey.Args[0]);
                        else
                            context.CreateErrorMessage(ValidatorStrings.DuplicateValue2Format, _duplicateText!);

                        return;
                    }

                    dict.Add(item.PrimaryKey, null);
                }
            }
            else
            {
                var dict = new Dictionary<object?, object?>();
                foreach (var item in items.Where(x => x != null))
                {
                    var val = _propertyExpression.GetValue(item!);
                    if (dict.ContainsKey(val))
                    {
                        context.CreateErrorMessage(ValidatorStrings.DuplicateValueFormat, _duplicateText!, val!);
                        return;
                    }

                    dict.Add(val, null);
                }
            }
        }
    }
}