// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoreEx.Results
{
    /// <summary>
    /// Provides the <see cref="Result"/> and <see cref="Result{T}"/> <c>Any</c> and <c>AnyAsync</c> extension methods to execute the corresponding function regardless of the <see cref="IResult"/> state.
    /// </summary>
    [DebuggerStepThrough]
    public static class AnyExtensions
    {
        #region Synchronous

        /// <summary>
        /// Invokes the <paramref name="action"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Any(this Result result, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            action();
            return result;
        }

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Needed to enable fluent-style method-chaining.")]
        public static Result Any(this Result result, Func<Result> func) => (func ?? throw new ArgumentNullException(nameof(func))).Invoke();

        /// <summary>
        /// Invokes the <paramref name="action"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Any<T>(this Result<T> result, Action<Result<T>> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            action(result);
            return result;
        }

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Needed to enable fluent-style method-chaining.")]
        public static Result<U> Any<T, U>(this Result<T> result, Func<Result<U>> func) => (func ?? throw new ArgumentNullException(nameof(func))).Invoke();

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> Any<T, U>(this Result<T> result, Func<Result<T>, Result<U>> func) => (func ?? throw new ArgumentNullException(nameof(func))).Invoke(result);

        #endregion

        #region AsyncResult

        /// <summary>
        /// Invokes the <paramref name="action"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> Any(this Task<Result> result, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var r = await result.ConfigureAwait(false);
            action();
            return r;
        }

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> Any(this Task<Result> result, Func<Result> func)
        {
            var r = await result.ConfigureAwait(false);
            return (func ?? throw new ArgumentNullException(nameof(func))).Invoke();
        }

        /// <summary>
        /// Invokes the <paramref name="action"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> Any<T>(this Task<Result<T>> result, Action<Result<T>> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var r = await result.ConfigureAwait(false);
            action(r);
            return r;
        }

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> Any<T, U>(this Task<Result<T>> result, Func<Result<U>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            await result.ConfigureAwait(false);
            return func();
        }

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> Any<T, U>(this Task<Result<T>> result, Func<Result<T>, Result<U>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return func(r);
        }

        #endregion

        #region AsyncFunc

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsync(this Result result, Func<Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            await func().ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsync(this Result result, Func<Result, Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return await func(result).ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> AnyAsync<T>(this Result<T> result, Func<Result<T>, Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            await func(result).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Needed to enable fluent-style method-chaining.")]
        public static Task<Result<U>> AnyAsync<T, U>(this Result<T> result, Func<Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return func();
        }

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> AnyAsync<T, U>(this Result<T> result, Func<Result<T>, Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return await func(result).ConfigureAwait(false);
        }

        #endregion

        #region AsyncBoth

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsync(this Task<Result> result, Func<Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            await func().ConfigureAwait(false);
            return r;
        }

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> AnyAsync(this Task<Result> result, Func<Result, Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await func(r).ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> AnyAsync<T>(this Task<Result<T>> result, Func<Result<T>, Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            await func(r).ConfigureAwait(false);
            return r;
        }

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> AnyAsync<T, U>(this Task<Result<T>> result, Func<Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            await result.ConfigureAwait(false);
            return await func().ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes the <paramref name="func"/> regardless of the <paramref name="result"/> state.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> AnyAsync<T, U>(this Task<Result<T>> result, Func<Result<T>, Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await func(r).ConfigureAwait(false);
        }

        #endregion
    }
}