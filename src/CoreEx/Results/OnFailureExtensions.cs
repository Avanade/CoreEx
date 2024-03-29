﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Results
{
    public static partial class ResultsExtensions
    {
        #region Synchronous

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="action">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result OnFailure(this Result result, Action<Exception> action)
        {
            ThrowIfNull(result, action, nameof(action));
            if (result.IsFailure)
                action(result.Error);

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result OnFailure(this Result result, Func<Result> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? func() : result;
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="action">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> OnFailure<T>(this Result<T> result, Action<Exception> action)
        {
            ThrowIfNull(result, action, nameof(action));
            if (result.IsFailure)
                action(result.Error);

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> OnFailure<T>(this Result<T> result, Func<Exception, T> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? Result<T>.Ok(func(result.Error)) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> OnFailure<T>(this Result<T> result, Func<Exception, Result<T>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? func(result.Error) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> OnFailureAs<T>(this Result result, Func<T> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? Result<T>.Ok(func()) : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> OnFailureAs<T>(this Result result, Func<Result<T>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? func() : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="action">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result OnFailureAs<T>(this Result<T> result, Action<Exception> action)
        {
            ThrowIfNull(result, action, nameof(action));
            if (result.IsFailure)
                action(result.Error);

            return result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result OnFailureAs<T>(this Result<T> result, Func<Exception, Result> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? func(result.Error) : result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> OnFailureAs<T, U>(this Result<T> result, Func<Exception, U> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? Result<U>.Ok(func(result.Error)) : result.Bind<T, U>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> OnFailureAs<T, U>(this Result<T> result, Func<Exception, Result<U>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? func(result.Error) : result.Bind<T, U>();
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result OnFailure(this Result result, Func<IToResult> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? func().ToResult() : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> OnFailure<T>(this Result<T> result, Func<Exception, ITypedToResult> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? func(result.Error).ToResult<T>() : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> OnFailure<T>(this Result<T> result, Func<Exception, IToResult<T>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? func(result.Error).ToResult() : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result OnFailureAs<T>(this Result<T> result, Func<Exception, IToResult> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? func(result.Error).ToResult() : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> OnFailureAs<T>(this Result result, Func<ITypedToResult> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? func().ToResult<T>() : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<T> OnFailureAs<T>(this Result result, Func<IToResult<T>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? func().ToResult() : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<U> OnFailureAs<T, U>(this Result<T> result, Func<Exception, ITypedToResult> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? func(result.Error).ToResult<U>() : result.Bind<T, U>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result<U> OnFailureAs<T, U>(this Result<T> result, Func<Exception, IToResult<U>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? func(result.Error).ToResult() : result.Bind<T, U>();
        }

        #endregion

        #region AsyncResult

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="action">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailure(this Task<Result> result, Action<Exception> action)
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
        public static async Task<Result> OnFailure(this Task<Result> result, Func<Result> func)
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
        public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> result, Action<Exception> action)
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
        public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> result, Func<Exception, T> func)
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
        public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> result, Func<Exception, Result<T>> func)
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
        public static async Task<Result<T>> OnFailureAs<T>(this Task<Result> result, Func<T> func)
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
        public static async Task<Result<T>> OnFailureAs<T>(this Task<Result> result, Func<Result<T>> func)
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
        public static async Task<Result> OnFailureAs<T>(this Task<Result<T>> result, Action<Exception> action)
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
        public static async Task<Result> OnFailureAs<T>(this Task<Result<T>> result, Func<Exception, Result> func)
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
        public static async Task<Result<U>> OnFailureAs<T, U>(this Task<Result<T>> result, Func<Exception, U> func)
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
        public static async Task<Result<U>> OnFailureAs<T, U>(this Task<Result<T>> result, Func<Exception, Result<U>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.OnFailureAs<T, U>(func);
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailure(this Task<Result> result, Func<IToResult> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.OnFailure(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> result, Func<Exception, ITypedToResult> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.OnFailure(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> result, Func<Exception, IToResult<T>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.OnFailure(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailureAs<T>(this Task<Result<T>> result, Func<Exception, IToResult> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.OnFailureAs(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> OnFailureAs<T>(this Task<Result> result, Func<ITypedToResult> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.OnFailureAs<T>(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> OnFailureAs<T>(this Task<Result> result, Func<IToResult<T>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.OnFailureAs(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> OnFailureAs<T, U>(this Task<Result<T>> result, Func<Exception, ITypedToResult> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return r.OnFailureAs<T, U>(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> OnFailureAs<T, U>(this Task<Result<T>> result, Func<Exception, IToResult<U>> func)
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
        public static async Task<Result> OnFailureAsync(this Result result, Func<Task> func)
        {
            ThrowIfNull(result, func);
            if (result.IsFailure)
                await func().ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailureAsync(this Result result, Func<Task<Result>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? await func().ConfigureAwait(false) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> OnFailureAsync<T>(this Result<T> result, Func<Exception, Task> func)
        {
            ThrowIfNull(result, func);
            if (result.IsFailure)
                await func(result.Error).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> OnFailureAsync<T>(this Result<T> result, Func<Exception, Task<T>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? Result<T>.Ok(await func(result.Error).ConfigureAwait(false)) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> OnFailureAsync<T>(this Result<T> result, Func<Exception, Task<Result<T>>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? await func(result.Error).ConfigureAwait(false) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> OnFailureAsAsync<T>(this Result result, Func<Task<T>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? Result<T>.Ok(await func().ConfigureAwait(false)) : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> OnFailureAsAsync<T>(this Result result, Func<Task<Result<T>>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? await func().ConfigureAwait(false) : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailureAsAsync<T>(this Result<T> result, Func<Exception, Task> func)
        {
            ThrowIfNull(result, func);
            if (result.IsFailure)
                await func(result.Error).ConfigureAwait(false);

            return result.Bind();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailureAsAsync<T>(this Result<T> result, Func<Exception, Task<Result>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? await func(result.Error).ConfigureAwait(false) : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailureAsAsync<T, U>(this Result<T> result, Func<Exception, Task<U>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? Result<U>.Ok(await func(result.Error).ConfigureAwait(false)) : result.Bind<T, U>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailureAsAsync<T, U>(this Result<T> result, Func<Exception, Task<Result<U>>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? await func(result.Error).ConfigureAwait(false) : result.Bind<T, U>();
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailureAsync(this Result result, Func<Task<IToResult>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? (await func().ConfigureAwait(false)).ToResult() : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> OnFailureAsync<T>(this Result<T> result, Func<Exception, Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? (await func(result.Error).ConfigureAwait(false)).ToResult<T>() : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> OnFailureAsync<T>(this Result<T> result, Func<Exception, Task<IToResult<T>>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? (await func(result.Error).ConfigureAwait(false)).ToResult() : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailureAsAsync<T>(this Result<T> result, Func<Exception, Task<IToResult>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? (await func(result.Error).ConfigureAwait(false)).ToResult() : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> OnFailureAsAsync<T>(this Result result, Func<Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? (await func().ConfigureAwait(false)).ToResult<T>() : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> OnFailureAsAsync<T>(this Result result, Func<Task<IToResult<T>>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? (await func().ConfigureAwait(false)).ToResult() : result.Bind<T>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> OnFailureAsAsync<T, U>(this Result<T> result, Func<Exception, Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? (await func(result.Error).ConfigureAwait(false)).ToResult<U>() : result.Bind<T, U>();
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> OnFailureAsAsync<T, U>(this Result<T> result, Func<Exception, Task<IToResult<U>>> func)
        {
            ThrowIfNull(result, func);
            return result.IsFailure ? (await func(result.Error).ConfigureAwait(false)).ToResult() : result.Bind<T, U>();
        }

        #endregion

        #region AsyncBoth

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailureAsync(this Task<Result> result, Func<Task> func)
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
        public static async Task<Result> OnFailureAsync(this Task<Result> result, Func<Task<Result>> func)
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
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> OnFailureAsync<T>(this Task<Result<T>> result, Func<Exception, Task> func)
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
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> OnFailureAsync<T>(this Task<Result<T>> result, Func<Exception, Task<T>> func)
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
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> OnFailureAsync<T>(this Task<Result<T>> result, Func<Exception, Task<Result<T>>> func)
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
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> OnFailureAsAsync<T>(this Task<Result> result, Func<Task<T>> func)
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
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<T>> OnFailureAsAsync<T>(this Task<Result> result, Func<Task<Result<T>>> func)
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
        /// <return>The resulting <see cref="Result"/>.</return>
        public static async Task<Result> OnFailureAsAsync<T>(this Task<Result<T>> result, Func<Exception, Task> func)
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
        /// <return>The resulting <see cref="Result"/>.</return>
        public static async Task<Result> OnFailureAsAsync<T>(this Task<Result<T>> result, Func<Exception, Task<Result>> func)
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
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<U>> OnFailureAsAsync<T, U>(this Task<Result<T>> result, Func<Exception, Task<U>> func)
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
        /// <return>The resulting <see cref="Result{T}"/>.</return>
        public static async Task<Result<U>> OnFailureAsAsync<T, U>(this Task<Result<T>> result, Func<Exception, Task<Result<U>>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsAsync(func).ConfigureAwait(false);
        }

        /* IToResult */

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailureAsync(this Task<Result> result, Func<Task<IToResult>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> OnFailureAsync<T>(this Task<Result<T>> result, Func<Exception, Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> OnFailureAsync<T>(this Task<Result<T>> result, Func<Exception, Task<IToResult<T>>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailureAsAsync<T>(this Task<Result<T>> result, Func<Exception, Task<IToResult>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> OnFailureAsAsync<T>(this Task<Result> result, Func<Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsAsync<T>(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> OnFailureAsAsync<T>(this Task<Result> result, Func<Task<IToResult<T>>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> OnFailureAsAsync<T, U>(this Task<Result<T>> result, Func<Exception, Task<ITypedToResult>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsAsync<T, U>(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> OnFailureAsAsync<T, U>(this Task<Result<T>> result, Func<Exception, Task<IToResult<U>>> func)
        {
            ThrowIfNull(result, func);
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsAsync<T, U>(func).ConfigureAwait(false);
        }

        #endregion
    }
}