namespace CoreEx.Validation;

/// <summary>
/// Provides entity validation.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <remarks>This is a synonym for the <see cref="Validator{TEntity}"/> (and inherits from) to enable <see href="https://docs.fluentvalidation.net/en/latest/">FluentValidation</see>-like syntax.</remarks>
public abstract class AbstractValidator<TEntity> : Validator<TEntity> where TEntity : class { }