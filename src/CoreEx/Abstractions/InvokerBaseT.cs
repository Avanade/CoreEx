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
    /// <remarks>All public methods result in <see cref="OnInvokeAsync{TResult}(TInvoker, Func{CancellationToken, Task{TResult}}, TArgs, CancellationToken)"/> being called to manage the underlying invocation. Where no result is specified 
    /// this defaults to '<c>object?</c>' for the purposes of execution.</remarks>
    public abstract class InvokerBase<TInvoker, TArgs>
    {
        /// <summary>
        /// Invokes a <paramref name="func"/> with a <typeparamref name="TResult"/> asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The arguments passed to the invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        protected virtual Task<TResult> OnInvokeAsync<TResult>(TInvoker invoker, Func<CancellationToken, Task<TResult>> func, TArgs? args, CancellationToken cancellationToken)
            => func(cancellationToken);

        #region Sync/NoResult

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="args">The arguments passed to the invoke.</param>
        public void Invoke(TInvoker invoker, Action action, TArgs? args = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(); return Task.FromResult<object?>(null!); }, args, CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the action.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="args">The arguments passed to the invoke.</param>
        public void Invoke<T1>(TInvoker invoker, T1 p1, Action<T1> action, TArgs? args = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(p1); return Task.FromResult<object?>(null!); }, args, CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the action.</param>
        /// <param name="p2">Parameter 2 to pass through to the action.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="args">The arguments passed to the invoke.</param>
        public void Invoke<T1, T2>(TInvoker invoker, T1 p1, T2 p2, Action<T1, T2> action, TArgs? args = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(p1, p2); return Task.FromResult<object?>(null!); }, args, CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the action.</param>
        /// <param name="p2">Parameter 2 to pass through to the action.</param>
        /// <param name="p3">Parameter 3 to pass through to the action.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="args">The arguments passed to the invoke.</param>
        public void Invoke<T1, T2, T3>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Action<T1, T2, T3> action, TArgs? args = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(p1, p2, p3); return Task.FromResult<object?>(null!); }, args, CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the action.</param>
        /// <param name="p2">Parameter 2 to pass through to the action.</param>
        /// <param name="p3">Parameter 3 to pass through to the action.</param>
        /// <param name="p4">Parameter 4 to pass through to the action.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="args">The arguments passed to the invoke.</param>
        public void Invoke<T1, T2, T3, T4>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Action<T1, T2, T3, T4> action, TArgs? args = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(p1, p2, p3, p4); return Task.FromResult<object?>(null!); }, args, CancellationToken.None).GetAwaiter().GetResult();

        #endregion

        #region Sync/Result

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <returns>The result.</returns>
        public TResult Invoke<TResult>(TInvoker invoker, Func<TResult> func, TArgs? args = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => Task.FromResult(func()), args, CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <returns>The result.</returns>
        public TResult Invoke<T1, TResult>(TInvoker invoker, T1 p1, Func<T1, TResult> func, TArgs? args = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => Task.FromResult(func(p1)), args, CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <returns>The result.</returns>
        public TResult Invoke<T1, T2, TResult>(TInvoker invoker, T1 p1, T2 p2, Func<T1, T2, TResult> func, TArgs? args = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => Task.FromResult(func(p1, p2)), args, CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <returns>The result.</returns>
        public TResult Invoke<T1, T2, T3, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Func<T1, T2, T3, TResult> func, TArgs? args = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => Task.FromResult(func(p1, p2, p3)), args, CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="p4">Parameter 4 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <returns>The result.</returns>
        public TResult Invoke<T1, T2, T3, T4, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Func<T1, T2, T3, T4, TResult> func, TArgs? args = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), _ => Task.FromResult(func(p1, p2, p3, p4)), args, CancellationToken.None).GetAwaiter().GetResult();

        #endregion

        #region Async/NoResult

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task InvokeAsync(TInvoker invoker, Func<CancellationToken, Task> func, TArgs? args = default, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(ct).ConfigureAwait(false); return (object?)null!; }, args, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task InvokeAsync<T1>(TInvoker invoker, T1 p1, Func<T1, CancellationToken, Task> func, TArgs? args = default, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, ct).ConfigureAwait(false); return (object?)null!; }, args, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task InvokeAsync<T1, T2>(TInvoker invoker, T1 p1, T2 p2, Func<T1, T2, CancellationToken, Task> func, TArgs? args = default, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, p2, ct).ConfigureAwait(false); return (object?)null!; }, args, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task InvokeAsync<T1, T2, T3>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Func<T1, T2, T3, CancellationToken, Task> func, TArgs? args = default, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, p2, p3, ct).ConfigureAwait(false); return (object?)null!; }, args, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="p4">Parameter 4 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task InvokeAsync<T1, T2, T3, T4>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Func<T1, T2, T3, T4, CancellationToken, Task> func, TArgs? args = default, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, p2, p3, p4, ct).ConfigureAwait(false); return (object?)null!; }, args, cancellationToken);

        #endregion

        #region Async/Result

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<TResult>(TInvoker invoker, Func<CancellationToken, Task<TResult>> func, TArgs? args = default, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(ct), args, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, TResult>(TInvoker invoker, T1 p1, Func<T1, CancellationToken, Task<TResult>> func, TArgs? args = default, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, ct), args, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, T2, TResult>(TInvoker invoker, T1 p1, T2 p2, Func<T1, T2, CancellationToken, Task<TResult>> func, TArgs? args = default, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, p2, ct), args, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, T2, T3, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Func<T1, T2, T3, CancellationToken, Task<TResult>> func, TArgs? args = default, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, p2, p3, ct), args, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="p4">Parameter 4 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, T2, T3, T4, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Func<T1, T2, T3, T4, CancellationToken, Task<TResult>> func, TArgs? args = default, CancellationToken cancellationToken = default)
            => OnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, p2, p3, p4, ct), args, cancellationToken);

        #endregion
    }
}