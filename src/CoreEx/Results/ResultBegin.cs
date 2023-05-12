// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Results
{
    public readonly partial struct Result
    {
        #region Begin

        /// <summary>
        /// Begins a new <see cref="Result"/> chain.
        /// </summary>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Begin() => Successful;

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by executing the <paramref name="action"/>.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Begin(Action action) => Successful.Then(action);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Begin(Func<Result> func) => Successful.Then(func);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="action"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Begin<T>(Action action) => Result<T>.Successful.Then(action);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Begin<T>(Func<T> func) => Result<T>.Successful.Then(func);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Begin<T>(Func<Result<T>> func) => Result<T>.Successful.Then(func);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Begin<T>() => Result<T>.Successful;

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain with the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The <see cref="Result{T}.Value"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Begin<T>(T value) => Begin(() => value);

        #endregion

        #region BeginAsync

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> BeginAsync(Func<Task> func) => Successful.ThenAsync(func);

        /// <summary>
        /// Begins a new <see cref="Result"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> BeginAsync(Func<Task<Result>> func) => Successful.ThenAsync(func);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> Begin<T>(Func<Task> func) => Result<T>.Successful.ThenAsync(func);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> BeginAsync<T>(Func<Task<T>> func) => Result<T>.Successful.ThenAsync(func);

        /// <summary>
        /// Begins a new <see cref="Result{T}"/> chain by executing the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> BeginAsync<T>(Func<Task<Result<T>>> func) => await Result<T>.Successful.ThenAsync(func).ConfigureAwait(false);

        #endregion
    }
}