// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Abstractions
{
    /// <summary>
    /// Wraps an <b>Invoke</b> enabling standard functionality to be added to all invocations. 
    /// </summary>
    /// <typeparam name="TInvoker">The owner (invoking) <see cref="Type"/>.</typeparam>
    /// <typeparam name="TArgs">The arguments <see cref="Type"/>.</typeparam>
    /// <remarks>All public methods result in <see cref="OnInvokeAsync{TResult}(TInvoker, Func{CancellationToken, Task{TResult}}, TArgs?, CancellationToken)"/> being called to maange the underlying invocation. Where no result is specified 
    /// this defaults to '<c>object?</c>' for the purposes of execution.</remarks>
    public abstract class InvokerBase<TInvoker, TArgs>
    {
        /// <summary>
        /// Invokes a <paramref name="func"/> with a <typeparamref name="TResult"/> asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The optional parameter passed to the invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        protected virtual Task<TResult> OnInvokeAsync<TResult>(TInvoker invoker, Func<CancellationToken, Task<TResult>> func, TArgs? args, CancellationToken cancellationToken)
            => func(cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="action">The function to invoke.</param>
        /// <param name="param">The optional parameter passed to the invoke.</param>
        public void Invoke(TInvoker invoker, Action action, TArgs? param = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(); return Task.FromResult<object?>(null!); }, param, CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>
        /// Invokes a <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="param">The optional parameter passed to the invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task InvokeAsync(TInvoker invoker, Func<CancellationToken, Task> func, TArgs? param, CancellationToken cancellationToken = default) 
            => OnInvokeAsync<object?>(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await (func ?? throw new ArgumentNullException(nameof(func))).Invoke(ct).ConfigureAwait(false); return null; }, param, cancellationToken);

        /// <summary>
        /// Invokes a <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task InvokeAsync(TInvoker invoker, Func<CancellationToken, Task> func, CancellationToken cancellationToken = default)
            => OnInvokeAsync<object?>(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await (func ?? throw new ArgumentNullException(nameof(func))).Invoke(ct).ConfigureAwait(false); return null; }, default, cancellationToken);

        /// <summary>
        /// Invokes a <paramref name="func"/> with a <typeparamref name="TResult"/> synchronously.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="param">The optional parameter passed to the invoke.</param>
        /// <returns>The result.</returns>
        public TResult Invoke<TResult>(TInvoker invoker, Func<TResult> func, TArgs? param = default) 
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => Task.FromResult((func ?? throw new ArgumentNullException(nameof(func))).Invoke()), param, CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>
        /// Invokes a <paramref name="func"/> with a <typeparamref name="TResult"/> asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="param">The optional parameter passed to the invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<TResult>(TInvoker invoker, Func<CancellationToken, Task<TResult>> func, TArgs? param, CancellationToken cancellationToken = default) 
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), func ?? throw new ArgumentNullException(nameof(func)), param, cancellationToken);

        /// <summary>
        /// Invokes a <paramref name="func"/> with a <typeparamref name="TResult"/> asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<TResult>(TInvoker invoker, Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), func ?? throw new ArgumentNullException(nameof(func)), default, cancellationToken);
    }
}