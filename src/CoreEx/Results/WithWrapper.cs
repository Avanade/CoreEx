﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Results
{
    /// <summary>
    /// Provides the common with/wrapped execution functionality.
    /// </summary>
    public abstract class WithWrapper
    {
        /// <summary>
        /// Performs the actual execution of the <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="result">The <see cref="IResult"/>.</param>
        /// <param name="func">The encapsulated function to execute.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        protected abstract IResult Execute(IResult result, Func<IResult> func);

        /// <summary>
        /// Performs the actual execution of the <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="result">The <see cref="IResult"/>.</param>
        /// <param name="func">The encapsulated function to execute.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        protected abstract Task<IResult> ExecuteAsync(IResult result, Func<Task<IResult>> func);

        #region Synchronous

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public Result Execute(Result result, Func<Result, Result> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result)Execute(result, () => func(result));
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public Result<T> Execute<T>(Result result, Func<Result, Result<T>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result<T>)Execute(result, () => func(result));
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public Result<U> Execute<T, U>(Result<T> result, Func<Result<T>, Result<U>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result<U>)Execute(result, () => func(result));
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public Result Execute<T>(Result<T> result, Func<Result<T>, Result> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result)Execute(result, () => func(result));
        }

        #endregion

        #region AsyncResult

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public async Task<Result> Execute(Task<Result> result, Func<Result, Result> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result)Execute(r, () => func(r));
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<T>> Execute<T>(Task<Result> result, Func<Result, Result<T>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result<T>)Execute(r, () => func(r));
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public async Task<Result> Execute<T>(Task<Result<T>> result, Func<Result<T>, Result> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result)Execute(r, () => func(r));
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<U>> Execute<T, U>(Task<Result<T>> result, Func<Result<T>, Result<U>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result<U>)Execute(r, () => func(r));
        }

        #endregion

        #region AsyncFunc

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public async Task<Result> ExecuteAsync(Result result, Func<Result, Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result)await ExecuteAsync(result, async () => await func(result).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<T>> ExecuteAsync<T>(Result result, Func<Result, Task<Result<T>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result<T>)await ExecuteAsync(result, async () => await func(result).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public async Task<Result> ExecuteAsync<T>(Result<T> result, Func<Result<T>, Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result)await ExecuteAsync(result, async () => await func(result).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<U>> ExecuteAsync<T, U>(Result<T> result, Func<Result<T>, Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result<U>)await ExecuteAsync(result, async () => await func(result).ConfigureAwait(false)).ConfigureAwait(false);
        }

        #endregion

        #region AsyncBoth

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public async Task<Result> ExecuteAsync(Task<Result> result, Func<Result, Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result)await ExecuteAsync(r, async () => await func(r).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<T>> ExecuteAsync<T>(Task<Result> result, Func<Result, Task<Result<T>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result<T>)await ExecuteAsync(r, async () => await func(r).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public async Task<Result> ExecuteAsync<T>(Task<Result<T>> result, Func<Result<T>, Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result)await ExecuteAsync(r, async () => await func(r).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<U>> ExecuteAsync<T, U>(Task<Result<T>> result, Func<Result<T>, Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result<U>)await ExecuteAsync(r, async () => await func(r).ConfigureAwait(false)).ConfigureAwait(false);
        }

        #endregion
    }
}