namespace CoreEx.Validation;

/// <summary>
/// Provides an implementation of the <see cref="InlineValidator{TValue}"/> that can be used directly for inline-style validation that also supports <see cref="IValidatorEx{TValue}"/>.
/// </summary>
/// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
/// <param name="configure">The action to configure the <see cref="ValidatingInlineValidator{T}"/>.</param>
internal sealed class ValidatingInlineValidator<TValue>(Action<ValidatingInlineValidator<TValue>.Validator>? configure) : InlineValidator<TValue>(configure), IValidatorEx<TValue>
{
    /// <summary>
    /// Overrides the property and JSON names.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The <see cref="ValidatingInlineValidator{TValue}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This will apply to all instances in which the <see cref="ValidatingInlineValidator{TValue}"/> is used; therefore, caution is required when using. This is intended for advanced usage only.</remarks>
    public ValidatingInlineValidator<TValue> WithName(string name)
    {
        OverrideName = name.ThrowIfNullOrEmpty();
        return this;
    }

    /// <summary>
    /// Overrides the property text.
    /// </summary>
    /// <param name="text">The property text.</param>
    /// <returns>The <see cref="ValidatingInlineValidator{TValue}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This will apply to all instances in which the <see cref="ValidatingInlineValidator{TValue}"/> is used; therefore, caution is required when using. This is intended for advanced usage only.</remarks>
    public ValidatingInlineValidator<TValue> WithText(LText text)
    {
        OverrideText = text;
        return this;
    }

    /// <inheritdoc/>
    Task<IValidationContext<TValue>> IValidatorEx<TValue>.ValidateAsync(TValue value, ValidationArgs? args, CancellationToken cancellationToken) => ValidateInternalAsync(value, args, cancellationToken);

    /// <inheritdoc/>
    async Task IValidatorEx<TValue>.ValidateAndThrowAsync(TValue value, ValidationArgs? args, CancellationToken cancellationToken)
        => (await ValidateInternalAsync(value, args, cancellationToken).ConfigureAwait(false)).ThrowOnError();

    /// <inheritdoc/>
    async Task<IValidationResult<TValue>> IValidator<TValue>.ValidateAsync(TValue value, CancellationToken cancellationToken)
        => await ValidateInternalAsync(value, null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Performs the <see cref="IValidator{T}"/> validation from above methods.
    /// </summary>
    private async Task<IValidationContext<TValue>> ValidateInternalAsync(TValue value, ValidationArgs? args, CancellationToken cancellationToken)
    {
        // Validate the value.
        args ??= new ValidationArgs();
        var r = await new ValueValidator<TValue>(value, Validation.ValueName, null, Validation.ValueText, c => c.Common(this), null, null).ValidateAsync(new ValidationValue<TValue>(value), args, cancellationToken);

        // Transform the context to expose TValue.
        var vc = new ValueValidationContext(r);
        return vc;
    }

    /// <inheritdoc/>
    Task IValidatorEx<TValue>.ValidateAsync(IValidationContext<TValue> context, CancellationToken cancellationToken)
        => throw new NotSupportedException($"{nameof(ValidateAsync)} is not supported by the {nameof(ValidatingInlineValidator<>)} class.");

    /// <summary>
    /// Custom <see cref="IValidationContext{TEntity}"/> that transforms the underlying <see cref="ValidationValue{TValue}.Value"/> and related-context.
    /// </summary>
    internal readonly struct ValueValidationContext(IValidationContext<ValidationValue<TValue>> parent) : IValidationContext<TValue>
    {
        private readonly IValidationContext<ValidationValue<TValue>> _parent = parent;

        /// <inheritdoc/>
        public Type EntityType => _parent.EntityType;

        /// <inheritdoc/>
        public string? FullyQualifiedEntityName => _parent.FullyQualifiedEntityName;

        /// <inheritdoc/>
        public string? FullyQualifiedJsonEntityName => _parent.FullyQualifiedJsonEntityName;

        /// <inheritdoc/>
        public bool UseJsonNames => _parent.UseJsonNames;

        /// <inheritdoc/>
        public JsonSerializerOptions? JsonSerializerOptions => _parent.JsonSerializerOptions;

        /// <inheritdoc/>
        public IServiceProvider? ServiceProvider => _parent.ServiceProvider;

        /// <inheritdoc/>
        public IDictionary<string, object?> Parameters => _parent.Parameters;

        /// <inheritdoc/>
        public TValue? Value => _parent.Value is null ? default : _parent.Value.Value;

        /// <inheritdoc/>
        public bool HasErrors => _parent.HasErrors;

        /// <inheritdoc/>
        public MessageItemCollection? Messages => _parent.Messages;

        /// <inheritdoc/>
        public bool HasError(string fullyQualifiedPropertyName) => _parent.HasError(fullyQualifiedPropertyName);

        /// <inheritdoc/>
        public IValidationResult ThrowOnError() => _parent.ThrowOnError();

        /// <inheritdoc/>
        public Exception? ToException() => _parent.ToException();

        /// <inheritdoc/>
        public Result ToResult() => HasErrors ? Result.ValidationError(Messages!) : Result.Success;
    }
}