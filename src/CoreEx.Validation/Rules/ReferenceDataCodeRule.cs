namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides an <see cref="Enum"/> <see langword="string"/> validation.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
public class ReferenceDataCodeRule<TEntity> : PropertyRuleBase<TEntity, string> where TEntity : class
{
    private readonly ReferenceDataWith _with;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumStringRule{TEntity}"/> class.
    /// </summary>
    /// <param name="with">Extended <see cref="ReferenceDataWith"/> configuration.</param>
    public ReferenceDataCodeRule(Func<ReferenceDataWith, ReferenceDataWith> with)
    {
        _with = new ReferenceDataWith(this);
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
    /// Provides additional configuration options for the <see cref="ReferenceDataCodeRule{TEntity}"/>.
    /// </summary>
    public class ReferenceDataWith
    {
        private readonly ReferenceDataCodeRule<TEntity> _rule;
        private bool _allowInactive;
        private bool _overrideValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataWith"/> class.
        /// </summary>
        internal ReferenceDataWith(ReferenceDataCodeRule<TEntity> rule) => _rule = rule;

        /// <summary>
        /// Gets the configured validator.
        /// </summary>
        internal Func<PropertyContext<TEntity, string>, bool>? Validator { get; set; }

        /// <summary>
        /// Indicates whether to allow an <see cref="IReferenceData"/> value where <see cref="IReferenceData.IsActive"/> is set to <see langword="false"/>.
        /// </summary>
        /// <returns></returns>
        public ReferenceDataWith AllowInactive()
        {
            _allowInactive = true;
            return this;
        }

        /// <summary>
        /// Indicates whether the value should be overridden with the parsed value.
        /// </summary>
        /// <remarks>The value must be mutable otherwise an <see cref="InvalidOperationException"/> will be thrown at runtime.</remarks>
        public ReferenceDataWith Override()
        {
            _overrideValue = true;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="IReferenceData"/> <see cref="Type"/> used to validate the value.
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
        public ReferenceDataWith With<TRef>() where TRef : IReferenceData
        {
            Validator = Validator is not null ? throw new InvalidOperationException("The ReferenceDataCode rule can only have one validator.") : context =>
            {
                if (ReferenceDataOrchestrator.Current.GetByTypeRequired<TRef>().TryGetByCode(context.Value!, out var rd) && rd.IsValid)
                {
                    if (_allowInactive || rd.IsActive)
                    {
                        if (_overrideValue)
                            context.Override(rd.Code!);

                        return true;
                    }
                }

                return false;
            };

            return this;
        }
    }
}