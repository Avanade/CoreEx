// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Results
{
    /// <summary>
    /// Provides the <see cref="Result"/> and <see cref="Result{T}"/> <c>Match</c> and <c>MatchAsync</c> extension methods to execute the corresponding function when either <see cref="IResult.IsSuccessful"/> or <see cref="IResult.IsFailure"/>.
    /// </summary>
    public static class MatchExtensions
    {
        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="onSuccess">The success function.</param>
        /// <param name="onFailure">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Match(this Result result, Action onSuccess, Action<Exception> onFailure)
            => result.Match(() =>
            {
                onSuccess();
                return result;
            }, e =>
            {
                onFailure(e);
                return result;
            });

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="onSuccess">The success function.</param>
        /// <param name="onFailure">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Match(this Result result, Func<Result> onSuccess, Func<Exception, Result> onFailure)
        {
            ThrowIfNull(onSuccess, onFailure);
            return result.IsSuccessful ? onSuccess() : onFailure(result.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success function.</param>
        /// <param name="onFailure">The failure function.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Match<T>(this Result<T> result, Action<T> onSuccess, Action<Exception> onFailure)
            => result.Match(v =>
            {
                onSuccess(v);
                return result;
            }, e =>
            {
                onFailure(e);
                return result;
            });

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success function.</param>
        /// <param name="onFailure">The failure function.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Match<T>(this Result<T> result, Func<T, Result<T>> onSuccess, Func<Exception, Result<T>> onFailure)
        {
            ThrowIfNull(onSuccess, onFailure);
            return result.IsSuccessful ? onSuccess(result.Value) : onFailure(result.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success <see cref="Action"/>.</param>
        /// <param name="onFailure">The failure <see cref="Action"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> Match(this Task<Result> result, Action onSuccess, Action<Exception> onFailure)
        { 
            var r = await result.ConfigureAwait(false);
            return r.Match(() =>
            {
                onSuccess();
                return r;
            }, e =>
            {
                onFailure(e);
                return r;
            });
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success <see cref="Action"/>.</param>
        /// <param name="onFailure">The failure <see cref="Action"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> Match(this Task<Result> result, Func<Result> onSuccess, Func<Exception, Result> onFailure)
        {
            ThrowIfNull(onSuccess, onFailure);
            var r = await result.ConfigureAwait(false);
            return r.IsSuccessful ? onSuccess() : onFailure(r.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success <see cref="Action"/>.</param>
        /// <param name="onFailure">The failure <see cref="Action"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> Match<T>(this Task<Result<T>> result, Action<T> onSuccess, Action<Exception> onFailure)
        {
            var r = await result.ConfigureAwait(false);
            return r.Match(v =>
            {
                onSuccess(v);
                return r;
            }, e =>
            {
                onFailure(e);
                return r;
            });
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success <see cref="Action"/>.</param>
        /// <param name="onFailure">The failure <see cref="Action"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> Match<T, U>(this Task<Result<T>> result, Func<T, Result<U>> onSuccess, Func<Exception, Result<U>> onFailure)
        {
            ThrowIfNull(onSuccess, onFailure);
            var r = await result.ConfigureAwait(false);
            return r.IsSuccessful ? onSuccess(r.Value) : onFailure(r.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success <see cref="Action"/>.</param>
        /// <param name="onFailure">The failure <see cref="Action"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> MatchAsync(this Result result, Func<Task> onSuccess, Func<Exception, Task> onFailure)
            => await result.MatchAsync(async () =>
            {
                await onSuccess().ConfigureAwait(false);
                return result;
            }, async e =>
            {
                await onFailure(e).ConfigureAwait(false);
                return result;
            }).ConfigureAwait(false);

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success <see cref="Action"/>.</param>
        /// <param name="onFailure">The failure <see cref="Action"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> MatchAsync(this Result result, Func<Task<Result>> onSuccess, Func<Exception, Task<Result>> onFailure)
        {
            ThrowIfNull(onSuccess, onFailure);
            return result.IsSuccessful ? onSuccess() : onFailure(result.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success <see cref="Action"/>.</param>
        /// <param name="onFailure">The failure <see cref="Action"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result<T>> MatchAsync<T>(this Result<T> result, Func<T, Task> onSuccess, Func<Exception, Task> onFailure)
            => result.MatchAsync(async v =>
            {
                await onSuccess(v).ConfigureAwait(false);
                return result;
            }, async e =>
            {
                await onFailure(e).ConfigureAwait(false);
                return result;
            });

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success <see cref="Action"/>.</param>
        /// <param name="onFailure">The failure <see cref="Action"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result<U>> MatchAsync<T, U>(this Result<T> result, Func<T, Task<Result<U>>> onSuccess, Func<Exception, Task<Result<U>>> onFailure)
        {
            ThrowIfNull(onSuccess, onFailure);
            return result.IsSuccessful ? onSuccess(result.Value) : onFailure(result.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success <see cref="Action"/>.</param>
        /// <param name="onFailure">The failure <see cref="Action"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> MatchAsync(this Task<Result> result, Func<Task> onSuccess, Func<Exception, Task> onFailure)
        {
            ThrowIfNull(onSuccess, onFailure);
            var r = await result.ConfigureAwait(false);
            if (r.IsSuccessful)
                await onSuccess().ConfigureAwait(false);
            else
                await onFailure(r.Error).ConfigureAwait(false);

            return r;
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success <see cref="Action"/>.</param>
        /// <param name="onFailure">The failure <see cref="Action"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> MatchAsync(this Task<Result> result, Func<Task<Result>> onSuccess, Func<Exception, Task<Result>> onFailure)
        {
            ThrowIfNull(onSuccess, onFailure);
            var r = await result.ConfigureAwait(false);
            return r.IsSuccessful ? await onSuccess().ConfigureAwait(false) : await onFailure(r.Error).ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success <see cref="Action"/>.</param>
        /// <param name="onFailure">The failure <see cref="Action"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> MatchAsync<T>(this Task<Result<T>> result, Func<T, Task> onSuccess, Func<Exception, Task> onFailure)
        {
            ThrowIfNull(onSuccess, onFailure);
            var r = await result.ConfigureAwait(false);
            if (r.IsSuccessful)
                await onSuccess(r.Value).ConfigureAwait(false);
            else
                await onFailure(r.Error).ConfigureAwait(false);

            return r;
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="onSuccess"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccessful"/>; otherwise, invokes the <paramref name="onFailure"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="onSuccess">The success <see cref="Action"/>.</param>
        /// <param name="onFailure">The failure <see cref="Action"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> MatchAsync<T, U>(this Task<Result<T>> result, Func<T, Task<Result<U>>> onSuccess, Func<Exception, Task<Result<U>>> onFailure)
        {
            ThrowIfNull(onSuccess, onFailure);
            var r = await result.ConfigureAwait(false);
            return r.IsSuccessful ? await onSuccess(r.Value).ConfigureAwait(false): await onFailure(r.Error).ConfigureAwait(false);
        }

        /// <summary>
        /// Check parameters and throw where null.
        /// </summary>
        private static void ThrowIfNull(object onSuccess, object onFailure)
        {
            if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));
            if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));
        }
    }
}