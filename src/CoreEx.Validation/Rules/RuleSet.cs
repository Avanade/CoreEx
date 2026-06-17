namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides a validation rule set for an entity, in that it groups <see cref="Rules"/> together that are only validated when the specified condition (predicate) is <see langword="true"/>.
/// </summary>
/// <typeparam name="TEntity">the entity <see cref="Type"/>.</typeparam>
/// <param name="predicate">The predicate to determine whether the <see cref="RuleSet{TEntity}"/> is to be validated.</param>
public sealed class RuleSet<TEntity>(Predicate<ValidationContext<TEntity>> predicate) : ValidatorBase<TEntity, RuleSet<TEntity>>, IRootPropertyRule<TEntity> where TEntity : class
{
    private readonly Predicate<ValidationContext<TEntity>> _predicate = predicate.ThrowIfNull();

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
        // Check the predicate.
        if (!_predicate(context))
            return;

        // Execute the rules in the set.
        foreach (var rule in Rules)
        {
            await rule.ValidateAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    Task IPropertyRule<TEntity>.ValidateAsync(IPropertyContext<TEntity> context, CancellationToken cancellationToken) => throw new NotImplementedException();

    /// <inheritdoc/>
    internal override Task ValidateAsync(IValidationContext<TEntity> context, CancellationToken cancellationToken) => throw new NotImplementedException();
}