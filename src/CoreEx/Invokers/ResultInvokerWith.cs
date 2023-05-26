// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;
using System;
using System.Threading.Tasks;

namespace CoreEx.Invokers
{
    /// <summary>
    /// Represents the <see cref="IResult"/> <see cref="InvokerBase"/> with capability.
    /// </summary>
    /// <typeparam name="T">The <see cref="IResult"/> <see cref="Type"/>.</typeparam>
    public readonly struct ResultInvokerWith<T> where T : IResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResultInvokerWith{T}"/>.
        /// </summary>
        /// <param name="result">The originating <see cref="IResult"/>.</param>
        /// <param name="invoker">The <see cref="InvokerBase"/>.</param>
        /// <param name="owner">The owner/invoker.</param>
        /// <param name="args">The <see cref="InvokerArgs"/>.</param>
        public ResultInvokerWith(T result, InvokerBase invoker, object owner, InvokerArgs? args = null)
        {
            Result = result ?? throw new System.ArgumentNullException(nameof(result));
            Invoker = invoker ?? throw new System.ArgumentNullException(nameof(invoker));
            Owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
            Args = args ?? InvokerArgs.Default;
        }

        /// <summary>
        /// Gets the originating result.
        /// </summary>
        public T Result { get; }

        /// <summary>
        /// Gets the <see cref="InvokerBase"/>.
        /// </summary>
        public InvokerBase Invoker { get; }

        /// <summary>
        /// Gets the owner/invoker.
        /// </summary>
        public object Owner { get; }

        /// <summary>
        /// Gets the <see cref="InvokerArgs"/>.
        /// </summary>
        public InvokerArgs Args { get; }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <see cref="Result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public T With(Func<T, T> func)
        {
            var result = Result;
            return result.IsSuccess ? Invoker.Invoke(Owner, () => func(result), Args) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <see cref="Result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public Task<T> WithAsync(Func<T, Task<T>> func)
        {
            var result = Result;
            return result.IsSuccess ? Invoker.InvokeAsync(Owner, _ => func(result), Args, default) : Task.FromResult(result);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <see cref="Result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public U WithAs<U>(Func<T, U> func) where U : IResult
        {
            var result = Result;
            return result.IsSuccess ? Invoker.Invoke(Owner, () => func(result), Args) : default!;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <see cref="Result"/> is <see cref="Result.IsSuccess"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public async Task<U> WithAsAsync<U>(Func<T, Task<U>> func) where U : IResult
        {
            var result = Result;
            return result.IsSuccess ? await Invoker.InvokeAsync(Owner, _ => func(result), Args, default).ConfigureAwait(false) : default!;
        }
    }
}