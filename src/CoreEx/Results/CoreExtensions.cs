// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Diagnostics;

namespace CoreEx.Results
{
    /// <summary>
    /// Provides the <see cref="Result"/> and <see cref="Result{T}"/> core extension methods.
    /// </summary>
    [DebuggerStepThrough]
    public static class CoreExtensions
    {
        /// <summary>
        /// Unwraps the <paramref name="result"/> and where <see cref="Result{T}.IsSuccess"/> invokes the <paramref name="func"/> and returns the resulting <see cref="Result{T}"/>; 
        /// otherwise, where <see cref="Result{T}.IsFailure"/> returns a resulting instance with the corresponding <see cref="Result{T}.Error"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The binding (mapping) function.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<U> Bind<T, U>(this Result<T> result, Func<T, Result<U>> func)
        {
            if (func is null) throw new ArgumentNullException(nameof(func));
            return result.IsSuccess ? func(result.Value) : new Result<U>(result.Error!);
        }

        /// <summary>
        /// Unwraps the <paramref name="result"/> and where <see cref="Result{T}.IsSuccess"/> invokes the <paramref name="func"/> and returns the resulting <see cref="Result{T}"/>; 
        /// otherwise, where <see cref="Result{T}.IsFailure"/> returns a resulting instance with the corresponding <see cref="Result{T}.Error"/>.
        /// </summary>
        /// <typeparam name="T">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="func">The binding (mapping) function.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Bind<T>(this Result result, Func<Result<T>> func)
        {
            if (func is null) throw new ArgumentNullException(nameof(func));
            return result.IsSuccess ? func() : new Result<T>(result.Error!);
        }

        /// <summary>
        /// Binds/converts the <see cref="Result{T}"/> to a corresponding <see cref="Result"/> losing the <see cref="Result{T}.Value"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static Result Bind<T>(this Result<T> result) => result.IsSuccess ? Result.Success : new Result(result.Error);

        /// <summary>
        /// Maps a <paramref name="value"/> into a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to map.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Map<T>(T value) => new(value);

        /// <summary>
        /// Combines the <paramref name="result"/> and <paramref name="other"/> into a single <see cref="Result"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="other">The other <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>Where either or both are failures, a failure state will be returned with the corresponding <see cref="Result{T}.Error"/>; where both contains errors these will be aggregated into an <see cref="AggregateException"/>.</remarks>
        public static Result Combine(this Result result, Result other)
        {
            if (result.IsFailure && other.IsFailure)
                return new Result(new AggregateException(result.Error, other.Error));

            return result.IsFailure ? result : other;
        }

        /// <summary>
        /// Combines the <paramref name="result"/> and <paramref name="other"/> into a single <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="other">The other <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>Where either or both are failures, a failure state will be returned with the corresponding <see cref="Result{T}.Error"/>; where both contains errors these will be aggregated into an <see cref="AggregateException"/>.</remarks>
        public static Result<T> Combine<T>(this Result result, Result<T> other)
        {
            if (result.IsFailure && other.IsFailure)
                return new Result<T>(new AggregateException(result.Error, other.Error));

            if (result.IsFailure)
                return new Result<T>(result.Error!);

            return other;
        }

        /// <summary>
        /// Combines the <paramref name="result"/> and <paramref name="other"/> into a single <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The other and resulting <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="other">The other <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>Where either or both are failures, a failure state will be returned with the corresponding <see cref="Result{T}.Error"/>; where both contains errors these will be aggregated into an <see cref="AggregateException"/>.
        /// <para>Where both are successful and, <typeparamref name="T"/> and <typeparamref name="U"/> are the same <see cref="Type"/>, the <paramref name="result"/> <see cref="Result{T}.Value"/> is used as the returned value (i.e <paramref name="other"/> is ignored).</para></remarks>
        public static Result<U> Combine<T, U>(this Result<T> result, Result<U> other) 
        {
            if (result.IsFailure && other.IsFailure)
                return new Result<U>(new AggregateException(result.Error, other.Error));

            if (result.IsFailure)
                return new Result<U>(result.Error!);

            if (other.IsFailure)
                return other;

            return result.Value is U uv ? uv : other;
        }
    }
}