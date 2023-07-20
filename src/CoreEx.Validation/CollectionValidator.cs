// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Localization;
using CoreEx.Results;
using CoreEx.Validation.Rules;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides collection validation.
    /// </summary>
    /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    public class CollectionValidator<TColl, TItem> : ValidatorBase<TColl> where TColl : class, IEnumerable<TItem>
    {
        private ICollectionRuleItem? _item;
        private Func<ValidationContext<TColl>, Task<Result>>? _additionalAsync;

        /// <summary>
        /// Gets or sets the minimum count;
        /// </summary>
        public int MinCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum count.
        /// </summary>
        public int? MaxCount { get; set; }

        /// <summary>
        /// Indicates whether the underlying collection items can be null.
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

                if (typeof(TItem) != value.ItemType)
                    throw new ArgumentException($"A CollectionRule TProperty ItemType '{typeof(TItem).Name}' must be the same as the Item {value.ItemType.Name}");

                _item = value;
            }
        }

        /// <summary>
        /// Gets or sets the friendly text name used in validation messages.
        /// </summary>
        /// <remarks>Defaults to the <see cref="ValidationArgs.FullyQualifiedEntityName"/> formatted as sentence case where specified; otherwise, 'Value'.</remarks>
        public LText? Text { get; set; }

        /// <inheritdoc/>
        public override Task<ValidationContext<TColl>> ValidateAsync(TColl? value, ValidationArgs? args = null, CancellationToken cancellationToken = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return ValidationInvoker.Current.InvokeAsync(this, async cancellationToken =>
            {
                args ??= new ValidationArgs();
                if (string.IsNullOrEmpty(args.FullyQualifiedEntityName))
                    args.FullyQualifiedEntityName = Validation.ValueNameDefault;

                if (string.IsNullOrEmpty(args.FullyQualifiedEntityName))
                    args.FullyQualifiedJsonEntityName = Validation.ValueNameDefault;

                var context = new ValidationContext<TColl>(value, args);

                var i = 0;
                var hasNullItem = false;
                var hasItemErrors = false;
                foreach (var item in value)
                {
                    if (!AllowNullItems && item == null)
                        hasNullItem = true;

                    // Validate and merge.
                    if (item != null && Item?.ItemValidator != null)
                    {
                        var name = $"[{i}]";
                        var ic = new PropertyContext<TColl, TItem>(context, item, name, name);
                        var ia = ic.CreateValidationArgs();
                        var ir = await Item.ItemValidator.ValidateAsync(item, ia, cancellationToken).ConfigureAwait(false);
                        context.MergeResult(ir);
                        if (context.FailureResult is not null)
                            return context;

                        if (ir.HasErrors)
                            hasItemErrors = true;
                    }

                    i++;
                }

                var text = new Lazy<LText>(() => Text ?? PropertyExpression.ConvertToSentenceCase(args?.FullyQualifiedEntityName) ?? PropertyExpression.ConvertToSentenceCase(Validation.ValueNameDefault)!);
                if (hasNullItem)
                    context.AddMessage(Entities.MessageType.Error, ValidatorStrings.CollectionNullItemFormat, new object?[] { text.Value, null });

                // Check the length/count.
                if (i < MinCount)
                    context.AddMessage(Entities.MessageType.Error, ValidatorStrings.MinCountFormat, new object?[] { text.Value, null, MinCount });
                else if (MaxCount.HasValue && i > MaxCount.Value)
                    context.AddMessage(Entities.MessageType.Error, ValidatorStrings.MaxCountFormat, new object?[] { text.Value, null, MaxCount });

                // Check for duplicates.
                if (!hasItemErrors && Item != null)
                {
                    var pctx = new PropertyContext<TColl, TColl>(text.Value, context, value);
                    Item.DuplicateValidation(pctx, context.Value);
                }

                if (context.FailureResult is not null)
                    return context;

                var result = await OnValidateAsync(context).ConfigureAwait(false);
                if (result.IsSuccess && _additionalAsync != null)
                    result = await _additionalAsync(context).ConfigureAwait(false);

                context.SetFailureResult(result);
                return context;
            }, cancellationToken);
        }

        /// <summary>
        /// Validate the entity value (post all configured property rules) enabling additional validation logic to be added by the inheriting classes.
        /// </summary>
        /// <param name="context">The <see cref="ValidationContext{TEntity}"/>.</param>
        /// <returns>The corresponding <see cref="Result"/>.</returns>
        protected virtual Task<Result> OnValidateAsync(ValidationContext<TColl> context) => Task.FromResult(Result.Success);

        /// <summary>
        /// Validate the entity value (post all configured property rules) enabling additional validation logic to be added.
        /// </summary>
        /// <param name="additionalAsync">The asynchronous function to invoke.</param>
        /// <returns>The <see cref="CollectionValidator{TColl, TItem}"/>.</returns>
        public CollectionValidator<TColl, TItem> AdditionalAsync(Func<ValidationContext<TColl>, Task<Result>> additionalAsync)
        {
            if (_additionalAsync != null)
                throw new InvalidOperationException("Additional can only be defined once for a CollectionValidator.");

            _additionalAsync = additionalAsync ?? throw new ArgumentNullException(nameof(additionalAsync));
            return this;
        }
    }
}