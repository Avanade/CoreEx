namespace CoreEx.Results;

partial struct Result
{
    /// <summary>
    /// Begins a new <see cref="Result"/> chain.
    /// </summary>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static Result Go() => Success;

    /// <summary>
    /// Begins a new <see cref="Result"/> chain by executing the <paramref name="action"/>.
    /// </summary>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static Result Go(Action action) => Success.Then(action);

    /// <summary>
    /// Begins a new <see cref="Result"/> chain by executing the <paramref name="func"/>.
    /// </summary>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static Result Go(Func<Result> func) => Success.Then(func);

    /// <summary>
    /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="result"/>.
    /// </summary>
    /// <param name="result">The starting <see cref="Result"/>.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static Result Go(Result result) => result;

    /// <summary>
    /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> Go<T>(Func<T> func) => Result<T>.Success.Then(_ => func());

    /// <summary>
    /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> Go<T>(Func<Result<T>> func) => Result<T>.Success.Then(_ => func());

    /// <summary>
    /// Begins a new <see cref="Result{T}"/> chain.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> Go<T>() => Result<T>.Success;

    /// <summary>
    /// Begins a new <see cref="Result{T}"/> chain with the specified <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The starting value.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> Go<T>(T value) => Result<T>.Ok(value);

    /// <summary>
    /// Begins a new <see cref="Result{T}"/> chain by starting with the <paramref name="result"/>.
    /// </summary>
    /// <param name="result">The starting <see cref="Result{T}"/>.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> Go<T>(Result<T> result) => result;

    /// <summary>
    /// Begins a new <see cref="Result"/> chain by executing the <paramref name="func"/>.
    /// </summary>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> GoAsync(Func<Task> func) => await Success.ThenAsync(func).ConfigureAwait(false);

    /// <summary>
    /// Begins a new <see cref="Result"/> chain by executing the <paramref name="func"/>.
    /// </summary>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> GoAsync(Func<Task<Result>> func) => await Success.ThenAsync(func).ConfigureAwait(false);

    /// <summary>
    /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="result"/>.
    /// </summary>
    /// <param name="result">The starting <see cref="Result"/>.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> GoAsync(Task<Result> result) => await result.ConfigureAwait(false);

    /// <summary>
    /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> GoAsync<T>(Func<Task<Result<T>>> func) => await Result<T>.Success.ThenAsync(async _ => await func().ConfigureAwait(false)).ConfigureAwait(false);

    /// <summary>
    /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> GoAsync<T>(Func<Task<T>> func) => await Result<T>.Success.ThenAsync(async _ => await func().ConfigureAwait(false)).ConfigureAwait(false);

    /// <summary>
    /// Begins a new <see cref="Result{T}"/> chain by starting with the <paramref name="result"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The starting <see cref="Result{T}"/>.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> GoAsync<T>(Task<Result<T>> result) => await result.ConfigureAwait(false);
}