// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoreEx.Results
{
    /// <summary>
    /// Provides the <see cref="Result"/> and <see cref="Result{T}"/> <c>Any</c> and <c>AnyAsync</c> extension methods to execute the corresponding function regardless of the <see cref="IResult.IsSuccess"/> or <see cref="IResult.IsFailure"/> state.
    /// </summary>
    [DebuggerStepThrough]
    public static class AnyExtensions
    {
        #region Synchronous

        /// <summary>
        /// Executes the <paramref name="action"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Any(this Result result, Action action)
        {
            ThrowIfNull(result, action, nameof(action));
            action();
            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Any(this Result result, Func<Result> func)
        {
            ThrowIfNull(result, func);
            return func();
        }

        /// <summary>
        /// Executes the <paramref name="action"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="action">The <see cref="Action{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Any<T>(this Result<T> result, Action<T> action)
        {
            ThrowIfNull(result, action, nameof(action));
            action(result.Value);
            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Any<T>(this Result<T> result, Func<T, T> func)
        {
            ThrowIfNull(result, func);
            return Result<T>.Ok(func(result.Value));
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Any<T>(this Result<T> result, Func<T, Result<T>> func)
        {
            ThrowIfNull(result, func);
            return func(result.Value);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> AnyAs<T>(this Result result, Func<T> func)
        {
            ThrowIfNull(result, func);
            return Result<T>.Ok(func());
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> AnyAs<T>(this Result result, Func<Result<T>> func)
        {
            ThrowIfNull(result, func);
            return func();
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="action">The <see cref="Action{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result AnyAs<T>(this Result<T> result, Action<T> action)
        {
            ThrowIfNull(result, action, nameof(action));
            action(result.Value);
            return result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result AnyAs<T>(this Result<T> result, Func<T, Result> func)
        {
            ThrowIfNull(result, func);
            return func(result.Value);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> AnyAs<T, U>(this Result<T> result, Func<T, U> func)
        {
            ThrowIfNull(result, func);
            return Result<U>.Ok(func(result.Value));
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> AnyAs<T, U>(this Result<T> result, Func<T, Result<U>> func)
        {
            ThrowIfNull(result, func);
            return func(result.Value);
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Any(this Result result, Func<IToResult> func)
        {
            ThrowIfNull(result, func);
            return func().ToResult();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> Any<T>(this Result<T> result, Func<T, ITypedToResult> func)
        {
            ThrowIfNull(result, func);
            return func(result.Value).ToResult<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> Any<T>(this Result<T> result, Func<T, IToResult<T>> func)
        {
            ThrowIfNull(result, func);
            return func(result.Value).ToResult();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result AnyAs<T>(this Result<T> result, Func<T, IToResult> func)
        {
            ThrowIfNull(result, func);
            return func(result.Value).ToResult();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> AnyAs<T>(this Result result, Func<ITypedToResult> func)
        {
            ThrowIfNull(result, func);
            return func().ToResult<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> AnyAs<T>(this Result result, Func<IToResult<T>> func)
        {
            ThrowIfNull(result, func);
            return func().ToResult();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<U> AnyAs<T, U>(this Result<T> result, Func<T, ITypedToResult> func)
        {
            ThrowIfNull(result, func);
            return func(result.Value).ToResult<U>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<U> AnyAs<T, U>(this Result<T> result, Func<T, IToResult<U>> func)
        {
            ThrowIfNull(result, func);
            return func(result.Value).ToResult();
        }

        #endregion

        #region AsyncResult

        /// <summary>
        /// Executes the <paramref name="action"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> Any(this Task<Result> result, Action action)
        {
            ThrowIfNull(result, action, nameof(action));
            var r = await result.ConfigureAwait(false);
            return r.Any(action);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> Any(this Task<Result> result, Func<Result> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.Any(func);
        }

        /// <summary>
        /// Executes the <paramref name="action"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="action">The <see cref="Action{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> Any<T>(this Task<Result<T>> result, Action<T> action)
        {
            ThrowIfNull(result, action, nameof(action));
            var r = await result.ConfigureAwait(false);
            return r.Any(action);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> Any<T>(this Task<Result<T>> result, Func<T, T> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.Any(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> Any<T>(this Task<Result<T>> result, Func<T, Result<T>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.Any(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> AnyAs<T>(this Task<Result> result, Func<T> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.AnyAs(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> AnyAs<T>(this Task<Result> result, Func<Result<T>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.AnyAs(func);
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="action">The <see cref="Action{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAs<T>(this Task<Result<T>> result, Action<T> action)
        {
            ThrowIfNull(result, action, nameof(action));
            var r = await result.ConfigureAwait(false);
            return r.AnyAs(action);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAs<T>(this Task<Result<T>> result, Func<T, Result> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.AnyAs(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> AnyAs<T, U>(this Task<Result<T>> result, Func<T, U> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.AnyAs<T, U>(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> AnyAs<T, U>(this Task<Result<T>> result, Func<T, Result<U>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.AnyAs<T, U>(func);
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> Any(this Task<Result> result, Func<IToResult> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.Any(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> Any<T>(this Task<Result<T>> result, Func<T, ITypedToResult> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.Any(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> Any<T>(this Task<Result<T>> result, Func<T, IToResult<T>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.Any(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAs<T>(this Task<Result<T>> result, Func<T, IToResult> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.AnyAs(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> AnyAs<T>(this Task<Result> result, Func<ITypedToResult> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.AnyAs<T>(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> AnyAs<T>(this Task<Result> result, Func<IToResult<T>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.AnyAs(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> AnyAs<T, U>(this Task<Result<T>> result, Func<T, ITypedToResult> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.AnyAs<T, U>(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> AnyAs<T, U>(this Task<Result<T>> result, Func<T, IToResult<U>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.AnyAs<T, U>(func);
        }

        #endregion

        #region AsyncFunc

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsync(this Result result, Func<Task> func)
        {
            ThrowIfNull(result, func);
            if (result.IsSuccess)
                await func().ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsync(this Result result, Func<Task<Result>> func)
        {
            ThrowIfNull(result, func);
            return await func().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> AnyAsync<T>(this Result<T> result, Func<T, Task> func)
        {
            ThrowIfNull(result, func);
            if (result.IsSuccess)
                await func(result.Value).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> AnyAsync<T>(this Result<T> result, Func<T, Task<T>> func)
        {
            ThrowIfNull(result, func);
            return Result<T>.Ok(await func(result.Value).ConfigureAwait(false));
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> AnyAsync<T>(this Result<T> result, Func<T, Task<Result<T>>> func)
        {
            ThrowIfNull(result, func);
            return await func(result.Value).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> AnyAsAsync<T>(this Result result, Func<Task<T>> func)
        {
            ThrowIfNull(result, func);
            return Result<T>.Ok(await func().ConfigureAwait(false));
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> AnyAsAsync<T>(this Result result, Func<Task<Result<T>>> func)
        {
            ThrowIfNull(result, func);
            return await func().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsAsync<T>(this Result<T> result, Func<T, Task> func)
        {
            ThrowIfNull(result, func);
            if (result.IsSuccess)
                await func(result.Value).ConfigureAwait(false);

            return result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsAsync<T>(this Result<T> result, Func<T, Task<Result>> func)
        {
            ThrowIfNull(result, func);
            return await func(result.Value).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> AnyAsAsync<T, U>(this Result<T> result, Func<T, Task<U>> func)
        {
            ThrowIfNull(result, func);
            return Result<U>.Ok(await func(result.Value).ConfigureAwait(false));
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> AnyAsAsync<T, U>(this Result<T> result, Func<T, Task<Result<U>>> func)
        {
            ThrowIfNull(result, func);
            return await func(result.Value).ConfigureAwait(false);
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsync(this Result result, Func<Task<IToResult>> func)
        {
            ThrowIfNull(result, func);
            return (await func().ConfigureAwait(false)).ToResult();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> AnyAsync<T>(this Result<T> result, Func<T, Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, func);
            return (await func(result.Value).ConfigureAwait(false)).ToResult<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> AnyAsync<T>(this Result<T> result, Func<T, Task<IToResult<T>>> func)
        {
            ThrowIfNull(result, func);
            return (await func(result.Value).ConfigureAwait(false)).ToResult();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsAsync<T>(this Result<T> result, Func<T, Task<IToResult>> func)
        {
            ThrowIfNull(result, func);
            return (await func(result.Value).ConfigureAwait(false)).ToResult();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> AnyAsAsync<T>(this Result result, Func<Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, func);
            return (await func().ConfigureAwait(false)).ToResult<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> AnyAsAsync<T>(this Result result, Func<Task<IToResult<T>>> func)
        {
            ThrowIfNull(result, func);
            return (await func().ConfigureAwait(false)).ToResult();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> AnyAsAsync<T, U>(this Result<T> result, Func<T, Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, func);
            return (await func(result.Value).ConfigureAwait(false)).ToResult<U>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> AnyAsAsync<T, U>(this Result<T> result, Func<T, Task<IToResult<U>>> func)
        {
            ThrowIfNull(result, func);
            return (await func(result.Value).ConfigureAwait(false)).ToResult();
        }

        #endregion

        #region AsyncBoth

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsync(this Task<Result> result, Func<Task> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsync(this Task<Result> result, Func<Task<Result>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> AnyAsync<T>(this Task<Result<T>> result, Func<T, Task> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> AnyAsync<T>(this Task<Result<T>> result, Func<T, Task<T>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> AnyAsync<T>(this Task<Result<T>> result, Func<T, Task<Result<T>>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> AnyAsAsync<T>(this Task<Result> result, Func<Task<T>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> AnyAsAsync<T>(this Task<Result> result, Func<Task<Result<T>>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result"/>.</return>
        public static async Task<Result> AnyAsAsync<T>(this Task<Result<T>> result, Func<T, Task> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result"/>.</return>
        public static async Task<Result> AnyAsAsync<T>(this Task<Result<T>> result, Func<T, Task<Result>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<U>> AnyAsAsync<T, U>(this Task<Result<T>> result, Func<T, Task<U>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> regardless of the <paramref name="result"/> state (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<U>> AnyAsAsync<T, U>(this Task<Result<T>> result, Func<T, Task<Result<U>>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsAsync(func).ConfigureAwait(false);
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsync(this Task<Result> result, Func<Task<IToResult>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> AnyAsync<T>(this Task<Result<T>> result, Func<T, Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> AnyAsync<T>(this Task<Result<T>> result, Func<T, Task<IToResult<T>>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsAsync<T>(this Task<Result<T>> result, Func<T, Task<IToResult>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> AnyAsAsync<T>(this Task<Result> result, Func<Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsAsync<T>(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> AnyAsAsync<T>(this Task<Result> result, Func<Task<IToResult<T>>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> AnyAsAsync<T, U>(this Task<Result<T>> result, Func<T, Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsAsync<T, U>(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> AnyAsAsync<T, U>(this Task<Result<T>> result, Func<T, Task<IToResult<U>>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.AnyAsAsync<T, U>(func).ConfigureAwait(false);
        }

        #endregion

        /// <summary>
        /// Check parameters and throw where null.
        /// </summary>
        private static void ThrowIfNull(object result, object func, string? name = null)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (func == null) throw new ArgumentNullException(name ?? nameof(func));
        }
    }
}