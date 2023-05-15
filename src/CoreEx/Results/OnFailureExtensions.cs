// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoreEx.Results
{
    /// <summary>
    /// Provides the <see cref="Result"/> and <see cref="Result{T}"/> <c>OnFailure</c> and <c>OnFailureAsync</c> extension methods to execute the corresponding function when <see cref="IResult.IsFailure"/>.
    /// </summary>
    [DebuggerStepThrough]
    public static class OnFailureExtensions
    {
        #region Synchronous

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result OnFailure(this Result result, Action action)
        {
            if (result.IsFailure)
                (action ?? throw new ArgumentNullException(nameof(action))).Invoke();

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result OnFailure(this Result result, Func<Result> func)
            => result.IsFailure ? (func ?? throw new ArgumentNullException(nameof(func))).Invoke() : result;

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> OnFailure<T>(this Result<T> result, Action action)
        {
            if (result.IsFailure)
                (action ?? throw new ArgumentNullException(nameof(action))).Invoke();

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result OnFailure<T>(this Result<T> result, Func<Result> func)
            => result.IsFailure ? (func ?? throw new ArgumentNullException(nameof(func))).Invoke() : result;

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> OnFailure<T, U>(this Result<T> result, Func<U> func)
            => result.IsFailure ? Result<U>.Ok((func ?? throw new ArgumentNullException(nameof(func))).Invoke()) : result.Combine(Result<U>.None);

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> OnFailure<T, U>(this Result<T> result, Func<Result<U>> func)
            => result.IsFailure ? (func ?? throw new ArgumentNullException(nameof(func))).Invoke() : result.Combine(Result<U>.None);

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> OnFailure<T, U>(this Result<T> result, Func<Exception, Result<U>> func)
            => result.IsFailure ? (func ?? throw new ArgumentNullException(nameof(func))).Invoke(result.Error) : result.Combine(Result<U>.None);

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> OnFailure<T, U>(this Result<T> result, Func<Exception, U> func)
            => result.IsFailure ? (func ?? throw new ArgumentNullException(nameof(func))).Invoke(result.Error) : result.Combine(Result<U>.None);

        #endregion

        #region AsyncResult

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailure(this Task<Result> result, Action action)
        {
            var r = await result.ConfigureAwait(false);
            return r.OnFailure(action);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailure(this Task<Result> result, Func<Result> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.OnFailure(func);
        }

        /// <summary>
        /// Executes the <paramref name="action"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="action">The <see cref="Action"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> result, Action action)
        {
            var r = await result.ConfigureAwait(false);
            return r.OnFailure(action);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result> OnFailure<T>(this Task<Result<T>> result, Func<Result> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.OnFailure(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailure<T, U>(this Task<Result<T>> result, Func<U> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.OnFailure(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailure<T, U>(this Task<Result<T>> result, Func<Result<U>> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.OnFailure<T, U>(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailure<T, U>(this Task<Result<T>> result, Func<Exception, Result<U>> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.OnFailure(func);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailure<T, U>(this Task<Result<T>> result, Func<Exception, U> func)
        {
            var r = await result.ConfigureAwait(false);
            return r.OnFailure(func);
        }

        #endregion

        #region AsyncFunc

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailureAsync(this Result result, Func<Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            if (result.IsFailure)
                await func().ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailureAsync(this Result result, Func<Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsFailure ? await func().ConfigureAwait(false) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> OnFailureAsync<T>(this Result<T> result, Func<Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            if (result.IsFailure)
                await func().ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailureAsync<T, U>(this Result<T> result, Func<Task<U>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsFailure ? Result<U>.Ok(await func().ConfigureAwait(false)) : result.Combine(Result<U>.None);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result> OnFailureAsync<T>(this Result<T> result, Func<Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsFailure ? await func().ConfigureAwait(false) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailureAsync<T, U>(this Result<T> result, Func<Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsFailure ? await func().ConfigureAwait(false) : result.Combine(Result<U>.None);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailureAsync<T, U>(this Result<T> result, Func<Exception, Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsFailure ? await func(result.Error).ConfigureAwait(false) : result.Combine(Result<U>.None);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailureAsync<T, U>(this Result<T> result, Func<Exception, Task<U>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return result.IsFailure ? await func(result.Error).ConfigureAwait(false) : result.Combine(Result<U>.None);
        }

        #endregion

        #region AsyncBoth

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailureAsync(this Task<Result> result, Func<Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> OnFailureAsync(this Task<Result> result, Func<Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> OnFailureAsync<T>(this Task<Result<T>> result, Func<Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailureAsync<T, U>(this Task<Result<T>> result, Func<Task<U>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{T}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result> OnFailureAsync<T>(this Task<Result<T>> result, Func<Task<Result>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailureAsync<T, U>(this Task<Result<T>> result, Func<Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailureAsync<T, U>(this Task<Result<T>> result, Func<Exception, Task<Result<U>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsync(func).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <paramref name="result"/> is <see cref="Result.IsFailure"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<U>> OnFailureAsync<T, U>(this Task<Result<T>> result, Func<Exception, Task<U>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            var r = await result.ConfigureAwait(false);
            return await r.OnFailureAsync(func).ConfigureAwait(false);
        }

        #endregion
    }
}