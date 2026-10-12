namespace CoreEx.Database.Abstractions;

/// <summary>
/// Provides the default <see cref="RetryResiliency{TOwner}"/> wiring used by <see cref="DatabaseInvoker"/> for its opt-in <see cref="DatabaseArgsBase.RetryOnTransient"/> support.
/// </summary>
public static class DatabaseInvokerResiliency
{
    /// <summary>
    /// Creates the default <see cref="ResiliencePipeline{T}"/> used by <see cref="DatabaseInvoker"/> where <see cref="DatabaseArgsBase.RetryOnTransient"/> is <see langword="true"/> and no
    /// <see cref="DatabaseArgsBase.RetryResiliencePipeline"/> override has been supplied.
    /// </summary>
    /// <typeparam name="TResult">The invocation result <see cref="Type"/>.</typeparam>
    /// <param name="delay">The delay between retry attempts.</param>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts.</param>
    /// <param name="backoffType">The <see cref="DelayBackoffType"/> strategy.</param>
    /// <returns>A configured <see cref="ResiliencePipeline{T}"/> instance.</returns>
    /// <remarks>Classification of what is transient is deliberately <i>not</i> performed here via <c>ShouldHandle</c> - see <see cref="DatabaseInvoker"/>, which only ever surfaces a <see cref="Result{T}"/>
    /// failure to this pipeline once <see cref="IDatabase.IsTransientException(Exception)"/> has already confirmed the causing exception is transient; anything reaching this pipeline as a failure is
    /// therefore, by construction, always worth retrying.</remarks>
    public static ResiliencePipeline<Result<TResult>> CreateDefaultRetry<TResult>(TimeSpan? delay = null, int maxRetryAttempts = 3, DelayBackoffType backoffType = DelayBackoffType.Exponential)
        => RetryResiliency<IDatabase>.Create<Result<TResult>>(result => result.IsFailure, database => database.Logger ?? NullLogger.Instance, delay, maxRetryAttempts, backoffType);
}
