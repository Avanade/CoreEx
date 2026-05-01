namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides a collection (<see cref="IEnumerable{T}"/>) validation including item-based validation and duplicate checking.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/> (<see cref="IEnumerable{T}"/>).</typeparam>
/// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
public sealed class CollectionRule<TEntity, TProperty, TItem> : PropertyRuleBase<TEntity, TProperty> where TEntity : class where TProperty : IEnumerable<TItem?>
{
    private readonly Func<PropertyContext<TEntity, TProperty>, int>? _minCount;
    private readonly Func<PropertyContext<TEntity, TProperty>, int?>? _maxCount;
    private readonly With _with;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionRule{TEntity, TProperty, TItem}"/> class.
    /// </summary>
    /// <param name="minCount">The minimum count.</param>
    /// <param name="maxCount">The maximum count.</param>
    /// <param name="with">Extends configuration <see cref="With"/>.</param>
    public CollectionRule(Func<PropertyContext<TEntity, TProperty>, int>? minCount, Func<PropertyContext<TEntity, TProperty>, int?>? maxCount, Func<With, With>? with)
    {
        _minCount = minCount;
        _maxCount = maxCount;

        var w = new With(this);
        _with = with?.Invoke(w) ?? w;
    }

    /// <inheritdoc/>
    protected async override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    { 
        var minCount = _minCount?.Invoke(context) ?? 0;
        var maxCount = _maxCount?.Invoke(context);

        if (minCount < 0)
            throw new InvalidOperationException("Minimum count must not be negative.");

        if (maxCount.HasValue)
        {
            if (maxCount.Value < 0)
                throw new InvalidOperationException("Maximum count must not be negative.");

            if (maxCount.Value < minCount)
                throw new InvalidOperationException("Maximum count must not be less than minimum count.");
        }

        await _with.ValidateAsync(context, minCount, maxCount, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Provides additional configuration options for the <see cref="CollectionRule{TEntity, TProperty, TItem}"/>.
    /// </summary>
    public sealed class With
    {
        private readonly CollectionRule<TEntity, TProperty, TItem> _rule;
        private Func<ValidationArgs, IValidatorEx<TItem>>? _getValidator;
        private bool _hasAllowNullItems;
#pragma warning disable CA1859 // Use concrete types when possible for improved performance; not applicable here as interface is needed.
        private IItemDuplicateCheck? _itemDuplicateCheck;
#pragma warning restore CA1859 // Use concrete types when possible for improved performance

        /// <summary>
        /// Initializes a new instance of the <see cref="With"/> class.
        /// </summary>
        internal With(CollectionRule<TEntity, TProperty, TItem> rule) => _rule = rule;

        /// <summary>
        /// Indicates that one or more items can be <see langword="null"/>.
        /// </summary>
        /// <returns>The <see cref="With"/> to support fluent-style method-chaining.</returns>
        public With AllowNullItems()
        {
            _hasAllowNullItems = true;
            return this;
        }

        /// <summary>
        /// Sets the specified <b>Item</b> <paramref name="configure"/>.
        /// </summary>
        /// <returns>The <see cref="With"/> to support fluent-style method-chaining.</returns>
        public With WithItemValidator(Action<InlineValidator<TItem>.Validator>? configure) => WithItemValidator(new ValidatingInlineValidator<TItem>(configure));

        /// <summary>
        /// Sets the specified <b>Item</b> <paramref name="validator"/>.
        /// </summary>
        /// <returns>The <see cref="With"/> to support fluent-style method-chaining.</returns>
        public With WithItemValidator(IValidatorEx<TItem> validator)
        {
            _getValidator = _getValidator is not null ? throw new InvalidOperationException("The collection rule can only have one validator.") : _ => validator.ThrowIfNull();
            return this;
        }

        /// <summary>
        /// Sets the specified <b>Item</b> <typeparamref name="TValidator"/> service (resolved at validation runtime).
        /// </summary>
        /// <typeparam name="TValidator">The property validator <see cref="Type"/>.</typeparam>
        /// <returns>The <see cref="With"/> to support fluent-style method-chaining.</returns>
        public With WithItemValidator<TValidator>() where TValidator : IValidatorEx<TItem>
        {
            _getValidator = _getValidator is not null ? throw new InvalidOperationException("The collection rule can only have one validator.") : args => CoreEx.Validation.Validator.Get<TValidator>(args.ServiceProvider);
            return this;
        }

        /// <summary>
        /// Sets the specified keyed <b>Item</b> <typeparamref name="TValidator"/> service (resolved at validation runtime).
        /// </summary>
        /// <typeparam name="TValidator">The property validator <see cref="Type"/>.</typeparam>
        /// <param name="serviceKey">The service key.</param>
        /// <returns>The <see cref="With"/> to support fluent-style method-chaining.</returns>
        public With WithItemKeyedValidator<TValidator>(object? serviceKey) where TValidator : IValidatorEx<TItem>
        {
            _getValidator = _getValidator is not null ? throw new InvalidOperationException("The collection rule can only have one validator.") : args => CoreEx.Validation.Validator.GetKeyed<TValidator>(serviceKey, args.ServiceProvider);
            return this;
        }

        /// <summary>
        /// Sets the generic duplicate checking logic.
        /// </summary>
        /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
        /// <param name="keySelector">The key selector.</param>
        /// <param name="comparer">The equality comparer.</param>
        /// <param name="duplicateText">The duplicate <see cref="LText"/> to be used in the error message.</param>
        /// <returns>The <see cref="With"/> to support fluent-style method-chaining.</returns>
        internal With WithDuplicateCheckingInternal<TKey>(Func<TItem, TKey> keySelector, IEqualityComparer<TKey>? comparer, Func<LText> duplicateText)
        {
            _itemDuplicateCheck = _itemDuplicateCheck is not null ? throw new InvalidOperationException("The collection rule can only have one duplicate checker.") : new ItemDuplicateCheck<TKey>(keySelector, comparer, duplicateText);
            return this;
        }

        /// <summary>
        /// Validates each item within the collection.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="minCount">The minimum count.</param>
        /// <param name="maxCount">The maximum count.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        internal async Task ValidateAsync(PropertyContext<TEntity, TProperty> context, int minCount, int? maxCount, CancellationToken cancellationToken)
        {
            // Fast path where only checking for count.
            if (_hasAllowNullItems && _getValidator is null && _itemDuplicateCheck is null && context.Value is ICollection coll)
            {
                PostEnumerationValidation(context, false, minCount, maxCount, coll.Count);
                return;
            }

            // Enumerate and validate each item.
            var index = 0;
            var hasNullItem = false;
            var hasItemError = false;
            var hasDuplicate = false;

            var duplicateChecker = _itemDuplicateCheck?.CreateDuplicateChecker();

            foreach (var item in context.Value!)
            {
                // Handle null item(s).
                if (item is null)
                {
                    if (!_hasAllowNullItems)
                        hasNullItem = true;

                    index++;
                    continue;
                }

                // Validate the item.
                var hasError = false;
                if (_getValidator is not null)
                {
                    // Create the context args.
                    var args = CreateValidationArgs(context, index);

                    var last = context.GetCollectionIndexSafe();
                    context.SetCollectionIndex(index);

                    // Validate the item and merge the result.
                    try
                    {
                        var r = await _getValidator.Invoke(args).ValidateAsync(item, args, cancellationToken).ConfigureAwait(false);

                        context.MergeResult(r);
                        if (r.HasErrors)
                            hasItemError = hasError = true;
                    }
                    finally
                    {
                        context.SetCollectionIndex(last);
                    }
                }

                // Check for duplicates where applicable.
                if (!hasError && !hasDuplicate && duplicateChecker?.IsDuplicate(item) is true)
                    hasDuplicate = true;

                index++;
            }

            // Check for duplicates and error accordingly.
            if (!hasItemError && hasDuplicate)
                context.AddError(_rule.ErrorText ?? ValidatorStrings.DuplicateValueFormat, _itemDuplicateCheck!.DuplicateText());

            // Perform the standard post enumeration validation.
            PostEnumerationValidation(context, hasNullItem, minCount, maxCount, index);
        }

        /// <summary>
        /// Creates the <see cref="ValidationArgs"/> for the specified <paramref name="index"/>.
        /// </summary>
        private static ValidationArgs CreateValidationArgs(PropertyContext<TEntity, TProperty> context, int index)
        {
            var args = context.CreateValidationArgs();
            var indexer = $"[{index}]";
            args.FullyQualifiedEntityName += indexer;
            args.FullyQualifiedJsonEntityName += indexer;
            return args;
        }

        /// <summary>
        /// Performs the standatd post enumeration validation.
        /// </summary>
        private void PostEnumerationValidation(PropertyContext<TEntity, TProperty> context, bool hasNullItem, int minCount, int? maxCount, int count)
        {
            // Emit the null item error.
            if (hasNullItem)
                context.AddError(_rule.ErrorText ?? ValidatorStrings.CollectionNullItemFormat);

            // Check the length/count.
            if (count < minCount)
                context.AddError(_rule.ErrorText ?? ValidatorStrings.MinCountFormat, minCount);
            else if (maxCount.HasValue && count > maxCount.Value)
                context.AddError(_rule.ErrorText ?? ValidatorStrings.MaxCountFormat, maxCount);
        }
    }

    /// <summary>
    /// Enables the duplicate checking configuration for items within a collection.
    /// </summary>
    internal interface IItemDuplicateCheck
    {
        /// <summary>
        /// Gets the duplicate <see cref="LText"/> to be used in the error message.
        /// </summary>
        Func<LText> DuplicateText { get; }

        /// <summary>
        /// Create the runtime <see cref="IItemDuplicateChecker"/>.
        /// </summary>
        /// <returns>The <see cref="IItemDuplicateChecker"/>.</returns>
        IItemDuplicateChecker CreateDuplicateChecker();
    }

    /// <summary>
    /// Enables the runtime duplicate checking for items within a collection.
    /// </summary>
    internal interface IItemDuplicateChecker
    {
        /// <summary>
        /// Indicates whether the specified <paramref name="item"/> is a duplicate.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><see langword="true"/> indicates a duplicate; otherwise, <see langword="false"/>.</returns>
        bool IsDuplicate(TItem item);
    }

    /// <summary>
    /// Provides duplicate checking configuration for items within a collection.
    /// </summary>
    /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    /// <param name="comparer">The equality comparer.</param>
    /// <param name="duplicateText">The duplicate <see cref="LText"/> function.</param>
    internal sealed class ItemDuplicateCheck<TKey>(Func<TItem, TKey> keySelector, IEqualityComparer<TKey>? comparer, Func<LText> duplicateText) : IItemDuplicateCheck
    {
        /// <summary>
        /// Gets the key selector.
        /// </summary>
        public Func<TItem, TKey> KeySelector { get; } = keySelector.ThrowIfNull();

        /// <summary>
        /// Gets the equality comparer.
        /// </summary>
        public IEqualityComparer<TKey>? Comparer { get; } = comparer;

        /// <inheritdoc/>
        public Func<LText> DuplicateText { get; } = duplicateText.ThrowIfNull();

        /// <inheritdoc/>
        public IItemDuplicateChecker CreateDuplicateChecker() => new ItemDuplicateChecker<TKey>(this);
    }

    /// <summary>
    /// Provides runtime duplicate checking for items within a collection.
    /// </summary>
    /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
    internal sealed class ItemDuplicateChecker<TKey> : IItemDuplicateChecker
    {
        private readonly ItemDuplicateCheck<TKey> _config;
        private readonly HashSet<TKey> _keys;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemDuplicateChecker{TKey}"/> class.
        /// </summary>
        /// <param name="config">The <see cref="ItemDuplicateCheck{TKey}"/>.</param>
        public ItemDuplicateChecker(ItemDuplicateCheck<TKey> config)
        {
            _config = config.ThrowIfNull();
            _keys = new HashSet<TKey>(_config.Comparer);
        }

        /// <inheritdoc/>
        public bool IsDuplicate(TItem item) => !_keys.Add(_config.KeySelector(item));
    }
}