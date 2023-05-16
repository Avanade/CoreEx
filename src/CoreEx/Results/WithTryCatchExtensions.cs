// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;
using CoreEx.Results.Abstractions;

namespace CoreEx.Results
{
    /// <summary>
    /// Provides the <see cref="Result"/> and <see cref="Result{T}"/> <c>WithTryCatch</c> and <c>WithTryCatchAsync</c> extension methods to execute the corresponding function with a '<c>try/catch</c>' that will result in a failure where an 
    /// <see cref="Exception"/> is encountered; see <see cref="WithTryCatchWrapper"/>.
    /// </summary>
    public static class WithTryCatchExtensions
    {
        #region Synchronous

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WithTryCatch(this Result result, Func<Result, Result> func) => WithTryCatchWrapper.Default.Execute(result, func);

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> WithTryCatch<T>(this Result result, Func<Result, Result<T>> func) => WithTryCatchWrapper.Default.Execute(result, func);

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WithTryCatch<T>(this Result<T> result, Func<Result<T>, Result> func) => WithTryCatchWrapper.Default.Execute(result, func);

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The input <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> WithTryCatch<T, U>(this Result<T> result, Func<Result<T>, Result<U>> func) => WithTryCatchWrapper.Default.Execute(result, func);

        #endregion

        #region AsyncResult

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithTryCatch(this Task<Result> result, Func<Result, Result> func) => WithTryCatchWrapper.Default.Execute(result, func);

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> WithTryCatch<T>(this Task<Result> result, Func<Result, Result<T>> func) => WithTryCatchWrapper.Default.Execute(result, func);

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithTryCatch<T>(this Task<Result<T>> result, Func<Result<T>, Result> func) => WithTryCatchWrapper.Default.Execute(result, func);

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The input <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<U>> WithTryCatch<T, U>(this Task<Result<T>> result, Func<Result<T>, Result<U>> func) => WithTryCatchWrapper.Default.Execute(result, func);

        #endregion

        #region AsyncFunc

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithTryCatchAsync(this Result result, Func<Result, Task<Result>> func) => WithTryCatchWrapper.Default.ExecuteAsync(result, func);

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> WithTryCatchAsync<T>(this Result result, Func<Result, Task<Result<T>>> func) => WithTryCatchWrapper.Default.ExecuteAsync(result, func);

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithTryCatchAsync<T>(this Result<T> result, Func<Result<T>, Task<Result>> func) => WithTryCatchWrapper.Default.ExecuteAsync(result, func);

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The input <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<U>> WithTryCatchAsync<T, U>(this Result<T> result, Func<Result<T>, Task<Result<U>>> func) => WithTryCatchWrapper.Default.ExecuteAsync(result, func);

        #endregion

        #region AsyncBoth

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithTryCatchAsync(this Task<Result> result, Func<Result, Task<Result>> func) => WithTryCatchWrapper.Default.ExecuteAsync(result, func);

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> WithTryCatchAsync<T>(this Task<Result> result, Func<Result, Task<Result<T>>> func) => WithTryCatchWrapper.Default.ExecuteAsync(result, func);

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithTryCatchAsync<T>(this Task<Result<T>> result, Func<Result<T>, Task<Result>> func) => WithTryCatchWrapper.Default.ExecuteAsync(result, func);

        /// <summary>
        /// Performs a <see cref="WithTryCatchWrapper"/> execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The input <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The function to wrap.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<U>> WithTryCatchAsync<T, U>(this Task<Result<T>> result, Func<Result<T>, Task<Result<U>>> func) => WithTryCatchWrapper.Default.ExecuteAsync(result, func);

        #endregion
    }
}