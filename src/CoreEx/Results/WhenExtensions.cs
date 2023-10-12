// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Results
{
    public static partial class ResultsExtensions
    {
        #region Synchronous

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result When(this Result result, Func<bool> condition, Action action, Action? otherwise = null)
        {
            ThrowIfNull(result, condition, action, nameof(action));
            if (result.IsSuccess)
            {
                if (condition())
                    action();
                else
                    otherwise?.Invoke();
            }

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result When(this Result result, Func<bool> condition, Func<Result> func, Func<Result>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result;

            if (condition())
                return func();
            else
                return otherwise is null ? result : otherwise();
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> When<T>(this Result<T> result, Predicate<T> condition, Action<T> action, Action<T>? otherwise = null)
        {
            ThrowIfNull(result, condition, action, nameof(action));
            if (result.IsSuccess)
            {
                if (condition(result.Value))
                    action(result.Value);
                else
                    otherwise?.Invoke(result.Value);
            }

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> When<T>(this Result<T> result, Predicate<T> condition, Func<T, T> func, Func<T, T>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result;

            if (condition(result.Value))
                return Result<T>.Ok(func(result.Value));
            else
                return otherwise is null ? result : Result<T>.Ok(otherwise(result.Value));
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> When<T>(this Result<T> result, Predicate<T> condition, Func<T, Result<T>> func, Func<T, Result<T>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result;

            if (condition(result.Value))
                return func(result.Value);
            else
                return otherwise is null ? result : otherwise(result.Value);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>  and <paramref name="condition"/> evaluates to <c>true</c> (the <paramref name="result"/> <see cref="Result{T}.Value"/> will not be lost).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> When<T>(this Result<T> result, Predicate<T> condition, Func<T, Result> func, Func<T, Result>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result;

            if (condition(result.Value))
                return func(result.Value).Combine(result);
            else
                return otherwise is null ? result : func(result.Value).Combine(result);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> WhenAs<T>(this Result result, Func<bool> condition, Func<T> func, Func<T>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind<T>();

            if (condition())
                return Result<T>.Ok(func());
            else
                return otherwise is null ? result.Bind<T>() : Result<T>.Ok(otherwise());
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> WhenAs<T>(this Result result, Func<bool> condition, Func<Result<T>> func, Func<Result<T>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind<T>();

            if (condition())
                return func();
            else
                return otherwise is null ? result.Bind<T>() : otherwise();
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WhenAs<T>(this Result<T> result, Predicate<T> condition, Action<T> action, Action<T>? otherwise = null)
        {
            ThrowIfNull(result, condition, action, nameof(action));
            if (result.IsSuccess)
            {
                if (condition(result.Value))
                    action(result.Value);
                else
                    otherwise?.Invoke(result.Value);
            }

            return result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WhenAs<T>(this Result<T> result, Predicate<T> condition, Func<T, Result> func, Func<T, Result>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind();

            if (condition(result.Value))
                return func(result.Value);
            else
                return otherwise is null ? result.Bind() : otherwise(result.Value);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> WhenAs<T, U>(this Result<T> result, Predicate<T> condition, Func<T, U> func, Func<T, U>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind<T, U>();

            if (condition(result.Value))
                return Result<U>.Ok(func(result.Value));
            else
                return otherwise is null ? result.Bind<T, U>() : Result<U>.Ok(otherwise(result.Value));
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> WhenAs<T, U>(this Result<T> result, Predicate<T> condition, Func<T, Result<U>> func, Func<T, Result<U>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind<T, U>();

            if (condition(result.Value))
                return func(result.Value);
            else
                return otherwise is null ? result.Bind<T, U>() : otherwise(result.Value);
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WhenFrom(this Result result, Func<bool> condition, Func<IToResult> func, Func<IToResult>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result;

            if (condition())
                return func().ToResult();
            else
                return otherwise is null ? result : otherwise().ToResult();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> WhenFrom<T>(this Result<T> result, Predicate<T> condition, Func<T, ITypedToResult> func, Func<T, ITypedToResult>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result;

            if (condition(result.Value))
                return func(result.Value).ToResult<T>();
            else
                return otherwise is null ? result : otherwise(result.Value).ToResult<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> WhenFrom<T>(this Result<T> result, Predicate<T> condition, Func<T, IToResult<T>> func, Func<T, IToResult<T>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result;

            if (condition(result.Value))
                return func(result.Value).ToResult();
            else
                return otherwise is null ? result : otherwise(result.Value).ToResult();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WhenFromAs<T>(this Result<T> result, Predicate<T> condition, Func<T, IToResult> func, Func<T, IToResult>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind();

            if (condition(result.Value))
                return func(result.Value).ToResult();
            else
                return otherwise is null ? result.Bind() : otherwise(result.Value).ToResult();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> WhenFromAs<T>(this Result result, Func<bool> condition, Func<ITypedToResult> func, Func<ITypedToResult>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind<T>();

            if (condition())
                return func().ToResult<T>();
            else
                return otherwise is null ? result.Bind<T>() : otherwise().ToResult<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> WhenFromAs<T>(this Result result, Func<bool> condition, Func<IToResult<T>> func, Func<IToResult<T>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind<T>();

            if (condition())
                return func().ToResult();
            else
                return otherwise is null ? result.Bind<T>() : otherwise().ToResult();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<U> WhenFromAs<T, U>(this Result<T> result, Predicate<T> condition, Func<T, ITypedToResult> func, Func<T, ITypedToResult>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind<T, U>();

            if (condition(result.Value))
                return func(result.Value).ToResult<U>();
            else
                return otherwise is null ? result.Bind<T, U>() : otherwise(result.Value).ToResult<U>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<U> WhenFromAs<T, U>(this Result<T> result, Predicate<T> condition, Func<T, IToResult<U>> func, Func<T, IToResult<U>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind<T, U>();

            if (condition(result.Value))
                return func(result.Value).ToResult();
            else
                return otherwise is null ? result.Bind<T, U>() : otherwise(result.Value).ToResult();
        }

        #endregion

        #region AsyncResult

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> When(this Task<Result> result, Func<bool> condition, Action action, Action? otherwise = null)
        {
            ThrowIfNull(result, condition, action, nameof(action));
            var r = await result.ConfigureAwait(false);
            return r.When(condition, action, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> When(this Task<Result> result, Func<bool> condition, Func<Result> func, Func<Result>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> When<T>(this Task<Result<T>> result, Predicate<T> condition, Action<T> action, Action<T>? otherwise = null)
        {
            ThrowIfNull(result, condition, action, nameof(action));
            var r = await result.ConfigureAwait(false);
            return r.When(condition, action, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> When<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, T> func, Func<T, T>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> When<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Result<T>> func, Func<T, Result<T>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (the <paramref name="result"/> <see cref="Result{T}.Value"/> will not be lost).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> When<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Result> func, Func<T, Result>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.When(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAs<T>(this Task<Result> result, Func<bool> condition, Func<T> func, Func<T>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenAs(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAs<T>(this Task<Result> result, Func<bool> condition, Func<Result<T>> func, Func<Result<T>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenAs(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="action">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAs<T>(this Task<Result<T>> result, Predicate<T> condition, Action<T> action, Action<T>? otherwise = null)
        {
            ThrowIfNull(result, condition, action, nameof(action));
            var r = await result.ConfigureAwait(false);
            return r.WhenAs(condition, action, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAs<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Result> func, Func<T, Result>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenAs(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAs<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, U> func, Func<T, U>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenAs(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <param name="otherwise">The <see cref="Func{T, TResult}"/> to invoke where condition/predicate is <c>false</c> (where <paramref name="result"/> is also <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAs<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Result<U>> func, Func<T, Result<U>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenAs(condition, func, otherwise);
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenFrom(this Task<Result> result, Func<bool> condition, Func<IToResult> func, Func<IToResult>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFrom(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFrom<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, ITypedToResult> func, Func<T, ITypedToResult>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFrom(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFrom<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, IToResult<T>> func, Func<T, IToResult<T>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFrom(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenFromAs<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, IToResult> func, Func<T, IToResult>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFromAs(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAs<T>(this Task<Result> result, Func<bool> condition, Func<ITypedToResult> func, Func<ITypedToResult>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFromAs<T>(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAs<T>(this Task<Result> result, Func<bool> condition, Func<IToResult<T>> func, Func<IToResult<T>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFromAs(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> WhenFromAs<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, ITypedToResult> func, Func<T, ITypedToResult>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFromAs<T, U>(condition, func, otherwise);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> WhenFromAs<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, IToResult<U>> func, Func<T, IToResult<U>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return r.WhenFromAs(condition, func, otherwise);
        }

        #endregion

        #region AsyncFunc

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync(this Result result, Func<bool> condition, Func<Task> func, Func<Task>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess)
            {
                if (condition())
                    await func().ConfigureAwait(false);
                else
                {
                    if (otherwise is not null)
                        await otherwise().ConfigureAwait(false);
                }
            }

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync(this Result result, Func<bool> condition, Func<Task<Result>> func, Func<Task<Result>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result;

            if (condition())
                return await func().ConfigureAwait(false);
            else
                return otherwise is not null ? await otherwise().ConfigureAwait(false) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task> func, Func<T, Task>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess)
            {
                if (condition(result.Value))
                    await func(result.Value).ConfigureAwait(false);
                else
                {
                    if (otherwise is not null)
                        await otherwise(result.Value).ConfigureAwait(false);
                }
            }

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<T>> func, Func<T, Task<T>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result;  

            if (condition(result.Value))
                return Result<T>.Ok(await func(result.Value).ConfigureAwait(false));
            else
                return otherwise is not null ? Result<T>.Ok(await otherwise(result.Value).ConfigureAwait(false)) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<Result<T>>> func, Func<T, Task<Result<T>>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result;

            if (condition(result.Value))
                return await func(result.Value).ConfigureAwait(false);
            else
                return otherwise is not null ? await otherwise(result.Value).ConfigureAwait(false) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (the <paramref name="result"/> <see cref="Result{T}.Value"/> will not be lost).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<Result>> func, Func<T, Task<Result>>? otherwise = null)
        {
            ThrowIfNull(result, func);
            if (result.IsFailure)
                return result;

            if (condition(result.Value))
                return await func(result.Value).ConfigureAwait(false);
            else
                return otherwise is not null ? await otherwise(result.Value).ConfigureAwait(false) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsAsync<T>(this Result result, Func<bool> condition, Func<Task<T>> func, Func<Task<T>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind<T>();

            if (condition())
                return Result<T>.Ok(await func().ConfigureAwait(false));
            else
                return otherwise is not null ? Result<T>.Ok(await otherwise().ConfigureAwait(false)) : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsAsync<T>(this Result result, Func<bool> condition, Func<Task<Result<T>>> func, Func<Task<Result<T>>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind<T>();

            if (condition())
                return await func().ConfigureAwait(false);
            else
                return otherwise is not null ? await otherwise().ConfigureAwait(false) : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task> func, Func<T, Task>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess)
            {
                if (condition(result.Value))
                    await func(result.Value).ConfigureAwait(false);
                else if (otherwise is not null)
                    await otherwise(result.Value).ConfigureAwait(false);
            }

            return result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<Result>> func, Func<T, Task<Result>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind();

            if (condition(result.Value))
                return await func(result.Value).ConfigureAwait(false);
            else
                return otherwise is not null ? await otherwise(result.Value).ConfigureAwait(false) : result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAsAsync<T, U>(this Result<T> result, Predicate<T> condition, Func<T, Task<U>> func, Func<T, Task<U>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess)
            {
                if (condition(result.Value))
                    return Result<U>.Ok(await func(result.Value).ConfigureAwait(false));
                else if (otherwise is not null)
                    return Result<U>.Ok(await otherwise(result.Value).ConfigureAwait(false));
            }

            return result.Bind<T, U>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> WhenAsAsync<T, U>(this Result<T> result, Predicate<T> condition, Func<T, Task<Result<U>>> func, Func<T, Task<Result<U>>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsFailure)
                return result.Bind<T, U>();

            if (condition(result.Value))
                return await func(result.Value).ConfigureAwait(false);
            else
                return otherwise is not null ? await otherwise(result.Value).ConfigureAwait(false) : result.Bind<T, U>();
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenFromAsync(this Result result, Func<bool> condition, Func<Task<IToResult>> func, Func<Task<IToResult>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess)
            {
                if (condition())
                    return (await func().ConfigureAwait(false)).ToResult();
                else if (otherwise is not null)
                    return (await otherwise().ConfigureAwait(false)).ToResult();
            }

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<ITypedToResult>> func, Func<T, Task<ITypedToResult>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess)
            {
                if (condition(result.Value))
                    return (await func(result.Value).ConfigureAwait(false)).ToResult<T>();
                else if (otherwise is not null)
                    return (await otherwise(result.Value).ConfigureAwait(false)).ToResult<T>();
            }

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<IToResult<T>>> func, Func<T, Task<IToResult<T>>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess)
            {
                if (condition(result.Value))
                    return (await func(result.Value).ConfigureAwait(false)).ToResult();
                else if (otherwise is not null)
                    return (await otherwise(result.Value).ConfigureAwait(false)).ToResult();
            }

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenFromAsAsync<T>(this Result<T> result, Predicate<T> condition, Func<T, Task<IToResult>> func, Func<T, Task<IToResult>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess)
            {
                if (condition(result.Value))
                    return (await func(result.Value).ConfigureAwait(false)).ToResult();
                else if (otherwise is not null)
                    return (await otherwise(result.Value).ConfigureAwait(false)).ToResult();
            }

            return result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsAsync<T>(this Result result, Func<bool> condition, Func<Task<ITypedToResult>> func, Func<Task<ITypedToResult>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess)
            {
                if (condition())
                    return (await func().ConfigureAwait(false)).ToResult<T>();
                else if (otherwise is not null)
                    return (await otherwise().ConfigureAwait(false)).ToResult<T>();
            }

            return result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsAsync<T>(this Result result, Func<bool> condition, Func<Task<IToResult<T>>> func, Func<Task<IToResult<T>>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess)
            {
                if (condition())
                    return (await func().ConfigureAwait(false)).ToResult();
                else if (otherwise is not null)
                    return (await otherwise().ConfigureAwait(false)).ToResult();
            }

            return result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> WhenFromAsAsync<T, U>(this Result<T> result, Predicate<T> condition, Func<T, Task<ITypedToResult>> func, Func<T, Task<ITypedToResult>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess)
            {
                if (condition(result.Value))
                    return (await func(result.Value).ConfigureAwait(false)).ToResult<U>();
                else if (otherwise is not null)
                    return (await otherwise(result.Value).ConfigureAwait(false)).ToResult<U>();
            }

            return result.Bind<T, U>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> WhenFromAsAsync<T, U>(this Result<T> result, Predicate<T> condition, Func<T, Task<IToResult<U>>> func, Func<T, Task<IToResult<U>>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            if (result.IsSuccess)
            {
                if (condition(result.Value))
                    return (await func(result.Value).ConfigureAwait(false)).ToResult();
                else if (otherwise is not null)
                    return (await otherwise(result.Value).ConfigureAwait(false)).ToResult();
            }

            return result.Bind<T, U>();
        }

        #endregion

        #region AsyncBoth

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync(this Task<Result> result, Func<bool> condition, Func<Task> func, Func<Task>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenAsync(this Task<Result> result, Func<bool> condition, Func<Task<Result>> func, Func<Task<Result>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> WhenAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task> func, Func<T, Task>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> WhenAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<T>> func, Func<T, Task<T>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> WhenAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<Result<T>>> func, Func<T, Task<Result<T>>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (the <paramref name="result"/> <see cref="Result{T}.Value"/> will not be lost).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> WhenAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<Result>> func, Func<T, Task<Result>>? otherwise = null)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> WhenAsAsync<T>(this Task<Result> result, Func<bool> condition, Func<Task<T>> func, Func<Task<T>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> WhenAsAsync<T>(this Task<Result> result, Func<bool> condition, Func<Task<Result<T>>> func, Func<Task<Result<T>>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <return>The resulting <see cref="Result"/>.</return>
        public static async Task<Result> WhenAsAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task> func, Func<T, Task>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <return>The resulting <see cref="Result"/>.</return>
        public static async Task<Result> WhenAsAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<Result>> func, Func<T, Task<Result>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<U>> WhenAsAsync<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<U>> func, Func<T, Task<U>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<U>> WhenAsAsync<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<Result<U>>> func, Func<T, Task<Result<U>>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenAsAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenFromAsync(this Task<Result> result, Func<bool> condition, Func<Task<IToResult>> func, Func<Task<IToResult>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<ITypedToResult>> func, Func<T, Task<ITypedToResult>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<IToResult<T>>> func, Func<T, Task<IToResult<T>>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> WhenFromAsAsync<T>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<IToResult>> func, Func<T, Task<IToResult>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsAsync<T>(this Task<Result> result, Func<bool> condition, Func<Task<ITypedToResult>> func, Func<Task<ITypedToResult>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsAsync<T>(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> WhenFromAsAsync<T>(this Task<Result> result, Func<bool> condition, Func<Task<IToResult<T>>> func, Func<Task<IToResult<T>>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsAsync(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> WhenFromAsAsync<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<ITypedToResult>> func, Func<T, Task<ITypedToResult>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsAsync<T, U>(condition, func, otherwise).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/> and <paramref name="condition"/> evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="condition">The condition/predicate that must also be evaluated.</param>
        /// <param name="func">The function to invoke where <paramref name="condition"/> is <c>true</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <param name="otherwise">The function to invoke where <paramref name="condition"/> is <c>false</c> (where <paramref name="result"/> is <see cref="Result.IsSuccess"/>).</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> WhenFromAsAsync<T, U>(this Task<Result<T>> result, Predicate<T> condition, Func<T, Task<IToResult<U>>> func, Func<T, Task<IToResult<U>>>? otherwise = null)
        {
            ThrowIfNull(result, condition, func);
            var r = await result.ConfigureAwait(false);
            return await r.WhenFromAsAsync(condition, func, otherwise).ConfigureAwait(false);
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