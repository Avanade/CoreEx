// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;
using System;
using System.Threading.Tasks;

namespace CoreEx.Invokers
{
    /// <summary>
    /// Provides the <see cref="Result"/> and <see cref="Result{T}"/> <c>WithDataSvc</c> and <c>WithDataSvcAsync</c> extension methods to execute the corresponding function with an <see cref="InvokerBase"/>; see <see cref="DataSvcInvoker"/>.
    /// </summary>
    public static class WithDataSvcExtensions
    {
        #region Synchronous

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WithDataSvc(this Result result, object? owner, Func<Result, Result> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.Execute(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> WithDataSvc<T>(this Result result, object? owner, Func<Result, Result<T>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.Execute(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result WithDataSvc<T>(this Result<T> result, object? owner, Func<Result<T>, Result> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.Execute(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The input <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> WithDataSvc<T, U>(this Result<T> result, object? owner, Func<Result<T>, Result<U>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.Execute(result, func, (owner, args));

        #endregion

        #region AsyncResult

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithDataSvc(this Task<Result> result, object? owner, Func<Result, Result> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.Execute(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> WithDataSvc<T>(this Task<Result> result, object? owner, Func<Result, Result<T>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.Execute(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithDataSvc<T>(this Task<Result<T>> result, object? owner, Func<Result<T>, Result> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.Execute(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The input <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<U>> WithDataSvc<T, U>(this Task<Result<T>> result, object? owner, Func<Result<T>, Result<U>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.Execute(result, func, (owner, args));

        #endregion

        #region AsyncFunc

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithDataSvcAsync(this Result result, object? owner, Func<Result, Task<Result>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> WithDataSvcAsync<T>(this Result result, object? owner, Func<Result, Task<Result<T>>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithDataSvcAsync<T>(this Result<T> result, object? owner, Func<Result<T>, Task<Result>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The input <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<U>> WithDataSvcAsync<T, U>(this Result<T> result, object? owner, Func<Result<T>, Task<Result<U>>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        #endregion

        #region AsyncBoth

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithDataSvcAsync(this Task<Result> result, object? owner, Func<Result, Task<Result>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<T>> WithDataSvcAsync<T>(this Task<Result> result, object? owner, Func<Result, Task<Result<T>>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Task<Result> WithDataSvcAsync<T>(this Task<Result<T>> result, object? owner, Func<Result<T>, Task<Result>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        /// <summary>
        /// Performs a <see cref="DataSvcInvoker"/> wrapper execution of the <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="T">The input <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="owner">The owner (parent).</param>
        /// <param name="func">The function to wrap.</param>
        /// <param name="args">The options <see cref="InvokerArgs"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Task<Result<U>> WithDataSvcAsync<T, U>(this Task<Result<T>> result, object? owner, Func<Result<T>, Task<Result<U>>> func, InvokerArgs? args = default)
            => WithInvokerWrapper<DataSvcInvoker>.Default.ExecuteAsync(result, func, (owner, args));

        #endregion
    }
}