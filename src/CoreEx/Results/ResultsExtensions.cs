// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoreEx.Results
{
    /// <summary>
    /// Provides the <see cref="Result"/> and <see cref="Result{T}"/> extension methods.
    /// </summary>
    [DebuggerStepThrough]
    public static partial class ResultsExtensions
    {
        /// <summary>
        /// Check parameters and throw where null.
        /// </summary>
        private static void ThrowIfNull(object result)
        {
            result.ThrowIfNull(nameof(result));
        }

        /// <summary>
        /// Check parameters and throw where null.
        /// </summary>
        private static void ThrowIfNull(object result, object func, string? name = null)
        {
            ThrowIfNull(result);
            func.ThrowIfNull(name ?? nameof(func));
        }

        /// <summary>
        /// Converts the <see cref="Result{T}"/> to a corresponding <see cref="Result"/> losing the <see cref="Result{T}.Value"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <returns>The corresponding <see cref="Result"/>.</returns>
        /// <remarks>This invokes <see cref="CoreExtensions.Bind{T}(Result{T})"/> internally to perform.</remarks>
        public static Result AsResult<T>(this Result<T> result) => result.Bind();

        /// <summary>
        /// Converts the <see cref="Result{T}"/> to a corresponding <see cref="Result"/> losing the <see cref="Result{T}.Value"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <returns>The corresponding <see cref="Result"/>.</returns>
        /// <remarks>This invokes <see cref="CoreExtensions.Bind{T}(Result{T})"/> internally to perform.</remarks>
        public static async Task<Result> AsResult<T>(this Task<Result<T>> result)
        {
            var r = await result.ConfigureAwait(false);
            return r.Bind();
        }

        /// <summary>
        /// Converts the <see cref="Result{T}"/> to a corresponding <see cref="Result{T}"/> (of <see cref="Type"/> <typeparamref name="U"/>) defaulting to <see cref="Result{T}.None"/> where <see cref="Result.IsSuccess"/> losing the 
        /// <see cref="Result{T}.Value"/>; otherwise, where <see cref="Result.IsFailure"/> returns a resulting instance with the corresponding <see cref="Result.Error"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
        /// <remarks>This invokes <see cref="CoreExtensions.Bind{T, U}(Result{T})"/> internally to perform.</remarks>
        public static Result<U> AsResult<T, U>(this Result<T> result) => result.Bind<T, U>();

        /// <summary>
        /// Converts the <see cref="Result{T}"/> to a corresponding <see cref="Result{T}"/> (of <see cref="Type"/> <typeparamref name="U"/>) defaulting to <see cref="Result{T}.None"/> where <see cref="Result.IsSuccess"/> losing the 
        /// <see cref="Result{T}.Value"/>; otherwise, where <see cref="Result.IsFailure"/> returns a resulting instance with the corresponding <see cref="Result.Error"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="U">The output (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
        /// <remarks>This invokes <see cref="CoreExtensions.Bind{T, U}(Result{T})"/> internally to perform.</remarks>
        public static async Task<Result<U>> AsResult<T, U>(this Task<Result<T>> result)
        {
            var r = await result.ConfigureAwait(false);
            return r.Bind<T, U>();
        }

        /// <summary>
        /// Verifies that the <see cref="Result{T}.Value"/> is not <c>null</c> where the <paramref name="result"/> is <see cref="Result{T}.IsSuccess"/> and throws a corresponding <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="name">The value name (defaults to <see cref="Validation.Validation.ValueNameDefault"/>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> ThrowIfNull<T>(this Result<T> result, string? name = null)
            => result.IsSuccess && result.Value == null ? throw new ArgumentNullException(name ?? Validation.Validation.ValueNameDefault) : result;

        /// <summary>
        /// Enables adjustment (changes) to a <see cref="Result{T}.Value"/> via an <paramref name="adjuster"/> action where the <paramref name="result"/> is <see cref="Result{T}.IsSuccess"/>
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="adjuster">The adjusting action (invoked only where the underlying <see cref="Result{T}.Value"/> is not <c>null</c>).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Adjusts<T>(this Result<T> result, Action<T> adjuster)
        {
            if (result.IsSuccess)
                result.Value.Adjust(adjuster);

            return result;
        }

        /// <summary>
        /// Checks whether the user has the required <paramref name="permission"/> (see <see cref="ExecutionContext.UserIsAuthorized(string)"/>).
        /// </summary>
        /// <typeparam name="TResult">The <see cref="Result"/> or <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="IResult"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>; where <c>null</c> will attempt to use <see cref="ExecutionContext.Current"/> where <see cref="ExecutionContext.HasCurrent"/>.</param>
        /// <param name="permission">The permission to validate.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public static TResult UserIsAuthorized<TResult>(this TResult result, ExecutionContext executionContext, string permission) where TResult : IResult
        {
            if (result.IsFailure)
                return result;

            executionContext ??= ExecutionContext.HasCurrent ? ExecutionContext.Current : executionContext.ThrowIfNull(nameof(executionContext));
            var r = executionContext.UserIsAuthorized(permission);
            return r.IsSuccess ? result : (TResult)result.ToFailure(r.Error);
        }

        /// <summary>
        /// Checks whether the user has the required permission as a combination of <paramref name="entity"/> and <paramref name="action"/> (see <see cref="ExecutionContext.UserIsAuthorized(string, string)"/>).
        /// </summary>
        /// <typeparam name="TResult">The <see cref="Result"/> or <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="IResult"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>; where <c>null</c> will attempt to use <see cref="ExecutionContext.Current"/> where <see cref="ExecutionContext.HasCurrent"/>.</param>
        /// <param name="entity">The entity name.</param>
        /// <param name="action">The action name.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public static TResult UserIsAuthorized<TResult>(this TResult result, ExecutionContext executionContext, string entity, string action) where TResult : IResult
        {
            if (result.IsFailure)
                return result;

            executionContext ??= ExecutionContext.HasCurrent ? ExecutionContext.Current : executionContext.ThrowIfNull(nameof(executionContext));
            var r = executionContext.UserIsAuthorized(entity, action);
            return r.IsSuccess ? result : (TResult)result.ToFailure(r.Error);
        }

        /// <summary>
        /// Checks whether the user has the required <paramref name="permission"/> (see <see cref="ExecutionContext.UserIsAuthorized(string)"/>).
        /// </summary>
        /// <typeparam name="TResult">The <see cref="Result"/> or <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="IResult"/>.</param>
        /// <param name="permission">The permission to validate.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public static TResult UserIsAuthorized<TResult>(this TResult result, string permission) where TResult : IResult => UserIsAuthorized(result, (ExecutionContext)null!, permission);

        /// <summary>
        /// Checks whether the user has the required permission as a combination of <paramref name="entity"/> and <paramref name="action"/> (see <see cref="ExecutionContext.UserIsAuthorized(string, string)"/>).
        /// </summary>
        /// <typeparam name="TResult">The <see cref="Result"/> or <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="IResult"/>.</param>
        /// <param name="entity">The entity name.</param>
        /// <param name="action">The action name.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public static TResult UserIsAuthorized<TResult>(this TResult result, string entity, string action) where TResult : IResult => UserIsAuthorized(result, null!, entity, action);

        /// <summary>
        /// Checks whether the user is in specified <paramref name="role"/> (see <see cref="ExecutionContext.UserIsInRole(string)"/>).
        /// </summary>
        /// <typeparam name="TResult">The <see cref="Result"/> or <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="IResult"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>; where <c>null</c> will attempt to use <see cref="ExecutionContext.Current"/> where <see cref="ExecutionContext.HasCurrent"/>.</param>
        /// <param name="role">The role name.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public static TResult UserIsInRole<TResult>(this TResult result, ExecutionContext executionContext, string role) where TResult : IResult
        {
            if (result.IsFailure)
                return result;

            executionContext ??= ExecutionContext.HasCurrent ? ExecutionContext.Current : executionContext.ThrowIfNull(nameof(executionContext));
            var r = executionContext.UserIsInRole(role);
            return r.IsSuccess ? result : (TResult)result.ToFailure(r.Error);
        }

        /// <summary>
        /// Checks whether the user is in specified <paramref name="role"/> (see <see cref="ExecutionContext.UserIsInRole(string)"/>).
        /// </summary>
        /// <typeparam name="TResult">The <see cref="Result"/> or <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="IResult"/>.</param>
        /// <param name="role">The role name.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        public static TResult UserIsInRole<TResult>(this TResult result, string role) where TResult : IResult => UserIsInRole(result, null!, role);
    }
}