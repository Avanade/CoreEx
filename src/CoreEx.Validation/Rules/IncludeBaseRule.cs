namespace CoreEx.Validation.Rules;

/// <summary>
/// Represents a rule that enables a base validator to be included.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <param name="validator">The base validator.</param>
/// <remarks>Implements <see cref="IRootPropertyRule{TEntity}"/> to enable usage for the likes of <see cref="ValidatorBase{TEntity, TSelf}.Rules"/>; however, acts as a pass-through proxy therefore will largely throw <see cref="NotImplementedException"/>.</remarks>
internal class IncludeBaseRule<TEntity>(IValidatorEx validator) : IRootPropertyRule<TEntity> where TEntity : class
{
    private readonly IValidatorEx _validator = validator.ThrowIfNull();

    /// <inheritdoc/>
    bool IRootPropertyRule<TEntity>.IsValueNullable => throw new NotImplementedException();

    /// <inheritdoc/>
    LText? IPropertyRule<TEntity>.ErrorText { get => throw new NotImplementedException(); }

    /// <inheritdoc/>
    public void SetText(LText? text) => throw new NotImplementedException();

    /// <inheritdoc/>
    void IPropertyRule<TEntity>.AddClause(IPropertyClause<TEntity> clause) => throw new NotImplementedException();

    /// <inheritdoc/>
    void IPropertyRule<TEntity>.Chain(IPropertyRule<TEntity> rule) => throw new NotImplementedException();

    /// <inheritdoc/>
    T IRootPropertyRule<TEntity>.GetNullableValueOrDefault<T>(TEntity entity) => throw new NotImplementedException();

    /// <inheritdoc/>
    bool IRootPropertyRule<TEntity>.IsNullableValueDefault(TEntity entity) => throw new NotImplementedException();

    /// <inheritdoc/>
    void IRootPropertyRule<TEntity>.SetFormat(string? format, IFormatProvider? formatProvider, char? quotingCharacter) => throw new NotImplementedException();

    /// <inheritdoc/>
    async Task IRootPropertyRule<TEntity>.ValidateAsync(ValidationContext<TEntity> context, CancellationToken cancellationToken)
    {
        // Create the args to pass through.
        var args = new ValidationArgs
        {
            FullyQualifiedEntityName = context.FullyQualifiedEntityName,
            FullyQualifiedJsonEntityName = context.FullyQualifiedJsonEntityName,
            UseJsonNames = context.UseJsonNames,
            Parameters = context.Parameters,
            JsonSerializerOptions = context.JsonSerializerOptions,
            ServiceProvider = context.ServiceProvider
        };

        // Validate.
        var vr = await _validator.ValidateAsync(context.Value, args, cancellationToken).ConfigureAwait(false);

        // Merge results.
        context.MergeResult(vr);
    }

    /// <inheritdoc/>
    Task IPropertyRule<TEntity>.ValidateAsync(IPropertyContext<TEntity> context, CancellationToken cancellationToken) => throw new NotImplementedException();
}