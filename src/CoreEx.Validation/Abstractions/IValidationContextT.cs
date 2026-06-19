namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Enables validation context.
/// </summary>
/// <typeparam name="T">The <see cref="Type"/>.</typeparam>
public interface IValidationContext<T> : IValidationContext, IValidationResult<T> { }