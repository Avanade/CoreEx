﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Invokers
{
    /// <summary>
    /// Wraps an <b>Invoke</b> enabling standard functionality to be added to all invocations. 
    /// </summary>
    /// <typeparam name="TInvoker">The owner (invoking) <see cref="Type"/>.</typeparam>
    /// <typeparam name="TArgs">The arguments <see cref="Type"/>.</typeparam>
    /// <remarks>All public methods result in either the synchronous <see cref="OnInvoke{TResult}(InvokeArgs, TInvoker, Func{TResult}, TArgs?)"/> or asynchronous <see cref="OnInvokeAsync{TResult}(InvokeArgs, TInvoker, Func{CancellationToken, Task{TResult}}, TArgs?, CancellationToken)"/>
    /// virtual methods being called to manage the underlying invocation; therefore, where overridding each should be overridden with the same logic. Where no result is specified this defaults to '<c>object?</c>' for the purposes of execution.</remarks>
    public abstract class InvokerBase<TInvoker, TArgs>
    {
        /// <summary>
        /// Invokes a <paramref name="func"/> with a <typeparamref name="TResult"/> synchronously.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="invokeArgs">The <see cref="InvokeArgs"/>.</param>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The arguments passed to the invoke.</param>
        /// <returns>The result.</returns>
        protected virtual TResult OnInvoke<TResult>(InvokeArgs invokeArgs, TInvoker invoker, Func<TResult> func, TArgs? args) => func();

        /// <summary>
        /// Invokes a <paramref name="func"/> with a <typeparamref name="TResult"/> asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="invokeArgs">The <see cref="InvokeArgs"/>.</param>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The arguments passed to the invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The result.</returns>
        protected virtual Task<TResult> OnInvokeAsync<TResult>(InvokeArgs invokeArgs, TInvoker invoker, Func<CancellationToken, Task<TResult>> func, TArgs? args, CancellationToken cancellationToken) => func(cancellationToken);

        /// <summary>
        /// Invoke the <see cref="OnInvoke{TResult}(InvokeArgs, TInvoker, Func{TResult}, TArgs?)"/> with tracing.
        /// </summary>
        private TResult TraceOnInvoke<TResult>(TInvoker invoker, Func<TResult> func, TArgs? args, string? memberName)
        {
            var ia = new InvokeArgs(GetType(), invoker?.GetType(), memberName);
            try
            {
                return ia.TraceResult(OnInvoke(ia, invoker, func, args));
            }
            finally
            {
                ia.Complete();
            }
        }

        /// <summary>
        /// Invoke the <see cref="OnInvokeAsync{TResult}(InvokeArgs, TInvoker, Func{CancellationToken, Task{TResult}}, TArgs?, CancellationToken)"/> with tracing.
        /// </summary>
        private async Task<TResult> TraceOnInvokeAsync<TResult>(TInvoker invoker, Func<CancellationToken, Task<TResult>> func, TArgs? args, string? memberName, CancellationToken cancellationToken)
        {
            var ia = new InvokeArgs(GetType(), invoker?.GetType(), memberName);
            try
            {
                return ia.TraceResult(await OnInvokeAsync(ia, invoker, func, args, cancellationToken).ConfigureAwait(false));
            }
            finally
            {
                ia.Complete();
            }
        }

        #region Sync/NoResult

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="args">The arguments passed to the invoke.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public void Invoke(TInvoker invoker, Action action, TArgs? args = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvoke<object?>(invoker ?? throw new ArgumentNullException(nameof(invoker)), () => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(); return null!; }, args, memberName);

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the action.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="args">The arguments passed to the invoke.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public void Invoke<T1>(TInvoker invoker, T1 p1, Action<T1> action, TArgs? args = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvoke<object?>(invoker ?? throw new ArgumentNullException(nameof(invoker)), () => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(p1); return null!; }, args, memberName);

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the action.</param>
        /// <param name="p2">Parameter 2 to pass through to the action.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="args">The arguments passed to the invoke.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public void Invoke<T1, T2>(TInvoker invoker, T1 p1, T2 p2, Action<T1, T2> action, TArgs? args = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvoke<object?>(invoker ?? throw new ArgumentNullException(nameof(invoker)), () => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(p1, p2); return null!; }, args, memberName);

        /// <summary>
        /// Invokes an <paramref name="action"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the action.</param>
        /// <param name="p2">Parameter 2 to pass through to the action.</param>
        /// <param name="p3">Parameter 3 to pass through to the action.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="args">The arguments passed to the invoke.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public void Invoke<T1, T2, T3>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Action<T1, T2, T3> action, TArgs? args = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvoke<object?>(invoker ?? throw new ArgumentNullException(nameof(invoker)), () => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(p1, p2, p3); return null!; }, args, memberName);

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
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public void Invoke<T1, T2, T3, T4>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Action<T1, T2, T3, T4> action, TArgs? args = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvoke<object?>(invoker ?? throw new ArgumentNullException(nameof(invoker)), () => { (action ?? throw new ArgumentNullException(nameof(action))).Invoke(p1, p2, p3, p4); return null!; }, args, memberName);

        #endregion

        #region Sync/Result

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public TResult Invoke<TResult>(TInvoker invoker, Func<TResult> func, TArgs? args = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvoke(invoker ?? throw new ArgumentNullException(nameof(invoker)), () => func(), args, memberName);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public TResult Invoke<T1, TResult>(TInvoker invoker, T1 p1, Func<T1, TResult> func, TArgs? args = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvoke(invoker ?? throw new ArgumentNullException(nameof(invoker)), () => func(p1), args, memberName);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public TResult Invoke<T1, T2, TResult>(TInvoker invoker, T1 p1, T2 p2, Func<T1, T2, TResult> func, TArgs? args = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvoke(invoker ?? throw new ArgumentNullException(nameof(invoker)), () => func(p1, p2), args, memberName);

        /// <summary>
        /// Invokes an <paramref name="func"/> synchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public TResult Invoke<T1, T2, T3, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Func<T1, T2, T3, TResult> func, TArgs? args = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvoke(invoker ?? throw new ArgumentNullException(nameof(invoker)), () => func(p1, p2, p3), args, memberName);

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
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public TResult Invoke<T1, T2, T3, T4, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Func<T1, T2, T3, T4, TResult> func, TArgs? args = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvoke(invoker ?? throw new ArgumentNullException(nameof(invoker)), () => func(p1, p2, p3, p4), args, memberName);

        #endregion

        #region Async/NoResult

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public Task InvokeAsync(TInvoker invoker, Func<CancellationToken, Task> func, TArgs? args, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(ct).ConfigureAwait(false); return (object?)null!; }, args, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public Task InvokeAsync<T1>(TInvoker invoker, T1 p1, Func<T1, CancellationToken, Task> func, TArgs? args, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, ct).ConfigureAwait(false); return (object?)null!; }, args, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public Task InvokeAsync<T1, T2>(TInvoker invoker, T1 p1, T2 p2, Func<T1, T2, CancellationToken, Task> func, TArgs? args, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, p2, ct).ConfigureAwait(false); return (object?)null!; }, args, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public Task InvokeAsync<T1, T2, T3>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Func<T1, T2, T3, CancellationToken, Task> func, TArgs? args, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, p2, p3, ct).ConfigureAwait(false); return (object?)null!; }, args, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="p4">Parameter 4 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public Task InvokeAsync<T1, T2, T3, T4>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Func<T1, T2, T3, T4, CancellationToken, Task> func, TArgs? args, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, p2, p3, p4, ct).ConfigureAwait(false); return (object?)null!; }, args, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public Task InvokeAsync(TInvoker invoker, Func<CancellationToken, Task> func, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(ct).ConfigureAwait(false); return (object?)null!; }, default, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public Task InvokeAsync<T1>(TInvoker invoker, T1 p1, Func<T1, CancellationToken, Task> func, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, ct).ConfigureAwait(false); return (object?)null!; }, default, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public Task InvokeAsync<T1, T2>(TInvoker invoker, T1 p1, T2 p2, Func<T1, T2, CancellationToken, Task> func, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, p2, ct).ConfigureAwait(false); return (object?)null!; }, default, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public Task InvokeAsync<T1, T2, T3>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Func<T1, T2, T3, CancellationToken, Task> func, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, p2, p3, ct).ConfigureAwait(false); return (object?)null!; }, default, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="p4">Parameter 4 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        public Task InvokeAsync<T1, T2, T3, T4>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Func<T1, T2, T3, T4, CancellationToken, Task> func, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), async ct => { await func(p1, p2, p3, p4, ct).ConfigureAwait(false); return (object?)null!; }, default, memberName, cancellationToken);

        #endregion

        #region Async/Result

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<TResult>(TInvoker invoker, Func<CancellationToken, Task<TResult>> func, TArgs? args, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(ct), args, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, TResult>(TInvoker invoker, T1 p1, Func<T1, CancellationToken, Task<TResult>> func, TArgs? args, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, ct), args, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, T2, TResult>(TInvoker invoker, T1 p1, T2 p2, Func<T1, T2, CancellationToken, Task<TResult>> func, TArgs? args, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, p2, ct), args, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, T2, T3, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Func<T1, T2, T3, CancellationToken, Task<TResult>> func, TArgs? args, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, p2, p3, ct), args, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="p4">Parameter 4 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="args">The <typeparamref name="TArgs"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, T2, T3, T4, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Func<T1, T2, T3, T4, CancellationToken, Task<TResult>> func, TArgs? args, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, p2, p3, p4, ct), args, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<TResult>(TInvoker invoker, Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(ct), default, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">TParameter 1 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, TResult>(TInvoker invoker, T1 p1, Func<T1, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, ct), default, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, T2, TResult>(TInvoker invoker, T1 p1, T2 p2, Func<T1, T2, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, p2, ct), default, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, T2, T3, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, Func<T1, T2, T3, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, p2, p3, ct), default, memberName, cancellationToken);

        /// <summary>
        /// Invokes an <paramref name="func"/> asynchronously.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="p1">Parameter 1 to pass through to the function.</param>
        /// <param name="p2">Parameter 2 to pass through to the function.</param>
        /// <param name="p3">Parameter 3 to pass through to the function.</param>
        /// <param name="p4">Parameter 4 to pass through to the function.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
        /// <returns>The result.</returns>
        public Task<TResult> InvokeAsync<T1, T2, T3, T4, TResult>(TInvoker invoker, T1 p1, T2 p2, T3 p3, T4 p4, Func<T1, T2, T3, T4, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
            => TraceOnInvokeAsync(invoker ?? throw new ArgumentNullException(nameof(invoker)), ct => func(p1, p2, p3, p4, ct), default, memberName, cancellationToken);

        #endregion
    }
}