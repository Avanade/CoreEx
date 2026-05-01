namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides the base <see cref="IPropertyRule{TEntity, TProperty}"/> capabilities.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
public abstract class PropertyRuleBase<TEntity, TProperty> : IPropertyRuleEx<TEntity, TProperty> where TEntity : class
{
    private List<IPropertyClause<TEntity>>? _clauses;
    private IPropertyRule<TEntity>? _chainedRule;

    /// <inheritdoc/>
    public LText? ErrorText { get; private set; }

    /// <summary>
    /// Indicates whether the validation will be performed where the property value is <see langword="null"/>.
    /// </summary>
    /// <remarks>When this is set to <see langword="false"/> and the property value is <see langword="null"/> then the underyling clauses (if any) and validation will be bypassed and the next chained rule in 
    /// sequence will be executed.</remarks>
    protected virtual bool ValidateWhenNull => false;

    /// <inheritdoc/>
    IPropertyRule<TEntity, TProperty> IPropertyRule<TEntity, TProperty>.WithMessage(LText errorText)
    {
        ErrorText = errorText;
        return this;
    }

    /// <inheritdoc/>
    void IPropertyRule<TEntity>.AddClause(IPropertyClause<TEntity> clause) => (_clauses ??= []).Add(clause.ThrowIfNull());

    /// <inheritdoc/>
    void IPropertyRule<TEntity>.Chain(IPropertyRule<TEntity> rule)
    {
        if (_chainedRule is not null)
            throw new InvalidOperationException("Each rule can only support a single chained rule.");

        _chainedRule = rule.ThrowIfNull();
    }

    /// <inheritdoc/>
    async Task IPropertyRule<TEntity>.ValidateAsync(IPropertyContext<TEntity> context, CancellationToken cancellationToken)
    {
        if (context is PropertyContext<TEntity, TProperty> ctx)
        {
            await ValidateInternalAsync(ctx, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Value is Nullable<T> and we need to convert it to T to continue.
        ctx = new PropertyContext<TEntity, TProperty>(context, context.Value is null ? default! : (TProperty)context.Value);
        await ValidateInternalAsync(ctx, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    Task IPropertyRuleEx<TEntity, TProperty>.ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken) => ValidateInternalAsync(context, cancellationToken);

    /// <summary>
    /// Validate the property value.
    /// </summary>
    private async Task ValidateInternalAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        // First, check whether rule is bypassed when null.
        if (!ValidateWhenNull && context.IsValueNull)
        {
            if (_chainedRule is not null)
                await _chainedRule!.ValidateAsync(context, cancellationToken).ConfigureAwait(false); 

            return;
        }

        // Next, check the clauses; if they are not satisfied then we don't execute the validation but we do execute the next chained rule (if any).
        var cr = await RootPropertyRule<TEntity, TProperty>.CheckClausesAsync(context, _clauses, cancellationToken).ConfigureAwait(false);
        if (cr)
            await OnValidateAsync(context, cancellationToken).ConfigureAwait(false);

        if (_chainedRule is not null && !context.IsInError)
            await _chainedRule!.ValidateAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validate the property value.
    /// </summary>
    /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected abstract Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken);
}