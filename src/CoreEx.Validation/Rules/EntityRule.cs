namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides entity validation.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
public sealed class EntityRule<TEntity, TProperty> : PropertyRuleBase<TEntity, TProperty> where TEntity : class where TProperty : class?
{
    internal Func<PropertyContext<TEntity, TProperty>, ValidationArgs, CancellationToken, Task<IValidationContext>>? _validationAsync;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityRule{TEntity, TProperty}"/> class.
    /// </summary>
    /// <param name="with">Extends configuration <see cref="With"/>.</param>
    public EntityRule(Action<With> with)
    {
        var erw = new With(this);
        with?.Invoke(erw);
    }

    /// <inheritdoc/>
    protected override async Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        var vr = await _validationAsync.ThrowIfNull().Invoke(context, context.CreateValidationArgs(), cancellationToken).ConfigureAwait(false);
        context.MergeResult(vr);
    }

    /// <summary>
    /// Provides additional configuration options for the <see cref="EntityRule{TEntity, TProperty}"/>.
    /// </summary>
    public class With
    {
        private readonly EntityRule<TEntity, TProperty> _rule;

        /// <summary>
        /// Initializes a new instance of the <see cref="With"/> class.
        /// </summary>
        /// <param name="rule">The owning <see cref="EntityRule{TEntity, TProperty}"/>.</param>
        internal With(EntityRule<TEntity, TProperty> rule) => _rule = rule;

        /// <summary>
        /// Sets the specified <paramref name="configure"/>.
        /// </summary>
        public void WithValidator(Action<InlineValidator<TProperty>.Validator>? configure) => WithValidator(new ValidatingInlineValidator<TProperty>(configure));

        /// <summary>
        /// Sets the specified <paramref name="validator"/>.
        /// </summary>
        public void WithValidator(IValidatorEx<TProperty> validator)
        {
            validator.ThrowIfNull();
            _rule._validationAsync = async (context, args, cancellationToken) =>
            {
                var value = context.Metadata.GetValue<TProperty>(context.Entity);
                return await validator.ValidateAsync(value, args, cancellationToken).ConfigureAwait(false);
            };
        }

        /// <summary>
        /// Sets the specified <typeparamref name="TValidator"/>.
        /// </summary>
        /// <typeparam name="TValidator">The property validator <see cref="Type"/>.</typeparam>
        public void WithValidator<TValidator>() where TValidator : IValidatorEx<TProperty> => _rule._validationAsync = async (context, args, cancellationToken) =>
        {
            var validator = Validator.Get<TValidator>(args.ServiceProvider);
            var value = context.Metadata.GetValue<TProperty>(context.Entity);
            return await validator.ValidateAsync(value, args, cancellationToken).ConfigureAwait(false);
        };

        /// <summary>
        /// Sets the specified keyed <typeparamref name="TValidator"/>.
        /// </summary>
        /// <typeparam name="TValidator">The property validator <see cref="Type"/>.</typeparam>
        /// <param name="serviceKey">The service key.</param>
        public void WithKeyedValidator<TValidator>(object? serviceKey) where TValidator : IValidatorEx<TProperty> => _rule._validationAsync = async (context, args, cancellationToken) =>
        {
            var validator = Validator.GetKeyed<TValidator>(serviceKey, args.ServiceProvider);
            var value = context.Metadata.GetValue<TProperty>(context.Entity);
            return await validator.ValidateAsync(value, args, cancellationToken).ConfigureAwait(false);
        };
    }
}