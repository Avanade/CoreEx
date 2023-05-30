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
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result When(this Result result, Func<bool> condition, Action action)
        {
            ThrowIfNull(result, condition, action, nameof(action));
            if (result.IsSuccess && condition())
                action();

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result When(this Result result, Func<bool> condition, Func<Result> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition() ? func() : result;
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The <see cref="Action{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> When<T>(this Result<T> result, Predicate<T> condition, Action<T> action)
        {
            ThrowIfNull(result, condition, action, nameof(action));
            if (result.IsSuccess && condition(result.Value))
                action(result.Value);

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> When<T>(this Result<T> result, Predicate<T> condition, Func<T, T> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? Result<T>.Ok(func(result.Value)) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> When<T>(this Result<T> result, Predicate<T> condition, Func<T, Result<T>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? func(result.Value) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> WhenAs<T>(this Result result, Func<bool> condition, Func<T> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition() ? Result<T>.Ok(func()) : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> WhenAs<T>(this Result result, Func<bool> condition, Func<Result<T>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition() ? func() : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The <see cref="Action{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WhenAs<T>(this Result<T> result, Predicate<T> condition, Action<T> action)
        {
            ThrowIfNull(result, condition, action, nameof(action));
            if (result.IsSuccess && condition(result.Value))
                action(result.Value);

            return result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WhenAs<T>(this Result<T> result, Predicate<T> condition, Func<T, Result> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? func(result.Value) : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> WhenAs<T, U>(this Result<T> result, Predicate<T> condition, Func<T, U> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? Result<U>.Ok(func(result.Value)) : result.Bind<T, U>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> WhenAs<T, U>(this Result<T> result, Predicate<T> condition, Func<T, Result<U>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? func(result.Value) : result.Bind<T, U>();
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WhenFrom(this Result result, Func<bool> condition, Func<IToResult> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition() ? func().ToResult() : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> WhenFrom<T>(this Result<T> result, Predicate<T> condition, Func<T, ITypedToResult> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? func(result.Value).ToResult<T>() : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> WhenFrom<T>(this Result<T> result, Predicate<T> condition, Func<T, IToResult<T>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? func(result.Value).ToResult() : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WhenFromAs<T>(this Result<T> result, Predicate<T> condition, Func<T, IToResult> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? func(result.Value).ToResult() : result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> WhenFromAs<T>(this Result result, Func<bool> condition, Func<ITypedToResult> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition() ? func().ToResult<T>() : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> WhenFromAs<T>(this Result result, Func<bool> condition, Func<IToResult<T>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition() ? func().ToResult() : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<U> WhenFromAs<T, U>(this Result<T> result, Predicate<T> condition, Func<T, ITypedToResult> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? func(result.Value).ToResult<U>() : result.Bind<T, U>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<U> WhenFromAs<T, U>(this Result<T> result, Predicate<T> condition, Func<T, IToResult<U>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? func(result.Value).ToResult() : result.Bind<T, U>();
        }

        #endregion

        #region AsyncResult

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> When(this Task<Result> result, Func<bool> condition, Action action)
        {
            ThrowIfNull(result, condition, action, nameof(action));
            var r = await result.ConfigureAwait(false);
            return r.When(condition, action);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> When(this Task<Result> result, Func<bool> condition, Func<Result> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The <see cref="Action{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> When<T>(this Task<Result<T>> result, Predicate<T> condition, Action<T> action)
        {
            ThrowIfNull(result, condition, action, nameof(action));
            var r = await result.ConfigureAwait(false);
            return r.When(condition, action);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> When<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, T> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> When<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Result<T>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAs<T>(this Task<Result> result, Func<bool> condition, Func<T> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenAs(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAs<T>(this Task<Result> result, Func<bool> condition, Func<Result<T>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenAs(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The <see cref="Action{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAs<T>(this Task<Result<T>> result, Predicate<T> condition, Action<T> action)
        {
            ThrowIfNull(result, condition, action, nameof(action));
            var r = await result.ConfigureAwait(false);
            return r.WhenAs(condition, action);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAs<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Result> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenAs(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAs<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, U> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenAs<T, U>(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAs<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Result<U>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenAs<T, U>(condition, func);
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenFrom(this Task<Result> result, Func<bool> condition, Func<IToResult> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFrom(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFrom<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, ITypedToResult> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFrom(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFrom<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, IToResult<T>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFrom(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenFromAs<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, IToResult> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFromAs(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAs<T>(this Task<Result> result, Func<bool> condition, Func<ITypedToResult> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFromAs<T>(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAs<T>(this Task<Result> result, Func<bool> condition, Func<IToResult<T>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFromAs(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> WhenFromAs<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, ITypedToResult> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFromAs<T, U>(condition, func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> WhenFromAs<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, IToResult<U>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFromAs<T, U>(condition, func);
        }

        #endregion

        #region AsyncFunc

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync(this Result result, Func<bool> condition, Func<Task> func)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess && condition())
                await func().ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync(this Result result, Func<bool> condition, Func<Task<Result>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition() ? await func().ConfigureAwait(false) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task> func)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess && condition(result.Value))
                await func(result.Value).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<T>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? Result<T>.Ok(await func(result.Value).ConfigureAwait(false)) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<Result<T>>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? await func(result.Value).ConfigureAwait(false) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsAsync<T>(this Result result, Func<bool> condition, Func<Task<T>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition() ? Result<T>.Ok(await func().ConfigureAwait(false)) : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsAsync<T>(this Result result, Func<bool> condition, Func<Task<Result<T>>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition() ? await func().ConfigureAwait(false) : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task> func)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess && condition(result.Value))
                await func(result.Value).ConfigureAwait(false);

            return result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<Result>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? await func(result.Value).ConfigureAwait(false) : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAsAsync<T, U>(this Result<T> result, Predicate<T> condition, Func<T, Task<U>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? Result<U>.Ok(await func(result.Value).ConfigureAwait(false)) : result.Bind<T, U>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAsAsync<T, U>(this Result<T> result, Predicate<T> condition, Func<T, Task<Result<U>>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? await func(result.Value).ConfigureAwait(false) : result.Bind<T, U>();
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenFromAsync(this Result result, Func<bool> condition, Func<Task<IToResult>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition() ? (await func().ConfigureAwait(false)).ToResult() : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? (await func(result.Value).ConfigureAwait(false)).ToResult<T>() : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<IToResult<T>>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? (await func(result.Value).ConfigureAwait(false)).ToResult() : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenFromAsAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<IToResult>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? (await func(result.Value).ConfigureAwait(false)).ToResult() : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsAsync<T>(this Result result, Func<bool> condition, Func<Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition() ? (await func().ConfigureAwait(false)).ToResult<T>() : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsAsync<T>(this Result result, Func<bool> condition, Func<Task<IToResult<T>>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition() ? (await func().ConfigureAwait(false)).ToResult() : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> WhenFromAsAsync<T, U>(this Result<T> result, Predicate<T> condition, Func<T, Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? (await func(result.Value).ConfigureAwait(false)).ToResult<U>() : result.Bind<T, U>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> WhenFromAsAsync<T, U>(this Result<T> result, Predicate<T> condition, Func<T, Task<IToResult<U>>> func)
        {
            ThrowIfNull(result, condition, func);
            return result.IsSuccess && condition(result.Value) ? (await func(result.Value).ConfigureAwait(false)).ToResult() : result.Bind<T, U>();
        }

        #endregion

        #region AsyncBoth

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync(this Task<Result> result, Func<bool> condition, Func<Task> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync(this Task<Result> result, Func<bool> condition, Func<Task<Result>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> WhenAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> WhenAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<T>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> WhenAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<Result<T>>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> WhenAsAsync<T>(this Task<Result> result, Func<bool> condition, Func<Task<T>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> WhenAsAsync<T>(this Task<Result> result, Func<bool> condition, Func<Task<Result<T>>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result"/>.</return>
        public static async Task<Result> WhenAsAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result"/>.</return>
        public static async Task<Result> WhenAsAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<Result>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<U>> WhenAsAsync<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<U>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<U>> WhenAsAsync<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<Result<U>>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsAsync(condition, func).ConfigureAwait(false);
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenFromAsync(this Task<Result> result, Func<bool> condition, Func<Task<IToResult>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<IToResult<T>>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenFromAsAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<IToResult>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsAsync<T>(this Task<Result> result, Func<bool> condition, Func<Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsAsync<T>(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsAsync<T>(this Task<Result> result, Func<bool> condition, Func<Task<IToResult<T>>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsAsync(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> WhenFromAsAsync<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsAsync<T, U>(condition, func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaulates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> WhenFromAsAsync<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<IToResult<U>>> func)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsAsync<T, U>(condition, func).ConfigureAwait(false);
        }

        #endregion

        /// <summary>
        /// Check parameters and throw where null.
        /// </summary>
        private static void ThrowIfNull(object result, object condition, object func, string? name = null)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            if (func == null) throw new ArgumentNullException(name ?? nameof(func));
        }
    }
}