// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoreEx.Results.Abstractions
{
    /// <summary>
    /// Provides the common with/wrapped execution functionality including arguments.
    /// </summary>
    /// <typeparam name="TArgs">The arguments <see cref="Type"/>.</typeparam>
    /// <reamrks>The underlying <c>Execute</c> and <c>ExecuteAsync</c> methods support <see cref="IResult"/> <see cref="Type"/> changes without explicit <c>As</c> method as it is assumed this is managed explicitly within wrapper
    /// and will avoid specifying more than once.</reamrks>    
    [DebuggerStepThrough]
    public abstract class WithWrapper<TArgs>
    {
        /// <summary>
        /// Performs the actual execution of the <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="result">The <see cref="IResult"/>.</param>
        /// <param name="func">The encapsulated function to execute.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        protected abstract IResult Execute(IResult result, Func<IResult> func, TArgs? args = default);

        /// <summary>
        /// Performs the actual execution of the <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="result">The <see cref="IResult"/>.</param>
        /// <param name="func">The encapsulated function to execute.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        protected abstract Task<IResult> ExecuteAsync(IResult result, Func<Task<IResult>> func, TArgs? args = default);

        #region Synchronous

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public Result Execute(Result result, Func<Result, Result> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result)Execute(result, () => func(result), args);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public Result<T> Execute<T>(Result result, Func<Result, Result<T>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result<T>)Execute(result, () => func(result), args);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public Result<T> Execute<T>(Result<T> result, Func<Result<T>, Result<T>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result<T>)Execute(result, () => func(result), args);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public Result Execute<T>(Result<T> result, Func<Result<T>, Result> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result)Execute(result, () => func(result), args);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public Result<U> Execute<T, U>(Result<T> result, Func<Result<T>, Result<U>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result<U>)Execute(result, () => func(result), args);
        }

        #endregion

        #region AsyncResult

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public async Task<Result> Execute(Task<Result> result, Func<Result, Result> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result)Execute(r, () => func(r), args);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<T>> Execute<T>(Task<Result> result, Func<Result, Result<T>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result<T>)Execute(r, () => func(r), args);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<T>> Execute<T>(Task<Result<T>> result, Func<Result<T>, Result<T>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result<T>)Execute(r, () => func(r), args);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public async Task<Result> Execute<T>(Task<Result<T>> result, Func<Result<T>, Result> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result)Execute(r, () => func(r), args);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<U>> Execute<T, U>(Task<Result<T>> result, Func<Result<T>, Result<U>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result<U>)Execute(r, () => func(r), args);
        }

        #endregion

        #region AsyncFunc

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public async Task<Result> ExecuteAsync(Result result, Func<Result, Task<Result>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result)await ExecuteAsync(result, async () => await func(result).ConfigureAwait(false), args).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<T>> ExecuteAsync<T>(Result result, Func<Result, Task<Result<T>>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result<T>)await ExecuteAsync(result, async () => await func(result).ConfigureAwait(false), args).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<T>> ExecuteAsync<T>(Result<T> result, Func<Result<T>, Task<Result<T>>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result<T>)await ExecuteAsync(result, async () => await func(result).ConfigureAwait(false), args).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public async Task<Result> ExecuteAsync<T>(Result<T> result, Func<Result<T>, Task<Result>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result)await ExecuteAsync(result, async () => await func(result).ConfigureAwait(false), args).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<U>> ExecuteAsync<T, U>(Result<T> result, Func<Result<T>, Task<Result<U>>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return (Result<U>)await ExecuteAsync(result, async () => await func(result).ConfigureAwait(false), args).ConfigureAwait(false);
        }

        #endregion

        #region AsyncBoth

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public async Task<Result> ExecuteAsync(Task<Result> result, Func<Result, Task<Result>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result)await ExecuteAsync(r, async () => await func(r).ConfigureAwait(false), args).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<T>> ExecuteAsync<T>(Task<Result> result, Func<Result, Task<Result<T>>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result<T>)await ExecuteAsync(r, async () => await func(r).ConfigureAwait(false), args).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<T>> ExecuteAsync<T>(Task<Result<T>> result, Func<Result<T>, Task<Result<T>>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result<T>)await ExecuteAsync(r, async () => await func(r).ConfigureAwait(false), args).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public async Task<Result> ExecuteAsync<T>(Task<Result<T>> result, Func<Result<T>, Task<Result>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result)await ExecuteAsync(r, async () => await func(r).ConfigureAwait(false), args).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a wrapped execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The optional arguments.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public async Task<Result<U>> ExecuteAsync<T, U>(Task<Result<T>> result, Func<Result<T>, Task<Result<U>>> func, TArgs? args = default)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return (Result<U>)await ExecuteAsync(r, async () => await func(r).ConfigureAwait(false), args).ConfigureAwait(false);
        }

        #endregion
    }
}