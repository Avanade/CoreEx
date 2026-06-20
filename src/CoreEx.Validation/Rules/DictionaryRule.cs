namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides a dictionary (<see cref="IDictionary"/>) validation including item-based validation.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/> (<see cref="IDictionary{TKey, TValue}"/>).</typeparam>
/// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
/// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
/// <remarks>The dictionary validation constrains the key and value to being defined using a <see langword="notnull"/> to limit usage challenges with this library (lack of generic covariance) limits native
/// support. However, having said that, a <see cref="Dictionary{TKey, TValue}"/> does not support null keys anyway.</remarks>
public sealed class DictionaryRule<TEntity, TProperty, TKey, TValue> : PropertyRuleBase<TEntity, TProperty> where TEntity : class where TProperty : IDictionary<TKey, TValue> where TKey : notnull where TValue : notnull
{
    private readonly Func<PropertyContext<TEntity, TProperty>, int>? _minCount;
    private readonly Func<PropertyContext<TEntity, TProperty>, int?>? _maxCount;
    private readonly With _with;

    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryRule{TEntity, TProperty, TKey, TValue}"/> class.
    /// </summary>
    /// <param name="minCount">The minimum count.</param>
    /// <param name="maxCount">The maximum count.</param>
    /// <param name="with">Extends configuration <see cref="With"/>.</param>
    public DictionaryRule(Func<PropertyContext<TEntity, TProperty>, int>? minCount, Func<PropertyContext<TEntity, TProperty>, int?>? maxCount, Func<With, With>? with)
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
    /// Provides additional configuration options for the <see cref="DictionaryRule{TEntity, TProperty, TKey, TValue}"/>.
    /// </summary>
    public sealed class With
    {
        private readonly DictionaryRule<TEntity, TProperty, TKey, TValue> _rule;
        private Func<ValidationArgs, IValidatorEx<TKey>>? _getKeyValidator;
        private Func<ValidationArgs, IValidatorEx<TValue>>? _getValueValidator;
        private bool _hasAllowNullValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="With"/> class.
        /// </summary>
        internal With(DictionaryRule<TEntity, TProperty, TKey, TValue> rule) => _rule = rule;

        /// <summary>
        /// Indicates that entries can have a <see langword="null"/> <see cref="DictionaryEntry.Value"/>.
        /// </summary>
        /// <returns>The <see cref="With"/> to support fluent-style method-chaining.</returns>
        public With AllowNullValues()
        {
            _hasAllowNullValues = true;
            return this;
        }

        /// <summary>
        /// Sets the specified <b>Key</b> <paramref name="configure"/>.
        /// </summary>
        /// <param name="configure">The action to configure the <see cref="InlineValidator{TKey}.Validator"/>.</param>
        /// <returns>The <see cref="With"/> to support fluent-style method-chaining.</returns>
        public With WithKeyValidator(Action<InlineValidator<TKey>.Validator>? configure) => WithKeyValidator(ValidatorStrings.KeyText, configure);

        /// <summary>
        /// Sets the specified <b>Key</b> <paramref name="configure"/>.
        /// </summary>
        /// <param name="text">The property text.</param>
        /// <param name="configure">The action to configure the <see cref="InlineValidator{TKey}.Validator"/>.</param>
        /// <returns>The <see cref="With"/> to support fluent-style method-chaining.</returns>
        public With WithKeyValidator(LText text, Action<InlineValidator<TKey>.Validator>? configure)
        {
            _getKeyValidator = _getKeyValidator is null
                ? _ => new ValidatingInlineValidator<TKey>(configure).WithName(Validation.KeyName).WithText(text)
                : throw new InvalidOperationException("The dictionary rule can only have one Key validator.");

            return this;
        }

        /// <summary>
        /// Sets the specified <b>Value</b> <paramref name="configure"/>.
        /// </summary>
        /// <returns>The <see cref="With"/> to support fluent-style method-chaining.</returns>
        public With WithValueValidator(Action<InlineValidator<TValue>.Validator>? configure) => WithValueValidator(new ValidatingInlineValidator<TValue>(configure));

        /// <summary>
        /// Sets the specified <b>Value</b> <paramref name="validator"/>.
        /// </summary>
        /// <returns>The <see cref="With"/> to support fluent-style method-chaining.</returns>
        public With WithValueValidator(IValidatorEx<TValue> validator)
        {
            _getValueValidator = _getValueValidator is not null ? throw new InvalidOperationException("The dictionary rule can only have one Value validator.") : _ => validator.ThrowIfNull();
            return this;
        }

        /// <summary>
        /// Sets the specified <b>Value</b> <typeparamref name="TValidator"/> service (resolved at validation runtime).
        /// </summary>
        /// <typeparam name="TValidator">The property validator <see cref="Type"/>.</typeparam>
        /// <returns>The <see cref="With"/> to support fluent-style method-chaining.</returns>
        public With WithValueValidator<TValidator>() where TValidator : IValidatorEx<TValue>
        {
            _getValueValidator = _getValueValidator is not null ? throw new InvalidOperationException("The dictionary rule can only have one Value validator.") : args => CoreEx.Validation.Validator.Get<TValidator>(args.ServiceProvider);
            return this;
        }

        /// <summary>
        /// Sets the specified keyed <b>Value</b> <typeparamref name="TValidator"/> service (resolved at validation runtime).
        /// </summary>
        /// <typeparam name="TValidator">The property validator <see cref="Type"/>.</typeparam>
        /// <param name="serviceKey">The service key.</param>
        /// <returns>The <see cref="With"/> to support fluent-style method-chaining.</returns>
        public With WithValueKeyedValidator<TValidator>(object? serviceKey) where TValidator : IValidatorEx<TValue>
        {
            _getValueValidator = _getValueValidator is not null ? throw new InvalidOperationException("The dictionary rule can only have one Value validator.") : args => CoreEx.Validation.Validator.GetKeyed<TValidator>(serviceKey, args.ServiceProvider);
            return this;
        }

        /// <summary>
        /// Validates each item within the dictionary.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="minCount">The minimum count.</param>
        /// <param name="maxCount">The maximum count.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        internal async Task ValidateAsync(PropertyContext<TEntity, TProperty> context, int minCount, int? maxCount, CancellationToken cancellationToken)
        {
            var hasNullKey = false;
            var hasNullValue = false;

            // Validate each item in the dictionary.
            foreach (var kvp in context.Value)
            {
                bool hasKeyError = false;

                // Validate the key.
                if (kvp.Key is null)
                    hasNullKey = true;
                else if (_getKeyValidator is not null)
                {
                    var args = context.CreateValidationArgs();

                    // Where the key is a string, then set the name on the validator to support better error messages.
                    var kv = _getKeyValidator.Invoke(args);
                    if (kvp.Key is string s && !string.IsNullOrEmpty(s) && kv is ValidatingInlineValidator<TKey> vilv)
                        vilv.WithName(s);

                    // Validate the key and merge the result.
                    var r = await kv.ValidateAsync(kvp.Key, args, cancellationToken).ConfigureAwait(false);
                    hasKeyError = r.HasErrors;
                    context.MergeResult(r);
                }

                // Validate the value (only where key is considered valid; i.e. not null and passes validation where a validator is specified).
                if (!hasKeyError)
                {
                    if (kvp.Value is null)
                        hasNullValue = true;
                    else if (_getValueValidator is not null)
                    {
                        var args = CreateValidationArgs(context, kvp.Key?.ToString());

                        var last = context.GetDictionaryKeySafe();
                        context.SetDictionaryKey(kvp.Key);

                        // Validate the value and merge the result.
                        try
                        {
                            var vv = _getValueValidator.Invoke(args);
                            var r = vv is ValidatingInlineValidator<TValue> vilv
                                ? await vilv.ValidateEntityAsync(kvp.Value, args, cancellationToken).ConfigureAwait(false)
                                : await vv.ValidateAsync(kvp.Value, args, cancellationToken).ConfigureAwait(false);

                            context.MergeResult(r);
                        }
                        finally
                        {
                            context.SetDictionaryKey(last);
                        }
                    }
                }
            }

            // Emit the key and/or value error(s).
            if (hasNullKey)
                context.AddError(_rule.ErrorText ?? ValidatorStrings.DictionaryNullKeyFormat);

            if (hasNullValue && !_hasAllowNullValues)
                context.AddError(_rule.ErrorText ?? ValidatorStrings.DictionaryNullValueFormat);

            // Check the length/count.
            var count = context.Value.Count;
            if (count < minCount)
                context.AddError(_rule.ErrorText ?? ValidatorStrings.MinCountFormat, minCount);
            else if (maxCount.HasValue && count > maxCount.Value)
                context.AddError(_rule.ErrorText ?? ValidatorStrings.MaxCountFormat, maxCount);
        }

        /// <summary>
        /// Creates the <see cref="ValidationArgs"/> for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
        /// <param name="key">The dictionary key.</param>
        /// <returns>The <see cref="ValidationArgs"/>.</returns>
        private static ValidationArgs CreateValidationArgs(PropertyContext<TEntity, TProperty> context, string? key)
        {
            var args = context.CreateValidationArgs();
            args.FullyQualifiedEntityName += $"[{(key ?? "null")}]";

            // Note: an indexer for a dictionary from a JSON perspective is simply a property name; i.e. no square brackets.
            args.FullyQualifiedJsonEntityName += $".{key ?? "null"}";
            return args;
        }
    }
}