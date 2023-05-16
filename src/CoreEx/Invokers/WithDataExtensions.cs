// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;
using System;
using System.Threading.Tasks;

namespace CoreEx.Invokers
{
    /// <summary>
    /// Provides the <see cref="Result"/> and <see cref="Result{T}"/> <c>WithData</c> and <c>WithDataAsync</c> extension methods to execute the corresponding function with an <see cref="InvokerBase"/>; see <see cref="DataInvoker"/>.
    /// </summary>
    public static class WithDataExtensions
    {
        #region Synchronous

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WithData(this Result result, object? owner, Func<Result, Result> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.Execute(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> WithData<T>(this Result result, object? owner, Func<Result, Result<T>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.Execute(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WithData<T>(this Result<T> result, object? owner, Func<Result<T>, Result> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.Execute(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The input <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> WithData<T, U>(this Result<T> result, object? owner, Func<Result<T>, Result<U>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.Execute(result, func, (owner, args));

        #endregion

        #region AsyncResult

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithData(this Task<Result> result, object? owner, Func<Result, Result> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.Execute(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> WithData<T>(this Task<Result> result, object? owner, Func<Result, Result<T>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.Execute(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithData<T>(this Task<Result<T>> result, object? owner, Func<Result<T>, Result> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.Execute(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The input <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<U>> WithData<T, U>(this Task<Result<T>> result, object? owner, Func<Result<T>, Result<U>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.Execute(result, func, (owner, args));

        #endregion

        #region AsyncFunc

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithDataAsync(this Result result, object? owner, Func<Result, Task<Result>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> WithDataAsync<T>(this Result result, object? owner, Func<Result, Task<Result<T>>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithDataAsync<T>(this Result<T> result, object? owner, Func<Result<T>, Task<Result>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The input <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<U>> WithDataAsync<T, U>(this Result<T> result, object? owner, Func<Result<T>, Task<Result<U>>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        #endregion

        #region AsyncBoth

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithDataAsync(this Task<Result> result, object? owner, Func<Result, Task<Result>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> WithDataAsync<T>(this Task<Result> result, object? owner, Func<Result, Task<Result<T>>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithDataAsync<T>(this Task<Result<T>> result, object? owner, Func<Result<T>, Task<Result>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The input <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<U>> WithDataAsync<T, U>(this Task<Result<T>> result, object? owner, Func<Result<T>, Task<Result<U>>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        #endregion
    }
}