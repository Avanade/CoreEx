namespace CoreEx.Results;

public static partial class ResultsExtensions
{
    #region Synchronous

    /// <summary>
    /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static Result OnFailure(this Result result, Action<Result> action)
    {
        ThrowIfNull(result, action, nameof(action));
        if (result.IsFailure)
            action(result);

        return result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static Result OnFailure(this Result result, Func<Result, Result> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? func(result) : result;
    }

    /// <summary>
    /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> OnFailure<T>(this Result<T> result, Action<Result<T>> action)
    {
        ThrowIfNull(result, action, nameof(action));
        if (result.IsFailure)
            action(result);

        return result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> OnFailure<T>(this Result<T> result, Func<Result<T>, T> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? Result<T>.Ok(func(result)) : result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> OnFailure<T>(this Result<T> result, Func<Result<T>, Result<T>> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? func(result) : result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> OnFailureAs<T>(this Result result, Func<Result, T> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? Result<T>.Ok(func(result)) : result.Bind<T>();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> OnFailureAs<T>(this Result result, Func<Result, Result<T>> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? func(result) : result.Bind<T>();
    }

    /// <summary>
    /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static Result OnFailureAs<T>(this Result<T> result, Action<Result<T>> action)
    {
        ThrowIfNull(result, action, nameof(action));
        if (result.IsFailure)
            action(result);

        return result.Bind();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static Result OnFailureAs<T>(this Result<T> result, Func<Result<T>, Result> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? func(result) : result.Bind();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<U> OnFailureAs<T, U>(this Result<T> result, Func<Result<T>, U> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? Result<U>.Ok(func(result)) : result.Bind<T, U>();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<U> OnFailureAs<T, U>(this Result<T> result, Func<Result<T>, Result<U>> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? func(result) : result.Bind<T, U>();
    }

    #endregion

    #region AsyncResult

    /// <summary>
    /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> OnFailure(this Task<Result> result, Action<Result> action)
    {
        ThrowIfNull(result, action, nameof(action));
        var r = await result.ConfigureAwait(false);
        return r.OnFailure(action);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> OnFailure(this Task<Result> result, Func<Result, Result> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.OnFailure(func);
    }

    /// <summary>
    /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> result, Action<Result<T>> action)
    {
        ThrowIfNull(result, action, nameof(action));
        var r = await result.ConfigureAwait(false);
        return r.OnFailure<T>(action);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> result, Func<Result<T>, T> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.OnFailure(func);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> result, Func<Result<T>, Result<T>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.OnFailure(func);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailureAs<T>(this Task<Result> result, Func<Result, T> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.OnFailureAs(func);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailureAs<T>(this Task<Result> result, Func<Result, Result<T>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.OnFailureAs(func);
    }

    /// <summary>
    /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> OnFailureAs<T>(this Task<Result<T>> result, Action<Result<T>> action)
    {
        ThrowIfNull(result, action, nameof(action));
        var r = await result.ConfigureAwait(false);
        return r.OnFailureAs(action);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> OnFailureAs<T>(this Task<Result<T>> result, Func<Result<T>, Result> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.OnFailureAs(func);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<U>> OnFailureAs<T, U>(this Task<Result<T>> result, Func<Result<T>, U> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.OnFailureAs<T, U>(func);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<U>> OnFailureAs<T, U>(this Task<Result<T>> result, Func<Result<T>, Result<U>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.OnFailureAs<T, U>(func);
    }

    #endregion

    #region AsyncFunc

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> OnFailureAsync(this Result result, Func<Result, Task> func)
    {
        ThrowIfNull(result, func);
        if (result.IsFailure)
            await func(result).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> OnFailureAsync(this Result result, Func<Result, Task<Result>> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? await func(result).ConfigureAwait(false) : result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailureAsync<T>(this Result<T> result, Func<Result<T>, Task> func)
    {
        ThrowIfNull(result, func);
        if (result.IsFailure)
            await func(result).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailureAsync<T>(this Result<T> result, Func<Result<T>, Task<T>> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? Result<T>.Ok(await func(result).ConfigureAwait(false)) : result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailureAsync<T>(this Result<T> result, Func<Result<T>, Task<Result<T>>> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? await func(result).ConfigureAwait(false) : result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailureAsAsync<T>(this Result result, Func<Result, Task<T>> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? Result<T>.Ok(await func(result).ConfigureAwait(false)) : result.Bind<T>();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailureAsAsync<T>(this Result result, Func<Result, Task<Result<T>>> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? await func(result).ConfigureAwait(false) : result.Bind<T>();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> OnFailureAsAsync<T>(this Result<T> result, Func<Result<T>, Task> func)
    {
        ThrowIfNull(result, func);
        if (result.IsFailure)
            await func(result).ConfigureAwait(false);

        return result.Bind();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> OnFailureAsAsync<T>(this Result<T> result, Func<Result<T>, Task<Result>> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? await func(result).ConfigureAwait(false) : result.Bind<T>();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<U>> OnFailureAsAsync<T, U>(this Result<T> result, Func<Result<T>, Task<U>> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? Result<U>.Ok(await func(result).ConfigureAwait(false)) : result.Bind<T, U>();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<U>> OnFailureAsAsync<T, U>(this Result<T> result, Func<Result<T>, Task<Result<U>>> func)
    {
        ThrowIfNull(result, func);
        return result.IsFailure ? await func(result).ConfigureAwait(false) : result.Bind<T, U>();
    }

    #endregion

    #region AsyncBoth

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> OnFailureAsync(this Task<Result> result, Func<Result, Task> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.OnFailureAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> OnFailureAsync(this Task<Result> result, Func<Result, Task<Result>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.OnFailureAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailureAsync<T>(this Task<Result<T>> result, Func<Result<T>, Task> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.OnFailureAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailureAsync<T>(this Task<Result<T>> result, Func<Result<T>, Task<T>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.OnFailureAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailureAsync<T>(this Task<Result<T>> result, Func<Result<T>, Task<Result<T>>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.OnFailureAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailureAsAsync<T>(this Task<Result> result, Func<Result, Task<T>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.OnFailureAsAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> OnFailureAsAsync<T>(this Task<Result> result, Func<Result, Task<Result<T>>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.OnFailureAsAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> OnFailureAsAsync<T>(this Task<Result<T>> result, Func<Result<T>, Task> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.OnFailureAsAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> OnFailureAsAsync<T>(this Task<Result<T>> result, Func<Result<T>, Task<Result>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.OnFailureAsAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<U>> OnFailureAsAsync<T, U>(this Task<Result<T>> result, Func<Result<T>, Task<U>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.OnFailureAsAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<U>> OnFailureAsAsync<T, U>(this Task<Result<T>> result, Func<Result<T>, Task<Result<U>>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.OnFailureAsAsync(func).ConfigureAwait(false);
    }

    #endregion
}