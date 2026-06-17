namespace CoreEx.Validation;

/// <summary>
/// Provides access to the validator capabilities.
/// </summary>
public static class Validator
{
    /// <summary>
    /// Create a new <see cref="Validator{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <returns>The <see cref="Validator{TEntity}"/>.</returns>
    public static Validator<TEntity> Create<TEntity>() where TEntity : class => new();

    /// <summary>
    /// Gets an instance of the pre-registered <typeparamref name="TValidator"/> service.
    /// </summary>
    /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
    /// <param name="serviceProvider">The optional <see cref="IServiceProvider"/>; defaults to <see cref="ExecutionContext.ServiceProvider"/>.</param>
    /// <returns>The <typeparamref name="TValidator"/> instance.</returns>
    public static TValidator Get<TValidator>(IServiceProvider? serviceProvider = null) where TValidator : IValidatorEx
        => serviceProvider is null ? ExecutionContext.GetRequiredService<TValidator>() : serviceProvider.GetRequiredService<TValidator>();

    /// <summary>
    /// Gets an instance of the pre-registered keyed <typeparamref name="TValidator"/> service.
    /// </summary>
    /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
    /// <param name="serviceKey">The service key.</param>
    /// <param name="serviceProvider">The optional <see cref="IServiceProvider"/>; defaults to <see cref="ExecutionContext.ServiceProvider"/>.</param>
    /// <returns>The <typeparamref name="TValidator"/> instance.</returns>
    public static TValidator GetKeyed<TValidator>(object? serviceKey, IServiceProvider? serviceProvider = null) where TValidator : IValidatorEx
        => serviceProvider is null ? ExecutionContext.GetRequiredKeyedService<TValidator>(serviceKey) : serviceProvider.GetRequiredKeyedService<TValidator>(serviceKey);

    /// <summary>
    /// Creates a new <see cref="CommonValidator{T}"/> inline.
    /// </summary>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="configure">The action to configure the resulting <see cref="CommonValidator{T}"/>.</param>
    /// <returns>The <see cref="CommonValidator{T}"/>.</returns>
    /// <remarks>A common validator must be defined using a <see langword="notnull"/> <typeparamref name="TValue"/> type to ensure broadest commonality and usage throughout.</remarks>
    public static CommonValidator<TValue> CreateCommon<TValue>(Action<CommonValidator<TValue>.Validator>? configure) where TValue : notnull => new(configure);
}