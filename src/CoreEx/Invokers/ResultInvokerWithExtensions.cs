// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;
using System;

namespace CoreEx.Invokers
{
    /// <summary>
    /// Provides the <see cref="ResultInvokerWith{T}"/> extension methods.
    /// </summary>
    public static class ResultInvokerWithExtensions
    {
        /// <summary>
        /// Initiates a <see cref="ManagerInvoker"/>-with operation (see <see cref="ResultInvokerWith{T}"/>) where <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IResult"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The originating <see cref="IResult"/>.</param>
        /// <param name="owner">The owner/invoker.</param>
        /// <param name="args">The <see cref="InvokerArgs"/>.</param>
        /// <returns>The <see cref="ResultInvokerWith{T}"/>.</returns>
        public static ResultInvokerWith<T> Manager<T>(this T result, object owner, InvokerArgs? args = null) where T : IResult => new(result, ManagerInvoker.Current, owner, args);

        /// <summary>
        /// Initiates a <see cref="DataSvcInvoker"/>-with operation (see <see cref="ResultInvokerWith{T}"/>) where <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IResult"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The originating <see cref="IResult"/>.</param>
        /// <param name="owner">The owner/invoker.</param>
        /// <param name="args">The <see cref="InvokerArgs"/>.</param>
        /// <returns>The <see cref="ResultInvokerWith{T}"/>.</returns>
        public static ResultInvokerWith<T> DataSvc<T>(this T result, object owner, InvokerArgs? args = null) where T : IResult => new(result, DataSvcInvoker.Current, owner, args);

        /// <summary>
        /// Initiates a <see cref="DataInvoker"/>-with operation (see <see cref="ResultInvokerWith{T}"/>) where <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IResult"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The originating <see cref="IResult"/>.</param>
        /// <param name="owner">The owner/invoker.</param>
        /// <param name="args">The <see cref="InvokerArgs"/>.</param>
        /// <returns>The <see cref="ResultInvokerWith{T}"/>.</returns>
        public static ResultInvokerWith<T> Data<T>(this T result, object owner, InvokerArgs? args = null) where T : IResult => new(result, DataInvoker.Current, owner, args);
    }
}