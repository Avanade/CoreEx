// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoreEx.Results.Abstractions
{
    /// <summary>
    /// Represents an <see cref="IResult"/> try-catch-with.
    /// </summary>
    /// <typeparam name="T">The <see cref="IResult"/> <see cref="Type"/>.</typeparam>
    [DebuggerStepThrough]
    public struct TryCatchWith<T> where T : IResult
    {
        private static readonly TryCatchWithInvoker<T> _invoker = new();

        private Dictionary<Type, (Func<Exception, Exception>?, Func<T, LText?>?)>? _catches = null;
        private readonly Func<T, LText?>? _defaultMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TryCatchWith{T}"/> with the originating <paramref name="result"/>.
        /// </summary>
        /// <param name="result">The originating result.</param>
        /// <param name="defaultMessage">The default message to use where the <see cref="Exception"/> has been caught.</param>
        /// <param name="alsoExecuteOnFailure">Indicates whether to also execute where the originating <paramref name="result"/> has a state of <see cref="IResult.IsFailure"/>.</param>
        public TryCatchWith(T result, Func<T, LText?>? defaultMessage = null, bool alsoExecuteOnFailure = false)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            _defaultMessage = defaultMessage;
            AlsoExecuteOnFailure = alsoExecuteOnFailure;
        }

        /// <summary>
        /// Gets the originating result.
        /// </summary>
        public T Result { get; }

        /// <summary>
        /// Indicates whether to also execute where the originating <see cref="Result"/> has a state of <see cref="IResult.IsFailure"/>.
        /// </summary>
        public bool AlsoExecuteOnFailure { get; }

        /// <summary>
        /// Configures the catching of the <typeparamref name="TException"/> with an optional <paramref name="message"/>.
        /// </summary>
        /// <typeparam name="TException">The <see cref="Exception"/> <see cref="Type"/> to catch.</typeparam>
        /// <param name="message">The optional message function to use for the resulting <see cref="BusinessException"/>.</param>
        /// <returns>The <see cref="TryCatchWith{T}"/> to support fluent-style method-chaining.</returns>
        public TryCatchWith<T> Catch<TException>(Func<T, LText?>? message = null) where TException : Exception
        {
            _catches ??= new Dictionary<Type, (Func<Exception, Exception>?, Func<T, LText?>?)>();
            _catches.Add(typeof(TException), (null, message));
            return this;
        }

        /// <summary>
        /// Configures the catching of the <typeparamref name="TException"/> with an <see cref="Exception"/> <paramref name="transformer"/>.
        /// </summary>
        /// <typeparam name="TException">The <see cref="Exception"/> <see cref="Type"/> to catch.</typeparam>
        /// <param name="transformer">The function to transform the caught exception into that to be used as the resulting <see cref="IResult.Error"/>.</param>
        /// <returns>The <see cref="TryCatchWith{T}"/> to support fluent-style method-chaining.</returns>
        public TryCatchWith<T> Catch<TException>(Func<TException, Exception> transformer) where TException : Exception
        {
            if (transformer == null) throw new ArgumentNullException(nameof(transformer));
            _catches ??= new Dictionary<Type, (Func<Exception, Exception>?, Func<T, LText?>?)>();
            _catches.Add(typeof(TException), (ex => transformer((TException)ex), null));
            return this;
        }

        /// <summary>
        /// Handles the <paramref name="exception"/> based on the previous <see cref="Catch{TEx}(Func{TEx, Exception})"/> and <see cref="Catch{TException}(Func{T, LText?}?)"/> configuration(s).
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> that has been caught.</param>
        /// <returns>The resulting <see cref="Exception"/> where handled; otherwise, <c>null</c>.</returns>
        public Exception? HandleException(Exception exception)
        {
            if (_catches == null)
                return new BusinessException(_defaultMessage?.Invoke(Result) ?? exception.Message, exception); ;

            if (!_catches.TryGetValue(exception.GetType(), out var result))
                return null;

            if (result.Item1 != null)
                return result.Item1(exception);

            return new BusinessException(result.Item2?.Invoke(Result) ?? _defaultMessage?.Invoke(Result) ?? exception.Message, exception);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <see cref="Result"/> is <see cref="Result.IsSuccess"/> or <see cref="AlsoExecuteOnFailure"/>.
        /// </summary>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public T With(Func<T, T> func)
        {
            var result = Result;
            return result.IsSuccess || AlsoExecuteOnFailure ? _invoker.Invoke(this, () => func(result)) : result;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <see cref="Result"/> is <see cref="Result.IsSuccess"/> or <see cref="AlsoExecuteOnFailure"/>.
        /// </summary>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public Task<T> WithAsync(Func<T, Task<T>> func)
        {
            var result = Result;
            return result.IsSuccess || AlsoExecuteOnFailure ? _invoker.InvokeAsync(this, _ => func(result), default) : Task.FromResult(result);
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <see cref="Result"/> is <see cref="Result.IsSuccess"/> or <see cref="AlsoExecuteOnFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public U WithAs<U>(Func<T, U> func) where U : IResult
        {
            var result = Result;
            return result.IsSuccess || AlsoExecuteOnFailure ? _invoker.Invoke(this, () => func(result)) : default!;
        }

        /// <summary>
        /// Executes the <paramref name="func"/> where the <see cref="Result"/> is <see cref="Result.IsSuccess"/> or <see cref="AlsoExecuteOnFailure"/> (as new <see cref="Result"/> <see cref="Type"/>).
        /// </summary>
        /// <param name="func">The <see cref="Func{T, TResult}"/> to invoke.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public async Task<U> WithAsAsync<U>(Func<T, Task<U>> func) where U : IResult
        {
            var result = Result;
            return result.IsSuccess || AlsoExecuteOnFailure ? await _invoker.InvokeAsync(this, _ => func(result), default).ConfigureAwait(false) : default!;
        }
    }
}