namespace CoreEx.Results;

public static partial class ResultsExtensions
{
    #region Synchronous

    /// <summary>
    /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static Result Then(this Result result, Action action)
    {
        ThrowIfNull(result, action, nameof(action));
        if (result.IsSuccess)
            action();

        return result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static Result Then(this Result result, Func<Result> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? func() : result;
    }

    /// <summary>
    /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> Then<T>(this Result<T> result, Action<T> action)
    {
        ThrowIfNull(result, action, nameof(action));
        if (result.IsSuccess)
            action(result.Value);

        return result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> Then<T>(this Result<T> result, Func<T, T> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? Result<T>.Ok(func(result.Value)) : result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> Then<T>(this Result<T> result, Func<T, Result<T>> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? func(result.Value) : result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (the <paramref name="result"/> <see cref="Result{T}.Value"/> will not be lost).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> Then<T>(this Result<T> result, Func<T, Result> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? func(result.Value).Combine(result) : result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> ThenAs<T>(this Result result, Func<T> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? Result<T>.Ok(func()) : result.Bind<T>();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<T> ThenAs<T>(this Result result, Func<Result<T>> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? func() : result.Bind<T>();
    }

    /// <summary>
    /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static Result ThenAs<T>(this Result<T> result, Action<T> action)
    {
        ThrowIfNull(result, action, nameof(action));
        if (result.IsSuccess)
            action(result.Value);

        return result.Bind();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static Result ThenAs<T>(this Result<T> result, Func<T, Result> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? func(result.Value) : result.Bind<T>();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<U> ThenAs<T, U>(this Result<T> result, Func<T, U> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? Result<U>.Ok(func(result.Value)) : result.Bind<T, U>();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static Result<U> ThenAs<T, U>(this Result<T> result, Func<T, Result<U>> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? func(result.Value) : result.Bind<T, U>();
    }

    #endregion

    #region AsyncResult

    /// <summary>
    /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> Then(this Task<Result> result, Action action)
    {
        ThrowIfNull(result, action, nameof(action));
        var r = await result.ConfigureAwait(false);
        return r.Then(action);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> Then(this Task<Result> result, Func<Result> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.Then(func);
    }

    /// <summary>
    /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> Then<T>(this Task<Result<T>> result, Action<T> action)
    {
        ThrowIfNull(result, action, nameof(action));
        var r = await result.ConfigureAwait(false);
        return r.Then(action);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> Then<T>(this Task<Result<T>> result, Func<T, T> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.Then(func);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> Then<T>(this Task<Result<T>> result, Func<T, Result<T>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.Then(func);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (the <paramref name="result"/> <see cref="Result{T}.Value"/> will not be lost).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> Then<T>(this Task<Result<T>> result, Func<T, Result> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.Then(func);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> ThenAs<T>(this Task<Result> result, Func<T> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.ThenAs(func);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> ThenAs<T>(this Task<Result> result, Func<Result<T>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.ThenAs(func);
    }

    /// <summary>
    /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> ThenAs<T>(this Task<Result<T>> result, Action<T> action)
    {
        ThrowIfNull(result, action, nameof(action));
        var r = await result.ConfigureAwait(false);
        return r.ThenAs(action);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> ThenAs<T>(this Task<Result<T>> result, Func<T, Result> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.ThenAs(func);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<U>> ThenAs<T, U>(this Task<Result<T>> result, Func<T, U> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.ThenAs<T, U>(func);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<U>> ThenAs<T, U>(this Task<Result<T>> result, Func<T, Result<U>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return r.ThenAs<T, U>(func);
    }

    #endregion

    #region AsyncFunc

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> ThenAsync(this Result result, Func<Task> func)
    {
        ThrowIfNull(result, func);
        if (result.IsSuccess)
            await func().ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> ThenAsync(this Result result, Func<Task<Result>> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? await func().ConfigureAwait(false) : result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> ThenAsync<T>(this Result<T> result, Func<T, Task> func)
    {
        ThrowIfNull(result, func);
        if (result.IsSuccess)
            await func(result.Value).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> ThenAsync<T>(this Result<T> result, Func<T, Task<T>> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? Result<T>.Ok(await func(result.Value).ConfigureAwait(false)) : result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> ThenAsync<T>(this Result<T> result, Func<T, Task<Result<T>>> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? await func(result.Value).ConfigureAwait(false) : result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (the <paramref name="result"/> <see cref="Result{T}.Value"/> will not be lost).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> ThenAsync<T>(this Result<T> result, Func<T, Task<Result>> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? (await func(result.Value).ConfigureAwait(false)).Combine(result) : result;
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> ThenAsAsync<T>(this Result result, Func<Task<T>> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? Result<T>.Ok(await func().ConfigureAwait(false)) : result.Bind<T>();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> ThenAsAsync<T>(this Result result, Func<Task<Result<T>>> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? await func().ConfigureAwait(false) : result.Bind<T>();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> ThenAsAsync<T>(this Result<T> result, Func<T, Task> func)
    {
        ThrowIfNull(result, func);
        if (result.IsSuccess)
            await func(result.Value).ConfigureAwait(false);

        return result.Bind();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> ThenAsAsync<T>(this Result<T> result, Func<T, Task<Result>> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? await func(result.Value).ConfigureAwait(false) : result.Bind<T>();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<U>> ThenAsAsync<T, U>(this Result<T> result, Func<T, Task<U>> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? Result<U>.Ok(await func(result.Value).ConfigureAwait(false)) : result.Bind<T, U>();
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<U>> ThenAsAsync<T, U>(this Result<T> result, Func<T, Task<Result<U>>> func)
    {
        ThrowIfNull(result, func);
        return result.IsSuccess ? await func(result.Value).ConfigureAwait(false) : result.Bind<T, U>();
    }

    #endregion

    #region AsyncBoth

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> ThenAsync(this Task<Result> result, Func<Task> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.ThenAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result"/>.</returns>
    public static async Task<Result> ThenAsync(this Task<Result> result, Func<Task<Result>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.ThenAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <return>The resulting <see cref="Result{T}"/>.</return>
    public static async Task<Result<T>> ThenAsync<T>(this Task<Result<T>> result, Func<T, Task> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.ThenAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <return>The resulting <see cref="Result{T}"/>.</return>
    public static async Task<Result<T>> ThenAsync<T>(this Task<Result<T>> result, Func<T, Task<T>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.ThenAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <return>The resulting <see cref="Result{T}"/>.</return>
    public static async Task<Result<T>> ThenAsync<T>(this Task<Result<T>> result, Func<T, Task<Result<T>>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.ThenAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (the <paramref name="result"/> <see cref="Result{T}.Value"/> will not be lost).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    public static async Task<Result<T>> ThenAsync<T>(this Task<Result<T>> result, Func<T, Task<Result>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.ThenAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <return>The resulting <see cref="Result{T}"/>.</return>
    public static async Task<Result<T>> ThenAsAsync<T>(this Task<Result> result, Func<Task<T>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.ThenAsAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <return>The resulting <see cref="Result{T}"/>.</return>
    public static async Task<Result<T>> ThenAsAsync<T>(this Task<Result> result, Func<Task<Result<T>>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.ThenAsAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <return>The resulting <see cref="Result"/>.</return>
    public static async Task<Result> ThenAsAsync<T>(this Task<Result<T>> result, Func<T, Task> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.ThenAsAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <return>The resulting <see cref="Result"/>.</return>
    public static async Task<Result> ThenAsAsync<T>(this Task<Result<T>> result, Func<T, Task<Result>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.ThenAsAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <return>The resulting <see cref="Result{T}"/>.</return>
    public static async Task<Result<U>> ThenAsAsync<T, U>(this Task<Result<T>> result, Func<T, Task<U>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.ThenAsAsync(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result{T}"/>.</param>
    /// <param name="func">The function to invoke.</param>
    /// <return>The resulting <see cref="Result{T}"/>.</return>
    public static async Task<Result<U>> ThenAsAsync<T, U>(this Task<Result<T>> result, Func<T, Task<Result<U>>> func)
    {
        ThrowIfNull(result, func);
        var r = await result.ConfigureAwait(false);
        return await r.ThenAsAsync(func).ConfigureAwait(false);
    }

    #endregion
}