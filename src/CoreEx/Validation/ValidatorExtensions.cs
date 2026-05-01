namespace CoreEx.Validation;

/// <summary>
/// Provides standard validator extensions.
/// </summary>
public static partial class ValidatorExtensions
{
    /// <summary>
    /// Validates using the <paramref name="validator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="validator">The function to get the <see cref="IValidator{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    /// <remarks>Where the <see cref="IValidationResult"/> <see cref="IValidationResult.HasErrors"/> the corresponding <see cref="IResult.Error"/> will be updated with the <see cref="IValidationResult.ToException"/>.</remarks>
    public static async Task<Result<T>> ValidateAsync<T>(this Result<T> result, Func<IValidator<T>> validator, CancellationToken cancellationToken = default) where T : class
    {
        validator.ThrowIfNull();

        return await result.ThenAsync(async v =>
        {
            var vi = validator() ?? throw new InvalidOperationException($"The {nameof(validator)} function must return a non-null instance to perform the requested validation.");
            var vr = await vi.ValidateAsync(v, cancellationToken).ConfigureAwait(false);
            return vr.ToResult();
        });
    }

    /// <summary>
    /// Validates using the <paramref name="validator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="validator">The function to get the <see cref="IValidator{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    /// <remarks>Where the <see cref="IValidationResult"/> <see cref="IValidationResult.HasErrors"/> the corresponding <see cref="IResult.Error"/> will be updated with the <see cref="IValidationResult.ToException"/>.</remarks>
    public static async Task<Result<T>> ValidateAsync<T>(this Task<Result<T>> result, Func<IValidator<T>> validator, CancellationToken cancellationToken = default) where T : class
    {
        validator.ThrowIfNull();

        return await result.ThenAsync(async v =>
        {
            var vi = validator() ?? throw new InvalidOperationException($"The {nameof(validator)} function must return a non-null instance to perform the requested validation.");
            var vr = await vi.ValidateAsync(v, cancellationToken).ConfigureAwait(false);
            return vr.ToResult();
        });
    }

    /// <summary>
    /// Validates using the <paramref name="validator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="validator">The <see cref="IValidator{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    /// <remarks>Where the <see cref="IValidationResult"/> <see cref="IValidationResult.HasErrors"/> the corresponding <see cref="IResult.Error"/> will be updated with the <see cref="IValidationResult.ToException"/>.</remarks>
    public static async Task<Result<T>> ValidateAsync<T>(this Result<T> result, IValidator<T> validator, CancellationToken cancellationToken = default) where T : class
    {
        validator.ThrowIfNull();

        return await result.ThenAsync(async v =>
        {
            var vr = await validator.ValidateAsync(v, cancellationToken).ConfigureAwait(false);
            return vr.ToResult();
        });
    }

    /// <summary>
    /// Validates using the <paramref name="validator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="validator">The <see cref="IValidator{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    /// <remarks>Where the <see cref="IValidationResult"/> <see cref="IValidationResult.HasErrors"/> the corresponding <see cref="IResult.Error"/> will be updated with the <see cref="IValidationResult.ToException"/>.</remarks>
    public static async Task<Result<T>> ValidateAsync<T>(this Task<Result<T>> result, IValidator<T> validator, CancellationToken cancellationToken = default) where T : class
    {
        validator.ThrowIfNull();

        return await result.ThenAsync(async v =>
        {
            var vr = await validator.ValidateAsync(v, cancellationToken).ConfigureAwait(false);
            return vr.ToResult();
        });
    }

    /// <summary>
    /// Validates using the <paramref name="multiValidator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="multiValidator">The function to get the <see cref="MultiValidator"/> instance.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> ValidateAsync(this Result result, Func<MultiValidator> multiValidator, CancellationToken cancellationToken = default)
    {
        multiValidator.ThrowIfNull();

        return await result.ThenAsync(async () =>
        {
            var mv = multiValidator() ?? throw new InvalidOperationException($"The {nameof(multiValidator)} function must return a non-null instance to perform the requested validation.");
            var vr = await mv.ValidateAsync(cancellationToken).ConfigureAwait(false);
            return vr.ToResult();
        });
    }

    /// <summary>
    /// Validates using the <paramref name="multiValidator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="multiValidator">The function to get the <see cref="MultiValidator"/> instance.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> ValidateAsync(this Task<Result> result, Func<MultiValidator> multiValidator, CancellationToken cancellationToken = default)
    {
        multiValidator.ThrowIfNull();

        return await result.ThenAsync(async () =>
        {
            var mv = multiValidator() ?? throw new InvalidOperationException($"The {nameof(multiValidator)} function must return a non-null instance to perform the requested validation.");
            var vr = await mv.ValidateAsync(cancellationToken).ConfigureAwait(false);
            return vr.ToResult();
        });
    }

    /// <summary>
    /// Validates using the <paramref name="multiValidator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="multiValidator">The function to get the <see cref="MultiValidator"/> instance.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> ValidateAsync<T>(this Result<T> result, Func<T, MultiValidator> multiValidator, CancellationToken cancellationToken = default)
    {
        multiValidator.ThrowIfNull();

        return await result.ThenAsync(async v =>
        {
            var mv = multiValidator(v) ?? throw new InvalidOperationException($"The {nameof(multiValidator)} function must return a non-null instance to perform the requested validation.");
            var vr = await mv.ValidateAsync(cancellationToken).ConfigureAwait(false);
            return vr.ToResult().Bind<T>();
        });
    }

    /// <summary>
    /// Validates using the <paramref name="multiValidator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="multiValidator">The function to get the <see cref="MultiValidator"/> instance.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> ValidateAsync<T>(this Task<Result<T>> result, Func<T, MultiValidator> multiValidator, CancellationToken cancellationToken = default)
    {
        multiValidator.ThrowIfNull();

        return await result.ThenAsync(async v =>
        {
            var mv = multiValidator(v) ?? throw new InvalidOperationException($"The {nameof(multiValidator)} function must return a non-null instance to perform the requested validation.");
            var vr = await mv.ValidateAsync(cancellationToken).ConfigureAwait(false);
            return vr.ToResult().Bind<T>();
        });
    }
}