// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides collection validation including <see cref="MinCount"/> and <see cref="MaxCount"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The collection property <see cref="Type"/>.</typeparam>
    public class CollectionRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class where TProperty : IEnumerable?
    {
        private readonly Type _itemType;
        private ICollectionRuleItem? _item;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionRule{TEntity, TProperty}"/> class.
        /// </summary>
        public CollectionRule() => _itemType = TypeReflector.GetCollectionItemType(typeof(TProperty)).ItemType!;

        /// <summary>
        /// Gets or sets the minimum count.
        /// </summary>
        public int MinCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum count.
        /// </summary>
        public int? MaxCount { get; set; }

        /// <summary>
        /// Indicates whether the underlying collection items can be <c>null</c>.
        /// </summary>
        public bool AllowNullItems { get; set; }

        /// <summary>
        /// Gets or sets the collection item validation configuration.
        /// </summary>
        public ICollectionRuleItem? Item
        {
            get => _item;

            set
            {
                if (value == null)
                {
                    _item = value;
                    return;
                }

                if (_itemType != value.ItemType)
                    throw new ArgumentException($"A CollectionRule TProperty ItemType '{_itemType.Name}' must be the same as the Item {value.ItemType.Name}");

                _item = value;
            }
        }

        /// <summary>
        /// Overrides the <b>Check</b> method and will not validate where performing a shallow validation.
        /// </summary>
        /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
        /// <returns><c>true</c> where validation is to continue; otherwise, <c>false</c> to stop.</returns>
        protected override bool Check(PropertyContext<TEntity, TProperty> context) => !context.ThrowIfNull(nameof(context)).Parent.ShallowValidation && base.Check(context);

        /// <inheritdoc/>
        protected override async Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            if (context.Value == null)
                return;

            // Where only validating count on an icollection do it quickly and exit.
            if (AllowNullItems && Item is null && context.Value is ICollection coll)
            {
                if (coll.Count < MinCount)
                    context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MinCountFormat, MinCount);
                else if (MaxCount.HasValue && coll.Count > MaxCount.Value)
                    context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MaxCountFormat, MaxCount);

                return;
            }

            // Iterate through the collection validating each of the items.
            var i = 0;
            var hasNullItem = false;
            var hasItemErrors = false;
            foreach (var item in context.Value)
            {
                // Create the context args.
                var args = context.CreateValidationArgs();
                var indexer = $"[{i++}]";
                args.FullyQualifiedEntityName += indexer;
                args.FullyQualifiedJsonEntityName += indexer;

                if (!AllowNullItems && item == null)
                    hasNullItem = true;

                // Validate and merge.
                if (item != null && Item?.ItemValidator != null)
                {
                    var r = await Item.ItemValidator.ValidateAsync(item, args, cancellationToken).ConfigureAwait(false);
                    context.MergeResult(r);
                    if (r.HasErrors)
                        hasItemErrors = true;
                }
            }

            if (hasNullItem)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.CollectionNullItemFormat);

            // Check the length/count.
            if (i < MinCount)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MinCountFormat, MinCount);
            else if (MaxCount.HasValue && i > MaxCount.Value)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MaxCountFormat, MaxCount);

            // Check for duplicates.
            if (!hasItemErrors)
                Item?.DuplicateValidation(context, context.Value);
        }
    }
}