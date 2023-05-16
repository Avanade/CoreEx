// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
using System;
using System.Threading.Tasks;

namespace CoreEx.Caching
{
    /// <summary>
    /// Provides <see cref="Result"/>-based <see cref="IRequestCache"/> extension methods.
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <see cref="CompositeKey"/> where it exists; otherwise, adds and returns the value created by the <paramref name="addFactory"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/></param>
        /// <param name="key">The key of the value to get or add.</param>
        /// <param name="addFactory">The factory function to create the <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding results are all <see cref="IResult.IsSuccess"/>.</remarks>
        public static Task<Result<T>> CacheGetOrAddAsync<T>(this Result result, IRequestCache cache, object? key, Func<Task<Result<T>>> addFactory)
            => CacheGetOrAddAsync(result, cache, new CompositeKey(key), addFactory);

        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <see cref="CompositeKey"/> where it exists; otherwise, adds and returns the value created by the <paramref name="addFactory"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/></param>
        /// <param name="key">The key of the value to get or add.</param>
        /// <param name="addFactory">The factory function to create the <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding results are all <see cref="IResult.IsSuccess"/>.</remarks>
        public static Task<Result<T>> CacheGetOrAddAsync<T>(this Result result, IRequestCache cache, CompositeKey key, Func<Task<Result<T>>> addFactory)
            => Task.FromResult(result).CacheGetOrAddAsync(cache, key, addFactory);

        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <see cref="CompositeKey"/> where it exists; otherwise, adds and returns the value created by the <paramref name="addFactory"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/></param>
        /// <param name="key">The key of the value to get or add.</param>
        /// <param name="addFactory">The factory function to create the <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding results are all <see cref="IResult.IsSuccess"/>.</remarks>
        public static Task<Result<T>> CacheGetOrAddAsync<T>(this Task<Result> result, IRequestCache cache, object? key, Func<Task<Result<T>>> addFactory)
            => CacheGetOrAddAsync(result, cache, new CompositeKey(key), addFactory);

        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <see cref="CompositeKey"/> where it exists; otherwise, adds and returns the value created by the <paramref name="addFactory"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="cache">The <see cref="IRequestCache"/></param>
        /// <param name="key">The key of the value to get or add.</param>
        /// <param name="addFactory">The factory function to create the <see cref="Result{T}"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The caching is only performed where the corresponding results are all <see cref="IResult.IsSuccess"/>.</remarks>
        public static async Task<Result<T>> CacheGetOrAddAsync<T>(this Task<Result> result, IRequestCache cache, CompositeKey key, Func<Task<Result<T>>> addFactory)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (addFactory == null) throw new ArgumentNullException(nameof(addFactory));

            var r = await result.ConfigureAwait(false);
            return await r.ThenAsync(async () =>
            {
                if (cache.TryGetValue<T>(key, out var val))
                    return Result.Ok(val!);

                var ar = await addFactory().ConfigureAwait(false);
                return ar.Then(v =>
                {
                    cache.SetValue(key, v);
                    return Result.Ok(v);
                });
            });
        }
    }
}