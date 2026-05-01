namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Provides the base inline validator functionality.
/// </summary>
/// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
/// <remarks>See also <see cref="CommonValidator{TValue}"/>.</remarks>
public abstract class InlineValidator<TValue>
{
    private readonly Validator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommonValidator{T}"/> class.
    /// </summary>
    /// <param name="configure">The action to configure the <see cref="CommonValidator{T}"/>.</param>
    internal InlineValidator(Action<Validator>? configure)
    {
        _validator = new();
        configure?.Invoke(_validator);
    }

    /// <summary>
    /// Gets or sets the property and JSON name override (where not <see langword="null"/>).
    /// </summary>
    /// <remarks>This will apply to all instances in which the <see cref="CommonValidator{TValue}"/> is used; therefore, caution is required when using. This is intended for advanced usage only.</remarks>
    protected string? OverrideName { get; set; }

    /// <summary>
    /// Gets or sets the property text override (where not <see langword="null"/>).
    /// </summary>
    /// <remarks>This will apply to all instances in which the <see cref="CommonValidator{TValue}"/> is used; therefore, caution is required when using. This is intended for advanced usage only.</remarks>
    protected LText? OverrideText { get; set; }

    /// <summary>
    /// Validates the value.
    /// </summary>
    /// <typeparam name="TEntity">The related entity <see cref="Type"/>.</typeparam>
    /// <param name="context">The related <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    internal async Task ValidateAsync<TEntity>(PropertyContext<TEntity, TValue> context, CancellationToken cancellationToken) where TEntity : class
    {
        var root = new RootPropertyRule<ValidationValue<TValue>, TValue>(
            new PropertyRuntimeMetadata<ValidationValue<TValue>, TValue>(OverrideName ?? context.Name, _ => context.Metadata.GetValue<TValue>(context.Entity), text: () => OverrideText ?? context.Metadata.Text, jsonName: OverrideName ?? context.Metadata.JsonName),
                context.IsValueNullable ? _ => context.GetNullableValueOrDefault<TValue>() : null, context.IsValueNullable ? _ => context.IsNullableValueDefault() : null);

        ValidationExtensions.Chain(root, _validator);

        var vv = new ValidationValue<TValue>(context.Value);
        var vc = new ValidationContext<ValidationValue<TValue>>(vv, context.CreateValidationArgs(true));
        var pc = new PropertyContext<ValidationValue<TValue>, TValue>(root, vc);

        // Execute the primary validation.
        await root.ValidateAsync(pc, cancellationToken).ConfigureAwait(false);

        // Execute the secondary validation.
        await OnValidateAsync(pc, cancellationToken).ConfigureAwait(false);

        // Merge results.
        context.MergeResult(vc);
    }

    /// <summary>
    /// Validate the common property value.
    /// </summary>
    /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected virtual Task OnValidateAsync(PropertyContext<ValidationValue<TValue>, TValue> context, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Provides the underlying <see cref="PropertyRuleBase{TEntity, TProperty}"/> enabling standardized configuration and validation behavior to be added/chained.
    /// </summary>
    public sealed class Validator : PropertyRuleBase<ValidationValue<TValue>, TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Validator"/> class.
        /// </summary>
        internal Validator() { }

        /// <inheritdoc/>
        protected override Task OnValidateAsync(PropertyContext<ValidationValue<TValue>, TValue> context, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}