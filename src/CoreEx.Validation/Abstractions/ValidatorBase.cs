namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Represents the base entity validator.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TSelf">The <see cref="ValidatorBase{TEntity, TSelf}"/> <see cref="Type"/>.</typeparam>
/// <remarks>This is intended for advanced scenarios where <see cref="ValidateAsync(TEntity, CoreEx.Validation.ValidationArgs?, CancellationToken)"/> must be explicitly overridden to implement the desired behavior; otherwise, it throws a <see cref="NotSupportedException"/>.
/// <para>Generally, use <see cref="Validator{TEntity}"/> (inherit from) or <see cref="Validator.Create{TEntity}"/> (in-line) to leverage.</para></remarks>
public abstract partial class ValidatorBase<TEntity, TSelf> : IValidatorEx<TEntity> where TEntity : class where TSelf : ValidatorBase<TEntity, TSelf>
{
    /// <summary>
    /// Gets the underlying <see cref="IRootPropertyRule{TEntity}"/> collection.
    /// </summary>
    protected List<IRootPropertyRule<TEntity>> Rules { get; } = [];

    /// <summary>
    /// Adds a <see cref="IPropertyRule{TEntity, TProperty}"/> to the validator <see cref="Rules"/> for the specified <paramref name="propertyExpression"/>.
    /// </summary>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <returns>The <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
    protected IRootPropertyRule<TEntity, TProperty> Property<TProperty>(Expression<Func<TEntity, TProperty?>> propertyExpression)
        => PropertyInternal<TProperty>(RuntimeMetadata.GetForExpression(propertyExpression.ThrowIfNull()), null, null);

    /// <summary>
    /// Adds a <see cref="IPropertyRule{TEntity, TProperty}"/> to the validator <see cref="Rules"/> for the specified <paramref name="propertyExpression"/>.
    /// </summary>
    /// <param name="propertyExpression">The property <see cref="Expression{TDelegate}"/>.</param>
    protected IRootPropertyRule<TEntity, TProperty?> Property<TProperty>(Expression<Func<TEntity, TProperty?>> propertyExpression) where TProperty : struct
    {
        var metadata = RuntimeMetadata.GetForExpression(propertyExpression.ThrowIfNull());
        var rule = new RootPropertyRule<TEntity, TProperty?>(metadata,
            e => metadata.GetValue<TProperty?>(e).GetValueOrDefault(),
            e => Comparer<TProperty>.Default.Compare(metadata.GetValue<TProperty?>(e).GetValueOrDefault(), default) == 0);

        Rules.Add(rule);
        return rule;
    }

    /// <summary>
    /// Adds a <see cref="IPropertyRule{TEntity, TProperty}"/> to the validator <see cref="Rules"/> for the specified <paramref name="propertyMetadata"/>.
    /// </summary>
    private RootPropertyRule<TEntity, TProperty> PropertyInternal<TProperty>(IPropertyRuntimeMetadata propertyMetadata, Func<TEntity, TProperty>? getNullableValue, Func<TEntity, bool>? isNullableValueDefault)
    {
        var rule = new RootPropertyRule<TEntity, TProperty>(propertyMetadata.ThrowIfNull(), getNullableValue, isNullableValueDefault);
        Rules.Add(rule);
        return rule;
    }

    /// <summary>
    /// Adds a <see cref="IPropertyRule{TEntity, TProperty}"/> to the validator <see cref="Rules"/> for the specified <paramref name="propertyExpression"/> enabling an inline <paramref name="configure"/> opportunity.
    /// </summary>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="configure">The action to configure the resulting <see cref="IPropertyRule{TEntity, TProperty}"/>.</param>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    public TSelf HasProperty<TProperty>(Expression<Func<TEntity, TProperty?>> propertyExpression, Action<IRootPropertyRule<TEntity, TProperty>>? configure = null)
        => HasPropertyInternal(RuntimeMetadata.GetForExpression(propertyExpression.ThrowIfNull()), configure, null, null);

    /// <summary>
    /// Adds a <see cref="IPropertyRule{TEntity, TProperty}"/> to the validator <see cref="Rules"/> for the specified <paramref name="propertyExpression"/> enabling an inline <paramref name="configure"/> opportunity.
    /// </summary>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="configure">The action to configure the resulting <see cref="IPropertyRule{TEntity, TProperty}"/>.</param>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    public TSelf HasProperty<TProperty>(Expression<Func<TEntity, TProperty?>> propertyExpression, Action<IRootPropertyRule<TEntity, TProperty?>>? configure = null) where TProperty : struct
    {
        var metadata = RuntimeMetadata.GetForExpression(propertyExpression.ThrowIfNull());
        return HasPropertyInternal(metadata, configure,
            e => metadata.GetValue<TProperty?>(e).GetValueOrDefault(),
            e => Comparer<TProperty>.Default.Compare(metadata.GetValue<TProperty?>(e).GetValueOrDefault(), default) == 0);
    }

    /// <summary>
    /// Adds a <see cref="IPropertyRule{TEntity, TProperty}"/> to the validator <see cref="Rules"/> for the specified <paramref name="propertyMetadata"/> enabling an inline <paramref name="configure"/> opportunity.
    /// </summary>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="propertyMetadata">The <see cref="IPropertyRuntimeMetadata"/>.</param>
    /// <param name="configure">The action to configure the resulting <see cref="IPropertyRule{TEntity, TProperty}"/>.</param>
    /// <param name="getNullableValue">A function to get the underlying nullable value.</param>
    /// <param name="isNullableValueDefault">A function to determine whether the underlying nullable value is its default.</param>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    internal TSelf HasPropertyInternal<TProperty>(IPropertyRuntimeMetadata propertyMetadata, Action<IRootPropertyRule<TEntity, TProperty>>? configure, Func<TEntity, TProperty>? getNullableValue, Func<TEntity, bool>? isNullableValueDefault)
    {
        var rule = PropertyInternal(propertyMetadata, getNullableValue, isNullableValueDefault);
        configure?.Invoke(rule);
        return (TSelf)this;
    }

    /// <summary>
    /// Adds a self-validation <see cref="IPropertyRule{TEntity, TProperty}"/> for the <typeparamref name="TEntity"/> to the validator <see cref="Rules"/>.
    /// </summary>
    /// <param name="configure">The action to configure the resulting <see cref="IPropertyRule{TEntity, TProperty}"/>.</param>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This may only be invoked once; otherwise, an <see cref="InvalidOperationException"/> will be thrown.</remarks>
    public TSelf Self(Action<IRootPropertyRule<TEntity, TEntity>>? configure = null)
    {
        if (Rules.Any(r => r is RootPropertyRule<TEntity, TEntity> rr && rr.Metadata is ISelfRuntimeMetadata))
            throw new InvalidOperationException("The 'Self' rule has already been defined for this validator; it may only be configured once.");

        // Uses a special SelfRuntimeMetadata/ISelfRuntimeMetadata to enable functionality internally.
        var srm = new SelfRuntimeMetadata<TEntity>();
        var rule = new RootPropertyRule<TEntity, TEntity>(srm, null, null);
        Rules.Add(rule);
        configure?.Invoke(rule);
        return (TSelf)this;
    }

    /// <summary>
    /// Adds a <see cref="IncludeBaseRule{TEntity}"/> to the validator <see cref="Rules"/> for the specified <paramref name="baseValidator"/>.
    /// </summary>
    /// <param name="baseValidator">The base <see cref="IValidatorEx"/>.</param>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    /// <remarks><typeparamref name="TInclude"/> must be the same or a base type of <typeparamref name="TEntity"/>; otherwise, an <see cref="ArgumentException"/> will be thrown.
    /// <para><i>Note:</i> the <paramref name="baseValidator"/> is added internally as an <see cref="IncludeBaseRule{TEntity}"/> rule; therefore, it will be executed in the order added in relation to other property-base rules.</para></remarks>
    protected TSelf Include<TInclude>(IValidatorEx<TInclude> baseValidator) where TInclude : class
    {
        baseValidator.ThrowIfNull();
        if (typeof(TInclude) == typeof(TEntity) || typeof(TInclude).IsAssignableFrom(typeof(TEntity)))
            return IncludeBase(baseValidator);

        throw new ArgumentException($"The specified base validator type '{typeof(TInclude).FullName}' must be the same or a base type of the entity type '{typeof(TEntity).FullName}'.", nameof(baseValidator));
    }

    /// <summary>
    /// Adds a <see cref="IncludeBaseRule{TEntity}"/> to the validator <see cref="Rules"/> for the specified <paramref name="baseValidator"/>.
    /// </summary>
    /// <param name="baseValidator">The base <see cref="IValidatorEx"/>.</param>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    internal TSelf IncludeBase<TInclude>(IValidatorEx<TInclude> baseValidator) where TInclude : class
    {
        Rules.Add(new IncludeBaseRule<TEntity>(baseValidator));
        return (TSelf)this;
    }

    /// <summary>
    /// Adds a <see cref="RuleSet{TEntity}"/> to the validator <see cref="Rules"/> enabling a conditional (<paramref name="predicate"/>) set of rules to be configured.
    /// </summary>
    /// <param name="predicate">The predicate to determine whether the <see cref="RuleSet{TEntity}"/> is to be validated.</param>
    /// <param name="configure">The action to configure the underlying set of rules.</param>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    public TSelf HasRuleSet(Predicate<ValidationContext<TEntity>> predicate, Action<RuleSet<TEntity>> configure)
    {
        var ruleSet = new RuleSet<TEntity>(predicate);
        configure?.Invoke(ruleSet);

        if (ruleSet.Rules.Count > 0)
            Rules.Add(ruleSet);

        return (TSelf)this;
    }

    /// <inheritdoc/>
    async Task<IValidationResult<TEntity>> IValidator<TEntity>.ValidateAsync(TEntity value, CancellationToken cancellationToken) => await ValidateAsync(value, null, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public virtual Task<IValidationContext<TEntity>> ValidateAsync(TEntity value, ValidationArgs? args, CancellationToken cancellationToken)
        => throw new NotSupportedException($"{nameof(ValidateAsync)} is not supported by the {nameof(ValidatorBase<,>)} class.");

    /// <inheritdoc/>
    public virtual Task ValidateAndThrowAsync(TEntity value, ValidationArgs? args, CancellationToken cancellationToken)
        => throw new NotSupportedException($"{nameof(ValidateAndThrowAsync)} is not supported by the {nameof(ValidatorBase<,>)} class.");

    /// <inheritdoc/>
    Task IValidatorEx<TEntity>.ValidateAsync(IValidationContext<TEntity> context, CancellationToken cancellationToken)
        => ValidateAsync(context, cancellationToken);

    /// <summary>
    /// Validate using the <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="IValidationContext{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IValidationContext{T}"/>.</returns>
    internal abstract Task ValidateAsync(IValidationContext<TEntity> context, CancellationToken cancellationToken);
}