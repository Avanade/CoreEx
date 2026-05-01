namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides an <see cref="Enum"/> <see langword="string"/> validation.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
public class EnumStringRule<TEntity> : PropertyRuleBase<TEntity, string> where TEntity : class
{
    private readonly EnumWith _with;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumStringRule{TEntity}"/> class.
    /// </summary>
    /// <param name="with">Extended <see cref="EnumWith"/> configuration.</param>
    public EnumStringRule(Func<EnumWith, EnumWith> with)
    {
        _with = new EnumWith(this);
        with.ThrowIfNull().Invoke(_with);
    }

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, string> context, CancellationToken cancellationToken)
    {
        if (_with.Validator is not null && !_with.Validator(context))
            context.AddError(ErrorText ?? ValidatorStrings.InvalidFormat);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Provides additional configuration options for the <see cref="EnumStringRule{TEntity}"/>.
    /// </summary>
    public class EnumWith
    {
        private readonly EnumStringRule<TEntity> _rule;
        private bool _ignoreCasing;
        private bool _overrideValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumWith"/> class.
        /// </summary>
        internal EnumWith(EnumStringRule<TEntity> rule) => _rule = rule;

        /// <summary>
        /// Gets the configured validator.
        /// </summary>
        internal Func<PropertyContext<TEntity, string>, bool>? Validator { get; set; }

        /// <summary>
        /// Indicates whether to ignore casing when parsing the <see cref="Enum"/> value.
        /// </summary>
        public EnumWith IgnoreCase()
        {
            _ignoreCasing = true;
            return this;
        }

        /// <summary>
        /// Indicates whether the value should be overridden with the parsed value.
        /// </summary>
        /// <remarks>The value must be mutable otherwise an <see cref="InvalidOperationException"/> will be thrown at runtime.</remarks>
        public EnumWith Override()
        {
            _overrideValue = true;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="Enum"/> <see cref="Type"/> used to validate the value.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> <see cref="Type"/>.</typeparam>
        /// <param name="allowed">An optional list of allowed values.</param>
        public EnumWith With<TEnum>(params TEnum[] allowed) where TEnum : struct, Enum 
        {
            Validator = Validator is not null ? throw new InvalidOperationException("The Enum rule can only have one validator.") : (context) =>
            {
                if (!Enum.TryParse<TEnum>(context.Value, _ignoreCasing, out var @enum))
                    return false;

                if (allowed is not null && allowed.Length > 0 && !allowed.Contains(@enum))
                    return false;

                if (_overrideValue)
                    context.Override(@enum.ToString());

                return true;
            };

            return this;
        }

        /// <summary>
        /// Sets the <see cref="Enum"/> <see cref="Type"/> used to validate the value.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Enum"/> <see cref="Type"/>.</typeparam>
        /// <param name="allowed">An optional list of allowed values.</param>
        public EnumWith With<TEnum>(IEnumerable<TEnum> allowed) where TEnum : struct, Enum => With(allowed?.ToArray() ?? []);
    }
}