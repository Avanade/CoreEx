namespace CoreEx.Validation;

/// <summary>
/// Provides entity validation with a <see cref="Default"/> singleton instance.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TSelf">The self <see cref="Type"/>.</typeparam>
public abstract class Validator<TEntity, TSelf> : Validator<TEntity> where TEntity : class where TSelf : Validator<TEntity, TSelf>, new()
{
    /// <summary>
    /// Gets the default singleton instance.
    /// </summary>
    public static TSelf Default { get; } = new TSelf();
}