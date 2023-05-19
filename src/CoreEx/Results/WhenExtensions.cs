// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoreEx.Results
{
    /// <summary>
    /// Provides the <see cref="Result"/> and <see cref="Result{T}"/> <c>When</c> and <c>WhenAsync</c> extension methods to execute the corresponding function when <see cref="IResult.IsSuccess"/> and the specified condition/predicate evaluates to <c>true</c>.
    /// </summary>
    [DebuggerStepThrough]
    public static class WhenExtensions
    {
        #region Synchronous

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result When(this Result result, Func<bool> condition, Action action)
        {
            if (result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke())
                (action ?? throw new ArgumentNullException(nameof(action))).Invoke();

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result When(this Result result, Func<bool> condition, Func<Result> func)
            => result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke() ? (func ?? throw new ArgumentNullException(nameof(func))).Invoke() : result;

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> When<T>(this Result result, Func<bool> condition, Func<T> func)
            => result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke() ? (func ?? throw new ArgumentNullException(nameof(func))).Invoke() : result;

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The <see cref="Action{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result When<T>(this Result<T> result, Predicate<T> condition, Action<T> action)
            => result.When(v => (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(v), () => (action ?? throw new ArgumentNullException(nameof(action))).Invoke(result.Value));

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result When<T>(this Result<T> result, Predicate<T> condition, Func<T, Result> func)
            => result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value) ? (func ?? throw new ArgumentNullException(nameof(func))).Invoke(result.Value) : result;

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> When<T>(this Result result, Func<bool> condition, Func<Result<T>> func)
            => result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke() ? (func ?? throw new ArgumentNullException(nameof(func))).Invoke() : result;

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> When<T>(this Result<T> result, Predicate<T> condition, Action action)
        {
            if (result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value))
                (action ?? throw new ArgumentNullException(nameof(action))).Invoke();

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result When<T>(this Result<T> result, Predicate<T> condition, Func<Result> func)
            => result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value) ? (func ?? throw new ArgumentNullException(nameof(func))).Invoke() : result;

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> When<T, U>(this Result<T> result, Predicate<T> condition, Func<U> func)
            => result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value) ? Result<U>.Ok((func ?? throw new ArgumentNullException(nameof(func))).Invoke()) : result.Combine(Result<U>.None);

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> When<T, U>(this Result<T> result, Predicate<T> condition, Func<Result<U>> func)
            => result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value) ? (func ?? throw new ArgumentNullException(nameof(func))).Invoke() : result.Combine(Result<U>.None);

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> When<T, U>(this Result<T> result, Predicate<T> condition, Func<T, Result<U>> func)
            => result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value) ? (func ?? throw new ArgumentNullException(nameof(func))).Invoke(result.Value) : result.Combine(Result<U>.None);

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> When<T, U>(this Result<T> result, Predicate<T> condition, Func<T, U> func)
            => result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value) ? (func ?? throw new ArgumentNullException(nameof(func))).Invoke(result.Value) : result.Combine(Result<U>.None);

        #endregion

        #region AsyncResult

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> When(this Task<Result> result, Func<bool> condition, Action action)
        {
            var r = await result.ConfigureAwait(false);
            return r.When(condition, action);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> When(this Task<Result> result, Func<bool> condition, Func<Result> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>. 
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> When<T>(this Task<Result> result, Func<bool> condition, Func<T> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The <see cref="Action{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> When<T>(this Task<Result<T>> result, Predicate<T> condition, Action<T> action)
        {
            var r = await result.ConfigureAwait(false);
            return r.When(condition, action);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> When<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Result> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> When<T>(this Task<Result> result, Func<bool> condition, Func<Result<T>> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> When<T>(this Task<Result<T>> result, Predicate<T> condition, Action action)
        {
            var r = await result.ConfigureAwait(false);
            return r.When(condition,action);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result> When<T>(this Task<Result<T>> result, Predicate<T> condition, Func<Result> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.When<T>(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> When<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<U> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> When<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<Result<U>> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.When<T, U>(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> When<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Result<U>> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> When<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, U> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func);
        }

        #endregion

        #region AsyncFunc

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync(this Result result, Func<bool> condition, Func<Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            if (result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke())
                await func().ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync(this Result result, Func<bool> condition, Func<Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke() ? await func().ConfigureAwait(false) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>. 
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Result result, Func<bool> condition, Func<Task<T>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke() ? await func().ConfigureAwait(false) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            if (result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value))
                await func(result.Value).ConfigureAwait(false);

            return result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value) ? await func(result.Value).ConfigureAwait(false) : result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Result result, Func<bool> condition, Func<Task<Result<T>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke() ? await func().ConfigureAwait(false) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Result<T> result, Predicate<T> condition, Func<Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            if (result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value))
                await func().ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAsync<T, U>(this Result<T> result, Predicate<T> condition, Func<Task<U>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value) ? Result<U>.Ok(await func().ConfigureAwait(false)) : result.Combine(Result<U>.None);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result> WhenAsync<T>(this Result<T> result, Predicate<T> condition, Func<Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value) ? await func().ConfigureAwait(false) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAsync<T, U>(this Result<T> result, Predicate<T> condition, Func<Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value) ? await func().ConfigureAwait(false) : result.Combine(Result<U>.None);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAsync<T, U>(this Result<T> result, Predicate<T> condition, Func<T, Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value) ? await func(result.Value).ConfigureAwait(false) : result.Combine(Result<U>.None);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAsync<T, U>(this Result<T> result, Predicate<T> condition, Func<T, Task<U>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(result.Value) ? await func(result.Value).ConfigureAwait(false) : result.Combine(Result<U>.None);
        }

        #endregion

        #region AsyncBoth

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync(this Task<Result> result, Func<bool> condition, Func<Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync(this Task<Result> result, Func<bool> condition, Func<Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>. 
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Task<Result> result, Func<bool> condition, Func<Task<T>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            if (r.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(r.Value))
                await func(r.Value).ConfigureAwait(false);

            return r.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return r.IsSuccess && (condition ?? throw new ArgumentNullException(nameof(condition))).Invoke(r.Value) ? await func(r.Value).ConfigureAwait(false) : r.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Task<Result> result, Func<bool> condition, Func<Task<Result<T>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAsync<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<Task<U>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result> WhenAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAsync<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAsync<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and the specified <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAsync<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<U>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        #endregion
    }
}