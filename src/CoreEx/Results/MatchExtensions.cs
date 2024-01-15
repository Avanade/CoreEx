// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Results
{
    public static partial class ResultsExtensions
    {
        #region Synchronous

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Match(this Result result, Action ok, Action<Exception> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            return result.Match(() =>
            {
                ok();
                return result;
            }, e =>
            {
                fail(e);
                return result;
            });
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Match(this Result result, Func<Result> ok, Func<Exception, Result> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            return result.IsSuccess ? ok() : fail(result.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> MatchAs<T>(this Result result, Func<Result<T>> ok, Func<Exception, Result<T>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            return result.IsSuccess ? ok() : fail(result.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Match<T>(this Result<T> result, Action<T> ok, Action<Exception> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            return result.Match(v =>
            {
                ok(v);
                return result;
            }, e =>
            {
                fail(e);
                return result;
            });
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Match<T>(this Result<T> result, Func<T, Result<T>> ok, Func<Exception, Result<T>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            return result.IsSuccess ? ok(result.Value) : fail(result.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> MatchAs<T, U>(this Result<T> result, Func<T, Result<U>> ok, Func<Exception, Result<U>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            return result.IsSuccess ? ok(result.Value) : fail(result.Error);
        }

        #endregion

        #region AsyncResult

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> Match(this Task<Result> result, Action ok, Action<Exception> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            var r = await result.ConfigureAwait(false);
            return r.Match(() =>
            {
                ok();
                return r;
            }, e =>
            {
                fail(e);
                return r;
            });
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> Match(this Task<Result> result, Func<Result> ok, Func<Exception, Result> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            var r = await result.ConfigureAwait(false);
            return r.IsSuccess ? ok() : fail(r.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> MatchAs<T>(this Task<Result> result, Func<Result<T>> ok, Func<Exception, Result<T>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            var r = await result.ConfigureAwait(false);
            return r.IsSuccess ? ok() : fail(r.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> Match<T>(this Task<Result<T>> result, Action<T> ok, Action<Exception> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            var r = await result.ConfigureAwait(false);
            return r.Match(v =>
            {
                ok(v);
                return r;
            }, e =>
            {
                fail(e);
                return r;
            });
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> Match<T>(this Task<Result<T>> result, Func<T, Result<T>> ok, Func<Exception, Result<T>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            var r = await result.ConfigureAwait(false);
            return r.IsSuccess ? ok(r.Value) : fail(r.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> MatchAs<T, U>(this Task<Result<T>> result, Func<T, Result<U>> ok, Func<Exception, Result<U>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            var r = await result.ConfigureAwait(false);
            return r.IsSuccess ? ok(r.Value) : fail(r.Error);
        }

        #endregion

        #region AsyncFunc

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> MatchAsync(this Result result, Func<Task> ok, Func<Exception, Task> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            return await result.MatchAsync(async () =>
            {
                await ok().ConfigureAwait(false);
                return result;
            }, async e =>
            {
                await fail(e).ConfigureAwait(false);
                return result;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> MatchAsync(this Result result, Func<Task<Result>> ok, Func<Exception, Task<Result>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            return result.IsSuccess ? ok() : fail(result.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> MatchAsAsync<T>(this Result result, Func<Task<Result<T>>> ok, Func<Exception, Task<Result<T>>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            return result.IsSuccess ? ok() : fail(result.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result<T>> MatchAsync<T>(this Result<T> result, Func<T, Task> ok, Func<Exception, Task> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            return result.MatchAsync(async v =>
            {
                await ok(v).ConfigureAwait(false);
                return result;
            }, async e =>
            {
                await fail(e).ConfigureAwait(false);
                return result;
            });
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result<T>> MatchAsync<T>(this Result<T> result, Func<T, Task<Result<T>>> ok, Func<Exception, Task<Result<T>>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            return result.IsSuccess ? ok(result.Value) : fail(result.Error);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result<U>> MatchAsAsync<T, U>(this Result<T> result, Func<T, Task<Result<U>>> ok, Func<Exception, Task<Result<U>>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            return result.IsSuccess ? ok(result.Value) : fail(result.Error);
        }

        #endregion

        #region AsyncBoth

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> MatchAsync(this Task<Result> result, Func<Task> ok, Func<Exception, Task> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            var r = await result.ConfigureAwait(false);
            if (r.IsSuccess)
                await ok().ConfigureAwait(false);
            else
                await fail(r.Error).ConfigureAwait(false);

            return r;
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> MatchAsync(this Task<Result> result, Func<Task<Result>> ok, Func<Exception, Task<Result>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            var r = await result.ConfigureAwait(false);
            return r.IsSuccess ? await ok().ConfigureAwait(false) : await fail(r.Error).ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> MatchAsAsync<T>(this Task<Result> result, Func<Task<Result<T>>> ok, Func<Exception, Task<Result<T>>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            var r = await result.ConfigureAwait(false);
            return r.IsSuccess ? await ok().ConfigureAwait(false) : await fail(r.Error).ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> MatchAsync<T>(this Task<Result<T>> result, Func<T, Task> ok, Func<Exception, Task> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            var r = await result.ConfigureAwait(false);
            if (r.IsSuccess)
                await ok(r.Value).ConfigureAwait(false);
            else
                await fail(r.Error).ConfigureAwait(false);

            return r;
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<T>> MatchAsAsync<T>(this Task<Result<T>> result, Func<T, Task<Result<T>>> ok, Func<Exception, Task<Result<T>>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            var r = await result.ConfigureAwait(false);
            return r.IsSuccess ? await ok(r.Value).ConfigureAwait(false) : await fail(r.Error).ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes (matches) the <paramref name="ok"/> function when the <paramref name="result"/> is <see cref="IResult.IsSuccess"/>; otherwise, invokes the <paramref name="fail"/> function.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="ok">The success function.</param>
        /// <param name="fail">The failure function.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result<U>> MatchAsAsync<T, U>(this Task<Result<T>> result, Func<T, Task<Result<U>>> ok, Func<Exception, Task<Result<U>>> fail)
        {
            MatchThrowIfNull(result, ok, fail);
            var r = await result.ConfigureAwait(false);
            return r.IsSuccess ? await ok(r.Value).ConfigureAwait(false): await fail(r.Error).ConfigureAwait(false);
        }

        /// <summary>
        /// Check parameters and throw where null.
        /// </summary>
        private static void MatchThrowIfNull(object result, object ok, object fail)
        {
            result.ThrowIfNull(nameof(result));
            ok.ThrowIfNull(nameof(ok));
            fail.ThrowIfNull(nameof(fail));
        }

        #endregion
    }
}