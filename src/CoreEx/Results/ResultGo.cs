// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Results
{
    public readonly partial struct Result
    {
        #region Go

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
        /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="result"/>.
        /// </summary>
        /// <param name="result">The starting <see cref="IToResult"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result GoFrom(IToResult result) => Success.ThenFrom(() => result);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="func"/>.
        /// </summary>
        /// <param name="func">The starting <see cref="IToResult"/> function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result GoFrom(Func<IToResult> func) => Success.ThenFrom(func);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Go<T>(Func<T> func) => Result<T>.None.Then(_ => func());

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Go<T>(Func<Result<T>> func) => Result<T>.None.Then(_ => func());

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Go<T>() => Result<T>.None;

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain with the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The starting value.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Go<T>(T value) => Go(() => Result<T>.Ok(value));

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by starting with the <paramref name="result"/>.
        /// </summary>
        /// <param name="result">The starting <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Go<T>(Result<T> result) => result;

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The starting <see cref="IToResult{T}"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> GoFrom<T>(IToResult<T> result) => Result<T>.None.ThenFrom(_ => result);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The starting <see cref="IToResult{T}"/> function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> GoFrom<T>(Func<IToResult<T>> func) => Result<T>.None.ThenFrom(_ => func());

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The starting <see cref="ITypedToResult"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> GoFrom<T>(ITypedToResult result) => Result<T>.None.ThenFrom(_ => result);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The starting <see cref="IToResult{T}"/> function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> GoFrom<T>(Func<ITypedToResult> func) => Result<T>.None.ThenFrom(_ => func());

        #endregion

        #region GoAsync

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> GoAsync(Func<Task> func) => Success.ThenAsync(func);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> GoAsync(Func<Task<Result>> func) => Success.ThenAsync(func);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="result"/>.
        /// </summary>
        /// <param name="result">The starting <see cref="Result"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> GoAsync(Task<Result> result) => await result.ConfigureAwait(false);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="result"/>.
        /// </summary>
        /// <param name="result">The starting <see cref="IToResult"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> GoFromAsync(Task<IToResult> result) => Success.ThenFromAsync(() => result);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="func"/>.
        /// </summary>
        /// <param name="func">The starting <see cref="IToResult"/> function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> GoFromAsync(Func<Task<IToResult>> func) => Success.ThenFromAsync(async () => await func());

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> GoAsync<T>(Func<Task<Result<T>>> func) => Result<T>.None.ThenAsync(async _ => await func().ConfigureAwait(false));

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> GoAsync<T>(Func<Task<T>> func) => Result<T>.None.ThenAsync(async _ => await func().ConfigureAwait(false));

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by starting with the <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The starting <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> GoAsync<T>(Task<Result<T>> result) => await result.ConfigureAwait(false);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The starting <see cref="IToResult{T}"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result<T>> GoFromAsync<T>(Task<IToResult<T>> result) => Result<T>.None.ThenFromAsync(_ => result);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The starting <see cref="IToResult{T}"/> function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result<T>> GoFromAsync<T>(Func<Task<IToResult<T>>> func) => Result<T>.None.ThenFromAsync(async _ => await func().ConfigureAwait(false));

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The starting <see cref="ITypedToResult"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result<T>> GoFromAsync<T>(Task<ITypedToResult> result) => Result<T>.None.ThenFromAsync(_ => result);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by starting with the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The starting <see cref="IToResult{T}"/> function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result<T>> GoFromAsync<T>(Func<Task<ITypedToResult>> func) => Result<T>.None.ThenFromAsync(async _ => await func().ConfigureAwait(false));

        #endregion
    }
}