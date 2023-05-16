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
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Go(Action action) => Success.Then(action);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Go(Func<Result> func) => Success.Then(func);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="action"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Go<T>(Action action) => Result<T>.None.Then(action);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Go<T>(Func<T> func) => Result<T>.None.Then(func);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Go<T>(Func<Result<T>> func) => Result<T>.None.Then(func);

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
        /// <param name="value">The <see cref="Result{T}.Value"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Go<T>(T value) => Go(() => value);

        #endregion

        #region GoAsync

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> GoAsync(Func<Task> func) => Success.ThenAsync(func);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> GoAsync(Func<Task<Result>> func) => Success.ThenAsync(func);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> GoAsync<T>(Func<Task> func) => Result<T>.None.ThenAsync(func);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> GoAsync<T>(Func<Task<Result<T>>> func) => await Result<T>.None.ThenAsync(func).ConfigureAwait(false);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> GoAsync<T>(Func<Task<T>> func) => Result<T>.None.ThenAsync(func);

        #endregion
    }
}