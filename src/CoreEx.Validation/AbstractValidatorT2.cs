namespace CoreEx.Validation;

/// <summary>
/// Provides entity validation.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TSelf">The self <see cref="Type"/>.</typeparam>
/// <remarks>This is a synonym for the <see cref="Validator{TEntity, TSelf}"/> (and inherits from) to enable <see href="https://docs.fluentvalidation.net/en/latest/">FluentValidation</see>-like syntax.</remarks>
public abstract class AbstractValidator<TEntity, TSelf> : Validator<TEntity, TSelf> where TEntity : class where TSelf : AbstractValidator<TEntity, TSelf>, new() { }